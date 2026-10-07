namespace GameManager.Core.Entities;

public class QuestScene
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public SceneMedia? Media { get; set; } // <-- Медиафайлы сцены (опционально)
    public int? VoteTimeoutSeconds { get; set; }
    public List<QuestChoice> Choices { get; set; } = new();
}