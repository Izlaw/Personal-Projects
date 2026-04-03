using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PersonalProjects.Models;

namespace PersonalProjects.Services;

public class SpotifyService
{
    private readonly HttpClient _http;
    private SpotifyTokenModel? _cachedToken;

    public SpotifyService(HttpClient http)
    {
        _http = http;
    }

    #region Methods

    /// <summary>
    /// Returns a valid Spotify access token, refreshing if expired.
    /// </summary>
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
    /// Search Spotify for a track matching the given query.
    /// Returns null if no match is found or on error.
    /// </summary>
    public async Task<SpotifyTrackModel?> SearchTrackAsync(string query, string token)
    {
        try
        {
            var encoded = Uri.EscapeDataString(query);
            var url = $"https://api.spotify.com/v1/search?q={encoded}&type=track&limit=5";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new("Bearer", token);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<SpotifySearchResponse>();
            if (result?.Tracks?.Items is null or { Count: 0 }) return null;

            // Find a track whose name closely matches the query (case-insensitive, trimmed)
            var normalizedQuery = query.Trim().ToLowerInvariant();
            var match = result.Tracks.Items
                .FirstOrDefault(t => t.Name.Trim().ToLowerInvariant() == normalizedQuery);

            if (match is null) return null;

            return new SpotifyTrackModel
            {
                Id = match.Id,
                Name = match.Name,
                AlbumArtUrl = match.Album?.Images?.FirstOrDefault()?.Url,
                SpotifyUrl = match.ExternalUrls?.Spotify ?? "",
                Artists = match.Artists?.Select(a => a.Name).ToList() ?? new()
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Greedy left-to-right sentence matching. Tries the longest phrase first,
    /// then falls back to shorter subsets. Returns one result per matched word/phrase.
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
            SpotifyMatchResultModel? matchedResult = null;

            // Try from longest phrase down to single word
            for (int len = words.Length - i; len >= 1; len--)
            {
                var phrase = string.Join(" ", words[i..(i + len)]);
                var track = await SearchTrackAsync(phrase, token);

                if (track is not null && usedTrackIds.Add(track.Id))
                {
                    matchedResult = new SpotifyMatchResultModel { Word = phrase, Track = track };
                    i += len;
                    break;
                }

                // Rate limiting guard
                await Task.Delay(100);
            }

            if (matchedResult is not null)
            {
                results.Add(matchedResult);
            }
            else
            {
                // No match found for current word — record it as unmatched and advance
                results.Add(new SpotifyMatchResultModel { Word = words[i], Track = null });
                i++;
            }
        }

        return results;
    }

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
