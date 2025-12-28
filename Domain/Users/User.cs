using Domain.Primitives;

namespace Domain.Users;

public class User : Entity<Guid>
{

    public string Email { get; protected set; } = String.Empty;

    public string AuthId { get; protected set; } = String.Empty;

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

}
