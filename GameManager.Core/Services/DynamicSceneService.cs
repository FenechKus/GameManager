using GameManager.Core.Entities;
using GameManager.Core.Enums;
using GameManager.Core.Interfaces;

namespace GameManager.Core.Services;

public class DynamicSceneService
{
    private readonly IRoomRegistry _roomRegistry;
    private readonly IWorldStateRegistry _worldStateRegistry;

    public DynamicSceneService(IRoomRegistry roomRegistry, IWorldStateRegistry worldStateRegistry)
    {
        _roomRegistry = roomRegistry;
        _worldStateRegistry = worldStateRegistry;
    }

    public QuestScene BuildDynamicScene(Guid roomId, QuestScene baseScene)
    {
        var currentRoom = _roomRegistry.GetRoom(roomId);
        if (currentRoom == null) return baseScene;

        // 1. Проверяем, есть ли другие команды в этой же сцене
        var otherRoomsInSameScene = _roomRegistry.GetAllRooms()
            .Where(r => r.Id != roomId && r.CurrentSceneId == baseScene.Id)
            .ToList();

        bool isContested = otherRoomsInSameScene.Any();

        var dynamicScene = new QuestScene
        {
            Id = baseScene.Id,
            Text = baseScene.Text,
            Media = baseScene.Media,
            VoteTimeoutSeconds = baseScene.VoteTimeoutSeconds,
            Choices = new List<QuestChoice>()
        };

        if (isContested)
        {
            dynamicScene.Text += "\n\n⚠️ **Внимание!** В этой комнате находится соперничающая команда! Из-за создавшегося напряжения вы не можете забрать уникальные предметы, пока они рядом.";
        }

        // 2. Фильтруем варианты
        foreach (var choice in baseScene.Choices)
        {
            // Проверка условий самой комнаты (HasItem, DoesNotHaveItem, MinScore и т.д.)
            if (!EvaluateConditions(choice.Conditions, currentRoom))
            {
                continue;
            }

            var takeItemAction = choice.Actions?.FirstOrDefault(a => a.Type == ActionType.AddItem);

            if (takeItemAction != null)
            {
                string itemId = takeItemAction.Target;

                // Защита: Если у команды УЖЕ ЕСТЬ этот предмет — кнопка повторного взятия скрывается
                if (currentRoom.Inventory.Contains(itemId))
                {
                    continue;
                }

                // Защита: Если предмет уникальный для всего мира
                if (takeItemAction.IsUnique)
                {
                    bool isItemClaimedInWorld = _worldStateRegistry.IsItemClaimed(itemId);

                    // Показываем вариант ТОЛЬКО если предмет еще свободен и нет соперников
                    if (!isContested && !isItemClaimedInWorld)
                    {
                        dynamicScene.Choices.Add(choice);
                    }

                    continue;
                }
            }

            // Обычный выбор без ограничения уникальности
            dynamicScene.Choices.Add(choice);
        }

        return dynamicScene;
    }

    private bool EvaluateConditions(List<ChoiceCondition>? conditions, RoomState room)
    {
        if (conditions == null || !conditions.Any()) return true;

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
}