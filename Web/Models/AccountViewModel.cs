namespace Web.Models;

public static class AccountViewModel
{
    /// <summary>
    /// The initials shown in the avatar when the account has no picture: first and last word of the name, else the email.
    /// </summary>
    public static string Initials(string? name, string? email)
    {
        var words = (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var initials = words.Length switch
        {
            0 => string.IsNullOrWhiteSpace(email) ? "?" : email.Trim()[..1],
            1 => words[0][..1],
            _ => $"{words[0][0]}{words[^1][0]}"
        };

        return initials.ToUpperInvariant();
    }
}
