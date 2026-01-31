# Ajouter la note

1. Domain Layer (Domain/Books/Book.cs)

  - Added Rating property (int, 0-5 where 0 = not rated)
  - Added new Create overload with Rating parameter
  - Updated Update method to include Rating

  2. Application Layer

  - CreateBookCommand.cs: Added Rating parameter with default value 0
  - CreateBookCommandHandler.cs: Passes Rating to Book.Create()
  - UpdateBookCommand.cs: Added Rating parameter
  - UpdateBookCommandHandler.cs: Passes Rating to book.Update()

  3. Persistence Layer

  - Migration AddRatingToBooks: Adds Rating column (integer, default 0) to Books table

  4. Web Layer

  - BookDialog.razor: Added MudRating component (1-5 stars)
  - BooksList.razor: Displays rating stars when book has a rating
  - BookUiDto.cs: Added Rating property
  - BookValidator.cs: Added validation rule (0-5 range)
  - IBooksService.cs & BooksService.cs: Added Rating parameter to Create/Update methods
  - BooksList.razor.cs: Passes Rating to service calls
  - Web.csproj: Added EF Core Design package reference (for migrations)

  5. Unit Tests Updated

  - BookTests.cs: Tests for Rating in Create and Update
  - CreateBookCommandHandlerTests.cs: Updated test data
  - UpdateBookCommandHandlerTests.cs: Updated all test commands with Rating
  - BooksServiceTests.cs: Updated Create/Update tests for 6/7 parameters
  - BookRepositoryTests.cs: Updated Update test with Rating assertion

  Build & Tests

  - Build: 0 errors, 0 warnings (for Rating-related code)
  - Tests: All 382 unit tests pass (44 Domain + 175 Application + 163 Web)

  To apply the migration to your database, run:

  dotnet ef database update --project Persistence --startup-project Web


# Parser avec Bedetheque


# Aller chercher la photo et ajouter l'image dans cdn images
