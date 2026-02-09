using Application.Helpers;
using Domain.Books;
using FluentValidation;

namespace Web.Validators;

public class BookValidator : AbstractValidator<BookUiDto>
{
    public BookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(BookConstants.MaxTitleLength).WithMessage($"Title must not exceed {BookConstants.MaxTitleLength} characters.");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required.")
            .MaximumLength(BookConstants.MaxIsbnLength).WithMessage($"ISBN must not exceed {BookConstants.MaxIsbnLength} characters.")
            .Must(BeValidISBN).WithMessage("ISBN must be a valid 10 or 13 digit number.");

        RuleFor(x => x.Serie)
            .NotEmpty().WithMessage("Serie is required.")
            .MaximumLength(BookConstants.MaxSerieLength).WithMessage($"Serie must not exceed {BookConstants.MaxSerieLength} characters.");

        RuleFor(x => x.VolumeNumber)
            .GreaterThan(0).WithMessage("Volume number must be greater than 0.");

        RuleFor(x => x.ImageLink)
            .MaximumLength(BookConstants.MaxImageLinkLength).WithMessage($"Image link must not exceed {BookConstants.MaxImageLinkLength} characters.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(0, 5).WithMessage("Rating must be between 0 and 5.");

        RuleFor(x => x.Authors)
            .MaximumLength(BookConstants.MaxAuthorsLength).WithMessage($"Authors must not exceed {BookConstants.MaxAuthorsLength} characters.");

        RuleFor(x => x.Publishers)
            .MaximumLength(BookConstants.MaxPublishersLength).WithMessage($"Publishers must not exceed {BookConstants.MaxPublishersLength} characters.");

        RuleFor(x => x.NumberOfPages)
            .GreaterThan(0).WithMessage("Number of pages must be greater than 0.")
            .When(x => x.NumberOfPages.HasValue);

        RuleFor(x => x.PublishDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Publish date must not be in the future.")
            .When(x => x.PublishDate.HasValue);
    }

    private static bool BeValidISBN(string isbn)
    {
        return IsbnHelper.IsValidISBN(isbn);
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        if (model is not BookUiDto dto)
        {
            return ["Invalid model for validation."];
        }

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return ["Property name is required."];
        }

        var ctx = ValidationContext<BookUiDto>.CreateWithOptions(dto, x => x.IncludeProperties(propertyName));
        var result = await ValidateAsync(ctx);
        if (result.IsValid)
        {
            return [];
        }
        return result.Errors.Select(e => e.ErrorMessage);
    };
}
