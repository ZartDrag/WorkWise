using Microsoft.EntityFrameworkCore;
using WorkWise.Api.Domain;

namespace WorkWise.Api.Infrastructure.Persistence;

public class WorkWiseDbContext(DbContextOptions<WorkWiseDbContext> options) : DbContext(options)
{
    public DbSet<WorkRequest> WorkRequests => Set<WorkRequest>();
    public DbSet<WorkRequestNote> WorkRequestNotes => Set<WorkRequestNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // pg_trgm powers the GIN trigram indexes used by case-insensitive "contains" search.
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<WorkRequest>(e =>
        {
            e.ToTable("work_requests", t =>
            {
                t.HasCheckConstraint("ck_work_requests_priority", "priority IN ('Low','Medium','High')");
                t.HasCheckConstraint("ck_work_requests_status", "status IN ('New','InProgress','Blocked','Completed')");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.ClientName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.DueDate).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.UpdatedAt).IsRequired();

            // Status filter + default ordering by due date.
            e.HasIndex(x => new { x.Status, x.DueDate }).HasDatabaseName("ix_work_requests_status_due_date");
            // Default unfiltered listing ordered by due date.
            e.HasIndex(x => x.DueDate).HasDatabaseName("ix_work_requests_due_date");
            // ILIKE '%term%' search on title and client name.
            e.HasIndex(x => x.Title).HasDatabaseName("ix_work_requests_title_trgm")
                .HasMethod("gin").HasOperators("gin_trgm_ops");
            e.HasIndex(x => x.ClientName).HasDatabaseName("ix_work_requests_client_name_trgm")
                .HasMethod("gin").HasOperators("gin_trgm_ops");

            e.HasMany(x => x.Notes)
                .WithOne(n => n.WorkRequest)
                .HasForeignKey(n => n.WorkRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkRequestNote>(e =>
        {
            e.ToTable("work_request_notes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            // Fetch a request's notes in chronological order.
            e.HasIndex(x => new { x.WorkRequestId, x.CreatedAt }).HasDatabaseName("ix_work_request_notes_work_request_id_created_at");
        });
    }
}
