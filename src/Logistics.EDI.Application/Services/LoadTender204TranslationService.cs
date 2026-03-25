using Logistics.EDI.Application.Abstractions;
using Logistics.EDI.Application.Contracts;
using Logistics.EDI.Domain.Abstractions;
using Logistics.EDI.Domain.Exceptions;
using Logistics.EDI.Domain.Models;

namespace Logistics.EDI.Application.Services;

public sealed class LoadTender204TranslationService : ILoadTender204TranslationService
{
    private readonly ILoadTender204Parser _parser;

    public LoadTender204TranslationService(ILoadTender204Parser parser)
    {
        _parser = parser;
    }

    public LoadTenderResponse Translate(string rawEdi)
    {
        if (string.IsNullOrWhiteSpace(rawEdi))
        {
            throw new EdiValidationException("EDI payload is required.");
        }

        ParsedLoadTenderDocument document = _parser.Parse(rawEdi);

        string transactionId = RequireValue(document.TransactionId, "ST transaction identifier is missing or malformed.");
        string loadNumber = RequireValue(document.LoadNumber, "B2 load number is missing or malformed.");
        string carrierAlphaCode = RequireValue(document.CarrierAlphaCode, "B2 carrier alpha code is missing or malformed.");
        string setPurpose = MapSetPurpose(document.SetPurposeCode);
        IReadOnlyList<StopDto> stops = MapStops(document.Stops);

        EnsureContainsStopType(stops, "Pickup");
        EnsureContainsStopType(stops, "Delivery");

        return new LoadTenderResponse(
            TransactionId: transactionId,
            LoadNumber: loadNumber,
            CarrierAlphaCode: carrierAlphaCode,
            SetPurpose: setPurpose,
            EstimatedDeliveryDate: FormatEstimatedDeliveryDate(document.EstimatedDeliveryDate),
            ShipperName: NullIfWhiteSpace(document.ShipperName),
            Stops: stops,
            Status: "Success");
    }

    private static IReadOnlyList<StopDto> MapStops(IReadOnlyList<ParsedStop> stops)
    {
        if (stops.Count == 0)
        {
            throw new EdiValidationException("At least one pickup stop and one delivery stop are required.");
        }

        return stops
            .Select(stop => new StopDto(
                Sequence: stop.Sequence,
                Type: MapStopType(stop.TypeCode),
                Name: NullIfWhiteSpace(stop.Name),
                ScheduledDateTime: stop.ScheduledDateTime?.ToUniversalTime().ToString("O")))
            .ToArray();
    }

    private static string MapSetPurpose(string? setPurposeCode) => setPurposeCode switch
    {
        "00" => "Original",
        "01" => "Cancellation",
        "04" => "Change",
        null or "" => throw new EdiValidationException("B2A set purpose is missing or malformed."),
        _ => throw new EdiValidationException($"B2A set purpose code '{setPurposeCode}' is not supported for v1.")
    };

    private static string MapStopType(string? typeCode) => typeCode switch
    {
        "Pickup" => "Pickup",
        "Delivery" => "Delivery",
        "CL" => "Pickup",
        "CU" => "Delivery",
        null or "" => throw new EdiValidationException("Stop type is missing or malformed."),
        _ => throw new EdiValidationException($"Stop type code '{typeCode}' is not supported for v1.")
    };

    private static void EnsureContainsStopType(IReadOnlyList<StopDto> stops, string type)
    {
        if (!stops.Any(stop => string.Equals(stop.Type, type, StringComparison.Ordinal)))
        {
            throw new EdiValidationException("At least one pickup stop and one delivery stop are required.");
        }
    }

    private static string? FormatEstimatedDeliveryDate(DateOnly? estimatedDeliveryDate)
    {
        return estimatedDeliveryDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O");
    }

    private static string RequireValue(string? value, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new EdiValidationException(message)
            : value;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
