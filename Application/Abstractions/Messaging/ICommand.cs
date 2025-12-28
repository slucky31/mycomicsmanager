namespace Application.Abstractions.Messaging;

#pragma warning disable CA1040
#pragma warning disable S2326 // Unused type parameters should be removed

public interface ICommand : IBaseCommand;


public interface ICommand<TResponse> : IBaseCommand;


public interface IBaseCommand;


#pragma warning restore S2326 // Unused type parameters should be removed
#pragma warning restore CA1040
