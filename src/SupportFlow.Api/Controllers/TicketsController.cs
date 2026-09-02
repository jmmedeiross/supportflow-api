using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportFlow.Api.Data;
using SupportFlow.Api.Dtos;
using SupportFlow.Api.Extensions;
using SupportFlow.Api.Models;

namespace SupportFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TicketResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TicketResponse>>> GetAll(
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        Guid currentUserId = User.GetUserId();

        IQueryable<Ticket> query = dbContext.Tickets.AsNoTracking();

        if (!User.IsSupportTeam())
        {
            query = query.Where(ticket => ticket.CreatedByUserId == currentUserId);
        }

        if (status.HasValue)
        {
            query = query.Where(ticket => ticket.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(ticket => ticket.Priority == priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(ticket =>
                EF.Functions.Like(ticket.Title, $"%{term}%") ||
                EF.Functions.Like(ticket.Description, $"%{term}%"));
        }

        int totalItems = await query.CountAsync(cancellationToken);
        List<Ticket> tickets = await IncludeTicketDetails(query)
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var result = new PagedResult<TicketResponse>(
            tickets.Select(ticket => ticket.ToResponse()).ToList(),
            page,
            pageSize,
            totalItems,
            totalPages);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        Ticket? ticket = await IncludeTicketDetails(dbContext.Tickets.AsNoTracking())
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanAccess(ticket))
        {
            return Forbid();
        }

        return Ok(ticket.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketResponse>> Create(
        CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = new Ticket
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority,
            CreatedByUserId = User.GetUserId()
        };

        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);

        Ticket createdTicket = await LoadTicketAsync(ticket.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, createdTicket.ToResponse());
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{UserRole.Agent},{UserRole.Admin}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> UpdateStatus(
        Guid id,
        UpdateTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        Ticket? ticket = await dbContext.Tickets.FindAsync(new object[] { id }, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.Version != request.Version)
        {
            return Conflict(CreateVersionConflictProblem());
        }

        try
        {
            ticket.ChangeStatus(request.Status);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Transição de status inválida",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(CreateVersionConflictProblem());
        }

        Ticket updatedTicket = await LoadTicketAsync(id, cancellationToken);
        return Ok(updatedTicket.ToResponse());
    }

    [HttpPatch("{id:guid}/assignment")]
    [Authorize(Roles = $"{UserRole.Agent},{UserRole.Admin}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> Assign(
        Guid id,
        AssignTicketRequest request,
        CancellationToken cancellationToken)
    {
        Ticket? ticket = await dbContext.Tickets.FindAsync(new object[] { id }, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.Version != request.Version)
        {
            return Conflict(CreateVersionConflictProblem());
        }

        AppUser? agent = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == request.AgentId, cancellationToken);

        if (agent is null || !UserRole.SupportTeam.Contains(agent.Role))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Responsável inválido",
                Detail = "O usuário informado precisa possuir o perfil Agent ou Admin.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        ticket.AssignTo(agent.Id);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(CreateVersionConflictProblem());
        }

        Ticket updatedTicket = await LoadTicketAsync(id, cancellationToken);
        return Ok(updatedTicket.ToResponse());
    }

    [HttpPost("{id:guid}/comments")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> AddComment(
        Guid id,
        AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        Ticket? ticket = await dbContext.Tickets.FindAsync(new object[] { id }, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanAccess(ticket))
        {
            return Forbid();
        }

        var comment = new TicketComment
        {
            TicketId = ticket.Id,
            AuthorId = User.GetUserId(),
            Message = request.Message.Trim()
        };

        dbContext.TicketComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        Ticket updatedTicket = await LoadTicketAsync(id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, updatedTicket.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        Ticket? ticket = await dbContext.Tickets.FindAsync(new object[] { id }, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        dbContext.Tickets.Remove(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool CanAccess(Ticket ticket)
    {
        return User.IsSupportTeam() || ticket.CreatedByUserId == User.GetUserId();
    }

    private Task<Ticket> LoadTicketAsync(Guid id, CancellationToken cancellationToken)
    {
        return IncludeTicketDetails(dbContext.Tickets.AsNoTracking())
            .SingleAsync(ticket => ticket.Id == id, cancellationToken);
    }

    private static IQueryable<Ticket> IncludeTicketDetails(IQueryable<Ticket> query)
    {
        return query
            .Include(ticket => ticket.CreatedByUser)
            .Include(ticket => ticket.AssignedToUser)
            .Include(ticket => ticket.Comments)
                .ThenInclude(comment => comment.Author);
    }

    private static ProblemDetails CreateVersionConflictProblem()
    {
        return new ProblemDetails
        {
            Title = "Conflito de atualização",
            Detail = "O chamado foi alterado por outra operação. Recarregue os dados e tente novamente.",
            Status = StatusCodes.Status409Conflict
        };
    }
}
