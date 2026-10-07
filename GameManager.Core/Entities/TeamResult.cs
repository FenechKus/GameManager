namespace GameManager.Core.Entities;

public class TeamResult
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<string> UniqueItems { get; set; } = new();
    public bool IsFinished { get; set; }
}