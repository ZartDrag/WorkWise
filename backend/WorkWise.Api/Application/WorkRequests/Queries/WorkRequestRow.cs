using WorkWise.Api.Application.WorkRequests.Dtos;

namespace WorkWise.Api.Application.WorkRequests.Queries;

/// <summary>Flat row shape Dapper maps into (Postgres "date" arrives as DateTime).</summary>
internal sealed class WorkRequestRow
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime DueDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int NoteCount { get; init; }

    public WorkRequestSummaryDto ToSummary() => new(
        Id, Title, ClientName, Description, Priority, Status,
        DateOnly.FromDateTime(DueDate), CreatedAt, UpdatedAt, NoteCount);
}

internal sealed class WorkRequestNoteRow
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
