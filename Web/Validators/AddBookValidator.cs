using FluentValidation;

namespace Web.Validators;

// A new book also needs the library it is added to.
public sealed class AddBookValidator : BookValidator
{
    public AddBookValidator()
    {
        RuleFor(x => x.LibraryId)
            .NotEmpty().WithMessage("Choose a library");
    }
}
