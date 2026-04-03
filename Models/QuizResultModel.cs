namespace PersonalProjects.Models;

public class QuizResultModel
{
    public QuizQuestionModel Question { get; set; } = null!;
    public WarframeAbilityModel? SelectedAbility { get; set; }
    public bool IsCorrect => SelectedAbility?.UniqueName == Question.CorrectAbility.UniqueName;
}
