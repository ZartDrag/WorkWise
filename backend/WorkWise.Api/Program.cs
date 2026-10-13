using Microsoft.EntityFrameworkCore;
using WorkWise.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("WorkWise")
    ?? throw new InvalidOperationException("Connection string 'WorkWise' is not configured.");

builder.Services.AddDbContext<WorkWiseDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DbInitializer.MigrateAndSeedAsync(app.Services);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
