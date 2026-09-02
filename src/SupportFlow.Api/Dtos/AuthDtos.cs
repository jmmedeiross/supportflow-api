using System.ComponentModel.DataAnnotations;

namespace SupportFlow.Api.Dtos;

public sealed record RegisterRequest(
    [Required, StringLength(120, MinimumLength = 3)] string FullName,
    [Required, EmailAddress, StringLength(180)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserResponse User);

public sealed record UserResponse(Guid Id, string FullName, string Email, string Role);
