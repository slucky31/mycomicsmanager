using FluentValidation;

namespace Web.Validators;
public class LibraryValidator : AbstractValidator<LibraryUiDto>
{
    public LibraryValidator()
    {
        const int maxNameLength = 100;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(maxNameLength).WithMessage($"Name must not exceed {maxNameLength} characters.");
        RuleFor(x => x.RelativePath)
            .MaximumLength(maxNameLength).WithMessage($"RelativePath must not exceed {maxNameLength} characters.");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        if (model is not LibraryUiDto dto)
        {
            return new[] { "Invalid model for validation." };
        }

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return new[] { "Property name is required." };
        }

        var ctx = ValidationContext<LibraryUiDto>.CreateWithOptions(dto, x => x.IncludeProperties(propertyName));
        var result = await ValidateAsync(ctx);
        if (result.IsValid)
        {
            return Array.Empty<string>();
        }
        return result.Errors.Select(e => e.ErrorMessage);
    };

}
