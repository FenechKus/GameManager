using System.Net;
using System.Text;
using GameManager.Core.Entities;
using GameManager.Core.Interfaces;

namespace GameManager.Core.Services;

public class LeaderboardService
{
    private readonly IRoomRegistry _roomRegistry;
    private readonly IWorldStateRegistry _worldStateRegistry;

    public LeaderboardService(IRoomRegistry roomRegistry, IWorldStateRegistry worldStateRegistry)
    {
        _roomRegistry = roomRegistry;
        _worldStateRegistry = worldStateRegistry;
    }

    // 👈 Принимаем список конкретных ID комнат этой игровой сессии
    public string BuildLeaderboardText(IEnumerable<Guid>? sessionRoomIds = null)
    {
        var rooms = _roomRegistry.GetAllRooms();

        // Если переданы ID комнат текущей сессии — фильтруем по ним!
        if (sessionRoomIds != null && sessionRoomIds.Any())
        {
            rooms = rooms.Where(r => sessionRoomIds.Contains(r.Id)).ToList();
        }

        if (!rooms.Any()) return "🏆 <b>Турнирная таблица пуста.</b>";

        var leaderboard = rooms
            .Select(room => new
            {
                RoomName = room.Name,
                Score = room.Score,
                UniqueItems = room.Inventory
                    .Where(item => _worldStateRegistry.IsItemClaimed(item))
                    .ToList()
            })
            .OrderByDescending(r => r.Score)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("🏆 <b>ТУРНИРНАЯ ТАБЛИЦА КВЕСТА</b> 🏆\n");

        int rank = 1;
        foreach (var team in leaderboard)
        {
            string medal = rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"<b>#{rank}</b>"
            };

            string safeRoomName = WebUtility.HtmlEncode(team.RoomName);
            string itemsText = team.UniqueItems.Any()
                ? string.Join(", ", team.UniqueItems.Select(i => $"<code>{WebUtility.HtmlEncode(i)}</code>"))
                : "<i>нет</i>";

            sb.AppendLine($"{medal} <b>Команда:</b> {safeRoomName}");
            sb.AppendLine($"   📊 <b>Очки:</b> {team.Score}");
            sb.AppendLine($"   🗝️ <b>Уникальные предметы:</b> {itemsText}");
            sb.AppendLine("-------------------------");

            rank++;
        }

        return sb.ToString();
    }
}