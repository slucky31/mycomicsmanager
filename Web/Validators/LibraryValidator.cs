using Domain.Libraries;
using FluentValidation;

namespace Web.Validators;

public class LibraryValidator : AbstractValidator<LibraryUiDto>
{
    public LibraryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(LibraryConstants.MaxNameLength)
            .WithMessage($"Name must not exceed {LibraryConstants.MaxNameLength} characters.");

        RuleFor(x => x.Color)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Color is required.")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Invalid color format.");

        RuleFor(x => x.Icon)
            .NotEmpty().WithMessage("Icon is required.")
            .MaximumLength(LibraryConstants.MaxIconLength)
            .WithMessage($"Icon name must not exceed {LibraryConstants.MaxIconLength} characters.");

        RuleFor(x => x.BookType)
            .IsInEnum().WithMessage("Please select a valid library type.");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        if (model is not LibraryUiDto dto)
        {
            return ["Invalid model for validation."];
        }

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return ["Property name is required."];
        }

        var ctx = ValidationContext<LibraryUiDto>.CreateWithOptions(dto, x => x.IncludeProperties(propertyName));
        var result = await ValidateAsync(ctx);
        if (result.IsValid)
        {
            return [];
        }
        return result.Errors.Select(e => e.ErrorMessage);
    };
}
