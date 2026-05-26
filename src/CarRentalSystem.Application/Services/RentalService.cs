using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarRentalSystem.Domain.Entities;
using CarRentalSystem.Domain.Common;
using CarRentalSystem.Domain.Strategies;
using CarRentalSystem.Application.Contracts;

namespace CarRentalSystem.Application.Services
{
    public class RentalService
    {
        private readonly List<Vehicle> _vehicles = new();
        private readonly List<RentalOrder> _orders = new();
        private readonly IDataStore<Vehicle> _vehicleStore;
        private readonly IDataStore<RentalOrder> _orderStore;

        public RentalService(IDataStore<Vehicle> vehicleStore, IDataStore<RentalOrder> orderStore)
        {
            _vehicleStore = vehicleStore;
            _orderStore = orderStore;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _vehicles.Clear();
                _orders.Clear();
                _vehicles.AddRange(await _vehicleStore.LoadAsync());
                _orders.AddRange(await _orderStore.LoadAsync());

                if (!_vehicles.Any()) SeedInitialData();
            }
            catch { SeedInitialData(); }
        }

        private void SeedInitialData()
        {
            _vehicles.Add(new Car { Brand = "Toyota", Model = "Camry", BasePricePerDay = 50, FuelType = "Gasoline" });
            _vehicles.Add(new Car { Brand = "Tesla", Model = "Model 3", BasePricePerDay = 80, FuelType = "Electric" });
        }

        public async Task SaveChangesAsync()
        {
            await _vehicleStore.SaveAsync(_vehicles);
            await _orderStore.SaveAsync(_orders);
        }

        // Use Case 1: Створення броні з патерном Strategy
        public async Task<Result<RentalOrder>> CreateRentalAsync(string name, string email, Guid vehicleId, int days, IRentalPricingStrategy strategy)
        {
            if (days <= 0 || days > 30) return Result<RentalOrder>.Failure("Некоректний термін оренди (допустимо 1-30 днів).");
            if (string.IsNullOrWhiteSpace(name)) return Result<RentalOrder>.Failure("Ім'я клієнта порожнє.");

            var vehicle = _vehicles.FirstOrDefault(v => v.Id == vehicleId);
            if (vehicle == null) return Result<RentalOrder>.Failure("Автомобіль не знайдено.");
            if (!vehicle.IsAvailable) return Result<RentalOrder>.Failure("Автомобіль уже заброньовано іншим клієнтом.");

            var customer = new Customer { FullName = name, Email = email };
            decimal finalCost = strategy.CalculatePrice(vehicle.BasePricePerDay, days);

            vehicle.ChangeAvailability(false);

            var order = new RentalOrder
            {
                Customer = customer,
                Vehicle = vehicle,
                RentalDays = days,
                TotalCost = finalCost
            };

            _orders.Add(order);
            await SaveChangesAsync();
            return Result<RentalOrder>.Success(order);
        }

        // Use Case 2: Повернення автомобіля та розрахунок штрафів
        public async Task<Result<RentalOrder>> ReturnVehicleAsync(Guid orderId, int actualDaysUsed)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId && o.IsActive);
            if (order == null) return Result<RentalOrder>.Failure("Активне замовлення не знайдене.");

            order.IsActive = false;
            order.Vehicle.ChangeAvailability(true);

            if (actualDaysUsed > order.RentalDays)
            {
                int overduedDays = actualDaysUsed - order.RentalDays;
                order.LateFine = overduedDays * (order.Vehicle.BasePricePerDay * 1.5m);
                order.TotalCost += order.LateFine;
            }

            await SaveChangesAsync();
            return Result<RentalOrder>.Success(order);
        }

        // Use Case 3: Скасування бронювання
        public async Task<Result<bool>> CancelOrderAsync(Guid orderId)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return Result<bool>.Failure("Замовлення не знайдено.");
            if (!order.IsActive) return Result<bool>.Failure("Замовлення вже закрите або скасоване.");

            order.IsActive = false;
            order.Vehicle.ChangeAvailability(true);
            _orders.Remove(order);

            await SaveChangesAsync();
            return Result<bool>.Success(true);
        }

        // --- LINQ Запити ---
        public IEnumerable<Vehicle> GetAvailableVehicles() => _vehicles.Where(v => v.IsAvailable).ToList();
        public IEnumerable<RentalOrder> GetActiveOrders() => _orders.Where(o => o.IsActive).ToList();
        public decimal GetTotalRevenue() => _orders.Sum(o => o.TotalCost);
        public IEnumerable<object> GetMostPopularBrands() => 
            _orders.GroupBy(o => o.Vehicle.Brand)
                   .Select(g => new { Brand = g.Key, Count = g.Count() })
                   .OrderByDescending(x => x.Count);
    }
}