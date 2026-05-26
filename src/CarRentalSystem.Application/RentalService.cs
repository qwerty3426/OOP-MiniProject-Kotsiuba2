using CarRentalSystem.Domain;

namespace CarRentalSystem.Application;

public class RentalService
{
    private readonly IRentalRepository _repository;

    public RentalService(IRentalRepository repository)
    {
        _repository = repository;
    }

    public RentalOrder CreateRental(string customerName, string customerEmail, Guid vehicleId, int days)
    {
        var vehicle = _repository.GetVehicleById(vehicleId);
        if (vehicle == null) throw new Exception("Vehicle not found!");

        var customer = new Customer(customerName, customerEmail);
        var order = new RentalOrder(customer, vehicle, days);

        _repository.AddOrder(order);
        return order;
    }

    public IEnumerable<Vehicle> GetAvailableCars()
    {
        return _repository.GetAllVehicles();
    }
}