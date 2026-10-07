using GameManager.Bot.Handlers;
using GameManager.Bot.Keyboards;
using GameManager.Bot.Observers;
using GameManager.Bot.Services;
using GameManager.Core.Interfaces;
using GameManager.Core.Services;
using GameManager.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = Host.CreateApplicationBuilder(args);

// 1. Конфигурация Telegram Bot API
var botToken = builder.Configuration["TelegramBot:Token"] 
    ?? throw new InvalidOperationException("Токен TelegramBot:Token не найден в appsettings.json");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

// 2. Инфраструктура и хранилища
var questsPath = Path.Combine(AppContext.BaseDirectory, "Quests");
builder.Services.AddSingleton<IQuestRepository>(new JsonQuestRepository(questsPath));
builder.Services.AddSingleton<IRoomRegistry, RoomRegistry>();
builder.Services.AddSingleton<IWorldStateRegistry, WorldStateRegistry>();
builder.Services.AddSingleton<IVoteRegistry, VoteRegistry>();
builder.Services.AddSingleton<UserInputStateRegistry>();

// 3. Билдеры клавиатур
builder.Services.AddSingleton<MainMenuKeyboardBuilder>();
builder.Services.AddSingleton<ChoiceKeyboardBuilder>();
builder.Services.AddSingleton<AdminKeyboardBuilder>();

// 4. Обработчики
builder.Services.AddSingleton<MenuCommandHandler>();
builder.Services.AddSingleton<UserCommandHandler>();
builder.Services.AddSingleton<AdminCommandHandler>();
builder.Services.AddSingleton<UpdateHandler>();

// 5. Игровой движок и Observers
builder.Services.AddSingleton<DynamicSceneService>();
builder.Services.AddSingleton<LeaderboardService>();
builder.Services.AddSingleton<IGameEngine, GameEngine>();
builder.Services.AddSingleton<TelegramNotificationObserver>();

// 6. Фоновые сервисы
builder.Services.AddHostedService<VoteTimeoutWorker>();

var host = builder.Build();

// Запускаем экспресс-тестирование движка при старте
//await GameManager.Tests.LoadTest.RunStressTestAsync(host.Services, teamCount: 200, playersPerTeam: 4);

// 7. Запуск подписки Telegram Bot API (Long Polling)
var botClient = host.Services.GetRequiredService<ITelegramBotClient>();
var updateHandler = host.Services.GetRequiredService<UpdateHandler>();
var gameEngine = host.Services.GetRequiredService<IGameEngine>();
var observer = host.Services.GetRequiredService<TelegramNotificationObserver>();

if (gameEngine is GameEngine concreteEngine)
{
    concreteEngine.Subscribe(observer);
}

using var cts = new CancellationTokenSource();

botClient.StartReceiving(
    updateHandler: updateHandler,
    receiverOptions: new ReceiverOptions
    {
        AllowedUpdates = []
    },
    cancellationToken: cts.Token
);

Console.WriteLine("🤖 GameManager Bot успешно запущен! Нажмите Ctrl+C для остановки.");

await host.RunAsync();