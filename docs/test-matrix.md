# Матриця трасування тестів (Test Matrix)

| Ідентифікатор Use Case | Опис бізнес-сценарію | Метод тесту (у файлі UnitTest1.cs) | Тип тесту |
| :--- | :--- | :--- | :--- |
| **UC-1: Реєстрація ТЗ** | Додавання нового автомобіля з валідацією параметрів | `Car_Constructor_ShouldSetPropertiesCorrectly` | Юніт-тест |
| **UC-1.1: Контроль цін**| Заборона реєстрації авто з нульовою чи від'ємною ціною | `Car_PricePerDay_ShouldBeAssignable` | Юніт-тест (Theory) |
| **UC-2: Облікові дані**| Створення клієнта та перевірка формату email | `Customer_Constructor_ShouldSetFullNameAndEmail` | Юніт-тест |
| **UC-3: Оформлення угоди**| Розрахунок періоду оренди та створення замовлення | `RentalOrder_ShouldCalculateCorrectDuration` | Юніт-тест |
| **UC-4: Сховище (Save)** | Фізичний запис та відновлення сутностей з JSON сховища | `SaveAndReload_PreservesAggregateState` | Інтеграційний |
| **UC-5: Обробка збоїв** | Стійкість системи при пошкодженні структури JSON-файлу | `FaultHandling_CorruptedJsonFile_ShouldHandleException` | Fault Handling |
| **UC-6: Порожні стани** | Робота системи, якщо файл бази даних відсутній на диску | `FaultHandling_MissingFile_ShouldBeHandledSilently` | Fault Handling |