using GameManager.Bot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GameManager.Bot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly MenuCommandHandler _menuCommandHandler;
    private readonly UserCommandHandler _userCommandHandler;
    private readonly AdminCommandHandler _adminCommandHandler;
    private readonly UserInputStateRegistry _inputStateRegistry;

    public UpdateHandler(
        MenuCommandHandler menuCommandHandler,
        UserCommandHandler userCommandHandler,
        AdminCommandHandler adminCommandHandler,
        UserInputStateRegistry inputStateRegistry)
    {
        _menuCommandHandler = menuCommandHandler;
        _userCommandHandler = userCommandHandler;
        _adminCommandHandler = adminCommandHandler;
        _inputStateRegistry = inputStateRegistry;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // 1. Обработка текстовых сообщений
        if (update.Type == UpdateType.Message && update.Message is { } message)
        {
            long chatId = message.Chat.Id;
            string? text = message.Text;

            if (string.IsNullOrEmpty(text)) return;

            if (text.StartsWith("/start"))
            {
                _inputStateRegistry.ClearState(chatId);
                await _menuCommandHandler.HandleStartAsync(chatId, cancellationToken);
                return;
            }

            // Если бот ждал от пользователя ввод PIN-кода/ID комнаты
            var currentState = _inputStateRegistry.GetState(chatId);
            if (currentState == UserState.AwaitingRoomIdToJoin)
            {
                _inputStateRegistry.ClearState(chatId);
                if (Guid.TryParse(text.Trim(), out var roomId))
                {
                    // Ввод ID комнаты игроком
                    await _userCommandHandler.HandleJoinRoomAsync(chatId, roomId, cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Некорректный формат ID комнаты. Попробуйте снова из главного меню.",
                        cancellationToken: cancellationToken);
                }
                return;
            }
        }

        // 2. Обработка Inline-кнопок (CallbackQuery)
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is { } callbackQuery)
        {
            long chatId = callbackQuery.Message!.Chat.Id;
            int messageId = callbackQuery.Message.MessageId;
            string data = callbackQuery.Data ?? string.Empty;

            // Ответ Телеграму, чтобы кнопка перестала «крутиться»
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            // Кнопка: Список квестов
            if (data == "menu_quests")
            {
                await _menuCommandHandler.HandleQuestListAsync(chatId, cancellationToken);
            }
            // Кнопка: Вернуться в главное меню
            else if (data == "menu_main")
            {
                await _menuCommandHandler.HandleStartAsync(chatId, cancellationToken);
            }
            // Кнопка: Подключиться к комнате
            else if (data == "menu_join")
            {
                _inputStateRegistry.SetState(chatId, UserState.AwaitingRoomIdToJoin);
                await botClient.SendMessage(chatId, "🔑 Введите ID комнаты, к которой хотите подключиться:", cancellationToken: cancellationToken);
            }
            // Кнопка: Выбор конкретного квеста (quest_select_{questId})
            else if (data.StartsWith("quest_select_"))
            {
                string questId = data.Replace("quest_select_", "");
                await _adminCommandHandler.HandleCreateRoomAsync(chatId, questId, cancellationToken);
            }
            // Кнопка: Голосование (vote_{roomId}_{choiceIndex})
            else if (data.StartsWith("vote_"))
            {
                var parts = data.Split('_');
                if (parts.Length == 3 && Guid.TryParse(parts[1], out var roomId) && int.TryParse(parts[2], out var choiceIndex))
                {
                    // Передаем messageId для удаления клавиатуры после клика
                    await _userCommandHandler.HandleVoteAsync(chatId, messageId, roomId, choiceIndex, cancellationToken);
                }
            }
            // Кнопка: Админ - Запуск игры (admin_start_{roomId})
            else if (data.StartsWith("admin_start_"))
            {
                if (Guid.TryParse(data.Replace("admin_start_", ""), out var roomId))
                {
                    await _adminCommandHandler.HandleStartGameAsync(chatId, roomId, cancellationToken);
                }
            }
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Telegram API Error]: {exception.Message}");
        return Task.CompletedTask;
    }
}