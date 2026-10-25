using Application.Interfaces;
using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookCandidateDisplayTests
{
    private static BookCandidate Candidate(string? serie = null, int? volume = null, string[]? publishers = null, DateOnly? date = null, string? isbn = null) =>
        new("Astérix et les Goths", serie, volume, publishers ?? [], date, isbn, null);

    [Theory]
    [InlineData("Astérix", 3, "Astérix · #3")]
    [InlineData(" Astérix ", 0, "Astérix")]
    [InlineData(null, 3, "#3")]
    [InlineData(" ", null, null)]
    public void SerieAndVolume_Should_ShowWhatTheSourceGives(string? serie, int? volume, string? expected)
    {
        BookCandidateDisplay.SerieAndVolume(Candidate(serie, volume)).Should().Be(expected);
    }

    [Fact]
    public void Edition_Should_ShowTheFirstPublisherAndTheYear()
    {
        BookCandidateDisplay.Edition(Candidate(publishers: ["Hachette", "Dargaud"], date: new DateOnly(2004, 6, 1))).Should().Be("Hachette · 2004");
        BookCandidateDisplay.Edition(Candidate(publishers: ["Hachette"])).Should().Be("Hachette");
        BookCandidateDisplay.Edition(Candidate()).Should().BeNull();
    }

    [Fact]
    public void Isbn_Should_SayTheEditionCannotBePicked_WhenItHasNoIsbn()
    {
        BookCandidateDisplay.Isbn(Candidate(isbn: "9782012101357")).Should().Be("ISBN 9782012101357");
        BookCandidateDisplay.Isbn(Candidate()).Should().Be("No ISBN: this edition cannot be picked");
    }

    [Theory]
    [InlineData(ProviderSearchState.Searching, 0, "searching…")]
    [InlineData(ProviderSearchState.Found, 1, "1 edition")]
    [InlineData(ProviderSearchState.Found, 3, "3 editions")]
    [InlineData(ProviderSearchState.NotFound, 0, "nothing found")]
    [InlineData(ProviderSearchState.Failed, 0, "could not be searched")]
    public void Summary_Should_SayWhatTheSourceFound(ProviderSearchState state, int count, string expected)
    {
        BookCandidateDisplay.Summary(state, count).Should().Be(expected);
    }
}
