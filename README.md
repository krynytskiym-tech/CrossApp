# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Склад. Сутності: Product, StockBatch, Warehouse, Movement. Призначення: облік залишків товарів по партіях.

## Структура solution

```
CrossApp/
├── CrossApp.slnx
├── README.md
└── src/
    ├── Core/            # class library, без точки входу
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/             # консольний застосунок
        ├── Cli.csproj   # ProjectReference на Core
        └── Program.cs   # лише форматування виводу
```

Напрямок залежності: `Cli → Core` (односторонній, без циклів).

## Запуск

```
dotnet build
dotnet run --project src/Cli
dotnet run --project src/Cli -- --json
```

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
