namespace CarRentalSystem.Domain;

public abstract class Vehicle
{
    public Guid Id { get; protected set; }
    public string Brand { get; protected set; }
    public string Model { get; protected set; }
    public decimal BasePricePerDay { get; protected set; }
    public bool IsAvailable { get; protected set; }

    protected Vehicle(string brand, string model, decimal basePricePerDay)
    {
        if (string.IsNullOrWhiteSpace(brand)) throw new ArgumentException("Brand cannot be empty.");
        if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("Model cannot be empty.");
        if (basePricePerDay <= 0) throw new ArgumentException("Price must be greater than zero.");

        Id = Guid.NewGuid();
        Brand = brand;
        Model = model;
        BasePricePerDay = basePricePerDay;
        IsAvailable = true;
    }

    public abstract decimal CalculateRentalCost(int days);

    public void ChangeAvailability(bool available)
    {
        IsAvailable = available;
    }
}