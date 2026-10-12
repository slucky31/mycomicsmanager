namespace Web.Components.Pages.Libraries.Views;

// Id of the ⋮ button of a book: the actions menu opens next to it on a tablet.
public static class BookActionsIds
{
    public static string Of(Guid bookId) => $"book-actions-{bookId}";
}
