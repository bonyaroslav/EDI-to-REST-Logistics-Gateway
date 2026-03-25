using System.Text;
using Logistics.EDI.Application.Abstractions;
using Logistics.EDI.Application.Contracts;
using Logistics.EDI.Application.Services;
using Logistics.EDI.Domain.Exceptions;
using Logistics.EDI.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ILoadTender204TranslationService, LoadTender204TranslationService>();
builder.Services.AddInfrastructureServices();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "EDI 204 Load Tender Integration Engine",
    status = "Phase 1 baseline ready"
}));

app.MapPost("/api/v1/edi/translate-204", async (HttpRequest request, ILoadTender204TranslationService translator) =>
{
    if (!request.HasTextPlainContentType())
    {
        ValidationErrorResponse unsupportedMediaTypeResponse = new(
            Error: "UnsupportedMediaType",
            Message: "Content-Type must be text/plain.",
            Status: StatusCodes.Status415UnsupportedMediaType);

        return Results.Json(unsupportedMediaTypeResponse, statusCode: StatusCodes.Status415UnsupportedMediaType);
    }

    using StreamReader reader = new(request.Body, Encoding.UTF8);
    string rawEdi = await reader.ReadToEndAsync();

    try
    {
        LoadTenderResponse response = translator.Translate(rawEdi);
        return Results.Ok(response);
    }
    catch (EdiValidationException exception)
    {
        ValidationErrorResponse response = new(
            Error: nameof(EdiValidationException),
            Message: exception.Message,
            Status: StatusCodes.Status400BadRequest);

        return Results.Json(response, statusCode: StatusCodes.Status400BadRequest);
    }
    catch (NotImplementedException exception)
    {
        ValidationErrorResponse response = new(
            Error: "NotImplemented",
            Message: exception.Message,
            Status: StatusCodes.Status501NotImplemented);

        return Results.Json(response, statusCode: StatusCodes.Status501NotImplemented);
    }
})
.Accepts<string>("text/plain")
.Produces<LoadTenderResponse>(StatusCodes.Status200OK)
.Produces<ValidationErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ValidationErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
.Produces<ValidationErrorResponse>(StatusCodes.Status501NotImplemented);

app.Run();

public partial class Program;

internal static class HttpRequestExtensions
{
    public static bool HasTextPlainContentType(this HttpRequest request)
    {
        return request.ContentType?.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase) == true;
    }
}
