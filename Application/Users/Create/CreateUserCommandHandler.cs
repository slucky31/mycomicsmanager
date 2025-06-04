using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Primitives;
using Domain.Users;

namespace Application.Users.Create;

public sealed class CreateUserCommandHandler(IRepository<User, Guid> userRepository, IUnitOfWork unitOfWork, IUserReadService userReadService) : ICommandHandler<CreateUserCommand, User>
{
    public async Task<Result<User>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return UsersError.BadRequest;
        }

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
