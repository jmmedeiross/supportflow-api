namespace SupportFlow.Api.Models;

public sealed class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public required string Description { get; set; }
    public TicketStatus Status { get; private set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public Guid CreatedByUserId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; private set; }
    public int Version { get; private set; } = 1;

    public AppUser CreatedByUser { get; set; } = null!;
    public AppUser? AssignedToUser { get; set; }
    public ICollection<TicketComment> Comments { get; set; } = [];

    public void ChangeStatus(TicketStatus nextStatus, DateTime? nowUtc = null)
    {
        if (nextStatus == Status)
        {
            return;
        }

        bool validTransition = Status switch
        {
            TicketStatus.Open => nextStatus is TicketStatus.InProgress or TicketStatus.Closed,
            TicketStatus.InProgress => nextStatus is TicketStatus.Open or TicketStatus.Resolved,
            TicketStatus.Resolved => nextStatus is TicketStatus.InProgress or TicketStatus.Closed,
            TicketStatus.Closed => false,
            _ => false
        };

        if (!validTransition)
        {
            throw new InvalidOperationException($"Invalid transition from {Status} to {nextStatus}.");
        }

        DateTime changedAt = nowUtc ?? DateTime.UtcNow;
        Status = nextStatus;
        UpdatedAtUtc = changedAt;
        ClosedAtUtc = nextStatus == TicketStatus.Closed ? changedAt : null;
        Version++;
    }

    public void AssignTo(Guid agentId)
    {
        AssignedToUserId = agentId;
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;
    }
}
