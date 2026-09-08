# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Склад. Сутності: Product, StockBatch, Warehouse, Movement.
Призначення: облік залишків товарів по партіях.

## Запуск

```bash
dotnet build
dotnet run --project src/Cli
```

## Середовище

.NET SDK 10.0, Ubuntu 24.04 x64

## Додаткове завдання (Self-contained публікація)

Порівняння розмірів збірок для двох платформ (.NET 10):

- `linux-x64` publish: 165.2 МБ
- `win-x64` publish: 160.8 МБ

```

```
