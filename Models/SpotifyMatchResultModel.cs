namespace PersonalProjects.Models;

public class SpotifyMatchResultModel
{
    public string Word { get; set; } = "";
    public SpotifyTrackModel? Track { get; set; }
    public bool IsMatched => Track is not null;
}
