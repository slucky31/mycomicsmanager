using Application.Interfaces;
using Domain.Primitives;
using Domain.Users;
using MediatR;
using MongoDB.Bson;

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
        var user = await userReadService.GetUserByAuthIdAndEmail(command.email, command.authId);
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
