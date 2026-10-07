using GameManager.Core.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace GameManager.Bot.Keyboards;

public class ChoiceKeyboardBuilder
{
    public InlineKeyboardMarkup BuildChoiceMenu(
        Guid roomId, 
        IReadOnlyList<QuestChoice> choices, 
        IReadOnlyDictionary<int, int>? voteCounts = null)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            
            // Формируем текст кнопки (с учетом голосов, если идет процесс голосования)
            string buttonText = choice.Text;
            if (voteCounts != null && voteCounts.TryGetValue(i, out int votes) && votes > 0)
            {
                buttonText = $"[{votes}] {choice.Text}";
            }

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: buttonText, 
                    // Индекс 'i' соответствует позиции в отфильтрованном списке доступных выборов
                    callbackData: $"vote_{roomId}_{i}")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }
}