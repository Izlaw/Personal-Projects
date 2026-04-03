using System.Text.Json.Serialization;

namespace PersonalProjects.Models;

/// <summary>Matches the shape of wwwroot/data/sound-map.json.</summary>
public class AbilitySoundMapModel
{
    [JsonPropertyName("sounds")]
    public Dictionary<string, string> Sounds { get; set; } = new();
}
