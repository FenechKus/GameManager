using GameManager.Core.Entities;

namespace GameManager.Core.Interfaces;

public interface IVoteRegistry
{
    VoteSession StartSession(Guid roomId, string sceneId, int totalPlayersCount, TimeSpan timeout);
    VoteSession? GetSession(Guid roomId);
    IEnumerable<VoteSession> GetAllSessions();
    void EndSession(Guid roomId);
}