namespace GameManager.Core.Interfaces;

public interface IWorldStateRegistry
{
    bool TryClaimItem(string itemId, Guid roomId);
    bool IsItemClaimed(string itemId);
    void ReleaseItemsByRoom(Guid roomId);
    void ResetWorld();
    
    Guid? GetItemOwner(string itemId);
}