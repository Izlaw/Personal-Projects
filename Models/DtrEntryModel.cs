namespace PersonalProjects.Models;

public class DtrEntryModel
{
    public string? Key { get; set; }       // Supabase row UUID
    public string Date { get; set; } = "";
    public string MorningLogin { get; set; } = "";
    public string MorningLogout { get; set; } = "";
    public string AfternoonLogin { get; set; } = "";
    public string AfternoonLogout { get; set; } = "";
    public double Hours { get; set; }
}
