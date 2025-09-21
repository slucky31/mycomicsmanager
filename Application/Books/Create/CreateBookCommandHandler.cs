using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.Create;

public sealed class CreateBookCommandHandler(IRepository<Book, Guid> bookRepository, IUnitOfWork unitOfWork) : ICommandHandler<CreateBookCommand, Book>
{
    public async Task<Result<Book>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        // Check if parameters are not null or empty
        if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.ISBN))
        {
            return BooksError.BadRequest;
        }

        // Validate ISBN format (basic check for 10 or 13 digits)
        if (!IsValidISBN(request.ISBN))
        {
            return BooksError.InvalidISBN;
        }

        // Check if a book with the same ISBN doesn't already exist
        var existingBooks = await bookRepository.ListAsync();
        if (existingBooks.Any(b => b.ISBN == request.ISBN))
        {
            return BooksError.Duplicate;
        }

        // Create Book
        var book = Book.Create(request.Serie, request.Title, request.ISBN, request.VolumeNumber, request.ImageLink);

        bookRepository.Add(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return book;
    }

    private static bool IsValidISBN(string isbn)
    {
        if (string.IsNullOrEmpty(isbn))
        {
            return false;
        }

        // Remove any dashes, spaces, and convert to uppercase
        var cleanIsbn = isbn.Replace("-", "", StringComparison.Ordinal)
                           .Replace(" ", "", StringComparison.Ordinal)
                           .ToUpperInvariant();

        // Check ISBN-10
        if (cleanIsbn.Length == 10)
        {
            return IsValidISBN10(cleanIsbn);
        }

        // Check ISBN-13
        if (cleanIsbn.Length == 13)
        {
            return IsValidISBN13(cleanIsbn);
        }

        return false;
    }

    private static bool IsValidISBN10(string isbn)
    {
        // First 9 characters must be digits, last can be digit or X
        for (int i = 0; i < 9; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                return false;
            }
        }

        char lastChar = isbn[9];
        if (!char.IsDigit(lastChar) && lastChar != 'X')
        {
            return false;
        }

        // Calculate checksum
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += (isbn[i] - '0') * (10 - i);
        }

        // Add the check digit
        if (lastChar == 'X')
        {
            sum += 10;
        }
        else
        {
            sum += lastChar - '0';
        }

        return sum % 11 == 0;
    }

    private static bool IsValidISBN13(string isbn)
    {
        // All characters must be digits
        for (int i = 0; i < 13; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                return false;
            }
        }

        // Calculate checksum
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = isbn[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        int actualCheckDigit = isbn[12] - '0';

        return checkDigit == actualCheckDigit;
    }
}