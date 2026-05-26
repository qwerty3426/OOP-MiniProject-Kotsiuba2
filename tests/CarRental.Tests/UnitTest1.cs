using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using CarRentalSystem.Domain.Entities;
using CarRentalSystem.Domain.Strategies;
using CarRentalSystem.Application.Services;
using CarRentalSystem.Application.Contracts;
using System.Collections.Generic;

namespace CarRental.Tests
{
    public class FakeStore<T> : IDataStore<T>
    {
        public Task<IReadOnlyCollection<T>> LoadAsync(System.Threading.CancellationToken c = default) => Task.FromResult<IReadOnlyCollection<T>>(new List<T>());
        public Task SaveAsync(IReadOnlyCollection<T> i, System.Threading.CancellationToken c = default) => Task.CompletedTask;
    }

    public class UnitTest1
    {
        private readonly RentalService _service = new(new FakeStore<Vehicle>(), new FakeStore<RentalOrder>());

        [Fact] 
        public async Task CreateRental_ShouldFail_WhenDaysAreNegative() 
        {
            await _service.InitializeAsync(); 
            var id = _service.GetAvailableVehicles().First().Id;
            var res = await _service.CreateRentalAsync("Артем", "a@a.com", id, -5, new StandardPricingStrategy());
            Assert.False(res.IsSuccess);
        }

        [Fact] 
        public async Task CreateRental_ShouldApplyStandardDiscount_ForLongPeriods() 
        {
            await _service.InitializeAsync(); 
            var id = _service.GetAvailableVehicles().First().Id;
            var res = await _service.CreateRentalAsync("Артем", "a@a.com", id, 6, new StandardPricingStrategy());
            Assert.Equal(270m, res.Value.TotalCost);
        }

        [Fact] 
        public async Task CreateRental_ShouldApplyPremiumStrategy_Correctly() 
        {
            await _service.InitializeAsync(); 
            var id = _service.GetAvailableVehicles().First().Id;
            var res = await _service.CreateRentalAsync("Артем", "a@a.com", id, 2, new PremiumPricingStrategy());
            Assert.Equal(115m, res.Value.TotalCost);
        }

        [Fact] 
        public async Task CreateRental_ShouldMarkVehicleAsUnavailable() 
        {
            await _service.InitializeAsync(); 
            var car = _service.GetAvailableVehicles().First();
            await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            Assert.False(car.IsAvailable);
        }

        [Fact] 
        public async Task CreateRental_ShouldFail_IfVehicleAlreadyRented() 
        {
            await _service.InitializeAsync(); 
            var car = _service.GetAvailableVehicles().First();
            await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            var sec = await _service.CreateRentalAsync("Іван", "i@i.com", car.Id, 3, new StandardPricingStrategy());
            Assert.False(sec.IsSuccess);
        }

        [Fact] 
        public async Task ReturnVehicle_ShouldChargeFine_WhenOverdue() 
        {
            await _service.InitializeAsync(); 
            var car = _service.GetAvailableVehicles().First();
            var order = await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            var ret = await _service.ReturnVehicleAsync(order.Value.Id, 4);
            Assert.Equal(150m, ret.Value.LateFine);
        }

        [Fact] 
        public async Task ReturnVehicle_ShouldMakeCarAvailableAgain() 
        {
            await _service.InitializeAsync(); 
            var car = _service.GetAvailableVehicles().First();
            var order = await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            await _service.ReturnVehicleAsync(order.Value.Id, 2);
            Assert.True(car.IsAvailable);
        }

        [Fact] 
        public async Task ValidateCustomerName_ShouldFail_IfEmpty() 
        {
            await _service.InitializeAsync(); 
            var car = _service.GetAvailableVehicles().First();
            var res = await _service.CreateRentalAsync("", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            Assert.False(res.IsSuccess);
        }

        [Fact] 
        public async Task ActiveOrdersLinq_ShouldReturnOnlyActive() 
        {
            await _service.InitializeAsync(); 
            var car = _service.GetAvailableVehicles().First();
            var order = await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            await _service.ReturnVehicleAsync(order.Value.Id, 2);
            Assert.Empty(_service.GetActiveOrders());
        }

        [Fact] 
        public void PricingStrategy_StandardName_ShouldMatch() 
        {
            var strat = new StandardPricingStrategy(); 
            Assert.Contains("Стандартний", strat.StrategyName);
        }

        [Fact] 
        public async Task LINQ_GetTotalRevenue_ShouldSumAllOrders() 
        {
            await _service.InitializeAsync(); 
            var cars = _service.GetAvailableVehicles().ToList();
            await _service.CreateRentalAsync("А", "a@a.com", cars[0].Id, 2, new StandardPricingStrategy());
            Assert.True(_service.GetTotalRevenue() > 0);
        }

        [Fact] 
        public void PremiumPricingStrategy_NameCheck() 
        {
            var strat = new PremiumPricingStrategy(); 
            Assert.Contains("Преміум", strat.StrategyName);
        }
    }
}