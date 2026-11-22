using Application.Abstractions.Messaging;
using Application.Books.AddReadingDate;
using Application.Books.Create;
using Application.Books.Delete;
using Application.Books.DeleteReadingDate;
using Application.Books.GetById;
using Application.Books.IsbnAvailability;
using Application.Books.IsbnOwners;
using Application.Books.List;
using Application.Books.Search;
using Application.Books.Update;
using Application.Interfaces;
using Domain.Books;

namespace Web.Services;

// The query handlers of BooksService, grouped so that its constructor stays short (S107).
public sealed record BookQueryHandlers(
    IQueryHandler<GetBookByIdQuery, Book> GetById,
    IQueryHandler<GetBooksQuery, List<Book>> GetBooks,
    IQueryHandler<GetPagedBooksQuery, IPagedList<BookSummaryDto>> GetPagedBooks,
    IQueryHandler<SearchBooksQuery, BookSearchResult> Search,
    IQueryHandler<IsIsbnAvailableQuery, bool> IsIsbnAvailable,
    IQueryHandler<GetIsbnCandidateOwnersQuery, IReadOnlyList<IsbnOwnerDto>> GetIsbnCandidateOwners,
    IQueryHandler<GetIsbnOwnerQuery, IReadOnlyList<IsbnOwnerDto>> GetIsbnOwner);

// The command handlers of BooksService, grouped so that its constructor stays short (S107).
public sealed record BookCommandHandlers(
    ICommandHandler<CreateBookCommand, Book> Create,
    ICommandHandler<UpdateBookCommand, Book> Update,
    ICommandHandler<DeleteBookCommand> Delete,
    ICommandHandler<DeleteBooksCommand, DeleteBooksResult> DeleteBooks,
    ICommandHandler<AddReadingDateCommand, ReadingDate> AddReadingDate,
    ICommandHandler<DeleteReadingDateCommand> DeleteReadingDate);
