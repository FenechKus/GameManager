using GameManager.Core.Entities;
using GameManager.Core.Enums;
using GameManager.Core.Events;
using GameManager.Core.Interfaces;

namespace GameManager.Core.Services;

public class GameEngine : IGameEngine
{
    private readonly IRoomRegistry _roomRegistry;
    private readonly IQuestRepository _questRepository;
    private readonly DynamicSceneService _dynamicSceneService;
    private readonly List<IObserver<SceneChangedEvent>> _observers = new();
    private readonly IWorldStateRegistry _worldStateRegistry;
    private readonly LeaderboardService _leaderboardService;

    public GameEngine(IRoomRegistry roomRegistry, IQuestRepository questRepository, DynamicSceneService dynamicSceneService, IWorldStateRegistry worldStateRegistry, LeaderboardService leaderboardService)
    {
        _roomRegistry = roomRegistry;
        _questRepository = questRepository;
        _dynamicSceneService = dynamicSceneService;
        _worldStateRegistry = worldStateRegistry;
        _leaderboardService = leaderboardService;
    }

    public IDisposable Subscribe(IObserver<SceneChangedEvent> observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
        return new Unsubscriber(_observers, observer);
    }

    private void NotifySceneChanged(Guid roomId, string sceneId)
    {
        var @event = new SceneChangedEvent(roomId, sceneId);
        foreach (var observer in _observers)
        {
            observer.OnNext(@event);
        }
    }

    public Task<RoomState> CreateRoomAsync(string roomName, string questId)
    {
        var oldFinishedRooms = _roomRegistry.GetAllRooms()
            .Where(r => r.Status == RoomStatus.Finished)
            .ToList();

        foreach (var oldRoom in oldFinishedRooms)
        {
            // 2. Освобождаем уникальные предметы старых комнат 👈
            _worldStateRegistry.ReleaseItemsByRoom(oldRoom.Id);

            // 3. Удаляем саму комнату из реестра
            _roomRegistry.RemoveRoom(oldRoom.Id);
        }
        
        var room = new RoomState(Guid.NewGuid(), roomName, questId);
        _roomRegistry.RegisterRoom(room);
        return Task.FromResult(room);
    }

    public Task AssignModeratorAsync(Guid roomId, long chatId)
    {
        var room = GetRequiredRoom(roomId);
        var player = room.Players.FirstOrDefault(p => p.ChatId == chatId);
        if (player != null)
        {
            player.Role = PlayerRole.Moderator;
        }
        else
        {
            room.AddPlayer(new Player(chatId, "Moderator", PlayerRole.Moderator));
        }
        return Task.CompletedTask;
    }

    public async Task ConnectPlayerAsync(Guid roomId, Player player)
    {
        var room = _roomRegistry.GetRoom(roomId);
        if (room == null)
        {
            throw new InvalidOperationException("Комната не найдена.");
        }

        if (room.Status != RoomStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException("Квест уже запущен! Присоединиться нельзя.");
        }

        room.AddPlayer(player);
        await Task.CompletedTask;
    }

    public async Task StartQuestAsync(Guid roomId)
    {
        var room = GetRequiredRoom(roomId);
        var quest = await _questRepository.GetQuestByIdAsync(room.QuestId);

        room.SetStatus(RoomStatus.InProcess);
        room.ChangeScene(quest.StartSceneId);
        
        if (!room.VisitedScenes.Contains(quest.StartSceneId))
        {
            room.VisitedScenes.Add(quest.StartSceneId);
        }

        NotifySceneChanged(room.Id, quest.StartSceneId);
    }

    public async Task<List<QuestChoice>> GetAvailableChoicesAsync(Guid roomId)
    {
        var room = GetRequiredRoom(roomId);
        var quest = await _questRepository.GetQuestByIdAsync(room.QuestId);

        if (!quest.Scenes.TryGetValue(room.CurrentSceneId, out var currentScene))
            return new List<QuestChoice>();

        // 1. Применяем динамические правила (соперничество, уникальные предметы)
        var dynamicScene = _dynamicSceneService.BuildDynamicScene(roomId, currentScene);

        // 2. Фильтруем по локальным условиям комнаты (HasItem, MinScore, Flags)
        return dynamicScene.Choices
            .Where(choice => EvaluateConditions(choice.Conditions, room))
            .ToList();
    }

    public async Task MakeChoiceAsync(Guid roomId, int choiceIndex)
{
    var room = _roomRegistry.GetRoom(roomId);
    if (room == null) return;

    // 1. Получаем ТОЛЬКО доступные варианты для этой комнаты на текущий момент
    var availableChoices = await GetAvailableChoicesAsync(roomId);

    if (!availableChoices.Any())
    {
        Console.WriteLine($"⚠️ [Game Engine]: В комнате {roomId} нет доступных вариантов действий.");
        return;
    }

    // 2. Валидация индекса среди ДОСТУПНЫХ кнопок
    if (choiceIndex < 0 || choiceIndex >= availableChoices.Count)
    {
        Console.WriteLine($"⚠️ [Game Engine]: Некорректный индекс выбора ({choiceIndex}). Сброс на первый доступный.");
        choiceIndex = 0;
    }

    var choice = availableChoices[choiceIndex];

    // 3. Повторный Guard Check (Защита)
    if (!EvaluateConditions(choice.Conditions, room))
    {
        Console.WriteLine($"⛔ [Game Engine Guard]: Комната {roomId} попыталась выполнить '{choice.Text}', но условия (Conditions) не выполнены!");
        return; // ПРЕРЫВАЕМ переход
    }

    //Console.WriteLine($"🔀 [Game Action]: В комнате {roomId} выбран вариант №{choiceIndex + 1} ('{choice.Text}'). Переход: {room.CurrentSceneId} ➔ {choice.NextSceneId}");

    // 4. Применяем экшены (выдача предметов, очков и т.д.)
    if (choice.Actions != null && choice.Actions.Any())
    {
        ApplyActions(choice.Actions, room);
        foreach (var action in choice.Actions)
        {
            Console.WriteLine($"🎁 [State Update]: Комната {roomId} выполнила действие '{action.Type}' ➔ Target: '{action.Target}' | Текущие очки: {room.Score}");
        }
    }

    // 5. Обновляем текущую сцену
    room.ChangeScene(choice.NextSceneId);
    
    if (!room.VisitedScenes.Contains(choice.NextSceneId))
    {
        room.VisitedScenes.Add(choice.NextSceneId);
    }

    // 6. Проверяем финал
    var nextScene = await GetCurrentSceneAsync(room.QuestId, room.CurrentSceneId);
    if (nextScene.Choices == null || !nextScene.Choices.Any())
    {
        room.SetStatus(RoomStatus.Finished);

        Console.WriteLine($"🏁 [Game Finished]: Комната '{room.Name}' ({roomId}) финишировала! Очки: {room.Score}.");

        // Проверяем финал только для активных комнат данного квеста
        if (AreAllRoomsFinished(room.QuestId))
        {
            Console.WriteLine("\n🎉 [ALL TEAMS FINISHED]: Все команды завершили квест!");

            // Получаем только комнаты текущей игры (не сброшенные/старые)
            var currentSessionRooms = GetActiveRoomsForQuest(room.QuestId);
            string leaderboardText = _leaderboardService.BuildLeaderboardText(currentSessionRooms.Select(r => r.Id));

            Console.WriteLine(leaderboardText);

            // Оповещаем каждую комнату строго по ОДНОМУ разу
            foreach (var r in currentSessionRooms)
            {
                NotifySceneChanged(r.Id, r.CurrentSceneId);
            }

            return; // 👈 ВАЖНО: Выходим из метода, чтобы не было второго повторного NotifySceneChanged ниже!
        }
    }

    // Оповещаем UI для обычных переходов
    NotifySceneChanged(room.Id, room.CurrentSceneId);

}

    private async Task<QuestScene> GetCurrentSceneAsync(string questId, string sceneId)
    {
        var quest = await _questRepository.GetQuestByIdAsync(questId);
        if (quest != null && quest.Scenes.TryGetValue(sceneId, out var scene))
        {
            return scene;
        }
        throw new KeyNotFoundException($"Сцена '{sceneId}' не найдена в квесте '{questId}'.");
    }

    private bool EvaluateConditions(List<ChoiceCondition> conditions, RoomState room)
    {
        if (conditions == null) return true;

        foreach (var condition in conditions)
        {
            switch (condition.Type)
            {
                case ConditionType.HasItem:
                    if (!room.Inventory.Contains(condition.Target)) return false;
                    break;
                case ConditionType.DoesNotHaveItem:
                    if (room.Inventory.Contains(condition.Target)) return false;
                    break;
                case ConditionType.MinScore:
                    if (room.Score < condition.Value) return false;
                    break;
                case ConditionType.VisitedScene:
                    if (!room.VisitedScenes.Contains(condition.Target)) return false;
                    break;
                case ConditionType.NotVisitedScene:
                    if (room.VisitedScenes.Contains(condition.Target)) return false;
                    break;
                case ConditionType.SceneVisitedByOthers:
                    bool isVisitedByOthers = _roomRegistry.GetAllRooms()
                        .Any(r => r.Id != room.Id && r.VisitedScenes.Contains(condition.Target));
                    if (!isVisitedByOthers) return false;
                    break;
                case ConditionType.FlagIsTrue:
                    if (!room.Flags.Contains(condition.Target)) return false;
                    break;
            }
        }
        return true;
    }

    private void ApplyActions(List<ChoiceAction> actions, RoomState room)
    {
        if (actions == null) return;

        foreach (var action in actions)
        {
            switch (action.Type)
            {
                case ActionType.AddItem:
                    room.Inventory.Add(action.Target);
                    if (action.IsUnique)
                    {
                        _worldStateRegistry.TryClaimItem(action.Target, room.Id);
                    }
                    break;

                case ActionType.RemoveItem:
                    room.Inventory.Remove(action.Target);
                    break;
                case ActionType.AddScore:
                    room.AddScore(action.Value);
                    break;
                case ActionType.SetFlag:
                    room.Flags.Add(action.Target);
                    break;
            }
        }
    }

    private RoomState GetRequiredRoom(Guid roomId)
    {
        return _roomRegistry.GetRoom(roomId) 
               ?? throw new KeyNotFoundException($"Игровая комната с ID '{roomId}' не найдена.");
    }
    
    private List<RoomState> GetActiveRoomsForQuest(string questId)
    {
        // Берем только те комнаты текущего квеста, которые участвовали в игре
        return _roomRegistry.GetAllRooms()
            .Where(r => r.QuestId == questId && r.Status == RoomStatus.Finished)
            .ToList();
    }

    private bool AreAllRoomsFinished(string questId)
    {
        var activeQuestRooms = _roomRegistry.GetAllRooms()
            .Where(r => r.QuestId == questId && r.Status != RoomStatus.WaitingForPlayers)
            .ToList();

        if (!activeQuestRooms.Any()) return false;

        return activeQuestRooms.All(r => r.Status == RoomStatus.Finished);
    }

    private class Unsubscriber : IDisposable
    {
        private readonly List<IObserver<SceneChangedEvent>> _observers;
        private readonly IObserver<SceneChangedEvent> _observer;

        public Unsubscriber(List<IObserver<SceneChangedEvent>> observers, IObserver<SceneChangedEvent> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observer != null && _observers.Contains(_observer))
            {
                _observers.Remove(_observer);
            }
        }
    }
}