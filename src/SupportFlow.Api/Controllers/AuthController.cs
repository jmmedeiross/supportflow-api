using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportFlow.Api.Data;
using SupportFlow.Api.Dtos;
using SupportFlow.Api.Models;
using SupportFlow.Api.Services;

namespace SupportFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext dbContext, ITokenService tokenService) : ControllerBase
{
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        bool emailInUse = await dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (emailInUse)
        {
            return Conflict(new ProblemDetails
            {
                Title = "E-mail já cadastrado",
                Detail = "Já existe uma conta utilizando este endereço de e-mail.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = string.Empty,
            Role = UserRole.Customer
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        AuthResponse response = BuildAuthResponse(user);
        return Created("/api/auth/login", response);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        AppUser? user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return Unauthorized(CreateInvalidCredentialsProblem());
        }

        PasswordVerificationResult verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(CreateInvalidCredentialsProblem());
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(BuildAuthResponse(user));
    }

    private AuthResponse BuildAuthResponse(AppUser user)
    {
        (string token, DateTime expiresAtUtc) = tokenService.CreateToken(user);
        return new AuthResponse(
            token,
            expiresAtUtc,
            new UserResponse(user.Id, user.FullName, user.Email, user.Role));
    }

    private static ProblemDetails CreateInvalidCredentialsProblem()
    {
        return new ProblemDetails
        {
            Title = "Credenciais inválidas",
            Detail = "E-mail ou senha inválidos.",
            Status = StatusCodes.Status401Unauthorized
        };
    }
}
