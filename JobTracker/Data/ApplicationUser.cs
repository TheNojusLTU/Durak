using Microsoft.AspNetCore.Identity;

namespace JobTracker.Data;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public string AvatarKey { get; set; } = string.Empty;
}