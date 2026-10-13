using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WorkWise.Api.Application.Common;
using WorkWise.Api.Application.WorkRequests.Dtos;
using WorkWise.Api.Domain;
using WorkWise.Api.Infrastructure.Persistence;

namespace WorkWise.Api.Application.WorkRequests.Commands;

public record UpdateWorkRequestStatusCommand(Guid Id, string? Status);

public class UpdateWorkRequestStatusCommandValidator : AbstractValidator<UpdateWorkRequestStatusCommand>
{
    public UpdateWorkRequestStatusCommandValidator()
    {
        RuleFor(x => x.Status).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Status is required.")
            .Must(EnumParsing.IsValidName<WorkRequestStatus>)
            .WithMessage($"Status must be one of: {EnumParsing.AllowedValues<WorkRequestStatus>()}.");
    }
}

public class UpdateWorkRequestStatusCommandHandler(
    WorkWiseDbContext db,
    IValidator<UpdateWorkRequestStatusCommand> validator) : ICommandHandler<UpdateWorkRequestStatusCommand, WorkRequestSummaryDto>
{
    public async Task<WorkRequestSummaryDto> HandleAsync(UpdateWorkRequestStatusCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        EnumParsing.TryParseName<WorkRequestStatus>(command.Status, out var status);

        var entity = await db.WorkRequests.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
                     ?? throw new NotFoundException("Work request", command.Id);

        if (entity.Status != status)
        {
            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        var noteCount = await db.WorkRequestNotes.CountAsync(n => n.WorkRequestId == entity.Id, cancellationToken);
        return new WorkRequestSummaryDto(
            entity.Id, entity.Title, entity.ClientName, entity.Description,
            entity.Priority.ToString(), entity.Status.ToString(), entity.DueDate,
            entity.CreatedAt, entity.UpdatedAt, noteCount);
    }
}
