using JobTracker.Data;

namespace JobTracker.Auth;

public sealed record RegisterRequest(
    string? Username,
    string? DisplayName,
    string? Password,
    string? Email = null,
    string? AvatarKey = null);

public sealed record LoginRequest(string? Username, string? Password);

public sealed record UserResponse(string Id, string Username, string DisplayName, string? Email, string AvatarKey)
{
    public static UserResponse FromUser(ApplicationUser user) =>
        new(user.Id, user.UserName!, user.DisplayName, user.Email, user.AvatarKey);
}

public sealed record CsrfResponse(string RequestToken, string HeaderName);
