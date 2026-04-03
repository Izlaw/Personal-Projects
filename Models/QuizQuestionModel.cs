namespace PersonalProjects.Models;

public class QuizQuestionModel
{
    public WarframeAbilityModel CorrectAbility { get; set; } = null!;
    /// <summary>Always 4 choices (1 correct + 3 distractors), pre-shuffled.</summary>
    public List<WarframeAbilityModel> Choices { get; set; } = new();
}
