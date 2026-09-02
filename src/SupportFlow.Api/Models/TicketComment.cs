namespace SupportFlow.Api.Models;

public sealed class TicketComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public required string Message { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = null!;
    public AppUser Author { get; set; } = null!;
}
