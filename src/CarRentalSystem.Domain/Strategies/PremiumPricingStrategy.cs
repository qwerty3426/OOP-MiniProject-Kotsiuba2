namespace CarRentalSystem.Domain.Strategies
{
    public class PremiumPricingStrategy : IRentalPricingStrategy
    {
        public string StrategyName => "Преміум тариф (+15% за підвищений сервіс і страховку)";
        public decimal CalculatePrice(decimal basePrice, int days)
        {
            return (basePrice * days) * 1.15m;
        }
    }
}