using GameManager.Core.Entities;

namespace GameManager.Core.Interfaces;

public interface IRoomRegistry
{
    void RegisterRoom(RoomState room);
    RoomState? GetRoom(Guid roomId);
    RoomState? GetRoomByPlayerChatId(long chatId);
    IReadOnlyCollection<RoomState> GetAllRooms();
    void RemoveRoom(Guid roomId);
    
    List<TeamResult> GetLeaderboard();
}