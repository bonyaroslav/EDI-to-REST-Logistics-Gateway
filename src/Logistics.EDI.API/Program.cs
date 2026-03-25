using Logistics.EDI.Application.Contracts;
using Logistics.EDI.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructureServices();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "EDI 204 Load Tender Integration Engine",
    status = "Phase 1 baseline ready"
}));

app.MapPost("/api/v1/edi/translate-204", () =>
{
    ValidationErrorResponse response = new(
        Error: "NotImplemented",
        Message: "Phase 1 establishes the solution skeleton and locked contracts. Translation behavior starts in Phase 2.",
        Status: StatusCodes.Status501NotImplemented);

    return Results.Json(response, statusCode: StatusCodes.Status501NotImplemented);
})
.Accepts<string>("text/plain")
.Produces<LoadTenderResponse>(StatusCodes.Status200OK)
.Produces<ValidationErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ValidationErrorResponse>(StatusCodes.Status501NotImplemented);

app.Run();
