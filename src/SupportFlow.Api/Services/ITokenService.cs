using SupportFlow.Api.Models;

namespace SupportFlow.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(AppUser user);
}
