﻿using Domain.Primitives;
using MongoDB.Bson;

namespace Domain.Libraries;

public class Library : Entity<ObjectId> {

    public string Name { get; protected set; } = String.Empty;

    public static Library Create(string name)
    {
        var library = new Library
        {
            Id = ObjectId.GenerateNewId(),
            Name = name
        };
        return library;
    }

    public static Library Create(string name, ObjectId id)
    {
        var library = new Library
        {
            Id = id,
            Name = name
        };
        return library;
    }

    public void Update(string name)
    {
        Name = name;
    }
}
