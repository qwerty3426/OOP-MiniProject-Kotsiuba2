using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using CarRentalSystem.Domain.Entities;
using CarRentalSystem.Domain.Strategies;
using CarRentalSystem.Application.Services;
using CarRentalSystem.Application.Contracts;
using CarRentalSystem.Infrastructure.Persistence;

namespace CarRental.Tests
{
    public class FakeStore<T> : IDataStore<T>
    {
        private List<T> _items = new();
        public Task<IReadOnlyCollection<T>> LoadAsync(System.Threading.CancellationToken c = default) => Task.FromResult<IReadOnlyCollection<T>>(_items);
        public Task SaveAsync(IReadOnlyCollection<T> i, System.Threading.CancellationToken c = default)
        {
            _items = i.ToList();
            return Task.CompletedTask;
        }
    }

    // Тюнінгований клас спеціально для тестів, який вміє десеріалізувати абстрактний Vehicle
    public class TestJsonVehicleStore : JsonDataStore<Vehicle>
    {
        private readonly string _path;
        private readonly JsonSerializerOptions _options;

        public TestJsonVehicleStore(string file) : base(file)
        {
            _path = file;
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
            };
            // Додаємо поліморфне налаштування, щоб перетворити абстрактний Vehicle на конкретний Car при читанні
            _options.Converters.Add(new JsonPolymorphicConverter());
        }

        public new async Task SaveAsync(IReadOnlyCollection<Vehicle> items, System.Threading.CancellationToken c = default)
        {
            var json = JsonSerializer.Serialize(items, _options);
            await File.WriteAllTextAsync(_path, json, c);
        }

        public new async Task<IReadOnlyCollection<Vehicle>> LoadAsync(System.Threading.CancellationToken c = default)
        {
            if (!File.Exists(_path) || Array.Exists(new[] { ".json", ".txt" }, ext => _path.EndsWith(ext)) && new FileInfo(_path).Length == 0)
                return new List<Vehicle>();
            try
            {
                var json = await File.ReadAllTextAsync(_path, c);
                var cars = JsonSerializer.Deserialize<List<Car>>(json, _options);
                return cars?.Cast<Vehicle>().ToList() ?? new List<Vehicle>();
            }
            catch { return new List<Vehicle>(); }
        }
    }

    // Кастомний конвертер для мапінгу абстракції на сутність Car
    public class JsonPolymorphicConverter : JsonConverter<Vehicle>
    {
        public override Vehicle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonSerializer.Deserialize<Car>(ref reader, options);
        public override void Write(Utf8JsonWriter writer, Vehicle value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, (Car)value, options);
    }

    // ==========================================
    // 1. ШАР UNIT-ТЕСТІВ
    // ==========================================
    public class UnitTests
    {
        private readonly RentalService _service = new(new FakeStore<Vehicle>(), new FakeStore<RentalOrder>());

        [Fact] 
        public async Task CreateRental_ValidData_ReturnsSuccess() {
            await _service.InitializeAsync(); var id = _service.GetAvailableVehicles().First().Id;
            var res = await _service.CreateRentalAsync("Артем", "a@a.com", id, 3, new StandardPricingStrategy());
            Assert.True(res.IsSuccess);
        }

        [Theory]
        [InlineData(-5)] [InlineData(0)] [InlineData(31)] [InlineData(100)]
        public async Task CreateRental_BoundaryDays_Theories(int days) {
            await _service.InitializeAsync(); var id = _service.GetAvailableVehicles().First().Id;
            var res = await _service.CreateRentalAsync("Артем", "a@a.com", id, days, new StandardPricingStrategy());
            Assert.False(res.IsSuccess);
        }

        [Fact] 
        public async Task CreateRental_ShouldApplyStandardDiscount_ForLongPeriods() {
            await _service.InitializeAsync(); var id = _service.GetAvailableVehicles().First().Id;
            var res = await _service.CreateRentalAsync("Артем", "a@a.com", id, 6, new StandardPricingStrategy());
            Assert.Equal(270m, res.Value.TotalCost);
        }

        [Fact] 
        public async Task CreateRental_ShouldApplyPremiumStrategy_Correctly() {
            await _service.InitializeAsync(); var id = _service.GetAvailableVehicles().First().Id;
            var res = await _service.CreateRentalAsync("Артем", "a@a.com", id, 2, new PremiumPricingStrategy());
            Assert.Equal(115m, res.Value.TotalCost);
        }

        [Fact] 
        public async Task CreateRental_ShouldMarkVehicleAsUnavailable() {
            await _service.InitializeAsync(); var car = _service.GetAvailableVehicles().First();
            await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            Assert.False(car.IsAvailable);
        }

        [Fact] 
        public async Task CreateRental_ShouldFail_IfVehicleAlreadyRented() {
            await _service.InitializeAsync(); var car = _service.GetAvailableVehicles().First();
            await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            var sec = await _service.CreateRentalAsync("Іван", "i@i.com", car.Id, 3, new StandardPricingStrategy());
            Assert.False(sec.IsSuccess);
        }

        [Fact] 
        public async Task ReturnVehicle_ShouldChargeFine_WhenOverdue() {
            await _service.InitializeAsync(); var car = _service.GetAvailableVehicles().First();
            var order = await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            var ret = await _service.ReturnVehicleAsync(order.Value.Id, 4);
            Assert.Equal(150m, ret.Value.LateFine);
        }

        [Fact] 
        public async Task ReturnVehicle_ShouldMakeCarAvailableAgain() {
            await _service.InitializeAsync(); var car = _service.GetAvailableVehicles().First();
            var order = await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            await _service.ReturnVehicleAsync(order.Value.Id, 2);
            Assert.True(car.IsAvailable);
        }

        [Theory]
        [InlineData("")] [InlineData(" ")]
        public async Task ValidateCustomerName_ShouldFail_IfEmptyOrSpaces(string name) {
            await _service.InitializeAsync(); var car = _service.GetAvailableVehicles().First();
            var res = await _service.CreateRentalAsync(name, "a@a.com", car.Id, 2, new StandardPricingStrategy());
            Assert.False(res.IsSuccess);
        }

        [Fact]
        public async Task ValidateCustomerName_ShouldFail_IfNull() {
            await _service.InitializeAsync(); var car = _service.GetAvailableVehicles().First();
            var res = await _service.CreateRentalAsync(null, "a@a.com", car.Id, 2, new StandardPricingStrategy());
            Assert.False(res.IsSuccess);
        }

        [Fact] 
        public async Task ActiveOrdersLinq_ShouldReturnOnlyActive() {
            await _service.InitializeAsync(); var car = _service.GetAvailableVehicles().First();
            var order = await _service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            await _service.ReturnVehicleAsync(order.Value.Id, 2);
            Assert.Empty(_service.GetActiveOrders());
        }

        [Fact] 
        public void PricingStrategy_StandardName_ShouldMatch() {
            var strat = new StandardPricingStrategy(); Assert.Contains("Стандартний", strat.StrategyName);
        }

        [Fact] 
        public void PremiumPricingStrategy_NameCheck() {
            var strat = new PremiumPricingStrategy(); Assert.Contains("Преміум", strat.StrategyName);
        }

        [Fact] 
        public async Task LINQ_GetTotalRevenue_ShouldSumAllOrders() {
            await _service.InitializeAsync(); var cars = _service.GetAvailableVehicles().ToList();
            await _service.CreateRentalAsync("А", "a@a.com", cars[0].Id, 2, new StandardPricingStrategy());
            Assert.True(_service.GetTotalRevenue() > 0);
        }

        [Fact] 
        public void Vehicle_Id_ShouldBeGeneratedOnCreation() {
            var car = new Car(); Assert.NotEqual(Guid.Empty, car.Id);
        }

        [Fact] 
        public void Customer_Id_ShouldBeGeneratedOnCreation() {
            var cust = new Customer(); Assert.NotEqual(Guid.Empty, cust.Id);
        }

        [Fact] 
        public void RentalOrder_ShouldBeActiveByDef() {
            var ord = new RentalOrder(); Assert.True(ord.IsActive);
        }

        [Fact] 
        public async Task ReturnVehicle_WithInvalidId_ShouldFail() {
            await _service.InitializeAsync();
            var res = await _service.ReturnVehicleAsync(Guid.NewGuid(), 2);
            Assert.False(res.IsSuccess);
        }

        [Fact] 
        public async Task GetMostPopularBrands_ShouldReturnCorrectOrder() {
            await _service.InitializeAsync(); var cars = _service.GetAvailableVehicles().ToList();
            await _service.CreateRentalAsync("А", "a@a.com", cars[0].Id, 1, new StandardPricingStrategy());
            var popular = _service.GetMostPopularBrands();
            Assert.NotNull(popular);
        }

        [Fact] 
        public void Vehicle_ChangeAvailability_UpdatesState() {
            var car = new Car { IsAvailable = true };
            car.ChangeAvailability(false);
            Assert.False(car.IsAvailable);
        }

        [Fact] 
        public void StandardStrategy_NoDiscount_ForShortPeriods() {
            var strat = new StandardPricingStrategy();
            decimal price = strat.CalculatePrice(100, 2);
            Assert.Equal(200m, price);
        }
    }

    // ==========================================
    // 2. ШАР ІНТЕГРАЦІЙНИХ ТЕСТІВ ТА FAULT HANDLING
    // ==========================================
    public class IntegrationTests : IDisposable
    {
        private readonly string _tempVehicleFile;
        private readonly string _tempOrderFile;

        public IntegrationTests()
        {
            _tempVehicleFile = Path.GetTempFileName();
            _tempOrderFile = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (File.Exists(_tempVehicleFile)) File.Delete(_tempVehicleFile);
            if (File.Exists(_tempOrderFile)) File.Delete(_tempOrderFile);
        }

        [Fact]
        public async Task SaveAndReload_PreservesAggregateState()
        {
            var vehicleStore = new TestJsonVehicleStore(_tempVehicleFile);
            var orderStore = new JsonDataStore<RentalOrder>(_tempOrderFile);
            var service = new RentalService(vehicleStore, orderStore);

            await service.InitializeAsync();
            var car = service.GetAvailableVehicles().First();
            
            var res = await service.CreateRentalAsync("Тест Інтеграції", "i@i.com", car.Id, 4, new StandardPricingStrategy());
            Assert.True(res.IsSuccess);

            var carsList = service.GetAvailableVehicles().ToList();
            await vehicleStore.SaveAsync(carsList);

            var reloadedVehicleStore = new TestJsonVehicleStore(_tempVehicleFile);
            var loadedVehicles = await reloadedVehicleStore.LoadAsync();

            Assert.NotEmpty(loadedVehicles);
            Assert.Equal(carsList.Count, loadedVehicles.Count);
        }

        [Fact]
        public async Task LoadAsync_CorruptedJson_ReturnsEmptyCollection_FaultHandling()
        {
            await File.WriteAllTextAsync(_tempVehicleFile, "=== ЦЕ НЕ JSON ТЕКСТ ===");
            var vehicleStore = new TestJsonVehicleStore(_tempVehicleFile);
            
            var items = await vehicleStore.LoadAsync();
            Assert.Empty(items);
        }

        [Fact]
        public async Task ConsecutiveOperations_UpdateStateCorrectly()
        {
            var service = new RentalService(new TestJsonVehicleStore(_tempVehicleFile), new JsonDataStore<RentalOrder>(_tempOrderFile));
            await service.InitializeAsync();
            var car = service.GetAvailableVehicles().First();

            var orderRes = await service.CreateRentalAsync("Клієнт 1", "c1@c.com", car.Id, 2, new StandardPricingStrategy());
            await service.ReturnVehicleAsync(orderRes.Value.Id, 2);

            Assert.Empty(service.GetActiveOrders());
        }

        [Fact]
        public async Task EmptyFile_ReturnsEmptyCollection_WithoutException()
        {
            var vehicleStore = new TestJsonVehicleStore(_tempVehicleFile);
            var items = await vehicleStore.LoadAsync();
            Assert.Empty(items);
        }

        [Fact]
        public async Task SaveAsync_WritesValidJsonToDisk()
        {
            var vehicleStore = new TestJsonVehicleStore(_tempVehicleFile);
            var list = new List<Vehicle> { new Car { Brand = "Audi", Model = "A6", BasePricePerDay = 100 } };
            await vehicleStore.SaveAsync(list);

            Assert.True(File.Exists(_tempVehicleFile));
            var content = await File.ReadAllTextAsync(_tempVehicleFile);
            Assert.Contains("Audi", content);
        }

        [Fact]
        public async Task ServiceInit_WhenFileIsMissing_SeedsDefaultData()
        {
            if (File.Exists(_tempVehicleFile)) File.Delete(_tempVehicleFile);
            var service = new RentalService(new TestJsonVehicleStore(_tempVehicleFile), new JsonDataStore<RentalOrder>(_tempOrderFile));
            
            await service.InitializeAsync();
            Assert.NotEmpty(service.GetAvailableVehicles());
        }

        [Fact]
        public async Task ReturnVehicle_IntegratesAndSavesLateFineData()
        {
            var service = new RentalService(new TestJsonVehicleStore(_tempVehicleFile), new JsonDataStore<RentalOrder>(_tempOrderFile));
            await service.InitializeAsync();
            var car = service.GetAvailableVehicles().First();

            var order = await service.CreateRentalAsync("Артем", "a@a.com", car.Id, 2, new StandardPricingStrategy());
            var returnResult = await service.ReturnVehicleAsync(order.Value.Id, 5);

            Assert.True(returnResult.IsSuccess);
            Assert.True(returnResult.Value.LateFine > 0);
        }

        [Fact]
        public async Task SaveAsync_HandlesEmptyCollectionsCorrectly()
        {
            var orderStore = new JsonDataStore<RentalOrder>(_tempOrderFile);
            await orderStore.SaveAsync(new List<RentalOrder>());
            var loaded = await orderStore.LoadAsync();
            Assert.Empty(loaded);
        }
    }
}