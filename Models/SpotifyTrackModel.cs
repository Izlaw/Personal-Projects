namespace PersonalProjects.Models;

public class SpotifyTrackModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AlbumArtUrl { get; set; }
    public string SpotifyUrl { get; set; } = "";
    public List<string> Artists { get; set; } = new();
}
