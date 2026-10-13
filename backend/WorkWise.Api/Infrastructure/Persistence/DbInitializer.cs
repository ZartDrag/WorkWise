using Microsoft.EntityFrameworkCore;
using WorkWise.Api.Domain;

namespace WorkWise.Api.Infrastructure.Persistence;

public static class DbInitializer
{
    /// <summary>Applies pending migrations and seeds sample data (Development only).</summary>
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkWiseDbContext>();
        await db.Database.MigrateAsync();

        if (await db.WorkRequests.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        WorkRequest Make(string title, string client, string desc, WorkRequestPriority p, WorkRequestStatus s, int dueInDays, params string[] notes)
        {
            var id = Guid.NewGuid();
            return new WorkRequest
            {
                Id = id, Title = title, ClientName = client, Description = desc,
                Priority = p, Status = s, DueDate = today.AddDays(dueInDays),
                CreatedAt = now, UpdatedAt = now,
                Notes = notes.Select(n => new WorkRequestNote { Id = Guid.NewGuid(), WorkRequestId = id, Content = n, CreatedAt = now }).ToList()
            };
        }

        db.WorkRequests.AddRange(
            Make("Quarterly NAV report automation", "Northwind Capital", "Automate the quarterly NAV report export.", WorkRequestPriority.High, WorkRequestStatus.InProgress, 5, "Kick-off call done; data sources confirmed."),
            Make("Fix login timeout on client portal", "Contoso Ltd", "Users are logged out after 2 minutes.", WorkRequestPriority.High, WorkRequestStatus.New, 2),
            Make("Add CSV export to expense module", "Fabrikam Inc", "Finance needs CSV export of allocations.", WorkRequestPriority.Medium, WorkRequestStatus.Blocked, 10, "Waiting on sample file from client."),
            Make("Update onboarding email templates", "Adventure Works", "Refresh branding in onboarding emails.", WorkRequestPriority.Low, WorkRequestStatus.Completed, -3),
            Make("Integrate SSO with Entra ID", "Contoso Ltd", "Replace local auth with Entra ID SSO.", WorkRequestPriority.Medium, WorkRequestStatus.New, 21),
            Make("Performance review of trade import job", "Northwind Capital", "Nightly import is taking over 40 minutes.", WorkRequestPriority.High, WorkRequestStatus.InProgress, 7)
        );
        await db.SaveChangesAsync();
    }
}
