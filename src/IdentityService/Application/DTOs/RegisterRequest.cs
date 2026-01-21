using System.ComponentModel.DataAnnotations;

namespace IdentityService.Application.DTOs;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;

    /// <summary>
    /// Customer / Courier / MerchantAdmin
    /// </summary>
    [Required]
    public string Role { get; set; } = default!;

    /// <summary>
    /// Tenant / brand identifier
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
}