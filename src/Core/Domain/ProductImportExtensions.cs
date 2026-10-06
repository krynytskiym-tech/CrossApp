using Core.Dto;

namespace Core.Domain;

public static class ProductImportExtensions
{
    // Додаткове завдання 1: ImportResult<ProductDto> → сутності + перелік тих,
    // що не пройшли інваріанти (до помилок розбору файлу додаються доменні).
    public static ImportResult<Product> ToDomain(this ImportResult<ProductDto> import)
    {
        var products = new List<Product>();
        var errors = new List<string>(import.Errors);

        foreach (ProductDto dto in import.Items)
        {
            try
            {
                products.Add(Product.FromDto(dto));
            }
            catch (ArgumentException ex)
            {
                errors.Add($"запис '{dto.Id}' / SKU '{dto.Sku}': {ex.Message.ReplaceLineEndings(" ")}");
            }
        }

        return new ImportResult<Product>(products, errors);
    }
}
