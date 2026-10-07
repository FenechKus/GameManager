using GameManager.Core.Entities;
using GameManager.Core.Events;

namespace GameManager.Core.Interfaces;

public interface IGameEngine : IObservable<SceneChangedEvent>
{
    Task<RoomState> CreateRoomAsync(string roomName, string questId);
    Task AssignModeratorAsync(Guid roomId, long chatId);
    Task ConnectPlayerAsync(Guid roomId, Player player);
    Task StartQuestAsync(Guid roomId);
    Task<List<QuestChoice>> GetAvailableChoicesAsync(Guid roomId);
    Task MakeChoiceAsync(Guid roomId, int choiceIndex);
}