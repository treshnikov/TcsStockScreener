using BotScreener.Domain;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace BotScreener.App
{
    /// <summary>
    /// Scans given a set of tickers, collect their prices, and pass the notification to the notification sender according to notification rules.
    /// </summary>
    public class Screener
    {
        private readonly string _tcsToken;
        private readonly string[] _tickersToScan;
        private readonly NotificationRule[] _notificationRules;
        private readonly Func<NotificationRule, MarketInstrument, CandlePayload, CandlePayload, Task> _sendNotification;
        private static readonly TimeSpan _sleepTime = TimeSpan.FromMilliseconds(800);

        public Screener(string tcsToken, string[] tickersToScan, NotificationRule[] notificationRules,
            Func<NotificationRule, MarketInstrument, CandlePayload, CandlePayload, Task> sendNotification)
        {
            _tcsToken = tcsToken;
            _tickersToScan = tickersToScan;
            _notificationRules = notificationRules;
            _sendNotification = sendNotification;
        }
        public async Task Scan()
        {
            // connect
            var connection = ConnectionFactory.GetSandboxConnection(_tcsToken);
            var context = connection.Context;

            // get tickers
            var tickersToScan = _tickersToScan;

            // scan stock and etf markets
            var stocks = await context.MarketStocksAsync();
            var etfs = await context.MarketEtfsAsync();
            var instruments = new List<MarketInstrument>(stocks.Instruments);
            instruments.AddRange(etfs.Instruments);
            instruments = instruments.Where(i => tickersToScan.Contains(i.Ticker)).ToList();

            // calculate delta
            var idx = 0;
            foreach (var instrument in instruments)
            {
                try
                {
                    foreach (var notificationRule in _notificationRules)
                    {
                        await HandleInstrument(context, instrument, notificationRule);
                        await Task.Delay(_sleepTime);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    await Task.Delay(_sleepTime);
                }

                idx++;
                Console.Title = $"[{idx} / {instruments.Count}] {instrument.Ticker}";
            }
        }

        private async Task HandleInstrument(Context context, MarketInstrument instrument, NotificationRule notificationRule)
        {
            // try get candles
            var timePeriodInHours = notificationRule.TimePeriodInHours;
            var interval = CandleInterval.FiveMinutes;
            if (timePeriodInHours >= 12)
            {
                interval = CandleInterval.Hour;
            }

            var start = DateTime.UtcNow.AddHours(-2.5 * timePeriodInHours);
            var end = DateTime.UtcNow;
            var candles = await context.MarketCandlesAsync(instrument.Figi, start, end, interval);
            if (candles.Candles.Count == 0)
            {
                Log.Debug($"No candles for {instrument.Ticker}\t{start:dd.MM.yy HH:mm} - {end:dd.MM.yy HH:mm}");
                await Task.Delay(_sleepTime);

                return;
            }

            // handle a candle with maximum timestamp
            var lastCandle = candles.Candles.OrderByDescending(i => i.Time).First();

            // try to find a previuos candle
            var previousCandleTimestamp = lastCandle.Time.AddHours(-1 * timePeriodInHours);
            var previousCandles = candles.Candles.Where(i => i.Time <= previousCandleTimestamp).ToList();
            var attempNumber = 1;
            //in case it is the start of the day then try to get the last price from the first previous working day
            while (previousCandles.Count == 0)
            {
                start = previousCandleTimestamp.AddHours(-12 * attempNumber);
                end = previousCandleTimestamp.AddHours(-12 * (attempNumber - 1));
                Log.Debug($"Can not find a previous candle for {instrument.Ticker} for {end:dd.MM.yy HH:mm}");
                Log.Debug($"Try to fetch candles for {instrument.Ticker} for {previousCandleTimestamp.AddHours(-12 * attempNumber):dd.MM.yy HH:mm} - {previousCandleTimestamp:dd.MM.yy HH:mm}");

                previousCandles = (await context.MarketCandlesAsync(instrument.Figi,
                    start,
                    end,
                    interval)).Candles;
                attempNumber++;
                continue;
            }
            var firstCandle = previousCandles.OrderByDescending(i => i.Time).First();

            if (notificationRule.IsActual(firstCandle.Close, lastCandle.Close))
            {
                await _sendNotification(notificationRule, instrument, firstCandle, lastCandle);
            }

            LogTicker(instrument, notificationRule, lastCandle, firstCandle);
        }

        private static void LogTicker(MarketInstrument instrument, NotificationRule notificationRule, CandlePayload lastCandle, CandlePayload firstCandle)
        {
            var priceChange = lastCandle.Close - firstCandle.Close;
            var priceChangeInPercent = 100 * priceChange / firstCandle.Close;

            var msg =
                $"${instrument.Ticker} " +
                $"{notificationRule.TimePeriodInHours}h / {priceChangeInPercent.ToString("F2")} % / {(priceChange >= 0 ? "+" : "")}{priceChange.ToString("F2")} {instrument.Currency} / " +
                $"{firstCandle.Close} {instrument.Currency} -> {lastCandle.Close} {instrument.Currency} / " +
                $"{firstCandle.Time:dd.MM.yyyy HH:mm} -> {lastCandle.Time:dd.MM.yyyy HH:mm}";

            Log.Write(notificationRule.IsActual(firstCandle.Close, lastCandle.Close)
                ? Serilog.Events.LogEventLevel.Warning
                : Serilog.Events.LogEventLevel.Information,
                msg);
        }
    }
}