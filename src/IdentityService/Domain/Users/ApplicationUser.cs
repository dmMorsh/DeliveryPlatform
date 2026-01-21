using Microsoft.AspNetCore.Identity;

namespace IdentityService.Domain.Users;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Role { get; set; } = default!;
    public Guid TenantId { get; set; }
}
