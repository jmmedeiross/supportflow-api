using System.ComponentModel.DataAnnotations;
using SupportFlow.Api.Models;

namespace SupportFlow.Api.Dtos;

public sealed record CreateTicketRequest(
    [Required, StringLength(150, MinimumLength = 5)] string Title,
    [Required, StringLength(4000, MinimumLength = 10)] string Description,
    TicketPriority Priority = TicketPriority.Medium);

public sealed record UpdateTicketStatusRequest(TicketStatus Status, int Version);

public sealed record AssignTicketRequest(Guid AgentId, int Version);

public sealed record AddCommentRequest(
    [Required, StringLength(2000, MinimumLength = 2)] string Message);

public sealed record CommentResponse(
    Guid Id,
    string Message,
    Guid AuthorId,
    string AuthorName,
    DateTime CreatedAtUtc);

public sealed record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CreatedByUserId,
    string CreatedByName,
    Guid? AssignedToUserId,
    string? AssignedToName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ClosedAtUtc,
    int Version,
    IReadOnlyList<CommentResponse> Comments);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
