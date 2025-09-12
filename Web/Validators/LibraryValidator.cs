using FluentValidation;

namespace Web.Validators;
public class LibraryValidator : AbstractValidator<LibraryUiDto>
{
    public LibraryValidator()
    {
        const int maxNameLength = 100;
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(maxNameLength).WithMessage("Name must not exceed 100 characters.");
        RuleFor(x => x.RelativePath).NotEmpty().WithMessage("RelativePath is required.").MaximumLength(maxNameLength).WithMessage("RelativePath must not exceed 200 characters.");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<LibraryUiDto>.CreateWithOptions((LibraryUiDto)model, x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
        {
            return Array.Empty<string>();
        }
        return result.Errors.Select(e => e.ErrorMessage);
    };

}
