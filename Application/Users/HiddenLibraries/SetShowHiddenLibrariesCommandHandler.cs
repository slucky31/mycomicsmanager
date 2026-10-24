using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Primitives;
using Domain.Users;

namespace Application.Users.HiddenLibraries;

public sealed class SetShowHiddenLibrariesCommandHandler(IRepository<User, Guid> userRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<SetShowHiddenLibrariesCommand>
{
    public async Task<Result> Handle(SetShowHiddenLibrariesCommand request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty)
        {
            return UsersError.BadRequest;
        }

        var user = await userRepository.GetByIdAsync(request.UserId);
        if (user is null)
        {
            return UsersError.NotFound;
        }

        user.UpdateShowHiddenLibraries(request.Show);
        userRepository.Update(user);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error! : Result.Success();
    }
}
