// <copyright file="Library.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Primitives;
using MongoDB.Bson;

namespace Domain.Libraries;

public class Library : Entity<LibraryId>
{
    public Library()
    { }

    public string Name { get; private set; } = String.Empty;

    public static Library Create(string name)
    {
        var library = new Library
        {
            Id = new LibraryId(Guid.NewGuid()),
            Name = name
        };
        return library;
    }

    public void Update(string name)
    {
        Name = name;
    }
}
