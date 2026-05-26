using System;
using System.Linq;
using System.Threading.Tasks;
using CarRentalSystem.Domain.Entities;
using CarRentalSystem.Domain.Strategies;
using CarRentalSystem.Application.Services;
using CarRentalSystem.Infrastructure.Persistence;

namespace CarRentalSystem.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var vehicleStore = new JsonDataStore<Vehicle>("vehicles.json");
            var orderStore = new JsonDataStore<RentalOrder>("orders.json");
            var service = new RentalService(vehicleStore, orderStore);

            await service.InitializeAsync();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== СИСТЕМА ОРЕНДИ АВТО (Лабораторна 35) ===");
                Console.WriteLine("1. Переглянути доступні автомобілі");
                Console.WriteLine("2. Оформити нове бронювання");
                Console.WriteLine("3. Зафіксувати повернення автомобіля (та штрафи)");
                Console.WriteLine("4. Переглянути активні замовлення");
                Console.WriteLine("5. Аналітика: Загальний дохід та популярність брендів");
                Console.WriteLine("0. Вихід");
                Console.Write("\nОберіть дію: ");

                var input = Console.ReadLine();
                if (input == "0") break;

                switch (input)
                {
                    case "1":
                        Console.WriteLine("\nДоступні автомобілі:");
                        foreach (var v in service.GetAvailableVehicles())
                            Console.WriteLine($"- ID: {v.Id} | {v.Brand} {v.Model} | Ціна/доба: {v.BasePricePerDay}$");
                        break;

                    case "2":
                        var vehicles = service.GetAvailableVehicles().ToList();
                        if (!vehicles.Any()) { Console.WriteLine("Немає вільних авто."); break; }

                        Console.Write("Введіть ваше ім'я: "); string name = Console.ReadLine() ?? "";
                        Console.Write("Введіть email: "); string email = Console.ReadLine() ?? "";
                        
                        for (int i = 0; i < vehicles.Count; i++)
                            Console.WriteLine($"{i + 1}. {vehicles[i].Brand} {vehicles[i].Model}");
                        
                        Console.Write("Оберіть номер машини: ");
                        if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 1 || idx > vehicles.Count) break;

                        Console.Write("Кількість днів оренди: ");
                        int.TryParse(Console.ReadLine(), out int days);

                        Console.WriteLine("Оберіть тариф: 1 - Стандарт, 2 - Преміум (+15%)");
                        IRentalPricingStrategy strategy = Console.ReadLine() == "2" 
                            ? new PremiumPricingStrategy() 
                            : new StandardPricingStrategy();

                        var res = await service.CreateRentalAsync(name, email, vehicles[idx - 1].Id, days, strategy);
                        if (res.IsSuccess)
                            Console.WriteLine($"[УСПІХ] Бронь створено! Загальна сума: {res.Value.TotalCost}$ за тарифом '{strategy.StrategyName}'");
                        else
                            Console.WriteLine($"[ПОМИЛКА] {res.Error}");
                        break;

                    case "3":
                        var activeOrders = service.GetActiveOrders().ToList();
                        if (!activeOrders.Any()) { Console.WriteLine("Немає активних оренд."); break; }

                        for (int i = 0; i < activeOrders.Count; i++)
                            Console.WriteLine($"{i + 1}. Клієнт: {activeOrders[i].Customer.FullName} | Авто: {activeOrders[i].Vehicle.Brand}");

                        Console.Write("Оберіть номер замовлення для повернення: ");
                        if (!int.TryParse(Console.ReadLine(), out int oIdx) || oIdx < 1 || oIdx > activeOrders.Count) break;

                        Console.Write("Скільки днів авто було в оренді за фактом? ");
                        int.TryParse(Console.ReadLine(), out int actualDays);

                        var retRes = await service.ReturnVehicleAsync(activeOrders[oIdx - 1].Id, actualDays);
                        if (retRes.IsSuccess)
                            Console.WriteLine($"[УСПІХ] Авто повернуто! Фінальна вартість: {retRes.Value.TotalCost}$ (Штраф за прострочення: {retRes.Value.LateFine}$)");
                        break;

                    case "4":
                        Console.WriteLine("\nАктивні замовлення:");
                        foreach (var o in service.GetActiveOrders())
                            Console.WriteLine($"- Замовлення {o.Id} | Клієнт: {o.Customer.FullName} | Вартість: {o.TotalCost}$");
                        break;

                    case "5":
                        Console.WriteLine($"\nЗагальний фінансовий дохід системи: {service.GetTotalRevenue()}$");
                        Console.WriteLine("Рейтинг популярності брендів (Групування LINQ):");
                        foreach (dynamic b in service.GetMostPopularBrands())
                            Console.WriteLine($"- Бренд: {b.Brand} -> Кількість оренд: {b.Count}");
                        break;
                }
                Console.WriteLine("\nНатисніть Enter для продовження...");
                Console.ReadLine();
            }
        }
    }
}