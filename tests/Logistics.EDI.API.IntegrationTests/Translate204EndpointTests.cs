using System.Net;
using System.Text;
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

        using StringContent content = new(SamplePayloads.Valid204, Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"transactionId\":\"0001\"", body);
        Assert.Contains("\"loadNumber\":\"9999999\"", body);
        Assert.Contains("\"setPurpose\":\"Original\"", body);
        Assert.Contains("\"shipperName\":\"DIGIS LOGISTICS\"", body);
        Assert.Contains("\"type\":\"Pickup\"", body);
        Assert.Contains("\"type\":\"Delivery\"", body);
    }

    [Fact]
    public async Task PostMalformedPayload_WithRealParser_ReturnsStructuredBadRequest()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using StringContent content = new("NOT-EDI", Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"error\":\"EdiValidationException\"", body);
        Assert.Contains("\"message\":\"EDI payload is malformed or not a supported X12 document.\"", body);
    }

    [Fact]
    public async Task PostTextPlain_ReturnsTranslatedResponse()
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
                    new ParsedStop(1, "CL", "DIGIS LOGISTICS", null),
                    new ParsedStop(2, "CU", "DESTINATION DC", null)
                ])));
        using HttpClient client = factory.CreateClient();

        using StringContent content = new("ISA*00*...~", Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"transactionId\":\"0001\"", body);
        Assert.Contains("\"setPurpose\":\"Original\"", body);
        Assert.Contains("\"type\":\"Pickup\"", body);
        Assert.Contains("\"type\":\"Delivery\"", body);
    }

    [Fact]
    public async Task PostNonTextPlain_ReturnsUnsupportedMediaType()
    {
        await using TestWebApplicationFactory factory = new(new StubLoadTender204Parser(
            new ParsedLoadTenderDocument(null, null, null, null, null, null, Array.Empty<ParsedStop>())));
        using HttpClient client = factory.CreateClient();

        using StringContent content = new("{\"edi\":\"ISA*00*...~\"}", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.True(string.IsNullOrEmpty(body) || body.Contains("\"error\":\"UnsupportedMediaType\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PostInvalidPayload_ReturnsStructuredBadRequest()
    {
        await using TestWebApplicationFactory factory = new(new ThrowingLoadTender204Parser(
            new EdiValidationException("Mandatory segment 'GS' is missing or malformed.")));
        using HttpClient client = factory.CreateClient();

        using StringContent content = new("ISA*00*...~", Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await client.PostAsync("/api/v1/edi/translate-204", content);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"error\":\"EdiValidationException\"", body);
        Assert.Contains("\"message\":\"Mandatory segment 'GS' is missing or malformed.\"", body);
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

    private static class SamplePayloads
    {
        public const string Valid204 =
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~" +
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~" +
            "ST*204*0001~" +
            "B2**XXXX*9999999**PO~" +
            "B2A*00~" +
            "G62*37*20250116~" +
            "N1*SH*DIGIS LOGISTICS~" +
            "S5*1*CL~" +
            "N1*SF*DIGIS LOGISTICS~" +
            "S5*2*CU~" +
            "N1*ST*DESTINATION DC~" +
            "SE*10*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";
    }
}
