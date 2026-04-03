namespace PersonalProjects.Models;

public class NotepadTabModel
{
    public string? Key { get; set; }       // Supabase row UUID
    public string Title { get; set; } = "Untitled";
    public string Content { get; set; } = "";
    public int Order { get; set; }
    public long CreatedAt { get; set; }
}
