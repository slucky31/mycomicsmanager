using Application.Interfaces;
using Domain.Users;

namespace Application.Users;

public static class UserRepositoryExtensions
{
    // Whether the user shows their hidden libraries (#1215); an unknown user does not.
    public static async Task<bool> ShowsHiddenLibrariesAsync(this IRepository<User, Guid> userRepository, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(userRepository);

        var user = await userRepository.GetByIdAsync(userId);
        return user?.ShowHiddenLibraries ?? false;
    }
}
