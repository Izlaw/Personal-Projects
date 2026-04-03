using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PersonalProjects.Models;

namespace PersonalProjects.Services;

public class SpotifyService
{
    private readonly HttpClient _http;
    private SpotifyTokenModel? _cachedToken;
    private readonly Dictionary<string, List<SpotifyTrackModel>> _searchCache = new();
    private DateTime _lastRequestTime = DateTime.MinValue;
    private const int MinDelayMs = 100;

    public SpotifyService(HttpClient http)
    {
        _http = http;
    }

    #region Public Methods

    public async Task<string?> GetTokenAsync()
    {
        if (_cachedToken is not null && !_cachedToken.IsExpired)
            return _cachedToken.AccessToken;

        try
        {
            var response = await _http.PostAsync(AppConfig.SpotifyTokenProxyUrl, null);
            if (!response.IsSuccessStatusCode) return null;

            var tokenData = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>();
            if (tokenData is null) return null;

            _cachedToken = new SpotifyTokenModel
            {
                AccessToken = tokenData.AccessToken,
                ExpiresIn = tokenData.ExpiresIn,
                FetchedAt = DateTime.UtcNow
            };
            return _cachedToken.AccessToken;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Greedy left-to-right sentence matching using parallel batch searches.
    /// Tries the longest phrase first, batching candidates for parallel execution.
    /// </summary>
    public async Task<List<SpotifyMatchResultModel>> MatchSentenceAsync(string sentence)
    {
        var token = await GetTokenAsync();
        if (token is null) return new();

        var words = sentence.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<SpotifyMatchResultModel>();
        var usedTrackIds = new HashSet<string>();

        int i = 0;
        while (i < words.Length)
        {
            // Build candidates: longest phrase to shortest
            var candidates = Enumerable.Range(1, words.Length - i)
                .Select(len => new { Phrase = string.Join(" ", words[i..(i + len)]), EndIndex = i + len })
                .Reverse()
                .ToList();

            SpotifyMatchResultModel? found = null;

            // Search in parallel batches of 10
            for (int b = 0; b < candidates.Count; b += 10)
            {
                var batch = candidates.Skip(b).Take(10).ToList();
                var usedIds = usedTrackIds.ToArray();
                var tasks = batch.Select(c => SearchTrackAsync(c.Phrase, token, usedIds));
                var trackResults = await Task.WhenAll(tasks);

                for (int j = 0; j < trackResults.Length; j++)
                {
                    if (trackResults[j] is not null)
                    {
                        found = new SpotifyMatchResultModel
                        {
                            Word = batch[j].Phrase,
                            Track = trackResults[j]
                        };
                        usedTrackIds.Add(trackResults[j]!.Id);
                        i = batch[j].EndIndex;
                        break;
                    }
                }

                if (found is not null) break;
            }

            if (found is not null)
            {
                results.Add(found);
            }
            else
            {
                results.Add(new SpotifyMatchResultModel { Word = words[i], Track = null });
                i++;
            }
        }

        return results;
    }

    #endregion

    #region Private Methods

    private async Task<SpotifyTrackModel?> SearchTrackAsync(string query, string token, string[] avoidIds)
    {
        var cacheKey = query.ToLowerInvariant();

        if (_searchCache.TryGetValue(cacheKey, out var cached))
        {
            var unused = cached.FirstOrDefault(t => !avoidIds.Contains(t.Id));
            return unused ?? (cached.Count > 0 ? cached[0] : null);
        }

        await WaitForCooldown();

        var encoded = Uri.EscapeDataString(query);
        var url = $"https://api.spotify.com/v1/search?q={encoded}&type=track&limit=50";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", token);

        var response = await _http.SendAsync(request);

        // Token expired — refresh and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _cachedToken = null;
            var newToken = await GetTokenAsync();
            if (newToken is null) return null;

            await WaitForCooldown();
            using var retryRequest = new HttpRequestMessage(HttpMethod.Get, url);
            retryRequest.Headers.Authorization = new("Bearer", newToken);
            response = await _http.SendAsync(retryRequest);
        }

        // Rate limited — wait and retry
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(5);
            await Task.Delay(retryAfter);
            return await SearchTrackAsync(query, token, avoidIds);
        }

        if (!response.IsSuccessStatusCode)
        {
            _searchCache[cacheKey] = new();
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<SpotifySearchResponse>();
        var items = result?.Tracks?.Items;

        if (items is null or { Count: 0 })
        {
            _searchCache[cacheKey] = new();
            return null;
        }

        var normalizedQuery = Normalize(query);

        // Tier 1: normalized exact match
        var matches = items
            .Where(t => Normalize(t.Name) == normalizedQuery)
            .Select(ToTrackModel)
            .ToList();

        // Tier 2: case-insensitive exact match
        if (matches.Count == 0)
            matches = items
                .Where(t => t.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
                .Select(ToTrackModel)
                .ToList();

        // Tier 3: trimmed case-insensitive
        if (matches.Count == 0)
            matches = items
                .Where(t => t.Name.Trim().Equals(query.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(ToTrackModel)
                .ToList();

        _searchCache[cacheKey] = matches;

        var best = matches.FirstOrDefault(t => !avoidIds.Contains(t.Id));
        return best ?? (matches.Count > 0 ? matches[0] : null);
    }

    private async Task WaitForCooldown()
    {
        var elapsed = DateTime.UtcNow - _lastRequestTime;
        if (elapsed.TotalMilliseconds < MinDelayMs)
            await Task.Delay(MinDelayMs - (int)elapsed.TotalMilliseconds);
        _lastRequestTime = DateTime.UtcNow;
    }

    private static string Normalize(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var stripped = new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        return Regex.Replace(stripped, @"[''`""'\-]", "")
            .Replace("  ", " ")
            .Trim()
            .ToLowerInvariant();
    }

    private static SpotifyTrackModel ToTrackModel(SpotifyTrackItem t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        AlbumArtUrl = t.Album?.Images?.ElementAtOrDefault(1)?.Url
                   ?? t.Album?.Images?.FirstOrDefault()?.Url,
        SpotifyUrl = t.ExternalUrls?.Spotify ?? "",
        Artists = t.Artists?.Select(a => a.Name).ToList() ?? new()
    };

    #endregion

    // ─── Internal response shapes ────────────────────────────────────────────

    private sealed class SpotifyTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
        [JsonPropertyName("expires_in")]   public int ExpiresIn { get; set; }
    }

    private sealed class SpotifySearchResponse
    {
        [JsonPropertyName("tracks")] public SpotifyTracksContainer? Tracks { get; set; }
    }

    private sealed class SpotifyTracksContainer
    {
        [JsonPropertyName("items")] public List<SpotifyTrackItem>? Items { get; set; }
    }

    private sealed class SpotifyTrackItem
    {
        [JsonPropertyName("id")]            public string Id { get; set; } = "";
        [JsonPropertyName("name")]          public string Name { get; set; } = "";
        [JsonPropertyName("external_urls")] public SpotifyExternalUrls? ExternalUrls { get; set; }
        [JsonPropertyName("album")]         public SpotifyAlbum? Album { get; set; }
        [JsonPropertyName("artists")]       public List<SpotifyArtist>? Artists { get; set; }
    }

    private sealed class SpotifyExternalUrls
    {
        [JsonPropertyName("spotify")] public string? Spotify { get; set; }
    }

    private sealed class SpotifyAlbum
    {
        [JsonPropertyName("images")] public List<SpotifyImage>? Images { get; set; }
    }

    private sealed class SpotifyImage
    {
        [JsonPropertyName("url")] public string Url { get; set; } = "";
    }

    private sealed class SpotifyArtist
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
    }
}
