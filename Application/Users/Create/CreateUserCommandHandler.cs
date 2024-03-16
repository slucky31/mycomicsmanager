using Application.Data;
using Application.Interfaces;
using Application.Libraries.Create;
using Application.Libraries;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using MongoDB.Bson;
using Domain.Users;

namespace Application.Users.Create;

internal sealed class CreateUserCommandHandler(IRepository<User, ObjectId> userRepository, IUnitOfWork unitOfWork, IUserReadService userReadService) : IRequestHandler<CreateUserCommand, Result<User>>
{
    public async Task<Result<User>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Check if parameter are not null or empty
        if (string.IsNullOrEmpty(command.email) || string.IsNullOrEmpty(command.authId))
        {
            return UsersError.BadRequest;
        }

        // Check if a user with the same email or AuthId doesn't already exist
        var user = await userReadService.GetUserByAuthIdOrEmail(command.email, command.authId);        
        if (user is not null)
        {
            return UsersError.Duplicate;
        }

        // Create the user
        var newUser = User.Create(command.email, command.authId);
        userRepository.Add(newUser);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newUser;
    }
}
