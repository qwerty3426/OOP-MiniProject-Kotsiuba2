using System;
using CarRentalSystem.Application;
using CarRentalSystem.Domain;
using CarRentalSystem.Infrastructure;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        IRentalRepository repository = new InMemoryRentalRepository();
        RentalService rentalService = new RentalService(repository);

        var car1 = new Car("Toyota", "Camry", 50.0m, "Бензин");
        var car2 = new Car("Tesla", "Model 3", 80.0m, "Електро");
        repository.AddVehicle(car1);
        repository.AddVehicle(car2);

        Console.WriteLine("=== Лабораторна 34: Система оренди авто ===");
        Console.WriteLine("Доступні автомобілі для оренди:");
        
        foreach (var car in rentalService.GetAvailableCars())
        {
            Console.WriteLine($"- ID: {car.Id} | {car.Brand} {car.Model} | Ціна за добу: {car.BasePricePerDay}$ | Доступність: {car.IsAvailable}");
        }

        Console.WriteLine("\n--- Запуск вертикального зрізу: Оформлення броні ---");
        
        try
        {
            string clientName = "Артем Коцюба";
            string clientEmail = "artem@email.com";
            int days = 6; 
            Guid selectedId = car1.Id;

            Console.WriteLine($"Клієнт {clientName} бронює {car1.Brand} на {days} днів...");
            
            RentalOrder order = rentalService.CreateRental(clientName, clientEmail, selectedId, days);

            Console.WriteLine("\n[УСПІХ] Замовлення успішно створено!");
            Console.WriteLine($"Загальна вартість (зі знижкою): {order.TotalCost}$ (Замість {car1.BasePricePerDay * days}$)");
            Console.WriteLine($"Статус автомобіля {car1.Brand} після броні: Доступний = {car1.IsAvailable}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ПОМИЛКА] Щось пішло не так: {ex.Message}");
        }
    }
}