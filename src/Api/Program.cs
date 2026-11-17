using Api;
using Api.Contracts;
using Api.Endpoints;
using Api.Validation;
using Core.Abstractions;
using Core.Configuration;
using Core.Storage;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();                    // формат помилок RFC 9457
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton<IValidator<SaveProductRequest>, SaveProductRequestValidator>();

// Сховище: Singleton, потокобезпечне (lock усередині). Режим береться з конфігурації.
if (string.Equals(builder.Configuration["Storage:Mode"], "File", StringComparison.OrdinalIgnoreCase))
{
    var storage = builder.Configuration.GetSection("Storage").Get<StorageOptions>() ?? new StorageOptions();
    var path = storage.ResolveFullPath(AppContext.BaseDirectory);
    builder.Services.AddSingleton<ICatalogStore>(_ => new FileCatalogStore(path));
}
else
{
    builder.Services.AddSingleton<ICatalogStore, InMemoryCatalogStore>();
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar/v1
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapProducts();

app.Run();

// Потрібно для WebApplicationFactory<Program> в інтеграційних тестах.
public partial class Program { }
