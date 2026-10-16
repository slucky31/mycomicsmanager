using Application.ComicInfoSearch;
using Microsoft.Extensions.Options;

namespace Application.Settings;

// The configuration of the sources searched by ISBN, read by FeatureToggleDefaults.
public sealed record BookSourceOptions(
    IOptions<BnfCatalogueSettings> Bnf,
    IOptions<GoogleBooksSettings> GoogleBooks,
    IOptions<OpenLibrarySettings> OpenLibrary,
    IOptions<BubbleSettings> Bubble,
    IOptions<BedethequeSettings> Bedetheque);
