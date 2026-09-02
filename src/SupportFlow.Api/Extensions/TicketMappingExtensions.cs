using SupportFlow.Api.Dtos;
using SupportFlow.Api.Models;

namespace SupportFlow.Api.Extensions;

public static class TicketMappingExtensions
{
    public static TicketResponse ToResponse(this Ticket ticket)
    {
        return new TicketResponse(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.CreatedByUserId,
            ticket.CreatedByUser.FullName,
            ticket.AssignedToUserId,
            ticket.AssignedToUser?.FullName,
            ticket.CreatedAtUtc,
            ticket.UpdatedAtUtc,
            ticket.ClosedAtUtc,
            ticket.Version,
            ticket.Comments
                .OrderBy(comment => comment.CreatedAtUtc)
                .Select(comment => new CommentResponse(
                    comment.Id,
                    comment.Message,
                    comment.AuthorId,
                    comment.Author.FullName,
                    comment.CreatedAtUtc))
                .ToList());
    }
}
