using System.Net;
using System.Net.Http.Json;
using System.Text;
using Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Microsoft.AspNetCore.Hosting;

namespace Api.Tests;

// Додаткове завдання 4: інтеграційні тести через WebApplicationFactory (сховище в пам'яті, без мережі й диска).
public class ProductApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueSku() => $"T-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    private static object Body(string sku, string name = "Цемент М400", string unit = "мішок", int quantity = 40) =>
        new { sku, name, unit, quantity };

    private async Task<(HttpResponseMessage Response, ProductDto Dto)> CreateAsync(
        string? sku = null, string name = "Цемент М400", int quantity = 40)
    {
        var response = await _client.PostAsJsonAsync("/api/products", Body(sku ?? UniqueSku(), name, "мішок", quantity));
        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        return (response, dto!);
    }

    [Fact]
    public async Task Post_Valid_Returns201_WithLocation_AndBody()
    {
        var sku = UniqueSku();

        var (response, dto) = await CreateAsync(sku);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/products/{dto.Id}", response.Headers.Location!.ToString());
        Assert.Equal(sku, dto.Sku);
        Assert.Equal(40, dto.Quantity);
    }

    [Fact]
    public async Task Get_ByLocation_Returns200()
    {
        var (created, dto) = await CreateAsync();

        var response = await _client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var read = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(dto, read);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/products/ffffffff");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Invalid_Returns400_WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new { sku = "", name = "X", unit = "", quantity = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Sku", problem!.Errors.Keys);
        Assert.Contains("Unit", problem.Errors.Keys);
        Assert.Contains("Quantity", problem.Errors.Keys);
        Assert.DoesNotContain("Name", problem.Errors.Keys);
    }

    [Fact]
    public async Task Post_EmptyBody_Returns400_NotServerError()
    {
        var response = await _client.PostAsync("/api/products",
            new StringContent("", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WrongQuantityType_Returns400()
    {
        var content = new StringContent(
            "{\"sku\":\"A-1\",\"name\":\"Назва\",\"unit\":\"шт\",\"quantity\":\"abc\"}",
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/products", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Error_Responses_DoNotLeakInternals()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new { sku = "", name = "", unit = "", quantity = -5 });

        var text = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Exception", text);
        Assert.DoesNotContain("StackTrace", text);
        Assert.DoesNotContain("/home/", text);
    }

    [Fact]
    public async Task Put_ReplacesProduct_Returns200()
    {
        var (_, dto) = await CreateAsync();
        var newSku = UniqueSku();

        var response = await _client.PutAsJsonAsync($"/api/products/{dto.Id}",
            Body(newSku, "Цемент М500", "мішок", 55));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{dto.Id}");
        Assert.Equal("Цемент М500", updated!.Name);
        Assert.Equal(55, updated.Quantity);
        Assert.Equal(dto.Id, updated.Id);
    }

    [Fact]
    public async Task Put_EmptyName_Returns400()
    {
        var (_, dto) = await CreateAsync();

        var response = await _client.PutAsJsonAsync($"/api/products/{dto.Id}",
            new { sku = UniqueSku(), name = "", unit = "шт", quantity = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/products/ffffffff", Body(UniqueSku()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGet_Returns404()
    {
        var (_, dto) = await CreateAsync();

        var deleted = await _client.DeleteAsync($"/api/products/{dto.Id}");
        var after = await _client.GetAsync($"/api/products/{dto.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/products/ffffffff");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact] // DomainException -> 409 Conflict (додаткове завдання 3)
    public async Task Post_DuplicateSku_Returns409()
    {
        var sku = UniqueSku();
        await CreateAsync(sku);

        var response = await _client.PostAsJsonAsync("/api/products", Body(sku));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("уже існує", text);
        Assert.DoesNotContain("Exception", text);
    }

    [Fact] // пагінація і пошук (додаткове завдання 1)
    public async Task List_WithSearchAndPaging_ReturnsTotalAndPage()
    {
        var token = $"ZZ{Guid.NewGuid():N}"[..10];
        for (var i = 0; i < 3; i++)
            await CreateAsync(name: $"Товар {token} {i}");

        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?search={token}&page=1&pageSize=2");

        Assert.NotNull(page);
        Assert.Equal(3, page!.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(2, page.PageSize);
    }

    [Fact]
    public async Task List_InvalidPage_Returns400()
    {
        var response = await _client.GetAsync("/api/products?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LowStock_ReturnsOnlyItemsBelowThreshold()
    {
        var sku = UniqueSku();
        await CreateAsync(sku, quantity: 1);

        var lines = await _client.GetFromJsonAsync<List<StockLineDto>>("/api/products/low-stock?threshold=2");

        Assert.Contains(lines!, l => l.Sku == sku);
        Assert.All(lines!, l => Assert.True(l.Quantity < 2));
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_IsAvailable_InDevelopment_AndHidden_InProduction()
    {
        var dev = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, dev.StatusCode);

        using var production = factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        using var prodClient = production.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await prodClient.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await prodClient.GetAsync("/scalar/v1")).StatusCode);
    }
}
