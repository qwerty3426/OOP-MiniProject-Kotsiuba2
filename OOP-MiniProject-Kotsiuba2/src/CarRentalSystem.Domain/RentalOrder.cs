namespace CarRentalSystem.Domain;

public class RentalOrder
{
    public Guid Id { get; private set; }
    public Customer Customer { get; private set; }
    public Vehicle Vehicle { get; private set; }
    public int RentalDays { get; private set; }
    public decimal TotalCost { get; private set; }
    public DateTime OrderDate { get; private set; }

    public RentalOrder(Customer customer, Vehicle vehicle, int rentalDays)
    {
        if (customer == null) throw new ArgumentNullException(nameof(customer));
        if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
        if (!vehicle.IsAvailable) throw new InvalidOperationException("Vehicle is already rented.");
        if (rentalDays <= 0) throw new ArgumentException("Rental days must be at least 1 day.");

        Id = Guid.NewGuid();
        Customer = customer;
        Vehicle = vehicle;
        RentalDays = rentalDays;
        TotalCost = vehicle.CalculateRentalCost(rentalDays);
        OrderDate = DateTime.UtcNow;

        Vehicle.ChangeAvailability(false);
    }
}