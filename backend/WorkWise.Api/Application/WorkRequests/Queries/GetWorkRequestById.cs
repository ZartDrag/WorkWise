using Dapper;
using WorkWise.Api.Application.Common;
using WorkWise.Api.Application.WorkRequests.Dtos;
using WorkWise.Api.Infrastructure.Data;

namespace WorkWise.Api.Application.WorkRequests.Queries;

public record GetWorkRequestByIdQuery(Guid Id);

public class GetWorkRequestByIdQueryHandler(IDbConnectionFactory connectionFactory)
    : IQueryHandler<GetWorkRequestByIdQuery, WorkRequestDetailsDto>
{
    private const string Sql = """
        SELECT id AS Id, title AS Title, client_name AS ClientName, description AS Description,
               priority AS Priority, status AS Status, due_date AS DueDate,
               created_at AS CreatedAt, updated_at AS UpdatedAt, 0 AS NoteCount
        FROM work_requests
        WHERE id = @Id;

        SELECT id AS Id, content AS Content, created_at AS CreatedAt
        FROM work_request_notes
        WHERE work_request_id = @Id
        ORDER BY created_at ASC;
        """;

    public async Task<WorkRequestDetailsDto> HandleAsync(GetWorkRequestByIdQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        using var grid = await connection.QueryMultipleAsync(new CommandDefinition(Sql, new { query.Id }, cancellationToken: cancellationToken));

        var row = await grid.ReadSingleOrDefaultAsync<WorkRequestRow>()
                  ?? throw new NotFoundException("Work request", query.Id);
        var notes = await grid.ReadAsync<WorkRequestNoteRow>();

        return new WorkRequestDetailsDto(
            row.Id, row.Title, row.ClientName, row.Description, row.Priority, row.Status,
            DateOnly.FromDateTime(row.DueDate), row.CreatedAt, row.UpdatedAt,
            notes.Select(n => new WorkRequestNoteDto(n.Id, n.Content, n.CreatedAt)).ToList());
    }
}
