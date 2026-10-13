using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkWise.Api.Api.Middleware;
using WorkWise.Api.Application.Common;
using WorkWise.Api.Application.WorkRequests.Commands;
using WorkWise.Api.Application.WorkRequests.Dtos;
using WorkWise.Api.Application.WorkRequests.Queries;
using WorkWise.Api.Infrastructure.Data;
using WorkWise.Api.Infrastructure.Persistence;

const string CorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("WorkWise")
    ?? throw new InvalidOperationException("Connection string 'WorkWise' is not configured.");

// ---------- Persistence: EF Core for writes, Dapper for reads ----------
builder.Services.AddDbContext<WorkWiseDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
builder.Services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));

// ---------- CQRS handlers (no mediator: each handler is injected directly) ----------
builder.Services.AddScoped<ICommandHandler<CreateWorkRequestCommand, WorkRequestDetailsDto>, CreateWorkRequestCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateWorkRequestStatusCommand, WorkRequestSummaryDto>, UpdateWorkRequestStatusCommandHandler>();
builder.Services.AddScoped<ICommandHandler<AddWorkRequestNoteCommand, WorkRequestNoteDto>, AddWorkRequestNoteCommandHandler>();
builder.Services.AddScoped<IQueryHandler<ListWorkRequestsQuery, PagedResult<WorkRequestSummaryDto>>, ListWorkRequestsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetWorkRequestByIdQuery, WorkRequestDetailsDto>, GetWorkRequestByIdQueryHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateWorkRequestCommandValidator>();

// ---------- Web ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.AllowInputFormatterExceptionMessages = false)
    .ConfigureApiBehaviorOptions(options =>
    {
        // Malformed JSON / un-bindable values (e.g. "dueDate": "abc") also return ValidationProblemDetails
        // enriched with traceId + correlationId, matching the global exception middleware.
        options.InvalidModelStateResponseFactory = context =>
        {
            // Normalise keys ("$.dueDate" -> "dueDate") and drop the redundant "request" body error.
            var errors = context.ModelState
                .Where(kv => kv.Value is { Errors.Count: > 0 })
                .Select(kv => (Key: kv.Key == "$" ? "body" : kv.Key.StartsWith("$.") ? kv.Key[2..] : kv.Key, kv.Value!.Errors))
                .Where(e => !(e.Key == "request" && context.ModelState.Keys.Any(k => k.StartsWith("$"))))
                .GroupBy(e => e.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(e => e.Errors)
                          .Select(err => g.Key == "body" ? "The request body is not valid JSON."
                              : string.IsNullOrEmpty(err.ErrorMessage) || err.Exception is not null
                              ? $"The value provided for '{g.Key}' is invalid."
                              : err.ErrorMessage.StartsWith("The input was not valid") ? $"The value provided for '{g.Key}' is invalid." : err.ErrorMessage)
                          .Distinct().ToArray());
            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Instance = context.HttpContext.Request.Path
            };
            ProblemDetailsEnricher.Enrich(context.HttpContext, problem);
            return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        };
    });

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    await DbInitializer.MigrateAndSeedAsync(app.Services);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);

// Unknown routes / methods also get a ProblemDetails body.
app.UseStatusCodePages(async ctx =>
{
    var http = ctx.HttpContext;
    if (http.Response.ContentLength is > 0 || http.Response.ContentType is not null) return;
    var problem = new ProblemDetails
    {
        Status = http.Response.StatusCode,
        Title = ReasonPhrases(http.Response.StatusCode),
        Instance = http.Request.Path
    };
    ProblemDetailsEnricher.Enrich(http, problem);
    http.Response.ContentType = "application/problem+json";
    await http.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
});

app.MapControllers();

app.Run();

static string ReasonPhrases(int status) => Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status);
