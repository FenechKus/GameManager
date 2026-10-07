namespace GameManager.Core.Entities;

public class QuestModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StartSceneId { get; set; } = string.Empty;
    public Dictionary<string, QuestScene> Scenes { get; set; } = new();
}