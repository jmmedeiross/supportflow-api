namespace SupportFlow.Api.Models;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string Role { get; set; } = UserRole.Customer;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Ticket> CreatedTickets { get; set; } = [];
    public ICollection<Ticket> AssignedTickets { get; set; } = [];
    public ICollection<TicketComment> Comments { get; set; } = [];
}
