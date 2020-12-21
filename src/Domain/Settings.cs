using BotScreener.Domain;

namespace BotScreener.Domain
{
    public record Settings(
        string TcsToken, 
        string TelegramBotToken,
        string TelegramChatId,
        string[] TickersToScan ,
        NotificationRule[] NotificationRules);

}