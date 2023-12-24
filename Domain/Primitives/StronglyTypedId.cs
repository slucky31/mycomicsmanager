using MongoDB.Bson;

namespace Domain.Primitives;

public record StronglyObjectIdTypedId(ObjectId Id) : StronglyTypedId<ObjectId>(Id);

public abstract record StronglyTypedId<TValue>(TValue Id) where TValue : notnull;
