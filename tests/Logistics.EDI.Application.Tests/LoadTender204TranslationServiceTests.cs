using Logistics.EDI.Application.Services;
using Logistics.EDI.Domain.Abstractions;
using Logistics.EDI.Domain.Exceptions;
using Logistics.EDI.Domain.Models;

namespace Logistics.EDI.Application.Tests;

public sealed class LoadTender204TranslationServiceTests
{
    [Fact]
    public void Translate_BlankPayload_ThrowsRequiredPayloadValidationException()
    {
        LoadTender204TranslationService service = new(new StubLoadTender204Parser(CreateValidDocument()));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate(" "));

        Assert.Equal("EDI payload is required.", exception.Message);
    }

    [Fact]
    public void Translate_MapsParsedDocumentIntoLockedResponseContract()
    {
        ParsedLoadTenderDocument document = new(
            TransactionId: "0001",
            LoadNumber: "9999999",
            CarrierAlphaCode: "XXXX",
            SetPurposeCode: "00",
            EstimatedDeliveryDate: new DateOnly(2025, 1, 16),
            ShipperName: "DIGIS LOGISTICS",
            Stops:
            [
                new ParsedStop(1, "CL", "DIGIS LOGISTICS", new DateTimeOffset(2025, 1, 15, 8, 30, 0, TimeSpan.Zero)),
                new ParsedStop(2, "CU", "DESTINATION DC", null)
            ]);

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        var response = service.Translate("ISA*...~");

        Assert.Equal("0001", response.TransactionId);
        Assert.Equal("9999999", response.LoadNumber);
        Assert.Equal("XXXX", response.CarrierAlphaCode);
        Assert.Equal("Original", response.SetPurpose);
        Assert.Equal("2025-01-16T00:00:00.0000000Z", response.EstimatedDeliveryDate);
        Assert.Equal("DIGIS LOGISTICS", response.ShipperName);
        Assert.Collection(
            response.Stops,
            stop =>
            {
                Assert.Equal(1, stop.Sequence);
                Assert.Equal("Pickup", stop.Type);
                Assert.Equal("DIGIS LOGISTICS", stop.Name);
                Assert.Equal("2025-01-15T08:30:00.0000000+00:00", stop.ScheduledDateTime);
            },
            stop =>
            {
                Assert.Equal(2, stop.Sequence);
                Assert.Equal("Delivery", stop.Type);
                Assert.Equal("DESTINATION DC", stop.Name);
                Assert.Null(stop.ScheduledDateTime);
            });
        Assert.Equal("Success", response.Status);
    }

    [Fact]
    public void Translate_RejectsPayloadWithoutBothPickupAndDeliveryStops()
    {
        ParsedLoadTenderDocument document = new(
            TransactionId: "0001",
            LoadNumber: "9999999",
            CarrierAlphaCode: "XXXX",
            SetPurposeCode: "00",
            EstimatedDeliveryDate: new DateOnly(2025, 1, 16),
            ShipperName: "DIGIS LOGISTICS",
            Stops:
            [
                new ParsedStop(1, "CL", "DIGIS LOGISTICS", null)
            ]);

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal("At least one pickup stop and one delivery stop are required.", exception.Message);
    }

    [Fact]
    public void Translate_RejectsUnsupportedSetPurposeCodes()
    {
        ParsedLoadTenderDocument document = new(
            TransactionId: "0001",
            LoadNumber: "9999999",
            CarrierAlphaCode: "XXXX",
            SetPurposeCode: "99",
            EstimatedDeliveryDate: null,
            ShipperName: "DIGIS LOGISTICS",
            Stops:
            [
                new ParsedStop(1, "Pickup", "DIGIS LOGISTICS", null),
                new ParsedStop(2, "Delivery", "DESTINATION DC", null)
            ]);

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal("B2A set purpose code '99' is not supported for v1.", exception.Message);
    }

    [Theory]
    [InlineData("00", "Original")]
    [InlineData("01", "Cancellation")]
    [InlineData("04", "Change")]
    public void Translate_MapsSupportedSetPurposeCodes(string setPurposeCode, string expectedSetPurpose)
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            SetPurposeCode = setPurposeCode
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        var response = service.Translate("ISA*...~");

        Assert.Equal(expectedSetPurpose, response.SetPurpose);
    }

    [Theory]
    [InlineData(null, "ST transaction identifier is missing or malformed.")]
    [InlineData("", "ST transaction identifier is missing or malformed.")]
    [InlineData(" ", "ST transaction identifier is missing or malformed.")]
    public void Translate_RejectsMissingTransactionId(string? transactionId, string expectedMessage)
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            TransactionId = transactionId
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData(null, "B2 load number is missing or malformed.")]
    [InlineData("", "B2 load number is missing or malformed.")]
    [InlineData(" ", "B2 load number is missing or malformed.")]
    public void Translate_RejectsMissingLoadNumber(string? loadNumber, string expectedMessage)
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            LoadNumber = loadNumber
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData(null, "B2 carrier alpha code is missing or malformed.")]
    [InlineData("", "B2 carrier alpha code is missing or malformed.")]
    [InlineData(" ", "B2 carrier alpha code is missing or malformed.")]
    public void Translate_RejectsMissingCarrierAlphaCode(string? carrierAlphaCode, string expectedMessage)
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            CarrierAlphaCode = carrierAlphaCode
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Translate_RejectsMissingSetPurposeCode(string? setPurposeCode)
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            SetPurposeCode = setPurposeCode
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal("B2A set purpose is missing or malformed.", exception.Message);
    }

    [Fact]
    public void Translate_NormalizesBlankOptionalValuesToNull()
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            ShipperName = " ",
            Stops =
            [
                new ParsedStop(1, "Pickup", " ", null),
                new ParsedStop(2, "Delivery", "", null)
            ]
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        var response = service.Translate("ISA*...~");

        Assert.Null(response.ShipperName);
        Assert.Collection(
            response.Stops,
            stop =>
            {
                Assert.Equal("Pickup", stop.Type);
                Assert.Null(stop.Name);
            },
            stop =>
            {
                Assert.Equal("Delivery", stop.Type);
                Assert.Null(stop.Name);
            });
    }

    [Fact]
    public void Translate_FormatsEstimatedDeliveryDateAsUtcMidnightIso8601()
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            EstimatedDeliveryDate = new DateOnly(2025, 1, 16)
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        var response = service.Translate("ISA*...~");

        Assert.Equal("2025-01-16T00:00:00.0000000Z", response.EstimatedDeliveryDate);
    }

    [Theory]
    [InlineData("CL", "Pickup")]
    [InlineData("CU", "Delivery")]
    [InlineData("Pickup", "Pickup")]
    [InlineData("Delivery", "Delivery")]
    public void Translate_MapsSupportedStopTypeCodes(string stopTypeCode, string expectedStopType)
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            Stops =
            [
                new ParsedStop(1, stopTypeCode, "STOP 1", null),
                new ParsedStop(2, stopTypeCode == "CL" || stopTypeCode == "Pickup" ? "CU" : "CL", "STOP 2", null)
            ]
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        var response = service.Translate("ISA*...~");

        Assert.Equal(expectedStopType, response.Stops[0].Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Translate_RejectsMissingStopType(string? stopTypeCode)
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            Stops =
            [
                new ParsedStop(1, stopTypeCode, "DIGIS LOGISTICS", null),
                new ParsedStop(2, "CU", "DESTINATION DC", null)
            ]
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal("Stop type is missing or malformed.", exception.Message);
    }

    [Fact]
    public void Translate_RejectsUnsupportedStopTypeCode()
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            Stops =
            [
                new ParsedStop(1, "ZZ", "DIGIS LOGISTICS", null),
                new ParsedStop(2, "CU", "DESTINATION DC", null)
            ]
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal("Stop type code 'ZZ' is not supported for v1.", exception.Message);
    }

    [Fact]
    public void Translate_RejectsPayloadWithoutAnyStops()
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            Stops = Array.Empty<ParsedStop>()
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal("At least one pickup stop and one delivery stop are required.", exception.Message);
    }

    [Fact]
    public void Translate_RejectsPayloadWithoutPickupStop()
    {
        ParsedLoadTenderDocument document = CreateValidDocument() with
        {
            Stops =
            [
                new ParsedStop(1, "CU", "DESTINATION DC", null)
            ]
        };

        LoadTender204TranslationService service = new(new StubLoadTender204Parser(document));

        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => service.Translate("ISA*...~"));

        Assert.Equal("At least one pickup stop and one delivery stop are required.", exception.Message);
    }

    private static ParsedLoadTenderDocument CreateValidDocument()
    {
        return new ParsedLoadTenderDocument(
            TransactionId: "0001",
            LoadNumber: "9999999",
            CarrierAlphaCode: "XXXX",
            SetPurposeCode: "00",
            EstimatedDeliveryDate: new DateOnly(2025, 1, 16),
            ShipperName: "DIGIS LOGISTICS",
            Stops:
            [
                new ParsedStop(1, "Pickup", "DIGIS LOGISTICS", new DateTimeOffset(2025, 1, 15, 8, 30, 0, TimeSpan.Zero)),
                new ParsedStop(2, "Delivery", "DESTINATION DC", null)
            ]);
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
}
