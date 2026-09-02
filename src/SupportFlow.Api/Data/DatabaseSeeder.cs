using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportFlow.Api.Models;

namespace SupportFlow.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (!configuration.GetValue<bool>("Seed:Enabled"))
        {
            return;
        }

        string? adminPassword = configuration["Seed:AdminPassword"];
        string? agentPassword = configuration["Seed:AgentPassword"];

        if (string.IsNullOrWhiteSpace(adminPassword) || string.IsNullOrWhiteSpace(agentPassword))
        {
            return;
        }

        var hasher = new PasswordHasher<AppUser>();
        AppUser admin = await EnsureUserAsync(
            dbContext,
            hasher,
            "Administrador Demo",
            configuration["Seed:AdminEmail"] ?? "admin@supportflow.local",
            adminPassword,
            UserRole.Admin,
            cancellationToken);

        AppUser agent = await EnsureUserAsync(
            dbContext,
            hasher,
            "Agente Demo",
            configuration["Seed:AgentEmail"] ?? "agent@supportflow.local",
            agentPassword,
            UserRole.Agent,
            cancellationToken);

        if (!await dbContext.Tickets.AnyAsync(cancellationToken))
        {
            var sampleTicket = new Ticket
            {
                Title = "Erro ao acessar o painel",
                Description = "Usuário recebe uma mensagem de acesso negado ao abrir o painel principal.",
                Priority = TicketPriority.High,
                CreatedByUserId = admin.Id
            };
            sampleTicket.AssignTo(agent.Id);
            dbContext.Tickets.Add(sampleTicket);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<AppUser> EnsureUserAsync(
        AppDbContext dbContext,
        PasswordHasher<AppUser> hasher,
        string fullName,
        string email,
        string password,
        string role,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();
        AppUser? existingUser = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            return existingUser;
        }

        var user = new AppUser
        {
            FullName = fullName,
            Email = normalizedEmail,
            PasswordHash = string.Empty,
            Role = role
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}
