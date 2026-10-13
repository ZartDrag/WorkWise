namespace WorkWise.Api.Application.WorkRequests.Dtos;

// ---------- Request DTOs (API input). Properties are nullable so FluentValidation reports all missing fields. ----------

public record CreateWorkRequestRequest(
    string? Title,
    string? ClientName,
    string? Description,
    string? Priority,
    string? Status,
    DateOnly? DueDate,
    string? Notes);

public record UpdateWorkRequestStatusRequest(string? Status);

public record AddWorkRequestNoteRequest(string? Content);

// ---------- Response / read DTOs ----------

public record WorkRequestSummaryDto(
    Guid Id,
    string Title,
    string ClientName,
    string? Description,
    string Priority,
    string Status,
    DateOnly DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int NoteCount);

public record WorkRequestNoteDto(Guid Id, string Content, DateTime CreatedAt);

public record WorkRequestDetailsDto(
    Guid Id,
    string Title,
    string ClientName,
    string? Description,
    string Priority,
    string Status,
    DateOnly DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<WorkRequestNoteDto> Notes);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
