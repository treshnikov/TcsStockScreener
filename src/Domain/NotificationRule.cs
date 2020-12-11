using System;

namespace BotScreener.Domain
{
    public class NotificationRule
    {
        public decimal PriceChangeInPercent { get; set; }
        public float TimePeriodInHours { get; set; }
        public PriceDirection PriceDirection { get; set; }

        public bool IsActual(decimal priceBefore, decimal priceNow)
        {
            if (PriceDirection == PriceDirection.Increased)
            {
                return priceNow > priceBefore &&
                    100 * (priceNow - priceBefore) / priceBefore >= PriceChangeInPercent;
            }
            else if (PriceDirection == PriceDirection.Decrased)
            {
                return priceNow < priceBefore &&
                    100 * (priceBefore - priceNow) / priceBefore >= PriceChangeInPercent;
            }
            else
            {
                throw new ArgumentException($"No handler for {PriceDirection}");
            }
        }
        public override string ToString()
        {
            return $"{(PriceDirection == PriceDirection.Decrased ? "📉" : "📈") } {PriceDirection} in 🕓 {TimePeriodInHours.ToString()}h";
        }
    }
}
