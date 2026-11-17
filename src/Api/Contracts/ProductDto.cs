using Core.Domain;

namespace Api.Contracts;

// Вихідний контракт: поля JSON не залежать від внутрішньої будови Product.
public sealed record ProductDto(string Id, string Sku, string Name, string Unit, int Quantity);

public static class ProductMapping
{
    public static ProductDto ToContract(this Product product) =>
        new(product.Id, product.Sku, product.Name, product.Unit, product.Quantity);
}
