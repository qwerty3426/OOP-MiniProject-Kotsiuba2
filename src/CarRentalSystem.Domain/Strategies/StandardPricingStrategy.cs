namespace CarRentalSystem.Domain.Strategies
{
    public class StandardPricingStrategy : IRentalPricingStrategy
    {
        public string StrategyName => "Стандартний тариф (Знижка 10% від 5 днів)";
        public decimal CalculatePrice(decimal basePrice, int days)
        {
            decimal total = basePrice * days;
            if (days >= 5) total *= 0.90m;
            return total;
        }
    }
}