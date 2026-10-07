using GameManager.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameManager.Bot.Services;

public class VoteTimeoutWorker : BackgroundService
{
    private readonly IVoteRegistry _voteRegistry;
    private readonly IGameEngine _gameEngine;
    private readonly ILogger<VoteTimeoutWorker> _logger;

    public VoteTimeoutWorker(
        IVoteRegistry voteRegistry,
        IGameEngine gameEngine,
        ILogger<VoteTimeoutWorker> logger)
    {
        _voteRegistry = voteRegistry;
        _gameEngine = gameEngine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("⏳ VoteTimeoutWorker запущен и отслеживает таймауты голосований.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var activeSessions = _voteRegistry.GetAllSessions().ToList();

                foreach (var session in activeSessions)
                {
                    if (session.IsExpired)
                    {
                        _logger.LogInformation("⏱️ Время голосования истекло для комнаты {RoomId} (Сцена: {SceneId})", 
                            session.RoomId, session.SceneId);

                        // 1. Завершаем сессию в реестре, чтобы предотвратить повторные клики
                        _voteRegistry.EndSession(session.RoomId);

                        // 2. Определяем победивший вариант выбора
                        int winningChoice = session.GetWinningChoiceIndex();

                        // Защитный фоллбек: если никто не успел проголосовать (или вернулся -1), выбираем первый вариант (0)
                        if (winningChoice < 0)
                        {
                            _logger.LogWarning("⚠️ Голоса отсутствуют. Выбирается первый вариант по умолчанию (0) для комнаты {RoomId}", session.RoomId);
                            winningChoice = 0;
                        }

                        // 3. Передаем выбор в игровой движок
                        await _gameEngine.MakeChoiceAsync(session.RoomId, winningChoice);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при обработке таймаутов голосования в VoteTimeoutWorker.");
            }

            // Интервал проверки — 1 секунда
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("🛑 VoteTimeoutWorker остановлен.");
    }
}