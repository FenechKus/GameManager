using GameManager.Core.Entities;
using GameManager.Core.Interfaces;
using Telegram.Bot;

namespace GameManager.Bot.Handlers;

public class UserCommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IGameEngine _gameEngine;
    private readonly IVoteRegistry _voteRegistry;
    private readonly IRoomRegistry _roomRegistry; // 1. Добавляем поле для реестра комнат

    public UserCommandHandler(
        ITelegramBotClient botClient, 
        IGameEngine gameEngine, 
        IVoteRegistry voteRegistry,
        IRoomRegistry roomRegistry) // 2. Внедряем через конструктор
    {
        _botClient = botClient;
        _gameEngine = gameEngine;
        _voteRegistry = voteRegistry;
        _roomRegistry = roomRegistry;
    }

    public async Task HandleJoinRoomAsync(long chatId, Guid roomId, CancellationToken ct)
    {
        try
        {
            var player = new Player(chatId, $"User_{chatId}");

            // 3. Подключаем игрока к комнате через движок
            await _gameEngine.ConnectPlayerAsync(roomId, player);

            // 4. Получаем объект комнаты из реестра комнат для логирования
            var room = _roomRegistry.GetRoom(roomId);
            int currentPlayersCount = room?.Players.Count ?? 0;

            Console.WriteLine($"👤 [Room Log]: Игрок {chatId} подключился к комнате {roomId}. Всего участников: {currentPlayersCount}");

            await _botClient.SendMessage(
                chatId: chatId,
                text: $"✅ **Вы успешно подключились к комнате!**\n`{roomId}`\n\nОжидайте запуска игры модератором.",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            await _botClient.SendMessage(
                chatId: chatId,
                text: $"❌ **Не удалось подключиться:** {ex.Message}",
                cancellationToken: ct);
        }
    }

    public async Task HandleVoteAsync(long chatId, int messageId, Guid roomId, int choiceIndex, CancellationToken ct)
    {
        var session = _voteRegistry.GetSession(roomId);
        if (session == null || session.IsExpired)
        {
            try
            {
                await _botClient.EditMessageReplyMarkup(
                    chatId: chatId,
                    messageId: messageId,
                    replyMarkup: null,
                    cancellationToken: ct);
            }
            catch { }

            await _botClient.SendMessage(
                chatId: chatId, 
                text: "⚠️ Голосование уже завершено или неактивно.", 
                cancellationToken: ct);
            return;
        }

        bool isAllVoted = session.RegisterVote(chatId, choiceIndex);

        // Блокируем клавиатуру у проголосовавшего игрока
        try
        {
            await _botClient.EditMessageReplyMarkup(
                chatId: chatId,
                messageId: messageId,
                replyMarkup: null,
                cancellationToken: ct);
        }
        catch { }

        await _botClient.SendMessage(
            chatId: chatId,
            text: $"✅ Ваш голос принят! (Выбран вариант №{choiceIndex + 1})",
            cancellationToken: ct);

        if (isAllVoted)
        {
            int winningChoice = session.GetWinningChoiceIndex();
            _voteRegistry.EndSession(roomId);
            await _gameEngine.MakeChoiceAsync(roomId, winningChoice);
        }
    }
}