using BotScreener.Domain;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tinkoff.Trading.OpenApi.Models;

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

        public async Task SendNotification(NotificationRule notificationRule, MarketInstrument instrument, CandlePayload firstCandle, CandlePayload lastCandle)
        {
            if (_notificationTimestampToTicker == null)
            {
                await LoadLastSendTimes();
            }

            try
            {
                if (!_notificationTimestampToTicker.ContainsKey(instrument.Ticker) ||
                    (_notificationTimestampToTicker.ContainsKey(instrument.Ticker) &&
                    DateTime.UtcNow - _notificationTimestampToTicker[instrument.Ticker] >= TimeSpan.FromHours(notificationRule.TimePeriodInHours)))
                {
                    var msg = MakeNotificationMessage(notificationRule, instrument, firstCandle, lastCandle);

                    await _telegramBot.SendNotification(msg);
                    _notificationTimestampToTicker[instrument.Ticker] = DateTime.UtcNow;
                    await SaveLastSendTimes();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static string MakeNotificationMessage(NotificationRule notificationRule, MarketInstrument instrument, CandlePayload firstCandle, CandlePayload lastCandle)
        {
            var priceChange = lastCandle.Close - firstCandle.Close;
            var priceChangeInPercent = 100 * priceChange / firstCandle.Close;

            var msg =
                $"{(notificationRule.PriceDirection == PriceDirection.Decrased ? "📉" : "📈") } " +
                $"${instrument.Ticker} {lastCandle.Close} {instrument.Currency}\r\n" +
                $"{notificationRule.TimePeriodInHours}h / {priceChangeInPercent.ToString("F2")} % / {(priceChange >= 0 ? "+" : "")}{priceChange.ToString("F2")} {instrument.Currency}\r\n" +
                $"{"https://www.tinkoff.ru/invest/stocks/"}{instrument.Ticker}";
            return msg;
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
