using BotScreener.Domain;

namespace BotScreener.Domain
{
    public class Settings 
    {
        public string TcsToken { get; set; }

        public string TelegramBotToken { get; set; }

        public string TelegramChatId { get; set; }

        public string[] TickersToScan { get; set; }

        public NotificationRule[] NotificationRules { get; set; }
    }
}