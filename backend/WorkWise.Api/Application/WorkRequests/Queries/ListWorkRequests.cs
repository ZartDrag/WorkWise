using System.Text;
using Dapper;
using FluentValidation;
using WorkWise.Api.Application.Common;
using WorkWise.Api.Application.WorkRequests.Dtos;
using WorkWise.Api.Domain;
using WorkWise.Api.Infrastructure.Data;

namespace WorkWise.Api.Application.WorkRequests.Queries;

public record ListWorkRequestsQuery(string? Status, string? Search, int Page = 1, int PageSize = 10);

public class ListWorkRequestsQueryValidator : AbstractValidator<ListWorkRequestsQuery>
{
    public ListWorkRequestsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || EnumParsing.IsValidName<WorkRequestStatus>(s))
            .WithMessage($"Status must be one of: {EnumParsing.AllowedValues<WorkRequestStatus>()}.");
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class ListWorkRequestsQueryHandler(
    IDbConnectionFactory connectionFactory,
    IValidator<ListWorkRequestsQuery> validator) : IQueryHandler<ListWorkRequestsQuery, PagedResult<WorkRequestSummaryDto>>
{
    public async Task<PagedResult<WorkRequestSummaryDto>> HandleAsync(ListWorkRequestsQuery query, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var where = new StringBuilder("WHERE 1 = 1");
        var parameters = new DynamicParameters();

        if (EnumParsing.TryParseName<WorkRequestStatus>(query.Status, out var status))
        {
            where.Append(" AND wr.status = @Status");
            parameters.Add("Status", status.ToString());
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ILIKE '%term%' is served by the pg_trgm GIN indexes on title and client_name.
            where.Append(" AND (wr.title ILIKE @Pattern ESCAPE '\\' OR wr.client_name ILIKE @Pattern ESCAPE '\\')");
            parameters.Add("Pattern", $"%{EscapeLike(query.Search.Trim())}%");
        }

        parameters.Add("Limit", query.PageSize);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize);

        var sql = $"""
            SELECT COUNT(*) FROM work_requests wr {where};

            SELECT wr.id          AS Id,
                   wr.title       AS Title,
                   wr.client_name AS ClientName,
                   wr.description AS Description,
                   wr.priority    AS Priority,
                   wr.status      AS Status,
                   wr.due_date    AS DueDate,
                   wr.created_at  AS CreatedAt,
                   wr.updated_at  AS UpdatedAt,
                   (SELECT COUNT(*)::int FROM work_request_notes n WHERE n.work_request_id = wr.id) AS NoteCount
            FROM work_requests wr
            {where}
            ORDER BY wr.due_date ASC, wr.created_at DESC
            LIMIT @Limit OFFSET @Offset;
            """;

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        using var grid = await connection.QueryMultipleAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        var total = await grid.ReadSingleAsync<long>();
        var rows = await grid.ReadAsync<WorkRequestRow>();

        return new PagedResult<WorkRequestSummaryDto>(
            rows.Select(r => r.ToSummary()).ToList(), query.Page, query.PageSize, (int)total);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
