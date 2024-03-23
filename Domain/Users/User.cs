using System.Xml.Linq;
using Domain.Primitives;
using MongoDB.Bson;

namespace Domain.Users;
public class User : Entity<ObjectId>
{

    public string Email { get; protected set; } = String.Empty;

    public string AuthId { get; protected set; } = String.Empty;

    public static User Create(string email, string authId)
    {
        var user = new User
        {
            Id = ObjectId.GenerateNewId(),
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
