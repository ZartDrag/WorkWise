using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WorkWise.Api.Application.Common;
using WorkWise.Api.Application.WorkRequests.Dtos;
using WorkWise.Api.Domain;
using WorkWise.Api.Infrastructure.Persistence;

namespace WorkWise.Api.Application.WorkRequests.Commands;

public record AddWorkRequestNoteCommand(Guid WorkRequestId, string? Content);

public class AddWorkRequestNoteCommandValidator : AbstractValidator<AddWorkRequestNoteCommand>
{
    public AddWorkRequestNoteCommandValidator()
    {
        RuleFor(x => x.Content).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Note content is required.")
            .MaximumLength(2000);
    }
}

public class AddWorkRequestNoteCommandHandler(
    WorkWiseDbContext db,
    IValidator<AddWorkRequestNoteCommand> validator) : ICommandHandler<AddWorkRequestNoteCommand, WorkRequestNoteDto>
{
    public async Task<WorkRequestNoteDto> HandleAsync(AddWorkRequestNoteCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var workRequest = await db.WorkRequests.FirstOrDefaultAsync(x => x.Id == command.WorkRequestId, cancellationToken)
                          ?? throw new NotFoundException("Work request", command.WorkRequestId);

        var now = DateTime.UtcNow;
        var note = new WorkRequestNote
        {
            Id = Guid.NewGuid(),
            WorkRequestId = workRequest.Id,
            Content = command.Content!.Trim(),
            CreatedAt = now
        };
        db.WorkRequestNotes.Add(note);
        workRequest.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return new WorkRequestNoteDto(note.Id, note.Content, note.CreatedAt);
    }
}
