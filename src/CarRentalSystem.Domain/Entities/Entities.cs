using System;
using System.Text.Json.Serialization;

namespace CarRentalSystem.Domain.Entities
{
    public abstract class Vehicle
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal BasePricePerDay { get; set; }
        public bool IsAvailable { get; set; } = true;

        public void ChangeAvailability(bool available) => IsAvailable = available;
    }

    public class Car : Vehicle
    {
        public string FuelType { get; set; } = string.Empty;
    }

    public class Customer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class RentalOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Customer Customer { get; set; } = null!;
        public Vehicle Vehicle { get; set; } = null!;
        public int RentalDays { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true; // Для Use Case: Повернення
        public decimal LateFine { get; set; } = 0; // Для Use Case: Штрафи
    }
}