using OpenAI;
using OpenAI.Images;
using OpenAI.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OpenAI_chat
{
    internal class Program
    {

        static void Main(string[] args)
        {
            const string token = "5855008572:AAGjfG2GTFHV_EkcDP4RJIIKTxrR-sy0HWQ";
            var Bot = new TelegramBotClient(token);
            using var cts = new CancellationTokenSource();
            ReceiverOptions receiverOptions = new() { AllowedUpdates = { } };
            Bot.StartReceiving(HandleUpdateAsync,
                               HandleErrorAsync,
                               receiverOptions,
                               cts.Token);

            Console.WriteLine("The bot is running and waiting for a message..."); //  t.me/xlopec85_bot
            Console.ReadLine();
            cts.Cancel();
        }

        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            var api = new OpenAIClient(new OpenAIAuthentication("MY_API_KEY")); 

            Console.WriteLine($"Receive message type: {message.Type} from {message.From.FirstName} @{message.From.Username} at {message.Date}");

            if (message.Type != MessageType.Text)
                return;

            List<string> listSpam = new List<string>() { "http:", "https:", "wwww." };
            listSpam.Add("fuck you");

            bool spamWord = listSpam.Any(s => message.Text.ToLower().Contains(s));
            if (spamWord)
            {
                await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }

            bool helloTMS = message.Text.Contains("Ребята, всем привет!");
            if (helloTMS)
            {
                await botClient.SendAnimationAsync(
                chatId: message.Chat.Id,
                animation: "https://github.com/Pavel-Levanok/study/blob/main/images/cat_explosion2.gif?raw=true");
            }

            if (message.Text.ToLower() == "хаха")
            {
                await botClient.SendAnimationAsync(
                chatId: message.Chat.Id,
                animation: "https://raw.githubusercontent.com/Pavel-Levanok/study/main/images/jonah-hill-omg.gif");
            }

            if (message.Text.Contains("@xlopec85_bot"))
            {
                var result = await api.CompletionsEndpoint.CreateCompletionAsync(message.Text, max_tokens: 200, temperature: 0.1, model: Model.Davinci);
                await botClient.SendTextMessageAsync(message.Chat.Id, result.ToString());
            }

            if (message.Text.ToLower().Contains("image"))
            {
                var imageURLS = await api.ImagesEndPoint.GenerateImageAsync(message.Text, 1, ImageSize.Medium);
                await botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: imageURLS[0].ToString(),
                caption: "",
                parseMode: ParseMode.Html);
            }
        }
        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}

