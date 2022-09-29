namespace MyComicsManager.Model.Shared.Models;

public class PaginationComics
{
    public int TotalPages { get; set; }
    public IReadOnlyList<Comic> Data { get; set; }
}