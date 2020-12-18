using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using BotScreener.App;
using BotScreener.Domain;
using Newtonsoft.Json;
using System.Globalization;

namespace BotScreener
{
    class Program
    {
        private static void SetupStaticLogger()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Information()
               .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
               .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
               .WriteTo.File("bot.log")
               .CreateLogger();
        }

        static async Task Main(string[] args)
        {
            SetupStaticLogger();

            Settings settings = await GetSettings();
            var telegramBot = new TelegramBot(settings.TelegramBotToken, settings.TelegramChatId);
            await telegramBot.Start();
            var notificationSender = new NotificationSender(telegramBot);

            var screener = new Screener(settings.TcsToken, 
                                        settings.TickersToScan.Distinct().ToArray(), 
                                        settings.NotificationRules,
                                        notificationSender.SendNotification);

            while (true)
            {
                try
                {
                    //check working days and stock working hours
                    var dt = DateTime.UtcNow;
                    var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(dt);
                    if (day == DayOfWeek.Sunday || day == DayOfWeek.Saturday || dt.Hour < 7 || dt.Hour > 22)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        continue;
                    }

                    await screener.Scan();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            telegramBot.Stop();
        }

        private async static Task<Settings> GetSettings()
        {
            while (true)
            {
                try
                {
                    return JsonConvert.DeserializeObject<Settings>(File.ReadAllText("settings.json"));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

        }
    }
}
