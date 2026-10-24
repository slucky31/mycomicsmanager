using Domain.Primitives;

namespace Domain.Users;

public class User : Entity<Guid>
{

    public string Email { get; protected set; } = string.Empty;

    public string AuthId { get; protected set; } = string.Empty;

    // The hidden libraries are shown everywhere, as the others (#1215).
    public bool ShowHiddenLibraries { get; protected set; }

    public static User Create(string email, string authId)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            AuthId = authId
        };
        return user;
    }

    public void Update(string email, string authId)
    {
        Email = email;
        AuthId = authId;
    }

    public void UpdateShowHiddenLibraries(bool show) => ShowHiddenLibraries = show;

}
