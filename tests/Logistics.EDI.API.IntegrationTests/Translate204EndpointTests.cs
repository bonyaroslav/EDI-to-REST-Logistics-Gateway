using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Logistics.EDI.Application.Contracts;
using Logistics.EDI.Domain.Abstractions;
using Logistics.EDI.Domain.Exceptions;
using Logistics.EDI.Domain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.EDI.API.IntegrationTests;

public sealed class Translate204EndpointTests
{
    [Fact]
    public async Task PostTextPlain_WithRealParser_ReturnsTranslatedResponse()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using StringContent content = new(SamplePayloads.ValidOriginalTender, Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        LoadTenderResponse? body = await response.Content.ReadFromJsonAsync<LoadTenderResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("0001", body.TransactionId);
        Assert.Equal("9999999", body.LoadNumber);
        Assert.Equal("XXXX", body.CarrierAlphaCode);
        Assert.Equal("Original", body.SetPurpose);
        Assert.Equal("2025-01-16T00:00:00.0000000Z", body.EstimatedDeliveryDate);
        Assert.Equal("DIGIS LOGISTICS", body.ShipperName);
        Assert.Collection(
            body.Stops,
            stop =>
            {
                Assert.Equal(1, stop.Sequence);
                Assert.Equal("Pickup", stop.Type);
                Assert.Equal("DIGIS LOGISTICS", stop.Name);
            },
            stop =>
            {
                Assert.Equal(2, stop.Sequence);
                Assert.Equal("Delivery", stop.Type);
                Assert.Equal("DESTINATION DC", stop.Name);
            });
        Assert.Equal("Success", body.Status);
    }

    [Fact]
    public async Task PostMalformedPayload_WithRealParser_ReturnsStructuredBadRequest()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using StringContent content = new(SamplePayloads.MalformedPayload, Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("EdiValidationException", body.Error);
        Assert.Equal("EDI payload is malformed or not a supported X12 document.", body.Message);
        Assert.Equal((int)HttpStatusCode.BadRequest, body.Status);
    }

    [Fact]
    public async Task PostTextPlain_WithStubParser_ReturnsTranslatedResponse()
    {
        await using TestWebApplicationFactory factory = new(new StubLoadTender204Parser(
            new ParsedLoadTenderDocument(
                TransactionId: "0001",
                LoadNumber: "9999999",
                CarrierAlphaCode: "XXXX",
                SetPurposeCode: "00",
                EstimatedDeliveryDate: new DateOnly(2025, 1, 16),
                ShipperName: "DIGIS LOGISTICS",
                Stops:
                [
                    new ParsedStop(1, "CL", "DIGIS LOGISTICS"),
                    new ParsedStop(2, "CU", "DESTINATION DC")
                ])));
        using HttpClient client = factory.CreateClient();

        using StringContent content = new("ISA*00*...~", Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        LoadTenderResponse? body = await response.Content.ReadFromJsonAsync<LoadTenderResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("0001", body.TransactionId);
        Assert.Equal("Original", body.SetPurpose);
        Assert.Collection(
            body.Stops,
            stop => Assert.Equal("Pickup", stop.Type),
            stop => Assert.Equal("Delivery", stop.Type));
    }

    [Fact]
    public async Task PostNonTextPlain_ReturnsUnsupportedMediaType()
    {
        await using TestWebApplicationFactory factory = new(new StubLoadTender204Parser(
            new ParsedLoadTenderDocument(null, null, null, null, null, null, Array.Empty<ParsedStop>())));
        using HttpClient client = factory.CreateClient();

        using StringContent content = new("{\"edi\":\"ISA*00*...~\"}", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("UnsupportedMediaType", body.Error);
        Assert.Equal("Content-Type must be text/plain.", body.Message);
        Assert.Equal((int)HttpStatusCode.UnsupportedMediaType, body.Status);
    }

    [Fact]
    public async Task PostInvalidNearMatchTextPlainContentType_ReturnsUnsupportedMediaType()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using ByteArrayContent content = new(Encoding.UTF8.GetBytes(SamplePayloads.ValidOriginalTender));
        content.Headers.TryAddWithoutValidation("Content-Type", "text/plain-invalid");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("UnsupportedMediaType", body.Error);
        Assert.Equal("Content-Type must be text/plain.", body.Message);
        Assert.Equal((int)HttpStatusCode.UnsupportedMediaType, body.Status);
    }

    [Fact]
    public async Task PostInvalidPayload_ReturnsStructuredBadRequest()
    {
        await using TestWebApplicationFactory factory = new(new ThrowingLoadTender204Parser(
            new EdiValidationException("Mandatory segment 'GS' is missing or malformed.")));
        using HttpClient client = factory.CreateClient();

        using StringContent content = new("ISA*00*...~", Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("EdiValidationException", body.Error);
        Assert.Equal("Mandatory segment 'GS' is missing or malformed.", body.Message);
        Assert.Equal((int)HttpStatusCode.BadRequest, body.Status);
    }

    [Fact]
    public async Task PostBlankPayload_ReturnsStructuredBadRequest()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using StringContent content = new(" ", Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("EdiValidationException", body.Error);
        Assert.Equal("EDI payload is required.", body.Message);
        Assert.Equal((int)HttpStatusCode.BadRequest, body.Status);
    }

    [Fact]
    public async Task PostUnsupportedTransactionSet_WithRealParser_ReturnsStructuredBadRequest()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using StringContent content = new(SamplePayloads.UnsupportedTransaction990, Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("EdiValidationException", body.Error);
        Assert.Equal("Only ASC X12 204 transactions are supported.", body.Message);
        Assert.Equal((int)HttpStatusCode.BadRequest, body.Status);
    }

    [Fact]
    public async Task PostPayloadWithoutDeliveryStop_WithRealParser_ReturnsStructuredBadRequest()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using StringContent content = new(SamplePayloads.MissingDeliveryStop, Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("EdiValidationException", body.Error);
        Assert.Equal("At least one pickup stop and one delivery stop are required.", body.Message);
        Assert.Equal((int)HttpStatusCode.BadRequest, body.Status);
    }

    [Fact]
    public async Task PostUnexpectedFailure_ReturnsStructuredInternalServerError()
    {
        await using TestWebApplicationFactory factory = new(new ThrowingLoadTender204Parser(new InvalidOperationException("boom")));
        using HttpClient client = factory.CreateClient();

        using StringContent content = new(SamplePayloads.ValidOriginalTender, Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        ValidationErrorResponse? body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("InternalServerError", body.Error);
        Assert.Equal("An unexpected error occurred while processing the EDI payload.", body.Message);
        Assert.Equal((int)HttpStatusCode.InternalServerError, body.Status);
    }

    [Fact]
    public async Task SwaggerDocument_AdvertisesTextPlainRequestBody()
    {
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement requestBody = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/edi/translate-204")
            .GetProperty("post")
            .GetProperty("requestBody");

        Assert.True(requestBody.GetProperty("required").GetBoolean());

        JsonElement textPlainContent = requestBody
            .GetProperty("content")
            .GetProperty("text/plain");

        Assert.Equal("string", textPlainContent.GetProperty("schema").GetProperty("type").GetString());
    }

    private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly ILoadTender204Parser _parser;

        public TestWebApplicationFactory(ILoadTender204Parser parser)
        {
            _parser = parser;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                ServiceDescriptor[] parserRegistrations = services
                    .Where(service => service.ServiceType == typeof(ILoadTender204Parser))
                    .ToArray();

                foreach (ServiceDescriptor registration in parserRegistrations)
                {
                    services.Remove(registration);
                }

                services.AddScoped(_ => _parser);
            });
        }
    }

    private sealed class StubLoadTender204Parser : ILoadTender204Parser
    {
        private readonly ParsedLoadTenderDocument _document;

        public StubLoadTender204Parser(ParsedLoadTenderDocument document)
        {
            _document = document;
        }

        public ParsedLoadTenderDocument Parse(string rawEdi)
        {
            return _document;
        }
    }

    private sealed class ThrowingLoadTender204Parser : ILoadTender204Parser
    {
        private readonly Exception _exception;

        public ThrowingLoadTender204Parser(Exception exception)
        {
            _exception = exception;
        }

        public ParsedLoadTenderDocument Parse(string rawEdi)
        {
            throw _exception;
        }
    }

    private sealed record ValidationErrorResponse(string Error, string Message, int Status);

    private static class SamplePayloads
    {
        public static string ValidOriginalTender => SampleFile.Read("valid-original-tender.edi");
        public static string MalformedPayload => SampleFile.Read("malformed-payload.edi");
        public static string UnsupportedTransaction990 => SampleFile.Read("unsupported-transaction-990.edi");
        public static string MissingDeliveryStop => SampleFile.Read("missing-delivery-stop.edi");
    }

    private static class SampleFile
    {
        public static string Read(string fileName)
        {
            string repositoryRoot = FindRepositoryRoot();
            string path = Path.Combine(repositoryRoot, "samples", "204", fileName);
            return File.ReadAllText(path).ReplaceLineEndings(string.Empty);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Logistics.EDI.Gateway.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the repository root.");
        }
    }
}
