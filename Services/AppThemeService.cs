namespace PersonalProjects.Services;

public class AppThemeService
{
    public string AppBarColor { get; private set; } = "#000092";
    public string AppBarTextColor { get; private set; } = "#ffffff";

    public event Action? OnChange;

    public void SetTheme(string appBarColor, string textColor = "#ffffff")
    {
        AppBarColor = appBarColor;
        AppBarTextColor = textColor;
        OnChange?.Invoke();
    }

    public void Reset() => SetTheme("#000092");
}
