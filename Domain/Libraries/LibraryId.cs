namespace Domain.Libraries;

public record LibraryId(Guid Id) : StronglyTypedId<Guid>(Id);
public abstract record StronglyTypedId<TValue>(TValue Id) where TValue : notnull;
