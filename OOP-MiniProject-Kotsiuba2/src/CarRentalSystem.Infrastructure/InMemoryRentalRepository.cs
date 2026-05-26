using CarRentalSystem.Domain;

namespace CarRentalSystem.Infrastructure;

public class InMemoryRentalRepository : IRentalRepository
{
    private readonly List<Vehicle> _vehicles = new();
    private readonly List<RentalOrder> _orders = new();

    public void AddVehicle(Vehicle vehicle) => _vehicles.Add(vehicle);
    public IEnumerable<Vehicle> GetAllVehicles() => _vehicles;
    public Vehicle? GetVehicleById(Guid id) => _vehicles.FirstOrDefault(v => v.Id == id);
    public void AddOrder(RentalOrder order) => _orders.Add(order);
    public IEnumerable<RentalOrder> GetAllOrders() => _orders;
}