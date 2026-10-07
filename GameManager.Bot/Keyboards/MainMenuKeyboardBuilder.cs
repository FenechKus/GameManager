using Telegram.Bot.Types.ReplyMarkups;

namespace GameManager.Bot.Keyboards;

public class MainMenuKeyboardBuilder
{
    public InlineKeyboardMarkup BuildMainMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📜 Список квестов", "menu_quests"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔑 Подключиться к комнате", "menu_join")
            }
        });
    }

    public InlineKeyboardMarkup BuildQuestListMenu(IEnumerable<string> questIds)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var qId in questIds)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"🎮 {qId}", $"quest_select_{qId}")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад в меню", "menu_main")
        });

        return new InlineKeyboardMarkup(buttons);
    }
}