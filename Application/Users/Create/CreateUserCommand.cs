using Domain.Primitives;
using Domain.Users;
using MediatR;

namespace Application.Users.Create;
public record CreateUserCommand(string email, string authId) : IRequest<Result<User>>;
