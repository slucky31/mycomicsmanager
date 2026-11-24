using System.Security.Claims;
using Application.Users;
using Domain.Users;

namespace Web.Services;

/// <summary>
/// Finds the application user of an Auth0 principal. The Auth0 "sub" claim is the identity; the e-mail
/// is only trusted, when verified by Auth0, to find a legacy user that was never linked to a sub (#1368).
/// </summary>
internal static class UserIdentity
{
    internal static string? GetSub(ClaimsPrincipal principal) =>
        principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

    // Null when Auth0 has not verified the address: "name" (Identity.Name) or an unverified e-mail can be chosen freely.
    internal static string? GetVerifiedEmail(ClaimsPrincipal principal)
    {
        var verified = principal.FindFirstValue("email_verified");
        if (!bool.TryParse(verified, out var isVerified) || !isVerified)
        {
            return null;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        return string.IsNullOrWhiteSpace(email) ? null : email;
    }

    // A sub always reads "<connection>|<id>": a user without one predates the sub-based AuthId.
    internal static bool IsLegacy(User user) => !user.AuthId.Contains('|', StringComparison.Ordinal);

    /// <summary>
    /// The user linked to the sub, or else the legacy user with the verified e-mail. Null when none:
    /// a user already linked to another sub is never returned.
    /// </summary>
    internal static async Task<User?> FindUserAsync(ClaimsPrincipal principal, IUserReadService userReadService, CancellationToken ct)
    {
        var sub = GetSub(principal);
        if (!string.IsNullOrEmpty(sub))
        {
            var byAuthId = await userReadService.GetUserByAuthId(sub, ct);
            if (byAuthId.IsSuccess)
            {
                return byAuthId.Value;
            }
        }

        var email = GetVerifiedEmail(principal);
        if (email is null)
        {
            return null;
        }

        var byEmail = await userReadService.GetUserByEmail(email, ct);
        return byEmail.IsSuccess && IsLegacy(byEmail.Value!) ? byEmail.Value : null;
    }

    internal static async Task<Guid?> ResolveUserIdAsync(ClaimsPrincipal principal, IUserReadService userReadService, CancellationToken ct) =>
        (await FindUserAsync(principal, userReadService, ct))?.Id;
}
