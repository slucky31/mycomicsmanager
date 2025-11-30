using Application.Books.Helper;
using FluentValidation;

namespace Web.Validators;

public class BookValidator : AbstractValidator<BookUiDto>
{
    public BookValidator()
    {
        const int maxTitleLength = 200;
        const int maxSeriesLength = 200;
        const int maxIsbnLength = 20;
        const int maxImageLinkLength = 500;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(maxTitleLength).WithMessage($"Title must not exceed {maxTitleLength} characters.");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required.")
            .MaximumLength(maxIsbnLength).WithMessage($"ISBN must not exceed {maxIsbnLength} characters.")
            .Must(BeValidISBN).WithMessage("ISBN must be a valid 10 or 13 digit number.");

        RuleFor(x => x.Serie)
            .MaximumLength(maxSeriesLength).WithMessage($"Serie must not exceed {maxSeriesLength} characters.");

        RuleFor(x => x.VolumeNumber)
            .GreaterThan(0).WithMessage("Volume number must be greater than 0.");

        RuleFor(x => x.ImageLink)
            .MaximumLength(maxImageLinkLength).WithMessage($"Image link must not exceed {maxImageLinkLength} characters.");
    }

    private static bool BeValidISBN(string isbn)
    {
        return IsbnValidator.IsValidISBN(isbn);
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        if (model is not BookUiDto dto)
        {
            return new[] { "Invalid model for validation." };
        }

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return new[] { "Property name is required." };
        }

        var ctx = ValidationContext<BookUiDto>.CreateWithOptions(dto, x => x.IncludeProperties(propertyName));
        var result = await ValidateAsync(ctx);
        if (result.IsValid)
        {
            return Array.Empty<string>();
        }
        return result.Errors.Select(e => e.ErrorMessage);
    };
}
