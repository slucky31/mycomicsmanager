using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Primitives;
using Domain.Users;

namespace Application.Users.HiddenLibraries;

public sealed class GetShowHiddenLibrariesQueryHandler(IRepository<User, Guid> userRepository)
    : IQueryHandler<GetShowHiddenLibrariesQuery, bool>
{
    public async Task<Result<bool>> Handle(GetShowHiddenLibrariesQuery request, CancellationToken cancellationToken)
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

        return user.ShowHiddenLibraries;
    }
}
