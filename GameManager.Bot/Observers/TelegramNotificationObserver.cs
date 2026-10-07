using Telegram.Bot.Types.ReplyMarkups;

namespace GameManager.Bot.Observers;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using GameManager.Core.Interfaces;
using GameManager.Core.Events;
using GameManager.Core.Enums;
using GameManager.Core.Entities;
using GameManager.Bot.Keyboards;
using GameManager.Core.Services;

public class TelegramNotificationObserver : IObserver<SceneChangedEvent>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IRoomRegistry _roomRegistry;
    private readonly IQuestRepository _questRepository;
    private readonly IVoteRegistry _voteRegistry;
    private readonly ChoiceKeyboardBuilder _choiceKeyboardBuilder;
    private readonly DynamicSceneService _dynamicSceneService;
    private readonly IGameEngine _gameEngine;
    private readonly LeaderboardService _leaderboardService;

    public TelegramNotificationObserver(
        ITelegramBotClient botClient,
        IRoomRegistry roomRegistry,
        IQuestRepository questRepository,
        IVoteRegistry voteRegistry,
        ChoiceKeyboardBuilder choiceKeyboardBuilder,
        DynamicSceneService dynamicSceneService,
        IGameEngine gameEngine,
        LeaderboardService leaderboardService
        )
    {
        _botClient = botClient;
        _roomRegistry = roomRegistry;
        _questRepository = questRepository;
        _voteRegistry = voteRegistry;
        _choiceKeyboardBuilder = choiceKeyboardBuilder;
        _dynamicSceneService = dynamicSceneService;
        _gameEngine = gameEngine;
        _leaderboardService = leaderboardService;
    }

    public void OnCompleted() { }
    public void OnError(Exception error) { }

    public async void OnNext(SceneChangedEvent value)
    {
        try
        {
            var room = _roomRegistry.GetRoom(value.RoomId);
            if (room == null) return;

            var quest = await _questRepository.GetQuestByIdAsync(room.QuestId);
            if (quest == null || !quest.Scenes.TryGetValue(value.SceneId, out var baseScene)) return;

            // 1. Применяем динамическую адаптацию сцены
            var dynamicScene = _dynamicSceneService.BuildDynamicScene(value.RoomId, baseScene);

            // 2. Получаем только доступные варианты
            var availableChoices = await _gameEngine.GetAvailableChoicesAsync(value.RoomId);

            // 3. Инициализируем сессию голосования (если есть доступные варианты)
            if (availableChoices.Any())
            {
                int timeoutSeconds = dynamicScene.VoteTimeoutSeconds ?? 60;
                _voteRegistry.StartSession(
                    roomId: value.RoomId, 
                    sceneId: value.SceneId, 
                    totalPlayersCount: room.Players.Count, 
                    timeout: TimeSpan.FromSeconds(timeoutSeconds)
                );
            }

            // 4. Строим меню
            var keyboard = _choiceKeyboardBuilder.BuildChoiceMenu(value.RoomId, availableChoices);
            string captionText = dynamicScene.Text;

            // 5. Если квест завершен — прикрепляем турнирную таблицу
            if (room.Status == RoomStatus.Finished)
            {
                string leaderboardText = _leaderboardService.BuildLeaderboardText();
                captionText += $"\n\n{leaderboardText}";
            }

            // 6. Рассылаем всем игрокам комнаты
            foreach (var player in room.Players)
            {
                await SendSceneToPlayerAsync(player.ChatId, room.QuestId, dynamicScene, captionText, keyboard);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Observer Error]: Ошибка при обработке SceneChangedEvent для комнаты {value.RoomId}: {ex}");
        }
    }

    private async Task SendSceneToPlayerAsync(
        long chatId, 
        string questId,
        QuestScene scene, 
        string caption, 
        InlineKeyboardMarkup keyboard)
    {
        try
        {
            if (scene.Media != null && !string.IsNullOrWhiteSpace(scene.Media.FilePath))
            {
                string relativeQuestAssetsPath = Path.Combine("Quests", questId, "assets", scene.Media.FilePath);
                string fullPath = Path.Combine(AppContext.BaseDirectory, relativeQuestAssetsPath);

                if (!System.IO.File.Exists(fullPath))
                {
                    fullPath = relativeQuestAssetsPath;
                }

                if (!System.IO.File.Exists(fullPath))
                {
                    fullPath = Path.Combine(AppContext.BaseDirectory, "assets", scene.Media.FilePath);
                }

                if (System.IO.File.Exists(fullPath))
                {
                    await using var stream = System.IO.File.OpenRead(fullPath);
                    var inputFile = InputFile.FromStream(stream, Path.GetFileName(fullPath));

                    switch (scene.Media.Type)
                    {
                        case MediaType.Photo:
                            await _botClient.SendPhoto(
                                chatId: chatId, photo: inputFile, caption: caption, parseMode: ParseMode.Html, replyMarkup: keyboard);
                            return;

                        case MediaType.Audio:
                            await _botClient.SendAudio(
                                chatId: chatId, audio: inputFile, caption: caption, parseMode: ParseMode.Html, replyMarkup: keyboard);
                            return;

                        case MediaType.Video:
                            await _botClient.SendVideo(
                                chatId: chatId, video: inputFile, caption: caption, parseMode: ParseMode.Html, replyMarkup: keyboard);
                            return;

                        case MediaType.Animation:
                            await _botClient.SendAnimation(
                                chatId: chatId, animation: inputFile, caption: caption, parseMode: ParseMode.Html, replyMarkup: keyboard);
                            return;
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ [Observer Warning]: Медиафайл не найден по пути '{fullPath}'");
                }
            }

            await _botClient.SendMessage(
                chatId: chatId,
                text: caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: CancellationToken.None);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("can't parse entities"))
        {
            Console.WriteLine($"⚠️ [Parser Fallback]: Ошибка HTML-разметки для чата {chatId}. Отправляем чистый текст...");
            string plainText = System.Text.RegularExpressions.Regex.Replace(caption, "<.*?>", string.Empty);

            await _botClient.SendMessage(
                chatId: chatId,
                text: plainText,
                parseMode: ParseMode.None,
                replyMarkup: keyboard,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Send Message Error]: Не удалось отправить сообщение игроку {chatId}: {ex.Message}");
        }
    }
}