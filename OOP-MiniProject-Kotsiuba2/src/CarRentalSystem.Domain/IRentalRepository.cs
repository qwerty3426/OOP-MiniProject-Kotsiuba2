namespace CarRentalSystem.Domain;

public interface IRentalRepository
{
    void AddVehicle(Vehicle vehicle);
    IEnumerable<Vehicle> GetAllVehicles();
    Vehicle? GetVehicleById(Guid id);
    void AddOrder(RentalOrder order);
    IEnumerable<RentalOrder> GetAllOrders();
}