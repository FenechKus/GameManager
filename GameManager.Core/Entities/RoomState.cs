using GameManager.Core.Enums;
using GameManager.Core.Events;

namespace GameManager.Core.Entities;

public class RoomState
{
    public Guid Id { get; }
    public string Name { get; }
    public string QuestId { get; }
    public RoomStatus Status { get; private set; } = RoomStatus.WaitingForPlayers;
    public string CurrentSceneId { get; private set; } = string.Empty;

    public List<Player> Players { get; } = new();
    public HashSet<string> Inventory { get; } = new();
    public HashSet<string> Flags { get; } = new();
    public HashSet<string> VisitedScenes { get; } = new();
    public int Score { get; private set; }

    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public RoomState(Guid id, string name, string questId)
    {
        Id = id;
        Name = name;
        QuestId = questId;
    }

    public void AddPlayer(Player player)
    {
        if (!Players.Any(p => p.ChatId == player.ChatId))
            Players.Add(player);
    }

    public void SetStatus(RoomStatus newStatus)
    {
        Status = newStatus;
        _domainEvents.Add(new RoomStatusChangedEvent(Id, newStatus));
    }

    public void ChangeScene(string sceneId)
    {
        CurrentSceneId = sceneId;
        VisitedScenes.Add(sceneId);
        _domainEvents.Add(new SceneChangedEvent(Id, sceneId));
    }

    public void AddScore(int delta) => Score += delta;

    public void ClearDomainEvents() => _domainEvents.Clear();
}