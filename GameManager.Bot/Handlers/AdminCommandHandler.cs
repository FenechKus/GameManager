using GameManager.Bot.Keyboards;
using GameManager.Core.Interfaces;
using GameManager.Core.Services;
using Telegram.Bot;

namespace GameManager.Bot.Handlers;

public class AdminCommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IGameEngine _gameEngine;
    private readonly AdminKeyboardBuilder _adminKeyboardBuilder;
    private readonly IRoomRegistry _roomRegistry;
    private readonly IWorldStateRegistry _worldStateRegistry;

    public AdminCommandHandler(
        ITelegramBotClient botClient, 
        IGameEngine gameEngine, 
        AdminKeyboardBuilder adminKeyboardBuilder, IRoomRegistry roomRegistry, IWorldStateRegistry worldStateRegistry)
    {
        _botClient = botClient;
        _gameEngine = gameEngine;
        _adminKeyboardBuilder = adminKeyboardBuilder;
        _roomRegistry =  roomRegistry;
        _worldStateRegistry = worldStateRegistry;
    }

    public async Task HandleCreateRoomAsync(long chatId, string questId, CancellationToken ct)
{
    try
    {
        string roomName = $"Комната_{Random.Shared.Next(1000, 9999)}";
        
        var roomState = await _gameEngine.CreateRoomAsync(roomName, questId);
        Guid roomId = roomState.Id;

        await _gameEngine.AssignModeratorAsync(roomId, chatId);

        // 📝 Лог создания комнаты
        Console.WriteLine($"🏠 [Room Created]: Создана новая комната `{roomName}` (ID: {roomId}) | Модератор ChatID: {chatId} | Квест: '{questId}'");

        var keyboard = _adminKeyboardBuilder.BuildLobbyMenu(roomId);

        await _botClient.SendMessage(
            chatId: chatId,
            text: $"👑 **Комната успешно создана!**\n\n" +
                  $"🏷 Название: `{roomName}`\n" +
                  $"🔑 **ID комнаты:** `{roomId}`\n\n" +
                  $"Передайте этот ID игрокам для подключения. Когда все будут в сборе, нажмите «Запустить квест».",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ [Room Error]: Ошибка при создании комнаты: {ex.Message}");
        await _botClient.SendMessage(
            chatId: chatId,
            text: $"❌ Ошибка при создании комнаты: {ex.Message}",
            cancellationToken: ct);
    }
}
    
    public async Task ResetGameAsync()
    {
        // Полный сброс мира и реестра комнат
        _worldStateRegistry.ResetWorld(); // 👈 Очищает все забранные артефакты
    
        foreach (var room in _roomRegistry.GetAllRooms())
        {
            _roomRegistry.RemoveRoom(room.Id);
        }
    }

public async Task HandleStartGameAsync(long chatId, Guid roomId, CancellationToken ct)
{
    try
    {
        await _gameEngine.StartQuestAsync(roomId);

        // 📝 Лог запуска игры
        Console.WriteLine($"🚀 [Game Started]: Модератор {chatId} запустил квест в комнате {roomId}. Игра переведена в активное состояние.");

        await _botClient.SendMessage(
            chatId: chatId,
            text: "🚀 **Квест запущен!** Все участники получили первую сцену.",
            cancellationToken: ct);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ [Game Error]: Ошибка запуска игры в комнате {roomId}: {ex.Message}");
        await _botClient.SendMessage(
            chatId: chatId,
            text: $"❌ Не удалось запустить квест: {ex.Message}",
            cancellationToken: ct);
    }
}
}