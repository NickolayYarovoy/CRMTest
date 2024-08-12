using Microsoft.Extensions.Logging;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Hosting;
using Kernel;
using System.Text.RegularExpressions;
using CRMParkTest.DatabaseContext;

namespace CRMParkTest.Services
{
    /// <summary>
    /// Класс для получения обновлений с сервера
    /// </summary>
    internal class UpdateHandler : BackgroundService
    {
        ITelegramBotClient bot;
        ILogger<UpdateHandler> logger;
        DataBaseContext db;
        IKernel kernel;

        object lockDbObject = new();

        public UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger, IKernel kernel,
            DataBaseContext db)
        {
            this.bot = bot;
            this.logger = logger;
            this.kernel = kernel;
            this.db = db;
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is RequestException)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        /// <summary>
        /// Обработчик получения обновлений (сообщений)
        /// </summary>
        /// <param name="botClient">Клиент</param>
        /// <param name="update">Полученное обновление (сообщение)</param>
        /// <param name="cancellationToken"></param>
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await (update switch
            {
                { Message: { } message } => OnMessage(message),
                { }
                _ => UnknownUpdateHandlerAsync(update)
            });
        }

        /// <summary>
        /// Обработка полученного сообщения
        /// </summary>
        /// <param name="msg">Сообщение</param>
        private async Task OnMessage(Message msg)
        {
            logger.LogInformation("Receive message type: {MessageType}", msg.Type);
            if (msg.Text is not { } messageText)
            {
                // В случае, если в сообщении нет текста, отправляет пользователю предупреждение
                await bot.SendTextMessageAsync(msg.Chat, $"Поддерживаются только текстовые сообщения", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                return;
            }

            await CommandHandler(messageText, msg);
        }

        /// <summary>
        /// Обработчик команд бота
        /// </summary>
        /// <param name="messageText">Текст сообщения</param>
        /// <param name="msg">Сообщение</param>
        /// <param name="fromLast">Была ли использована команда /last</param>
        private async Task CommandHandler(string messageText, Message msg, bool fromLast = false)
        {
            string[] messageData = Regex.Replace(messageText, @"\s+", " ").Split(' ');

            // По названию команды производит выбор действия
            await (messageData[0] switch
            {
                "/start" => StartCommand(msg, fromLast),
                "/help" => UsageCommand(msg, fromLast),
                "/hello" => HelloCommand(msg, fromLast),
                "/inn" => CompanyInfoCommand(msg, messageData.Skip(1), fromLast),
                "/okved" => OkvedInfoCommand(msg, messageData.Skip(1), fromLast),
                "/egrul" => EgrulInfoCommand(msg, messageData.Skip(1), fromLast),
                "/last" => LastCommand(msg),
                _ => UnknownType(msg),
            });
        }

        /// <summary>
        /// Вывод выписок из ЕГРЮЛ
        /// </summary>
        /// <param name="msg">Сообщение пользователя</param>
        /// <param name="inns">Список ИНН компаний</param>
        /// <param name="fromLast">Была ли использована команда /last</param>
        private async Task EgrulInfoCommand(Message msg, IEnumerable<string> inns, bool fromLast)
        {
            // Если не была использована команда /last перезаписываем последнее сообщение в базе
            if(!fromLast)
                SaveLastValidCommand(msg);

            // Если список ИНН пуст, возвращаем ошибку пользователю
            if(inns.Count()== 0)
                await bot.SendTextMessageAsync(msg.Chat, $"Введите как минимум один ИНН", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());

            foreach (var inn in inns)
            {
                if (!Regex.IsMatch(inn, "[0-9]{10}"))
                {
                    await bot.SendTextMessageAsync(msg.Chat, $"Строка \"{inn}\" не является валидным ИНН", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                    continue;
                }

                try
                {
                    var result = await kernel.GetEgrul(inn);

                    InputFile iof = new InputFileStream(result, $"{inn}.pdf");

                    var send = await bot.SendDocumentAsync(msg.Chat.Id, iof);
                }
                catch (ArgumentException ex)
                {
                    await bot.SendTextMessageAsync(msg.Chat, $"Компания с ИНН \"{inn}\" не найдена", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                }
            }
        }

        /// <summary>
        /// Выполнение последней корректной команды пользователя
        /// </summary>
        /// <param name="msg">Сообщение пользователя</param>
        private async Task LastCommand(Message msg)
        {
            long chatId = msg.Chat.Id;
            string? messageData; // Последняя валидная команда пользователя
            // Т.к. используется база SQLite, запрещено многократное обращение к ней
            lock (lockDbObject)
            {
                messageData = (db.Commands.FirstOrDefault(x => x.Id == chatId))?.Text;
            }
            if (messageData == null)
            {
                await bot.SendTextMessageAsync(msg.Chat, $"Вы еще не ввели ни одной валидной команды!", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                return;
            }

            await CommandHandler(messageData, msg, true);
        }

        /// <summary>
        /// Сохранение в базе последней правильной команды пользователя
        /// </summary>
        /// <param name="msg"></param>
        private void SaveLastValidCommand(Message msg)
        {
            long chatId = msg.Chat.Id;
            string message = msg.Text;
            LastValidCommand? command;
            // Т.к. используется база SQLite, запрещено многократное обращение к ней
            lock (lockDbObject)
            {
                command = db.Commands.FirstOrDefault(x => x.Id == chatId);
            }
            if(command == null)
                db.Commands.Add(new(chatId, message)); // Если пользователь не вводил команды, создаем его в базе
            else
                command.Text = message;

            // Т.к. используется база SQLite, запрещено многократное обращение к ней
            lock (lockDbObject)
            {
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Получение информации о видах деятельности компании
        /// </summary>
        /// <param name="msg">Сообщение пользователя</param>
        /// <param name="inns">Список ИНН</param>
        /// <param name="fromLast">Была ли вызвана команда /last</param>
        private async Task OkvedInfoCommand(Message msg, IEnumerable<string> inns, bool fromLast)
        {
            // Если не была использована команда /last перезаписываем последнее сообщение в базе
            if (!fromLast)
                SaveLastValidCommand(msg);

            // Если список ИНН пуст, возвращаем ошибку пользователю
            if (inns.Count() == 0)
                await bot.SendTextMessageAsync(msg.Chat, $"Введите как минимум один ИНН", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());

            foreach (var inn in inns)
            {
                // Проверка валидности ИНН
                if (!Regex.IsMatch(inn, "[0-9]{10}"))
                {
                    await bot.SendTextMessageAsync(msg.Chat, $"Строка \"{inn}\" не является валидным ИНН", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                    continue;
                }

                try
                {
                    var result = await kernel.GetOkved(inn);
                    await bot.SendTextMessageAsync(msg.Chat, $"""
                        <b><a>Список видов деятельности компании с ИНН "{inn}":</a></b>
                        {string.Join('\n', result.Select(x => x.Code + " " + x.ActivityType))}
                        """, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                }
                catch (ArgumentException ex)
                {
                    await bot.SendTextMessageAsync(msg.Chat, $"Компания с ИНН \"{inn}\" не найдена", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                }
            }
        }

        /// <summary>
        /// Получение информации о компании
        /// </summary>
        /// <param name="msg">Сообщение пользователя</param>
        /// <param name="inns">Список ИНН</param>
        /// <param name="fromLast">Была ли вызвана команда /last</param>
        private async Task CompanyInfoCommand(Message msg, IEnumerable<string> inns, bool fromLast)
        {
            // Если не была использована команда /last перезаписываем последнее сообщение в базе
            if (!fromLast)
                SaveLastValidCommand(msg);

            // Если список ИНН пуст, возвращаем ошибку пользователю
            if (inns.Count() == 0)
                await bot.SendTextMessageAsync(msg.Chat, $"Введите как минимум один ИНН", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());

            foreach (var inn in inns)
            {
                if (!Regex.IsMatch(inn, "[0-9]{10}"))
                {
                    await bot.SendTextMessageAsync(msg.Chat, $"Строка \"{inn}\" не является валидным ИНН", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                    continue;
                }

                try
                {
                    var result = await kernel.GetCompanyInfo(inn);
                    await bot.SendTextMessageAsync(msg.Chat, $"""
                        <b><a>Информация о компании с ИНН "{inn}":</a></b>
                        Наименование компании: {result.Name}
                        Юридический адрес: {result.Address}
                        """, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                }
                catch (ArgumentException ex) 
                {
                    await bot.SendTextMessageAsync(msg.Chat, $"Компания с ИНН \"{inn}\" не найдена", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                }
            }
        }

        /// <summary>
        /// Вызов команды /start
        /// </summary>
        /// <param name="msg">Сообщение пользователя</param>
        /// <param name="fromLast">Была ли вызвана команда /last</param>
        private async Task StartCommand(Message msg, bool fromLast)
        {
            // Если не была использована команда /last перезаписываем последнее сообщение в базе
            if (!fromLast)
                SaveLastValidCommand(msg);

            const string usage = """
                Здравствуйте! Для получения информации о работе с ботов используйте команду /help.
            """;
            await bot.SendTextMessageAsync(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
        }

        /// <summary>
        /// Вызов команды /help
        /// </summary>
        /// <param name="msg">Сообщение пользователя</param>
        /// <param name="fromLast">Была ли вызвана команда /last</param>
        async Task UsageCommand(Message msg, bool fromLast)
        {
            // Если не была использована команда /last перезаписываем последнее сообщение в базе
            if (!fromLast)
                SaveLastValidCommand(msg);

            const string usage = """
                <b><u>Bot menu</u></b>:
                /start – начать общение с ботом;
                /help – получить справку о доступных командах;
                /hello – получить информацию о создателе бота;
                /inn – получить наименования и адреса компаний по ИНН;
                /okved - получить информацию о видах деятельности компании, остортированных в обратном алфавитном порядке;
                /egrul - получить выписку из ЕГРЮЛ по ИНН компании;
                /last – повторить последнее действие бота.
            """;
            await bot.SendTextMessageAsync(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
        }

        /// <summary>
        /// Вызов команды /hello
        /// </summary>
        /// <param name="msg">Сообщение пользователя</param>
        /// <param name="fromLast">Была ли вызвана команда /last</param>
        async Task HelloCommand(Message msg, bool fromLast)
        {
            // Если не была использована команда /last перезаписываем последнее сообщение в базе
            if (!fromLast)
                SaveLastValidCommand(msg);

            const string usage = """
                <b><u>Информация об авторе</u></b>:
                Имя: Яровой Николай Михайлович
                E-mail: kolaja36@gmail.com
                Ссылка на github: https://github.com/NickolayYarovoy
            """;
            await bot.SendTextMessageAsync(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
        }

        /// <summary>
        /// Вызов любой команды, отличной от действительных
        /// </summary>
        /// <param name="msg">Сообщение пользователя</param>
        /// <returns></returns>
        async Task UnknownType(Message msg)
        {
            const string usage = "Неизвестная команда, попробуйте ещё раз";
            await bot.SendTextMessageAsync(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
        }

        /// <summary>
        /// Получение неизвестного типа обновления (не сообщение)
        /// </summary>
        /// <param name="update">Обновление</param>
        private Task UnknownUpdateHandlerAsync(Update update)
        {
            logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Запуск приложения
        /// </summary>
        /// <param name="stoppingToken"></param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync);

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000);

            await bot.CloseAsync();
        }
    }
}
