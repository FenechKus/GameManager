using System.Collections.Concurrent;
using GameManager.Core.Entities;
using GameManager.Core.Enums;
using GameManager.Core.Interfaces;

namespace GameManager.Core.Services;

public class RoomRegistry : IRoomRegistry
{
    private readonly ConcurrentDictionary<Guid, RoomState> _rooms = new();

    public void RegisterRoom(RoomState room) => _rooms[room.Id] = room;

    public RoomState? GetRoom(Guid roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }

    public RoomState? GetRoomByPlayerChatId(long chatId)
    {
        return _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.ChatId == chatId));
    }
    
    public IReadOnlyCollection<RoomState> GetAllRooms()
    {
        return _rooms.Values.ToList().AsReadOnly();
    }
    
    public List<TeamResult> GetLeaderboard()
    {
        return _rooms.Values
            .Select(room => new TeamResult
            {
                RoomId = room.Id,
                RoomName = room.Name,
                Score = room.Score,
                // Фильтруем предметы в инвентаре комнаты, проверяя их на уникальность через WorldStateRegistry (или хранящийся список)
                UniqueItems = room.Inventory.ToList(), 
                IsFinished = room.Status == RoomStatus.Finished
            })
            .OrderByDescending(r => r.Score) // Сортировка от 1 места к последнему
            .ThenBy(r => r.RoomName)
            .ToList();
    }

    public void RemoveRoom(Guid roomId) => _rooms.TryRemove(roomId, out _);
}