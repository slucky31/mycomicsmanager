namespace MyComicsManager.Model.Shared;

public class PaginationComics
{
    public int TotalPages { get; set; }
    public IReadOnlyList<Comic> Data { get; set; }
}