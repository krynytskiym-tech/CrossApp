namespace Api.Contracts;

// Додаткове завдання 1: відповідь списку з пагінацією.
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record StockLineDto(string Sku, string Name, int Quantity, string Unit);
