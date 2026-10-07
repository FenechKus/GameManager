namespace GameManager.Core.Services;

using System.Collections.Concurrent;
using GameManager.Core.Entities;
using GameManager.Core.Interfaces;

public class WorldStateRegistry : IWorldStateRegistry
{
    private readonly ConcurrentDictionary<string, UniqueItemState> _items = new();
    private readonly object _claimLock = new();

    public bool TryClaimItem(string itemId, Guid roomId)
    {
        lock (_claimLock)
        {
            var item = _items.GetOrAdd(itemId, id => new UniqueItemState { Id = id });

            if (item.IsTaken)
            {
                return false; // Предмет уже забран другой командой
            }

            item.OwnerRoomId = roomId;
            Console.WriteLine($"🔑 [WorldState Log]: Уникальный предмет '{itemId}' забран комнатой {roomId}.");
            return true;
        }
    }
    
    public void ReleaseItemsByRoom(Guid roomId)
    {
        lock (_claimLock)
        {
            var itemsToRemove = _items.Where(kvp => kvp.Value.OwnerRoomId == roomId).Select(kvp => kvp.Key).ToList();
            foreach (var itemId in itemsToRemove)
            {
                _items.TryRemove(itemId, out _);
            }
            Console.WriteLine($"🧹 [WorldState Log]: Все предметы комнаты {roomId} возвращены в мир.");
        }
    }

    public bool IsItemClaimed(string itemId)
    {
        return _items.TryGetValue(itemId, out var item) && item.IsTaken;
    }

    public Guid? GetItemOwner(string itemId)
    {
        return _items.TryGetValue(itemId, out var item) ? item.OwnerRoomId : null;
    }

    public void ResetWorld() => _items.Clear();
}