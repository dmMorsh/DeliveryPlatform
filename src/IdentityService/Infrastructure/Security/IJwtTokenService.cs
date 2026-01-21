using IdentityService.Domain.Users;

namespace IdentityService.Infrastructure.Security;

public interface IJwtTokenService
{
    string GenerateAccessToken(ApplicationUser user);
}