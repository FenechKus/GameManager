using GameManager.Core.Enums;

namespace GameManager.Core.Entities;

public class Player
{
    public long ChatId { get; set; }
    public string Username { get;}
    public PlayerRole Role { get; set; }

    public Player(long chatId, string username, PlayerRole role = PlayerRole.Player)
    {
        ChatId = chatId;
        Username = username;
        Role = role;
    }
}