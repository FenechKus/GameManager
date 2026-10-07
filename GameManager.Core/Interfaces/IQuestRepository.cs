using GameManager.Core.Entities;

namespace GameManager.Core.Interfaces;

public interface IQuestRepository
{
    Task<QuestModel> GetQuestByIdAsync(string questId);
    Task<IEnumerable<string>> GetAvailableQuestIdsAsync();
    string GetAssetAbsolutePath(string questId, string relativeFilePath);
    
}