namespace WorkWise.Api.Domain;

public class WorkRequestNote
{
    public Guid Id { get; set; }
    public Guid WorkRequestId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public WorkRequest? WorkRequest { get; set; }
}
