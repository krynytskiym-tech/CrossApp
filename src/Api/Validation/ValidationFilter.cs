using FluentValidation;

namespace Api.Validation;

// Endpoint filter: перевіряє аргумент типу T до виклику обробника; помилки -> 400 ValidationProblem.
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["Тіло запиту відсутнє або має некоректний формат."]
            });
        }

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(context);
    }
}
