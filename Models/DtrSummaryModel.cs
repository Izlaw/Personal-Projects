namespace PersonalProjects.Models;

public class DtrSummaryModel
{
    public double TotalHours { get; set; }
    public int TotalDays { get; set; }
    public double AverageHoursPerDay { get; set; }
    public double RemainingHours { get; set; }
    public double RemainingDays { get; set; }
    public double ProgressPercent { get; set; }
}
