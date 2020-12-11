# Overview
This is a simple bot/screener that helps you find a good entry point to the stock market. The application scans a set of specified  tickers and sends notifications using Telegram messenger when the ticker price has changed by a specified percentage up or down over a specified period of time. For example, you can get a notification when Apple stock drops 2% within 6 hours.

The application uses [Tinkoff API](https://tinkoffcreditsystems.github.io/invest-openapi/) and requires a sandbox token to get stock prices.
![](https://github.com/treshnikov/TcsStockScreener/blob/master/img/screen.PNG)
# How to build
- Run `Scripts/publish.bat` or `Scripts/publish.sh` depending on your operating system - Windows or Linux.
- Copy `settings.json` to `publish` folder. 

# How to use
Edit `settings.json` and run the app:
- Specify `TcsToken`. You can generate a token at the settings page of your [account](www.tinkoff.ru/invest/settings). Sandbox token is required.
- Specify `TelegramBotToken`. Start dialog with [@botfather](https://t.me/botfather) to create the bot.
- Specify `TelegramChatId` - the id of chat or group where Telegram bot will send notifications.
- Specify `NotificationRules`. See examples below.
    - PriceChangeInPercent - stock price changing in percent.
    - TimePeriodInHours - hours during when the stock price has changed.
    - PriceDirection - `1` for price down and `0` for price rise. 
- Specify `TickersToScan` tickers to scan.

# Settings.json example
```json
{
  "TcsToken": "...",
  "TelegramBotToken": "...",
  "TelegramChatId": "...",
  "NotificationRules": [
    {
      "PriceChangeInPercent": 9.0,
      "TimePeriodInHours": 48.0,
      "PriceDirection": 1
    },
    {
      "PriceChangeInPercent": 3.5,
      "TimePeriodInHours": 2.0,
      "PriceDirection": 1
    }
  ],
  "TickersToScan": [
    "AAPL",
    "MSFT",
    "BABA"
  ]
}
```
# Remarks 
- Code is written to be quite stable over possible exceptions and network issues. So exceptions are processed quite simple but effective.
- Tinkoff API has some [limits for requests per second](https://tinkoffcreditsystems.github.io/invest-openapi/rest/). That is why you can periodically face `Task.Delay()` invocations in the code.
- Notifications of this app aren't trading recommendations.
