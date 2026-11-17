using System.Security.Cryptography;
using Api.Contracts;
using Api.Validation;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProducts(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        // GET /api/products?page=1&pageSize=20&search=...  -> { items, total, page, pageSize }
        group.MapGet("/", Results<Ok<PagedResult<ProductDto>>, ValidationProblem>
            (int? page, int? pageSize, string? search, ICatalogStore store) =>
        {
            int currentPage = page ?? 1;
            int size = pageSize ?? 20;

            var errors = new Dictionary<string, string[]>();
            if (currentPage < 1) errors["page"] = ["page має бути не менше 1."];
            if (size is < 1 or > 100) errors["pageSize"] = ["pageSize має бути від 1 до 100."];
            if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

            IEnumerable<Product> query = store.List();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase)
                                      || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

            var ordered = query.OrderBy(p => p.Sku, StringComparer.Ordinal)
                               .ThenBy(p => p.Id, StringComparer.Ordinal)
                               .ToList();
            var items = ordered.Skip((currentPage - 1) * size).Take(size)
                               .Select(p => p.ToContract()).ToList();

            return TypedResults.Ok(new PagedResult<ProductDto>(items, ordered.Count, currentPage, size));
        });

        // GET /api/products/low-stock?threshold=5  (варіант для складу)
        group.MapGet("/low-stock", Results<Ok<List<StockLineDto>>, ValidationProblem>
            (int? threshold, ICatalogStore store) =>
        {
            int limit = threshold ?? 5;
            if (limit < 0)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["threshold"] = ["threshold не може бути від'ємним."]
                });

            var lines = ReportService.LowStock(store.List(), limit)
                .Select(l => new StockLineDto(l.Sku, l.Name, l.Quantity, l.Unit))
                .ToList();
            return TypedResults.Ok(lines);
        });

        group.MapGet("/{id}", Results<Ok<ProductDto>, NotFound>
            (string id, ICatalogStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(found.ToContract())
                : TypedResults.NotFound());

        group.MapPost("/", Results<Created<ProductDto>, ValidationProblem>
            (SaveProductRequest request, ICatalogStore store) =>
        {
            EnsureSkuIsUnique(store, request.Sku, exceptId: null);

            var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            var created = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);
            store.Add(created);

            return TypedResults.Created($"/api/products/{created.Id}", created.ToContract());
        })
        .AddEndpointFilter<ValidationFilter<SaveProductRequest>>()
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", Results<Ok<ProductDto>, NotFound, ValidationProblem>
            (string id, SaveProductRequest request, ICatalogStore store) =>
        {
            if (store.GetById(id) is null) return TypedResults.NotFound();
            EnsureSkuIsUnique(store, request.Sku, exceptId: id);

            // PUT повністю замінює стан: створюємо валідний Product з тим самим id.
            var replacement = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);
            store.Update(replacement);
            return TypedResults.Ok(replacement.ToContract());
        })
        .AddEndpointFilter<ValidationFilter<SaveProductRequest>>()
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id}", Results<NoContent, NotFound>
            (string id, ICatalogStore store) =>
            store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound());
    }

    // Бізнес-правило: артикул унікальний. DomainException -> 409 (ApiExceptionHandler).
    private static void EnsureSkuIsUnique(ICatalogStore store, string sku, string? exceptId)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        bool exists = store.List().Any(p => p.Sku == normalized
                                         && !string.Equals(p.Id, exceptId, StringComparison.OrdinalIgnoreCase));
        if (exists)
            throw new DomainException($"Товар з артикулом {normalized} уже існує.");
    }
}
