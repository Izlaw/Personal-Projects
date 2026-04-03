namespace PersonalProjects.Models;

public class SpotifyTokenModel
{
    public string AccessToken { get; set; } = "";
    public int ExpiresIn { get; set; }
    public DateTime FetchedAt { get; set; }
    public bool IsExpired => DateTime.UtcNow >= FetchedAt.AddSeconds(ExpiresIn - 60);
}
