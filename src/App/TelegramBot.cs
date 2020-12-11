using Serilog;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace BotScreener.App
{
    public class TelegramBot
    {
        private TelegramBotClient _botClient;
        private readonly string _telegramBotToken;
        private readonly string _telegramChatId;

        public TelegramBot(string telegramBotToken, string telegramChatId)
        {
            _telegramBotToken = telegramBotToken;
            _telegramChatId = telegramChatId;
        }

        public async Task Start()
        {
            while (true)
            {
                try
                {
                    _botClient = new TelegramBotClient(_telegramBotToken);
                    var botInfo = await _botClient.GetMeAsync();
                    _botClient.OnMessage += async (s, e) => { await OnMessageReceived(s, e); };
                   _botClient.StartReceiving();
                    Log.Information($"Telegram bot has started. Id: {botInfo.Id}, Name: {botInfo.FirstName}.");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        private async Task OnMessageReceived(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e == null || e.Message == null)
            {
                return;
            }
            
            try
            {
                await _botClient.SendTextMessageAsync(e.Message.Chat.Id, 
                    $"UtcTime: {DateTime.UtcNow:dd.MM.yyyy hh:mm:ss}");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        public void Stop()
        {
            _botClient.StopReceiving();
        }

        public async Task SendNotification(string text)
        {
            try
            {
                await _botClient.SendTextMessageAsync(_telegramChatId, text, disableWebPagePreview: true);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}
