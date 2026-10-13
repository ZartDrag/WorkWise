using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkWise.Api.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WorkWiseDbContext>
{
    public WorkWiseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WorkWiseDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=workwise;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new WorkWiseDbContext(options);
    }
}
