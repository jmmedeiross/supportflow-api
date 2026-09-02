using SupportFlow.Api.Models;
using Xunit;

namespace SupportFlow.Tests;

public sealed class TicketTests
{
    [Fact]
    public void NewTicket_StartsOpenWithVersionOne()
    {
        Ticket ticket = CreateTicket();

        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(1, ticket.Version);
        Assert.Null(ticket.ClosedAtUtc);
    }

    [Fact]
    public void ChangeStatus_FromOpenToInProgress_UpdatesVersion()
    {
        Ticket ticket = CreateTicket();
        DateTime changedAt = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        ticket.ChangeStatus(TicketStatus.InProgress, changedAt);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(2, ticket.Version);
        Assert.Equal(changedAt, ticket.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeStatus_FromOpenToResolved_ThrowsException()
    {
        Ticket ticket = CreateTicket();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ticket.ChangeStatus(TicketStatus.Resolved));

        Assert.Contains("Invalid transition", exception.Message);
        Assert.Equal(TicketStatus.Open, ticket.Status);
    }

    [Fact]
    public void ChangeStatus_ToClosed_SetsClosedTimestamp()
    {
        Ticket ticket = CreateTicket();
        DateTime closedAt = new(2026, 9, 1, 15, 30, 0, DateTimeKind.Utc);

        ticket.ChangeStatus(TicketStatus.Closed, closedAt);

        Assert.Equal(TicketStatus.Closed, ticket.Status);
        Assert.Equal(closedAt, ticket.ClosedAtUtc);
    }

    [Fact]
    public void ChangeStatus_AfterClosed_ThrowsException()
    {
        Ticket ticket = CreateTicket();
        ticket.ChangeStatus(TicketStatus.Closed);

        Assert.Throws<InvalidOperationException>(
            () => ticket.ChangeStatus(TicketStatus.InProgress));
    }

    [Fact]
    public void AssignTo_StoresAgentAndUpdatesVersion()
    {
        Ticket ticket = CreateTicket();
        Guid agentId = Guid.NewGuid();

        ticket.AssignTo(agentId);

        Assert.Equal(agentId, ticket.AssignedToUserId);
        Assert.Equal(2, ticket.Version);
    }

    private static Ticket CreateTicket()
    {
        return new Ticket
        {
            Title = "Chamado de teste",
            Description = "Descrição detalhada para validar as regras do domínio.",
            CreatedByUserId = Guid.NewGuid()
        };
    }
}
