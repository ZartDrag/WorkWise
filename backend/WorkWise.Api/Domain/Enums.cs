namespace WorkWise.Api.Domain;

public enum WorkRequestPriority
{
    Low,
    Medium,
    High
}

public enum WorkRequestStatus
{
    New,
    InProgress,
    Blocked,
    Completed
}
