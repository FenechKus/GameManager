using System.Diagnostics;
using GameManager.Core.Entities;
using GameManager.Core.Interfaces;
using GameManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameManager.Tests;

public static class LoadTest
{
    public static async Task RunStressTestAsync(IServiceProvider services, int teamCount = 100, int playersPerTeam = 4)
    {
        Console.WriteLine($"\n🚀 === СТАРТ НАГРУЗОЧНОГО ТЕСТИРОВАНИЯ ===");
        Console.WriteLine($"📊 Параметры: {teamCount} комнат | {playersPerTeam} игроков в команде | Всего игроков: {teamCount * playersPerTeam}");

        var gameEngine = services.GetRequiredService<IGameEngine>();
        var roomRegistry = services.GetRequiredService<IRoomRegistry>();
        var worldRegistry = services.GetRequiredService<IWorldStateRegistry>();

        // Очищаем предыдущее состояние
        worldRegistry.ResetWorld();

        var roomIds = new List<Guid>();
        long totalChatIdCounter = 1000000;

        var sw = Stopwatch.StartNew();

        // 1. Симуляция параллельного создания комнат и подключения игроков
        Console.WriteLine("⏳ [1/3] Создание комнат и регистрация игроков...");
        var createTasks = Enumerable.Range(1, teamCount).Select(async i =>
        {
            var room = await gameEngine.CreateRoomAsync($"LoadTest_Team_{i}", "last_signal");
            
            // Назначаем модератора
            await gameEngine.AssignModeratorAsync(room.Id, totalChatIdCounter++);

            // Добавляем остальных 3 игроков
            for (int p = 1; p < playersPerTeam; p++)
            {
                var playerId = Interlocked.Increment(ref totalChatIdCounter);
                await gameEngine.ConnectPlayerAsync(room.Id, new Player(playerId, $"Player_{playerId}"));
            }

            lock (roomIds)
            {
                roomIds.Add(room.Id);
            }
        });

        await Task.WhenAll(createTasks);
        Console.WriteLine($"✅ Комнаты созданы за {sw.ElapsedMilliseconds} ms.");

        // 2. Симуляция параллельного старта всех игр
        sw.Restart();
        Console.WriteLine("⏳ [2/3] Массовый запуск всех квестов...");
        var startTasks = roomIds.Select(roomId => gameEngine.StartQuestAsync(roomId));
        await Task.WhenAll(startTasks);
        Console.WriteLine($"✅ Все {teamCount} комнат стартовали за {sw.ElapsedMilliseconds} ms.");

        // 3. Симуляция игрового процесса (Игроки одновременно делают выборы)
        sw.Restart();
        Console.WriteLine("⏳ [3/3] Симуляция симультанных ходов игроков...");

        int totalActionsCount = 0;

        var gameplayTasks = roomIds.Select(async roomId =>
        {
            // Симулируем 5 быстрых шагов/выборов в квесте для каждой команды
            for (int step = 0; step < 5; step++)
            {
                var choices = await gameEngine.GetAvailableChoicesAsync(roomId);
                if (!choices.Any()) break; // Команда дошла до финала

                // Имитируем случайный выбор из доступных
                int choiceIndex = Random.Shared.Next(0, choices.Count);
                await gameEngine.MakeChoiceAsync(roomId, choiceIndex);

                Interlocked.Increment(ref totalActionsCount);

                // Небольшая случайная задержка между ходами (эмуляция раздумий игроков 10-50мс)
                await Task.Delay(Random.Shared.Next(10, 50));
            }
        });

        await Task.WhenAll(gameplayTasks);
        sw.Stop();

        // 4. Подведение итогов нагрузки
        Console.WriteLine($"\n🏆 === ИТОГИ ТЕСТА НАГРУЗКИ ===");
        Console.WriteLine($"⏱️ Общее время прохождения: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"⚡ Обработано игровых ходов: {totalActionsCount}");
        Console.WriteLine($"📊 Производительность движка: {(totalActionsCount / (sw.ElapsedMilliseconds / 1000.0)):F2} операций/сек");
        Console.WriteLine($"💾 Зарегистрировано комнат в памяти: {roomRegistry.GetAllRooms().Count}");
        Console.WriteLine("===========================================\n");
    }
}