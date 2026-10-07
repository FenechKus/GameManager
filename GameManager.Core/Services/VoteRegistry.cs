using System.Collections.Concurrent;
using GameManager.Core.Entities;
using GameManager.Core.Interfaces;

namespace GameManager.Core.Services;

public class VoteRegistry : IVoteRegistry
{
    private readonly ConcurrentDictionary<Guid, VoteSession> _activeVotes = new();

    public VoteSession StartSession(Guid roomId, string sceneId, int totalPlayersCount, TimeSpan timeout)
    {
        var session = new VoteSession(roomId, sceneId, totalPlayersCount, timeout);
        _activeVotes[roomId] = session;
        return session;
    }

    public VoteSession? GetSession(Guid roomId)
    {
        _activeVotes.TryGetValue(roomId, out var session);
        return session;
    }

    public IEnumerable<VoteSession> GetAllSessions() => _activeVotes.Values;

    public void EndSession(Guid roomId) => _activeVotes.TryRemove(roomId, out _);
}