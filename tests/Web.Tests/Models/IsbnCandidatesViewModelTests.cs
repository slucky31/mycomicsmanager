using Application.Books.IsbnOwners;
using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class IsbnCandidatesViewModelTests
{
    private const string UsedIsbn = "9782800112343";
    private const string FreeIsbn = "2205056174";

    private static readonly IsbnOwnerDto s_owner = new(UsedIsbn, Guid.CreateVersion7(), "Blacksad", "Âme rouge", 4, "https://covers/blacksad.jpg", "BD");

    [Fact]
    public void Of_Should_GiveEachIsbnTheBookHoldingIt()
    {
        var view = IsbnCandidatesViewModel.Of([UsedIsbn, FreeIsbn], [s_owner]);

        view.Candidates.Should().Equal(
            new IsbnCandidateViewModel(UsedIsbn, new IsbnOwnerViewModel(s_owner.BookId, "Âme rouge", "Blacksad · Volume 4", "https://covers/blacksad.jpg", "BD")),
            new IsbnCandidateViewModel(FreeIsbn, null));
        view.Candidates[0].Owner!.CoverAlt.Should().Be("Cover of Âme rouge");
        view.FreeIsbns.Should().Equal(FreeIsbn);
        view.UsedCandidates.Should().Equal(view.Candidates[0]);
    }

    [Fact]
    public void Message_Should_TellTheBookMayBeADuplicate_WhenItsSingleIsbnIsUsed()
    {
        var view = IsbnCandidatesViewModel.Of([UsedIsbn], [s_owner]);

        view.AllUsed.Should().BeTrue();
        view.Message.Should().Be("The ISBN printed in this book is already used by another book. This one may be a duplicate: compare them.");
        view.ScanMessage.Should().Be("The ISBN found in the pages is already used by another book");
    }

    [Fact]
    public void Message_Should_TellTheBookMayBeADuplicate_WhenAllItsIsbnsAreUsed()
    {
        var view = IsbnCandidatesViewModel.Of([UsedIsbn, FreeIsbn], [s_owner, s_owner with { Isbn = FreeIsbn }]);

        view.Message.Should().Be("The ISBNs printed in this book are already used by other books. This one may be a duplicate: compare them.");
        view.ScanMessage.Should().Be("The ISBNs found in the pages are already used by other books");
    }

    [Fact]
    public void Message_Should_AskToPick_WhenSeveralIsbnsAreRead()
    {
        var view = IsbnCandidatesViewModel.Of([UsedIsbn, FreeIsbn], [s_owner]);

        view.AllUsed.Should().BeFalse();
        view.Message.Should().Be("Several ISBNs are printed in this book. Pick its own to fetch its information:");
        view.ScanMessage.Should().Be("Several ISBNs found in the pages: pick the book's own");
    }

    [Fact]
    public void Message_Should_OfferTheIsbn_WhenItsSingleIsbnIsNoLongerUsed()
    {
        var view = IsbnCandidatesViewModel.Of([FreeIsbn], []);

        view.Message.Should().Be("An ISBN is printed in this book. Use it to fetch its information:");
    }

    [Fact]
    public void None_Should_HaveNoCandidate()
    {
        IsbnCandidatesViewModel.None.AllUsed.Should().BeFalse();
    }
}
