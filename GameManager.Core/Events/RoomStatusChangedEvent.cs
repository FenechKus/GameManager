using GameManager.Core.Enums;

namespace GameManager.Core.Events;

public record RoomStatusChangedEvent(Guid RoomId, RoomStatus Status);