namespace CarRentalSystem.Domain;

public class Car : Vehicle
{
    public string FuelType { get; private set; }

    public Car(string brand, string model, decimal basePricePerDay, string fuelType) 
        : base(brand, model, basePricePerDay)
    {
        if (string.IsNullOrWhiteSpace(fuelType)) throw new ArgumentException("Fuel type cannot be empty.");
        FuelType = fuelType;
    }

    public override decimal CalculateRentalCost(int days)
    {
        if (days <= 0) throw new ArgumentException("Days must be greater than zero.");
        decimal total = BasePricePerDay * days;
        if (days > 5)
        {
            total *= 0.9m; // 10% знижки, якщо оренда більше 5 днів
        }
        return total;
    }
}