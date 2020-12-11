using BotScreener.Domain;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BotScreener.App
{
    /// <summary>
    /// Sends notification using telegram bot. 
    /// Limits the frequency of messages to send according to notification rules from the config. 
    /// For instance, send notification when the price of ticker fell down by 5% in 24h.
    /// </summary>
    public class NotificationSender
    {
        private readonly TelegramBot _telegramBot;
        private Dictionary<string, DateTime> _notificationTimestampToTicker;
        private readonly string _notificationTimestampToTickerCacheFileName = "_notificationTimestampToTicker";

        public NotificationSender(TelegramBot telegramBot)
        {
            _telegramBot = telegramBot;
        }

        public async Task SendNotification(NotificationRule notificationRule, string ticker, string text)
        {
            if (_notificationTimestampToTicker == null)
            {
                await LoadLastSendTimes();
            }

            try
            {
                if (!_notificationTimestampToTicker.ContainsKey(ticker) ||
                    (_notificationTimestampToTicker.ContainsKey(ticker) && DateTime.UtcNow - _notificationTimestampToTicker[ticker] >= TimeSpan.FromHours(notificationRule.TimePeriodInHours)))
                {
                    await _telegramBot.SendNotification(text);
                    _notificationTimestampToTicker[ticker] = DateTime.UtcNow;
                    await SaveLastSendTimes();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private async Task LoadLastSendTimes()
        {
            _notificationTimestampToTicker = new Dictionary<string, DateTime>();
            try
            {
                if (!File.Exists(_notificationTimestampToTickerCacheFileName))
                {
                    return;
                }

                _notificationTimestampToTicker = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(
                    File.ReadAllText(_notificationTimestampToTickerCacheFileName));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private async Task SaveLastSendTimes()
        {
            try
            {
                var data = JsonConvert.SerializeObject(_notificationTimestampToTicker, Formatting.Indented);
                File.WriteAllText(_notificationTimestampToTickerCacheFileName, data);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(1));

            }
        }
    }
}
