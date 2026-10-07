namespace GameManager.Core.Entities;

using System.Collections.Concurrent;

public class VoteSession
{
    private readonly ConcurrentDictionary<long, int> _playerVotes = new();

    public Guid RoomId { get; }
    public string SceneId { get; }
    public int TotalPlayersCount { get; }
    public DateTime CreatedAt { get; }
    public TimeSpan Timeout { get; }

    public bool IsExpired => DateTime.UtcNow - CreatedAt >= Timeout;

    public VoteSession(Guid roomId, string sceneId, int totalPlayersCount, TimeSpan timeout)
    {
        RoomId = roomId;
        SceneId = sceneId;
        TotalPlayersCount = totalPlayersCount;
        Timeout = timeout;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Регистрирует голос игрока и выводит лог в консоль.
    /// </summary>
    public bool RegisterVote(long chatId, int choiceIndex)
    {
        _playerVotes[chatId] = choiceIndex;

        // Вывод подробного лога о голосе в консоль бота
        Console.WriteLine($"🗳️ [Vote Log]: Игрок {chatId} проголосовал за вариант №{choiceIndex + 1} в комнате {RoomId} (Сцена: {SceneId}). Проголосовало: {_playerVotes.Count}/{TotalPlayersCount}");

        return _playerVotes.Count >= TotalPlayersCount;
    }

    public int GetWinningChoiceIndex()
    {
        if (_playerVotes.IsEmpty)
        {
            Console.WriteLine($"⚠️ [Vote Log]: Ни один игрок не проголосовал в комнате {RoomId}. Выбирается вариант по умолчанию (0).");
            return 0;
        }

        var voteGroups = _playerVotes.Values
            .GroupBy(choiceIndex => choiceIndex)
            .Select(group => new { ChoiceIndex = group.Key, Count = group.Count() })
            .ToList();

        int maxVotes = voteGroups.Max(g => g.Count);

        var topChoices = voteGroups
            .Where(g => g.Count == maxVotes)
            .Select(g => g.ChoiceIndex)
            .ToList();

        if (topChoices.Count == 1)
        {
            Console.WriteLine($"🏆 [Vote Log]: В комнате {RoomId} победил вариант №{topChoices[0] + 1} ({maxVotes} голосов).");
            return topChoices[0];
        }

        // Лог случайного выбора при ничьей
        int randomIndex = Random.Shared.Next(topChoices.Count);
        int selectedChoice = topChoices[randomIndex];

        Console.WriteLine($"⚖️ [Vote Log]: Ничья при выборе в комнате {RoomId}! Рандомно выбран вариант №{selectedChoice + 1} из лидирующих вариантов.");
        return selectedChoice;
    }
}