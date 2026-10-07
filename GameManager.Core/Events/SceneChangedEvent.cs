namespace GameManager.Core.Events;

public record SceneChangedEvent(Guid RoomId, string SceneId);