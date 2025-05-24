using Application.Abstractions.Messaging;
using Domain.Users;

namespace Application.Users.Create;
public record CreateUserCommand(string email, string authId) : ICommand<User>;
