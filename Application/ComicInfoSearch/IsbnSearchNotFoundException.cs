namespace Application.ComicInfoSearch;

public class IsbnSearchNotFoundException : Exception
{
    public IsbnSearchNotFoundException()
    {
    }

    public IsbnSearchNotFoundException(string message)
        : base(message)
    {
    }

    public IsbnSearchNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
