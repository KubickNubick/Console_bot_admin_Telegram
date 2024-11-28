using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Linq;
using System.Net;

namespace TelegramBotConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Telegram Bot Chat Scanner");
                Console.WriteLine("------------------------");

                // Запрос токена бота
                Console.Write("Введите токен бота: ");
                string botToken = Console.ReadLine();

                while (true)
                {
                    Console.WriteLine("\nВыберите действие:");
                    Console.WriteLine("1. Получить список чатов");
                    Console.WriteLine("2. Получить последние сообщения");
                    Console.WriteLine("3. Получить информацию о боте");
                    Console.WriteLine("4. Получить все Chat ID (включая неактивные)");
                    Console.WriteLine("5. Отправить тестовое сообщение");
                    Console.WriteLine("6. Получить ID канала");
                    Console.WriteLine("7. Выход");
                    Console.Write("\nВаш выбор: ");

                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            await ShowChatsInfo(botToken);
                            break;
                        case "2":
                            await ShowRecentMessages(botToken);
                            break;
                        case "3":
                            await ShowBotInfo(botToken);
                            break;
                        case "4":
                            await ShowAllChatIds(botToken);
                            break;
                        case "5":
                            await SendTestMessage(botToken);
                            break;
                        case "6":
                            await GetChannelId(botToken);
                            break;
                        case "7":
                            return;
                    }

                    Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nПроизошла ошибка: {ex.Message}");
                Console.WriteLine("Нажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
        }

        static async Task GetChannelId(string botToken)
        {
            try
            {
                Console.WriteLine("\nПолучение ID канала...");
                Console.WriteLine("Выберите способ получения ID канала:");
                Console.WriteLine("1. По пересланному сообщению");
                Console.WriteLine("2. По username канала");
                Console.Write("\nВаш выбор: ");

                var choice = Console.ReadLine();

                var bot = new TelegramBotClient(botToken);

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("\nИнструкция для получения ID канала:");
                        Console.WriteLine("1. Добавьте бота администратором в канал");
                        Console.WriteLine("2. Отправьте сообщение в канал");
                        Console.WriteLine("3. Перешлите это сообщение боту в личные сообщения");
                        Console.WriteLine("4. Нажмите Enter для поиска пересланных сообщений");
                        Console.ReadLine();

                        var updates = await bot.GetUpdatesAsync();
                        var channelPosts = updates
                            .Where(update =>
                                update.Message?.ForwardFromChat != null &&
                                update.Message.ForwardFromChat.Type == ChatType.Channel)
                            .Select(x => x.Message.ForwardFromChat)
                            .DistinctBy(x => x.Id)
                            .ToList();

                        if (!channelPosts.Any())
                        {
                            Console.WriteLine("\nКаналы не найдены!");
                            Console.WriteLine("Убедитесь, что вы переслали сообщение из канала боту.");
                        }
                        else
                        {
                            Console.WriteLine("\nНайденные каналы:");
                            Console.WriteLine("------------------");
                            foreach (var channel in channelPosts)
                            {
                                Console.WriteLine($"Название канала: {channel.Title}");
                                Console.WriteLine($"ID канала: {channel.Id}");
                                if (!string.IsNullOrEmpty(channel.Username))
                                    Console.WriteLine($"Username: @{channel.Username}");
                                Console.WriteLine("------------------");
                            }
                        }
                        break;

                    case "2":
                        Console.Write("\nВведите username канала (без @): ");
                        var channelUsername = Console.ReadLine();

                        try
                        {
                            var chat = await bot.GetChatAsync("@" + channelUsername);

                            Console.WriteLine("\nИнформация о канале:");
                            Console.WriteLine("--------------------");
                            Console.WriteLine($"ID канала: {chat.Id}");
                            Console.WriteLine($"Название: {chat.Title}");
                            if (!string.IsNullOrEmpty(chat.Username))
                                Console.WriteLine($"Username: @{chat.Username}");
                            if (!string.IsNullOrEmpty(chat.Description))
                                Console.WriteLine($"Описание: {chat.Description}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\nОшибка: {ex.Message}");
                            Console.WriteLine("\nВозможные причины:");
                            Console.WriteLine("1. Бот не добавлен в канал как администратор");
                            Console.WriteLine("2. Неверный username канала");
                            Console.WriteLine("3. У канала нет публичного username");
                        }
                        break;

                    default:
                        Console.WriteLine("Неверный выбор.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении ID канала: {ex.Message}");
            }
        }

        static async Task ShowAllChatIds(string botToken)
        {
            try
            {
                Console.WriteLine("\nПолучение всех Chat ID...");

                // Настройка прокси если нужно

                var bot = new TelegramBotClient(botToken);

                // Получаем обновления с максимально возможным offset
                var updates = await bot.GetUpdatesAsync(offset: -1, limit: 100);

                // Создаем HashSet для хранения уникальных чатов
                var allChats = new HashSet<(long id, string name, ChatType type)>();

                // Собираем информацию из обновлений
                foreach (var update in updates)
                {
                    if (update.Message?.Chat != null)
                    {
                        var chat = update.Message.Chat;
                        var chatName = chat.Type == ChatType.Private
                            ? $"{chat.FirstName} {chat.LastName}"
                            : chat.Title;
                        allChats.Add((chat.Id, chatName, chat.Type));
                    }
                }

                // Выводим результаты
                if (!allChats.Any())
                {
                    Console.WriteLine("\nВнимание: Не найдено активных чатов!");
                    Console.WriteLine("Для получения Chat ID выполните одно из следующих действий:");
                    Console.WriteLine("1. Отправьте сообщение боту в нужном чате");
                    Console.WriteLine("2. Добавьте бота в нужную группу");
                    Console.WriteLine("3. Назначьте бота администратором в канале");
                }
                else
                {
                    Console.WriteLine("\nНайденные Chat ID:");
                    Console.WriteLine("------------------");
                    foreach (var chat in allChats)
                    {
                        Console.WriteLine($"Chat ID: {chat.id}");
                        Console.WriteLine($"Название: {chat.name}");
                        Console.WriteLine($"Тип: {chat.type}");
                        Console.WriteLine("------------------");
                    }
                    Console.WriteLine($"Всего найдено чатов: {allChats.Count}");
                }

                Console.WriteLine("\nПримечания:");
                Console.WriteLine("- Отрицательный Chat ID означает группу или канал");
                Console.WriteLine("- Положительный Chat ID означает личный чат");
                Console.WriteLine("- Если нужный чат не отображается, отправьте в нём сообщение и повторите поиск");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении Chat ID: {ex.Message}");
            }
        }

        static async Task ShowChatsInfo(string botToken)
        {
            try
            {
                Console.WriteLine("\nПолучение списка чатов...");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                WebRequest.DefaultWebProxy = new WebProxy("fgtsrv.halykbank.nb:8080", true);
                var bot = new TelegramBotClient(botToken);
                var updates = await bot.GetUpdatesAsync();

                var uniqueChats = new HashSet<Chat>();
                foreach (var update in updates)
                {
                    if (update.Message?.Chat != null)
                    {
                        uniqueChats.Add(update.Message.Chat);
                    }
                }

                if (!uniqueChats.Any())
                {
                    Console.WriteLine("Чаты не найдены. Возможно, бот не получал сообщений за последнее время.");
                    return;
                }

                Console.WriteLine("\nНайденные чаты:");
                Console.WriteLine("----------------");
                foreach (var chat in uniqueChats)
                {
                    Console.WriteLine($"ID чата: {chat.Id}");
                    Console.WriteLine($"Тип: {chat.Type}");
                    Console.WriteLine($"Название: {chat.Title ?? "Личный чат"}");
                    if (chat.Type == ChatType.Private)
                    {
                        Console.WriteLine($"Имя: {chat.FirstName} {chat.LastName}");
                        Console.WriteLine($"Username: @{chat.Username}");
                    }
                    Console.WriteLine("----------------");
                }
                Console.WriteLine($"Всего найдено чатов: {uniqueChats.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении списка чатов: {ex.Message}");
            }
        }

        static async Task ShowRecentMessages(string botToken)
        {
            try
            {
                Console.WriteLine("\nПолучение последних сообщений...");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                WebRequest.DefaultWebProxy = new WebProxy("fgtsrv.halykbank.nb:8080", true);
                var bot = new TelegramBotClient(botToken);
                var updates = await bot.GetUpdatesAsync();

                var messages = updates
                    .Where(u => u.Message != null)
                    .Select(u => u.Message)
                    .OrderByDescending(m => m.Date)
                    .Take(10);

                if (!messages.Any())
                {
                    Console.WriteLine("Сообщения не найдены.");
                    return;
                }

                Console.WriteLine("\nПоследние сообщения:");
                Console.WriteLine("-------------------");
                foreach (var message in messages)
                {
                    Console.WriteLine($"Чат: {message.Chat.Title ?? $"{message.Chat.FirstName} {message.Chat.LastName}"}");
                    Console.WriteLine($"ID чата: {message.Chat.Id}");
                    Console.WriteLine($"Время: {message.Date.ToLocalTime()}");
                    Console.WriteLine($"Текст: {message.Text ?? "[не текстовое сообщение]"}");
                    Console.WriteLine("-------------------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении сообщений: {ex.Message}");
            }
        }

        static async Task ShowBotInfo(string botToken)
        {
            try
            {
                Console.WriteLine("\nПолучение информации о боте...");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                WebRequest.DefaultWebProxy = new WebProxy("fgtsrv.halykbank.nb:8080", true);
                var bot = new TelegramBotClient(botToken);
                var me = await bot.GetMeAsync();

                Console.WriteLine("\nИнформация о боте:");
                Console.WriteLine("------------------");
                Console.WriteLine($"ID: {me.Id}");
                Console.WriteLine($"Имя: {me.FirstName}");
                Console.WriteLine($"Username: @{me.Username}");
                Console.WriteLine($"Может присоединяться к группам: {(me.CanJoinGroups ? "Да" : "Нет")}");
                Console.WriteLine($"Может читать все сообщения группы: {(me.CanReadAllGroupMessages ? "Да" : "Нет")}");
                Console.WriteLine($"Поддерживает inline-режим: {(me.SupportsInlineQueries ? "Да" : "Нет")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении информации о боте: {ex.Message}");
            }
        }

        static async Task SendTestMessage(string botToken)
        {
            try
            {
                Console.Write("\nВведите Chat ID для отправки тестового сообщения: ");
                var chatId = Console.ReadLine();

                if (string.IsNullOrEmpty(chatId))
                {
                    Console.WriteLine("Chat ID не может быть пустым!");
                    return;
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                WebRequest.DefaultWebProxy = new WebProxy("fgtsrv.halykbank.nb:8080", true);
                var bot = new TelegramBotClient(botToken);

                Console.WriteLine("Отправка тестового сообщения...");
                var message = await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Тестовое сообщение от бота\nВремя: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    parseMode: ParseMode.Markdown);

                Console.WriteLine($"Сообщение успешно отправлено! Message ID: {message.MessageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
            }
        }
    }
}