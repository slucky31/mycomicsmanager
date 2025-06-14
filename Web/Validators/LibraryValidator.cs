using FluentValidation;

namespace Web.Validators;
public class LibraryValidator : AbstractValidator<LibraryUiDto>
{
    public LibraryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        RuleFor(x => x.RelativePath).NotEmpty().WithMessage("RelativePath is required.").MaximumLength(200).WithMessage("RelativePath must not exceed 00 characters.");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<LibraryUiDto>.CreateWithOptions((LibraryUiDto)model, x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
            return Array.Empty<string>();
        return result.Errors.Select(e => e.ErrorMessage);
    };

}
