using Xunit;
using CarRentalSystem.Domain;
using System;

namespace CarRentalSystem.Tests;

public class DomainTests
{
    [Fact]
    public void CreateCar_WithValidData_ShouldCreateSuccessfully()
    {
        var car = new Car("Audi", "A6", 70.0m, "Дизель");
        Assert.NotNull(car);
        Assert.Equal("Audi", car.Brand);
        Assert.True(car.IsAvailable);
    }

    [Fact]
    public void CreateCar_WithNegativePrice_ShouldThrowException()
    {
        Assert.Throws<ArgumentException>(() => new Car("Audi", "A6", -10.0m, "Дизель"));
    }

    [Fact]
    public void CalculateRentalCost_Under5Days_ShouldReturnBasePrice()
    {
        var car = new Car("Ford", "Focus", 30.0m, "Бензин");
        var cost = car.CalculateRentalCost(3);
        Assert.Equal(90.0m, cost);
    }

    [Fact]
    public void CalculateRentalCost_Over5Days_ShouldApplyPolymorphicDiscount()
    {
        var car = new Car("Ford", "Focus", 30.0m, "Бензин");
        var cost = car.CalculateRentalCost(10);
        Assert.Equal(270.0m, cost); 
    }

    [Fact]
    public void CreateRentalOrder_ShouldMakeVehicleUnavailable()
    {
        var customer = new Customer("John Doe", "john@test.com");
        var car = new Car("BMW", "M5", 100.0m, "Бензин");
        var order = new RentalOrder(customer, car, 3);
        
        Assert.False(car.IsAvailable);
        Assert.Equal(300.0m, order.TotalCost);
    }
}