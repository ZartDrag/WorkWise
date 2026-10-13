using Microsoft.AspNetCore.Mvc;
using WorkWise.Api.Application.Common;
using WorkWise.Api.Application.WorkRequests.Commands;
using WorkWise.Api.Application.WorkRequests.Dtos;
using WorkWise.Api.Application.WorkRequests.Queries;

namespace WorkWise.Api.Api;

[ApiController]
[Route("api/work-requests")]
[Produces("application/json")]
public class WorkRequestsController : ControllerBase
{
    /// <summary>List work requests with optional status filter, title/client search and paging.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<WorkRequestSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<WorkRequestSummaryDto>> List(
        [FromServices] IQueryHandler<ListWorkRequestsQuery, PagedResult<WorkRequestSummaryDto>> handler,
        [FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        => handler.HandleAsync(new ListWorkRequestsQuery(status, search, page, pageSize), ct);

    /// <summary>Get a single work request including its notes.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<WorkRequestDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<WorkRequestDetailsDto> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetWorkRequestByIdQuery, WorkRequestDetailsDto> handler,
        CancellationToken ct)
        => handler.HandleAsync(new GetWorkRequestByIdQuery(id), ct);

    /// <summary>Create a work request.</summary>
    [HttpPost]
    [ProducesResponseType<WorkRequestDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkRequestDetailsDto>> Create(
        [FromBody] CreateWorkRequestRequest request,
        [FromServices] ICommandHandler<CreateWorkRequestCommand, WorkRequestDetailsDto> handler,
        CancellationToken ct)
    {
        var created = await handler.HandleAsync(new CreateWorkRequestCommand(
            request.Title, request.ClientName, request.Description,
            request.Priority, request.Status, request.DueDate, request.Notes), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update the status of a work request.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<WorkRequestSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<WorkRequestSummaryDto> UpdateStatus(
        Guid id,
        [FromBody] UpdateWorkRequestStatusRequest request,
        [FromServices] ICommandHandler<UpdateWorkRequestStatusCommand, WorkRequestSummaryDto> handler,
        CancellationToken ct)
        => handler.HandleAsync(new UpdateWorkRequestStatusCommand(id, request.Status), ct);

    /// <summary>Add a note to a work request.</summary>
    [HttpPost("{id:guid}/notes")]
    [ProducesResponseType<WorkRequestNoteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkRequestNoteDto>> AddNote(
        Guid id,
        [FromBody] AddWorkRequestNoteRequest request,
        [FromServices] ICommandHandler<AddWorkRequestNoteCommand, WorkRequestNoteDto> handler,
        CancellationToken ct)
    {
        var note = await handler.HandleAsync(new AddWorkRequestNoteCommand(id, request.Content), ct);
        return CreatedAtAction(nameof(GetById), new { id }, note);
    }
}
