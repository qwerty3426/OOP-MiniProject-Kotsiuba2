# 1. Переходимо в правильну гілку
git checkout lab-34

# 2. Переписуємо README.md на чистий та красивий варіант (без зламаного синтаксису)
@'
# 🚗 Car Rental System — Підсумковий міні-проєкт

Автоматизована консольна система оренди та бронювання автомобілів на платформі **.NET 8**. 
Розробляється ітеративно з дотриманням принципів **Clean Architecture** та **SOLID**. Головна робоча гілка: **`lab-34`**.

---

## 🏗️ 1. Структура проєкту (Шари архітектури)
* **`src/CarRentalSystem.Domain`** — Сутності (`Vehicle`, `Car`, `Customer`, `RentalOrder`) та контракти (`IRentalRepository`). Без зовнішніх залежностей.
* **`src/CarRentalSystem.Application`** — Бізнес-сценарії (`RentalService`). Залежить суто від Домену.
* **`src/CarRentalSystem.Infrastructure`** — Інфраструктура (`InMemoryRentalRepository`). Залежить від Application та Domain.
* **`src/CarRentalSystem.Console`** — Інтерфейс користувача (UI) та демонстрація вертикального зрізу.
* **`tests/CarRentalSystem.Tests`** — Юніт-тести для захисту інваріантів домену (XUnit).

---

## 🛠️ 2. Інструкція із запуску та тестування

### 🏃 Запуск консольного застосунку
```bash
dotnet run --project src/CarRentalSystem.Console/CarRentalSystem.Console.csproj
