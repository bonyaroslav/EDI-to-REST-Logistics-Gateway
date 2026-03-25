using System.Text;
using Logistics.EDI.Application.Abstractions;
using Logistics.EDI.Application.Contracts;
using Logistics.EDI.Application.Services;
using Logistics.EDI.Domain.Exceptions;
using Logistics.EDI.Infrastructure.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<TextPlainRequestBodyOperationFilter>();
});
builder.Services.AddScoped<ILoadTender204TranslationService, LoadTender204TranslationService>();
builder.Services.AddInfrastructureServices();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        ValidationErrorResponse response = new(
            Error: "InternalServerError",
            Message: "An unexpected error occurred while processing the EDI payload.",
            Status: StatusCodes.Status500InternalServerError);

        await context.Response.WriteAsJsonAsync(response);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new
{
    name = "EDI 204 Load Tender Integration Engine",
    status = "Phase 6 portfolio-ready demo"
}));

app.MapPost(ApiContracts.Translate204Route, async (HttpRequest request, ILoadTender204TranslationService translator) =>
{
    if (!request.HasTextPlainContentType())
    {
        return Results.Json(
            new ValidationErrorResponse(
                Error: "UnsupportedMediaType",
                Message: "Content-Type must be text/plain.",
                Status: StatusCodes.Status415UnsupportedMediaType),
            statusCode: StatusCodes.Status415UnsupportedMediaType);
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
        return Results.Json(
            new ValidationErrorResponse(
                Error: nameof(EdiValidationException),
                Message: exception.Message,
                Status: StatusCodes.Status400BadRequest),
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.Produces<LoadTenderResponse>(StatusCodes.Status200OK)
.Produces<ValidationErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ValidationErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
.Produces<ValidationErrorResponse>(StatusCodes.Status500InternalServerError)
.WithName(ApiContracts.Translate204EndpointName);

app.Run();

public partial class Program;

internal static class HttpRequestExtensions
{
    public static bool HasTextPlainContentType(this HttpRequest request)
    {
        return MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue? mediaType)
            && mediaType.MediaType.HasValue
            && string.Equals(mediaType.MediaType.Value, "text/plain", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class TextPlainRequestBodyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!IsTranslate204Operation(context))
        {
            return;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["text/plain"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "Raw ASC X12 204 payload."
                    }
                }
            }
        };
    }

    private static bool IsTranslate204Operation(OperationFilterContext context)
    {
        return string.Equals(context.ApiDescription.RelativePath, ApiContracts.Translate204RelativePath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(context.ApiDescription.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase);
    }
}

internal static class ApiContracts
{
    public const string Translate204Route = "/api/v1/edi/translate-204";
    public const string Translate204RelativePath = "api/v1/edi/translate-204";
    public const string Translate204EndpointName = "TranslateLoadTender204";
}
