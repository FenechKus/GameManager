using GameManager.Bot.Keyboards;
using GameManager.Core.Interfaces;
using Telegram.Bot;

namespace GameManager.Bot.Handlers;

public class MenuCommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IQuestRepository _questRepository;
    private readonly MainMenuKeyboardBuilder _menuKeyboardBuilder;

    public MenuCommandHandler(
        ITelegramBotClient botClient, 
        IQuestRepository questRepository, 
        MainMenuKeyboardBuilder menuKeyboardBuilder)
    {
        _botClient = botClient;
        _questRepository = questRepository;
        _menuKeyboardBuilder = menuKeyboardBuilder;
    }

    public async Task HandleStartAsync(long chatId, CancellationToken ct)
    {
        var text = "👋 **Добро пожаловать в GameManager!**\n\n" +
                   "Это платформа для проведения интерактивных текстовых квестов.\n" +
                   "Выберите действие в меню ниже:";

        var keyboard = _menuKeyboardBuilder.BuildMainMenu();

        await _botClient.SendMessage(
            chatId: chatId,
            text: text,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    public async Task HandleQuestListAsync(long chatId, CancellationToken ct)
    {
        var questIds = await _questRepository.GetAvailableQuestIdsAsync();
        var keyboard = _menuKeyboardBuilder.BuildQuestListMenu(questIds);

        await _botClient.SendMessage(
            chatId: chatId,
            text: "📜 **Доступные квесты:**\nВыберите квест для просмотра информации или создания комнаты:",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}