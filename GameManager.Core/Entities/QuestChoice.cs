namespace GameManager.Core.Entities;

public class QuestChoice
{
    public string Text { get; set; } = string.Empty;
    public string NextSceneId { get; set; } = string.Empty;
    public List<ChoiceCondition> Conditions { get; set; } = new();
    public List<ChoiceAction> Actions { get; set; } = new();
}