using EcoApp.API.Data;
using EcoApp.API.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EcoApp.API.Services
{
    public class TelegramService
    {
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider; // ✅ Вместо прямого DbContext
        private TelegramBotClient? _botClient;
        private CancellationTokenSource? _cts;

        public TelegramService(IConfiguration config, IServiceProvider serviceProvider)
        {
            _config = config;
            _serviceProvider = serviceProvider;

            var token = _config["Telegram:BotToken"];
            if (!string.IsNullOrEmpty(token))
            {
                _botClient = new TelegramBotClient(token);
            }
        }

        public void StartPolling()
        {
            if (_botClient == null) return;

            _cts = new CancellationTokenSource();

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: new Telegram.Bot.Polling.ReceiverOptions
                {
                    AllowedUpdates = Array.Empty < UpdateType > ()
                },
                cancellationToken: _cts.Token
            );

            Console.WriteLine("[TELEGRAM] Polling started");
        }

        public void StopPolling()
        {
            _cts?.Cancel();
            Console.WriteLine("[TELEGRAM] Polling stopped");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text.Trim();
            var firstName = update.Message.From?.FirstName ?? "Пользователь";

            if (text.StartsWith("/start"))
            {
                await HandleStartCommand(botClient, chatId, text, firstName, cancellationToken);
                return;
            }

            if (text == "/stop")
            {
                await HandleStopCommand(botClient, chatId, cancellationToken);
                return;
            }
        }

        private async Task HandleStartCommand(ITelegramBotClient botClient, long chatId, string text, string firstName, CancellationToken cancellationToken)
        {
            var parts = text.Split(' ');
            int userId = 0;

            if (parts.Length > 1 && parts[1].StartsWith("USERID_"))
            {
                int.TryParse(parts[1].Replace("USERID_", ""), out userId);
            }

            // ✅ Создаём новый scope для DbContext
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService < AppDbContext > ();

            var existingByChatId = context.TelegramSubscriptions
                .FirstOrDefault(t => t.ChatId == chatId);

            if (existingByChatId != null)
            {
                if (userId > 0 && existingByChatId.UserId != userId)
                {
                    existingByChatId.UserId = userId;
                    context.SaveChanges();

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"✅ Аккаунт обновлён, {firstName}!",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                    return;
                }

                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"✅ Вы уже подписаны, {firstName}!\n\nОтписаться: /stop",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
                return;
            }

            if (userId > 0)
            {
                var existingByUserId = context.TelegramSubscriptions
                    .FirstOrDefault(t => t.UserId == userId);

                if (existingByUserId != null)
                {
                    existingByUserId.ChatId = chatId;
                    context.SaveChanges();

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"✅ Подписка обновлена, {firstName}!\n\nОтписаться: /stop",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                    return;
                }

                context.TelegramSubscriptions.Add(new TelegramSubscription
                {
                    UserId = userId,
                    ChatId = chatId
                });
                context.SaveChanges();

                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"🎉 Подписка активна, {firstName}!\n\nУведомления о:\n• 🎉 Мероприятиях\n• 📰 Статьях\n• 📋 Заданиях\n• ✅ Одобрении отчётов\n\nОтписаться: /stop",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);

                Console.WriteLine($"[TELEGRAM] Подписка: UserId={userId}, ChatId={chatId}");
                return;
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: $"🌿 Привет, {firstName}!\n\nЧтобы подписаться, откройте приложение EcoApp → Профиль → Подписаться на уведомления.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }

        private async Task HandleStopCommand(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService < AppDbContext > ();

            var subscription = context.TelegramSubscriptions
                .FirstOrDefault(t => t.ChatId == chatId);

            if (subscription != null)
            {
                context.TelegramSubscriptions.Remove(subscription);
                context.SaveChanges();

                await botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Вы отписались.\n\nЧтобы подписаться снова, используйте кнопку в приложении.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);

                Console.WriteLine($"[TELEGRAM] Отписка: ChatId={chatId}");
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Вы не были подписаны.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[TELEGRAM ERROR] {exception.Message}");
            return Task.CompletedTask;
        }

        public async Task NotifyAdminAsync(string message)
        {
            var adminChatId = _config["Telegram:AdminChatId"];
            if (string.IsNullOrEmpty(adminChatId) || _botClient == null)
            {
                Console.WriteLine($"[TELEGRAM] Admin: {message}");
                return;
            }

            try
            {
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: message,
                    parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TELEGRAM ERROR] Admin: {ex.Message}");
            }
        }

        public async Task NotifyUserAsync(int userId, string message)
        {
            if (_botClient == null)
            {
                Console.WriteLine($"[TELEGRAM] To user {userId}: {message}");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService < AppDbContext > ();

            var subscription = context.TelegramSubscriptions
                .FirstOrDefault(t => t.UserId == userId);

            if (subscription == null)
            {
                Console.WriteLine($"[TELEGRAM] User {userId} не подписан");
                return;
            }

            try
            {
                await _botClient.SendMessage(
                    chatId: subscription.ChatId,
                    text: message,
                    parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TELEGRAM ERROR] User {userId}: {ex.Message}");
            }
        }

        public async Task BroadcastToSubscribersAsync(string category, string message)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService < AppDbContext > ();

            var subscribers = context.TelegramSubscriptions
                .Where(s =>
                    (category == "events" && s.NotifyEvents) ||
                    (category == "articles" && s.NotifyArticles) ||
                    (category == "tasks" && s.NotifyTasks))
                .ToList();

            foreach (var sub in subscribers)
            {
                try
                {
                    await _botClient?.SendMessage(
                        chatId: sub.ChatId,
                        text: message,
                        parseMode: ParseMode.Markdown);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TELEGRAM BROADCAST ERROR] Chat {sub.ChatId}: {ex.Message}");
                }
            }
        }
    }
}