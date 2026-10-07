namespace GameManager.Core.Entities;

public class UniqueItemState
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Guid? OwnerRoomId { get; set; } // null, если предмет ещё никому не принадлежит
    public bool IsTaken => OwnerRoomId.HasValue;
}