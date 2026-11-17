using Api.Contracts;
using FluentValidation;

namespace Api.Validation;

// Додаткове завдання 2: правила винесено з DTO у FluentValidation. Ключі помилок: Sku, Name, Unit, Quantity.
public sealed class SaveProductRequestValidator : AbstractValidator<SaveProductRequest>
{
    public SaveProductRequestValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Артикул обов'язковий.");

        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Length <= 200)
            .WithMessage("Назва обов'язкова, до 200 символів.");

        RuleFor(x => x.Unit)
            .Must(unit => !string.IsNullOrWhiteSpace(unit) && unit.Length <= 20)
            .WithMessage("Одиниця виміру обов'язкова, до 20 символів.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Кількість не може бути від'ємною.");
    }
}
