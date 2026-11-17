namespace Api.Contracts;

// Вхідний контракт для POST і PUT. Правила валідації: SaveProductRequestValidator (FluentValidation).
public sealed record SaveProductRequest(string Sku, string Name, string Unit, int Quantity);
