using System.Collections.Concurrent;

namespace GameManager.Bot.Services;

public enum UserState
{
    None,
    AwaitingRoomIdToJoin
}

public class UserInputStateRegistry
{
    private readonly ConcurrentDictionary<long, UserState> _userStates = new();

    public void SetState(long userId, UserState state)
    {
        _userStates[userId] = state;
    }

    public UserState GetState(long userId)
    {
        return _userStates.TryGetValue(userId, out var state) ? state : UserState.None;
    }

    public void ClearState(long userId)
    {
        _userStates.TryRemove(userId, out _);
    }
}