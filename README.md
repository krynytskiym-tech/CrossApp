# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Склад. Сутності: Product, StockBatch, Warehouse, Movement. Призначення: облік залишків товарів по партіях.

## Структура solution

```
CrossApp/
├── CrossApp.slnx
├── README.md
├── data/
│   ├── sample.csv       # товари, CSV, 3 навмисно пошкоджені рядки
│   ├── sample.json      # ті самі товари, JSON, 3 пошкоджені записи
│   └── mixed.txt        # різнорідні рядки: P (товар) і W (склад)
└── src/
    ├── Core/            # class library, без точки входу
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs
    │   ├── Dto/         # ProductDto, WarehouseDto, ImportResult<T>, MixedImportResult, ImportStats
    │   └── Import/      # ProductCsvImporter, ProductJsonImporter, MixedCsvImporter
    └── Cli/             # консольний застосунок
        ├── Cli.csproj   # ProjectReference на Core
        └── Program.cs   # аргументи, вибір імпортера за розширенням, вивід
```

Напрямок залежності: `Cli → Core` (односторонній, без циклів).

## Запуск

```
dotnet build
dotnet run --project src/Cli
dotnet run --project src/Cli                        # data/sample.csv
dotnet run --project src/Cli -- data/sample.json    # JSON-імпортер
dotnet run --project src/Cli -- data/mixed.txt      # різнорідні рядки P/W
dotnet run --project src/Cli -- nonexistent.csv     # код виходу 1
```

Код виходу: `0` — успіх (навіть якщо частину рядків пропущено), `1` — файл не знайдено,
`2` — непідтримуване розширення.

## Середовище

.NET SDK 10.0, Ubuntu 24.04 x64

## Публікація: self-contained vs framework-dependent

```
dotnet publish src/Cli -c Release -r linux-x64 --self-contained true -o publish/sc
dotnet publish src/Cli -c Release -r linux-x64 --self-contained false -o publish/fd
```

| RID       | Режим               | Розмір publish | Потрібен runtime |
| --------- | ------------------- | -------------- | ---------------- |
| linux-x64 | self-contained      | 80 МБ          | ні               |
| linux-x64 | framework-dependent | 144 КБ         | так (.NET 10)    |

**Framework-dependent** — публікація містить лише код застосунку та NuGet-залежності;
на машині користувача має бути встановлений сумісний .NET Runtime. Каталог малий.

**Self-contained** — до публікації додається копія .NET Runtime для конкретного RID;
застосунок працює без попередньо встановленого .NET, але каталог значно більший
і прив'язаний до конкретної платформи.

Цікавий нюанс: `RID (від .NET)`, який повертає `RuntimeInformation.RuntimeIdentifier`,
відрізняється між публікаціями одного й того ж коду — self-contained повертає
узагальнений `linux-x64` (RID, під який публікували), а framework-dependent — більш
детальний `ubuntu.24.04-x64` (визначається під час виконання з системи).

## Multi-targeting

Не виконувалось у цій роботі (опційний крок 5). Проєкт Core і Cli збираються під
`net10.0`.

## Додаткове завдання (Self-contained публікація для різних RID)

Порівняння розмірів збірок для двох платформ (.NET 10):

- `linux-x64` publish: 165.2 МБ
- `win-x64` publish: 160.8 МБ

## Формат даних (лабораторна 3)

Кодування всіх файлів — UTF-8. Роздільник — крапка з комою (`;`), задано константою `Separator`.
Порожні рядки та рядки, що починаються з `#`, ігноруються.

| Файл | Імпортер | Формат |
| ---- | -------- | ------ |
| `*.csv` | `ProductCsvImporter` | `id;sku;name;unit;quantity`; заголовок необов'язковий (пропускається, якщо перший рядок починається з `id`) |
| `*.json` | `ProductJsonImporter` | масив об'єктів `{ "id", "sku", "name", "unit", "quantity" }`; імена полів без урахування регістру; кожен запис розбирається окремо |
| `*.txt` | `MixedCsvImporter` | `P;id;sku;name;unit;quantity` — товар, `W;id;name;city` — склад; префікс лише великою літерою |

Імпортер вибирається за розширенням файлу в `Program.cs` (switch expression).
Пошкоджені рядки не зупиняють імпорт: вони потрапляють у `Errors` з номером рядка (CSV, TXT) або записа (JSON).

У `sample.csv` навмисно пошкоджені рядки 12–14, у `sample.json` — записи 11–13, у `mixed.txt` — рядки 10–13.
Це тестові дані.

Обмеження JSON: якщо властивості `quantity` у записі немає зовсім, `System.Text.Json` підставляє `0`
(помилки не буде); для `null` або нечислового значення запис відхиляється.

## Додаткові завдання лабораторної 3

1. **JSON-імпортер** на тих самих типах (`System.Text.Json`), вибір імпортера за розширенням.
2. **Різнорідні рядки за префіксом**: один `switch` повертає або товар, або склад, або помилку.
3. **Статистика імпорту** одним рядком: `Статистика: усього 13, прийнято 10, пропущено 3 (23.1% помилок)`.
