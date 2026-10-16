using Application.Interfaces;

namespace Application.ComicInfoSearch;

// The catalogues ComicSearchService searches by ISBN.
public sealed record BookCatalogues(
    IBnfCatalogueService Bnf,
    IGoogleBooksService GoogleBooks,
    IOpenLibraryService OpenLibrary,
    IBubbleService Bubble,
    IBedethequeService Bedetheque);
