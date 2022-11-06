using System.Collections.Generic;
using MyComicsManager.Model.Shared.Models;

namespace MyComicsManagerApiTests.MockData;

public static class LibraryMockData
{
    public static List<Library> Get()
    {
        var libs = new List<Library>();
        libs.Add(
            new Library
            {
                Id = "1",
                RelPath = "lib1"
            });
        libs.Add(
            new Library
            {
                Id = "2",
                RelPath = "lib2"
            });
        libs.Add(
            new Library
            {
                Id = "3",
                RelPath = "lib3"
            });
        libs.Add(
            new Library
            {
                Id = "4",
                RelPath = "lib4"
            });
        return libs;
    }
}