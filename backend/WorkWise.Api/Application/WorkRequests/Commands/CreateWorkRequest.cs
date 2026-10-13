using FluentValidation;
using WorkWise.Api.Application.Common;
using WorkWise.Api.Application.WorkRequests.Dtos;
using WorkWise.Api.Domain;
using WorkWise.Api.Infrastructure.Persistence;

namespace WorkWise.Api.Application.WorkRequests.Commands;

public record CreateWorkRequestCommand(
    string? Title,
    string? ClientName,
    string? Description,
    string? Priority,
    string? Status,
    DateOnly? DueDate,
    string? Notes);

public class CreateWorkRequestCommandValidator : AbstractValidator<CreateWorkRequestCommand>
{
    public CreateWorkRequestCommandValidator()
    {
        RuleFor(x => x.Title).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);
        RuleFor(x => x.ClientName).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Client name is required.")
            .MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Priority is required.")
            .Must(EnumParsing.IsValidName<WorkRequestPriority>)
            .WithMessage($"Priority must be one of: {EnumParsing.AllowedValues<WorkRequestPriority>()}.");
        RuleFor(x => x.Status).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Status is required.")
            .Must(EnumParsing.IsValidName<WorkRequestStatus>)
            .WithMessage($"Status must be one of: {EnumParsing.AllowedValues<WorkRequestStatus>()}.");
        RuleFor(x => x.DueDate).NotNull().WithMessage("Due date is required.");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class CreateWorkRequestCommandHandler(
    WorkWiseDbContext db,
    IValidator<CreateWorkRequestCommand> validator) : ICommandHandler<CreateWorkRequestCommand, WorkRequestDetailsDto>
{
    public async Task<WorkRequestDetailsDto> HandleAsync(CreateWorkRequestCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        EnumParsing.TryParseName<WorkRequestPriority>(command.Priority, out var priority);
        EnumParsing.TryParseName<WorkRequestStatus>(command.Status, out var status);
        var now = DateTime.UtcNow;

        var entity = new WorkRequest
        {
            Id = Guid.NewGuid(),
            Title = command.Title!.Trim(),
            ClientName = command.ClientName!.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            Priority = priority,
            Status = status,
            DueDate = command.DueDate!.Value,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (!string.IsNullOrWhiteSpace(command.Notes))
        {
            entity.Notes.Add(new WorkRequestNote
            {
                Id = Guid.NewGuid(),
                WorkRequestId = entity.Id,
                Content = command.Notes.Trim(),
                CreatedAt = now
            });
        }

        db.WorkRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new WorkRequestDetailsDto(
            entity.Id, entity.Title, entity.ClientName, entity.Description,
            entity.Priority.ToString(), entity.Status.ToString(), entity.DueDate,
            entity.CreatedAt, entity.UpdatedAt,
            entity.Notes.Select(n => new WorkRequestNoteDto(n.Id, n.Content, n.CreatedAt)).ToList());
    }
}
