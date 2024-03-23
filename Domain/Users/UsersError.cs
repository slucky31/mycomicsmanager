using Domain.Primitives;

namespace Domain.Users;
public static class UsersError
{
    public static readonly TError BadRequest = new("USR400", "Verify the parameter of the request");
    public static readonly TError NotFound = new("USR404", "User not found");    
    public static readonly TError Duplicate = new("USR409", "A user is already created with this name ou authId");
}
