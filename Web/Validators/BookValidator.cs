using Application.Helpers;
using Domain.Books;
using FluentValidation;

namespace Web.Validators;

public class BookValidator : AbstractValidator<BookUiDto>
{
    public BookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Enter a title")
            .MaximumLength(BookConstants.MaxTitleLength).WithMessage($"Title must not exceed {BookConstants.MaxTitleLength} characters.");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("Enter the ISBN").When(x => x.IsPhysical, ApplyConditionTo.CurrentValidator)
            .MaximumLength(BookConstants.MaxIsbnLength).WithMessage($"ISBN must not exceed {BookConstants.MaxIsbnLength} characters.")
            // A blank ISBN is only exempt from format validation on a digital book (see BookUiDto.IsPhysical).
            .Must(BeValidISBN).WithMessage("Enter a valid ISBN: 10 or 13 digits")
                .When(x => x.IsPhysical || !string.IsNullOrWhiteSpace(x.ISBN), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Serie)
            .NotEmpty().WithMessage("Enter a series")
            .MaximumLength(BookConstants.MaxSerieLength).WithMessage($"Serie must not exceed {BookConstants.MaxSerieLength} characters.");

        RuleFor(x => x.VolumeNumber)
            .GreaterThan(0).WithMessage("Volume number must be greater than 0.");

        RuleFor(x => x.ImageLink)
            .MaximumLength(BookConstants.MaxImageLinkLength).WithMessage($"Image link must not exceed {BookConstants.MaxImageLinkLength} characters.");

        // 0 when the book was not rated: it is added without a reading.
        RuleFor(x => x.Rating)
            .InclusiveBetween(0, 5).WithMessage("The rating must be between 1 and 5 stars.");

        RuleFor(x => x.Authors)
            .MaximumLength(BookConstants.MaxAuthorsLength).WithMessage($"Authors must not exceed {BookConstants.MaxAuthorsLength} characters.");

        RuleFor(x => x.Publishers)
            .MaximumLength(BookConstants.MaxPublishersLength).WithMessage($"Publishers must not exceed {BookConstants.MaxPublishersLength} characters.");

        RuleFor(x => x.NumberOfPages)
            .GreaterThan(0).WithMessage("Number of pages must be greater than 0.")
            .When(x => x.NumberOfPages.HasValue);

        RuleFor(x => x.PublishDate)
            .Must(p => !p.HasValue || p.Value <= DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Publish date must not be in the future.");
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
