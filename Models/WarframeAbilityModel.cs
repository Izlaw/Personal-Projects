namespace PersonalProjects.Models;

public class WarframeAbilityModel
{
    /// <summary>DE internal path, e.g. "/Lotus/Powersuits/Mag/Pull". Used as the stable key.</summary>
    public string UniqueName { get; set; } = "";
    public string AbilityName { get; set; } = "";
    public string WarframeName { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Absolute URL to the icon image on the Warframe stat CDN.</summary>
    public string? IconUrl { get; set; }
    /// <summary>URL to the ability cast sound file. Null if not yet mapped in sound-map.json.</summary>
    public string? SoundUrl { get; set; }
    public bool HasSound => !string.IsNullOrEmpty(SoundUrl);
}
