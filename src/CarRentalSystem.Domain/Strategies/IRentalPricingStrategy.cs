namespace CarRentalSystem.Domain.Strategies
{
    public interface IRentalPricingStrategy
    {
        string StrategyName { get; }
        decimal CalculatePrice(decimal basePrice, int days);
    }
}