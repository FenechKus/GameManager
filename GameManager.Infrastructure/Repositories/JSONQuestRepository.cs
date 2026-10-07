using System.Text.Json;
using System.Text.Json.Serialization;
using GameManager.Core.Entities;
using GameManager.Core.Interfaces;

namespace GameManager.Infrastructure.Repositories;

public class JsonQuestRepository : IQuestRepository
{
    private readonly string _questsFolderPath;
    private readonly Dictionary<string, QuestModel> _questCache = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonQuestRepository(string questsFolderPath)
    {
        _questsFolderPath = questsFolderPath;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<QuestModel> GetQuestByIdAsync(string questId)
    {
        if (_questCache.TryGetValue(questId, out var cachedQuest))
            return cachedQuest;

        // Поиск файла по пути: Quests/{questId}/{questId}.json
        var filePath = Path.Combine(_questsFolderPath, questId, $"{questId}.json");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Сценарий квеста '{questId}' не найден по пути: {filePath}");
        }

        using var stream = File.OpenRead(filePath);
        var quest = await JsonSerializer.DeserializeAsync<QuestModel>(stream, _jsonOptions);

        if (quest == null)
        {
            throw new InvalidOperationException($"Не удалось распарсить сценарий '{questId}.json'.");
        }

        _questCache[questId] = quest;
        return quest;
    }

    public Task<IEnumerable<string>> GetAvailableQuestIdsAsync()
    {
        if (!Directory.Exists(_questsFolderPath))
        {
            Directory.CreateDirectory(_questsFolderPath);
            return Task.FromResult(Enumerable.Empty<string>());
        }

        // Сканируем все поддиректории в папке Quests
        var directories = Directory.GetDirectories(_questsFolderPath);
        var questIds = directories
            .Select(Path.GetFileName)
            .Where(dirName => !string.IsNullOrEmpty(dirName) && 
                              File.Exists(Path.Combine(_questsFolderPath, dirName, $"{dirName}.json")))!;

        return Task.FromResult<IEnumerable<string>>(questIds!);
    }

    /// <summary>
    /// Вспомогательный метод получения абсолютного пути к медиафайлу
    /// </summary>
    public string GetAssetAbsolutePath(string questId, string relativeFilePath)
    {
        return Path.Combine(_questsFolderPath, questId, "assets", relativeFilePath);
    }
}