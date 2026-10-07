using Telegram.Bot.Types.ReplyMarkups;

namespace GameManager.Bot.Keyboards;

public class AdminKeyboardBuilder
{
    public InlineKeyboardMarkup BuildLobbyMenu(Guid roomId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🚀 Запустить квест", $"admin_start_{roomId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("❌ Закрыть комнату", $"admin_close_{roomId}")
            }
        });
    }

    public InlineKeyboardMarkup BuildInGameAdminMenu(Guid roomId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⏸ Пауза", $"admin_pause_{roomId}"),
                InlineKeyboardButton.WithCallbackData("⏭ Принудительный шаг", $"admin_force_next_{roomId}")
            }
        });
    }
}