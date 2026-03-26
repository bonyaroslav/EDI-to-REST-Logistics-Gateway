using System.Globalization;
using Logistics.EDI.Domain.Abstractions;
using Logistics.EDI.Domain.Exceptions;
using Logistics.EDI.Domain.Models;
using indice.Edi;

namespace Logistics.EDI.Infrastructure.Parsing;

public sealed class LoadTender204Parser : ILoadTender204Parser
{
    private const string MalformedPayloadMessage = "EDI payload is malformed or not a supported X12 document.";
    private const string MissingGsMessage = "Mandatory segment 'GS' is missing or malformed.";
    private const string MissingStMessage = "Mandatory segment 'ST' is missing or malformed.";
    private const string MissingB2Message = "Mandatory segment 'B2' is missing or malformed.";
    private const string MissingB2AMessage = "Mandatory segment 'B2A' is missing or malformed.";
    private const string MissingSeMessage = "Mandatory segment 'SE' is missing or malformed.";
    private const string MissingGeMessage = "Mandatory segment 'GE' is missing or malformed.";
    private const string MissingIeaMessage = "Mandatory segment 'IEA' is missing or malformed.";
    private const string UnsupportedTransactionSetMessage = "Only ASC X12 204 transactions are supported.";
    private const string MismatchedTransactionSetControlNumbersMessage = "ST02 and SE02 control numbers must match.";
    private const string MultipleTransactionSetsNotSupportedMessage = "GE01 must be '1' because only one transaction set is supported.";
    private const string MultipleFunctionalGroupsNotSupportedMessage = "IEA01 must be '1' because only one functional group is supported.";
    private static readonly string[] SupportedEstimatedDeliveryDateQualifiers = ["37"];

    public ParsedLoadTenderDocument Parse(string rawEdi)
    {
        try
        {
            InternalLoadTender204Document document = Deserialize(rawEdi);

            InternalStSegment st = Require(document.TransactionSetHeader, MissingStMessage);
            if (!string.Equals(st.TransactionSetIdentifierCode, "204", StringComparison.Ordinal))
            {
                throw new EdiValidationException(UnsupportedTransactionSetMessage);
            }

            InternalB2Segment b2 = Require(document.BeginningSegment, MissingB2Message);
            InternalB2ASegment b2A = Require(document.SetPurposeSegment, MissingB2AMessage);
            Require(document.FunctionalGroupHeader, MissingGsMessage);
            InternalSeSegment se = Require(document.TransactionSetTrailer, MissingSeMessage);
            InternalGeSegment ge = Require(document.FunctionalGroupTrailer, MissingGeMessage);
            InternalIeaSegment iea = Require(document.InterchangeTrailer, MissingIeaMessage);

            ValidateTransactionSetTrailer(st, se);
            ValidateSingleTransactionEnvelope(ge, iea);

            return new ParsedLoadTenderDocument(
                TransactionId: st.TransactionSetControlNumber,
                LoadNumber: b2.LoadNumber,
                CarrierAlphaCode: b2.StandardCarrierAlphaCode,
                SetPurposeCode: b2A.SetPurposeCode,
                EstimatedDeliveryDate: ParseEstimatedDeliveryDate(document.DateSegments),
                ShipperName: document.Parties
                    .FirstOrDefault(party => string.Equals(party.EntityIdentifierCode, "SH", StringComparison.Ordinal))
                    ?.Name,
                Stops: MapStops(document.StopLoops));
        }
        catch (EdiValidationException)
        {
            throw;
        }
        catch (EdiException exception)
        {
            throw new EdiValidationException(MalformedPayloadMessage, exception);
        }
        catch (FormatException exception)
        {
            throw new EdiValidationException(MalformedPayloadMessage, exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new EdiValidationException(MalformedPayloadMessage, exception);
        }
    }

    private InternalLoadTender204Document Deserialize(string rawEdi)
    {
        using StringReader textReader = new(rawEdi);
        using EdiTextReader reader = new(textReader, EdiGrammar.NewX12())
        {
            SuppressBadEscapeSequenceErrors = false
        };

        // Deserialize into a token stream first so Infrastructure can keep a narrow,
        // parser-agnostic document shape and own only structural validation.
        List<RawSegment> segments = ReadSegments(reader);

        return MapSegments(segments);
    }

    private static List<RawSegment> ReadSegments(EdiTextReader reader)
    {
        List<RawSegment> segments = [];
        string? segmentName = null;
        List<string?>? elements = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case EdiToken.SegmentName:
                    if (segmentName is not null && elements is not null)
                    {
                        segments.Add(new RawSegment(segmentName, elements.ToArray()));
                    }

                    segmentName = reader.Value?.ToString();
                    elements = [];
                    break;

                case EdiToken.String:
                case EdiToken.Null:
                    if (segmentName is null || elements is null || !TryParsePath(reader.Path, out int elementIndex, out int componentIndex) || componentIndex != 0)
                    {
                        continue;
                    }

                    EnsureCapacity(elements, elementIndex + 1);
                    elements[elementIndex] = reader.TokenType == EdiToken.Null
                        ? null
                        : NullIfWhiteSpace(reader.Value?.ToString());
                    break;
            }
        }

        if (segmentName is not null && elements is not null)
        {
            segments.Add(new RawSegment(segmentName, elements.ToArray()));
        }

        if (segments.Count == 0)
        {
            throw new EdiValidationException(MalformedPayloadMessage);
        }

        return segments;
    }

    private static InternalLoadTender204Document MapSegments(IReadOnlyList<RawSegment> segments)
    {
        InternalGsSegment? gs = null;
        InternalStSegment? st = null;
        InternalB2Segment? b2 = null;
        InternalB2ASegment? b2A = null;
        InternalSeSegment? se = null;
        InternalGeSegment? ge = null;
        InternalIeaSegment? iea = null;
        List<InternalG62Segment> dateSegments = [];
        List<InternalN1Segment> parties = [];
        List<InternalStopLoop> stopLoops = [];
        InternalStopLoop? currentStopLoop = null;

        foreach (RawSegment segment in segments)
        {
            switch (segment.Name)
            {
                case "GS":
                    gs ??= ToGsSegment(segment);
                    break;

                case "ST":
                    st ??= ToStSegment(segment);
                    break;

                case "B2":
                    b2 ??= ToB2Segment(segment);
                    break;

                case "B2A":
                    b2A ??= ToB2ASegment(segment);
                    break;

                case "G62":
                    dateSegments.Add(ToG62Segment(segment));
                    break;

                case "N1":
                {
                    InternalN1Segment party = ToN1Segment(segment);
                    if (currentStopLoop is null)
                    {
                        parties.Add(party);
                    }
                    else
                    {
                        currentStopLoop = currentStopLoop with
                        {
                            Parties = currentStopLoop.Parties.Concat([party]).ToArray()
                        };

                        stopLoops[^1] = currentStopLoop;
                    }

                    break;
                }

                case "S5":
                    currentStopLoop = new InternalStopLoop(ToS5Segment(segment), []);
                    stopLoops.Add(currentStopLoop);
                    break;

                case "SE":
                    se ??= ToSeSegment(segment);
                    currentStopLoop = null;
                    break;

                case "GE":
                    ge ??= ToGeSegment(segment);
                    currentStopLoop = null;
                    break;

                case "IEA":
                    iea ??= ToIeaSegment(segment);
                    currentStopLoop = null;
                    break;
            }
        }

        return new InternalLoadTender204Document(
            FunctionalGroupHeader: gs,
            TransactionSetHeader: st,
            BeginningSegment: b2,
            SetPurposeSegment: b2A,
            DateSegments: dateSegments,
            Parties: parties,
            StopLoops: stopLoops,
            TransactionSetTrailer: se,
            FunctionalGroupTrailer: ge,
            InterchangeTrailer: iea);
    }

    private static IReadOnlyList<ParsedStop> MapStops(IReadOnlyList<InternalStopLoop> stopLoops)
    {
        return stopLoops
            .Select(loop => new ParsedStop(
                Sequence: ParseRequiredInt(loop.Stop.SequenceNumber, "S5 stop sequence is missing or malformed."),
                TypeCode: loop.Stop.StopReasonCode,
                Name: loop.Parties.Select(party => party.Name).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))))
            .ToArray();
    }

    private static void ValidateTransactionSetTrailer(InternalStSegment st, InternalSeSegment se)
    {
        string? stControlNumber = NullIfWhiteSpace(st.TransactionSetControlNumber);
        string? seControlNumber = NullIfWhiteSpace(se.TransactionSetControlNumber);

        if (string.IsNullOrWhiteSpace(seControlNumber))
        {
            throw new EdiValidationException(MissingSeMessage);
        }

        if (string.IsNullOrWhiteSpace(stControlNumber) || !string.Equals(stControlNumber, seControlNumber, StringComparison.Ordinal))
        {
            throw new EdiValidationException(MismatchedTransactionSetControlNumbersMessage);
        }
    }

    private static void ValidateSingleTransactionEnvelope(InternalGeSegment ge, InternalIeaSegment iea)
    {
        if (ParseRequiredInt(ge.NumberOfTransactionSetsIncluded, MissingGeMessage) != 1)
        {
            throw new EdiValidationException(MultipleTransactionSetsNotSupportedMessage);
        }

        if (ParseRequiredInt(iea.NumberOfIncludedGroups, MissingIeaMessage) != 1)
        {
            throw new EdiValidationException(MultipleFunctionalGroupsNotSupportedMessage);
        }
    }

    private static DateOnly? ParseEstimatedDeliveryDate(IReadOnlyList<InternalG62Segment> segments)
    {
        foreach (InternalG62Segment segment in segments)
        {
            if (!SupportedEstimatedDeliveryDateQualifiers.Contains(segment.DateQualifier, StringComparer.Ordinal))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(segment.Date))
            {
                return null;
            }

            if (!DateOnly.TryParseExact(segment.Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedDate))
            {
                throw new EdiValidationException("G62 date is missing or malformed.");
            }

            return parsedDate;
        }

        return null;
    }

    private static InternalGsSegment ToGsSegment(RawSegment segment) => new(
        FunctionalIdentifierCode: GetElement(segment, 0),
        ApplicationSenderCode: GetElement(segment, 1),
        ApplicationReceiverCode: GetElement(segment, 2));

    private static InternalStSegment ToStSegment(RawSegment segment) => new(
        TransactionSetIdentifierCode: GetElement(segment, 0),
        TransactionSetControlNumber: GetElement(segment, 1));

    private static InternalB2Segment ToB2Segment(RawSegment segment) => new(
        StandardCarrierAlphaCode: GetElement(segment, 1),
        LoadNumber: GetElement(segment, 2));

    private static InternalB2ASegment ToB2ASegment(RawSegment segment) => new(
        SetPurposeCode: GetElement(segment, 0));

    private static InternalG62Segment ToG62Segment(RawSegment segment) => new(
        DateQualifier: GetElement(segment, 0),
        Date: GetElement(segment, 1));

    private static InternalN1Segment ToN1Segment(RawSegment segment) => new(
        EntityIdentifierCode: GetElement(segment, 0),
        Name: GetElement(segment, 1));

    private static InternalS5Segment ToS5Segment(RawSegment segment) => new(
        SequenceNumber: GetElement(segment, 0),
        StopReasonCode: GetElement(segment, 1));

    private static InternalSeSegment ToSeSegment(RawSegment segment) => new(
        SegmentCount: GetElement(segment, 0),
        TransactionSetControlNumber: GetElement(segment, 1));

    private static InternalGeSegment ToGeSegment(RawSegment segment) => new(
        NumberOfTransactionSetsIncluded: GetElement(segment, 0),
        GroupControlNumber: GetElement(segment, 1));

    private static InternalIeaSegment ToIeaSegment(RawSegment segment) => new(
        NumberOfIncludedGroups: GetElement(segment, 0),
        InterchangeControlNumber: GetElement(segment, 1));

    private static string? GetElement(RawSegment segment, int index) =>
        index < segment.Elements.Count ? segment.Elements[index] : null;

    private static T Require<T>(T? value, string message)
        where T : class =>
        value ?? throw new EdiValidationException(message);

    private static int ParseRequiredInt(string? value, string message)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
        {
            throw new EdiValidationException(message);
        }

        return parsed;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryParsePath(string? path, out int elementIndex, out int componentIndex)
    {
        elementIndex = -1;
        componentIndex = -1;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        int firstStart = path.IndexOf('[');
        int firstEnd = path.IndexOf(']', firstStart + 1);
        int secondStart = path.IndexOf('[', firstEnd + 1);
        int secondEnd = path.IndexOf(']', secondStart + 1);

        if (firstStart < 0 || firstEnd < 0 || secondStart < 0 || secondEnd < 0)
        {
            return false;
        }

        return int.TryParse(path.AsSpan(firstStart + 1, firstEnd - firstStart - 1), NumberStyles.None, CultureInfo.InvariantCulture, out elementIndex)
            && int.TryParse(path.AsSpan(secondStart + 1, secondEnd - secondStart - 1), NumberStyles.None, CultureInfo.InvariantCulture, out componentIndex);
    }

    private static void EnsureCapacity(List<string?> elements, int requiredCount)
    {
        while (elements.Count < requiredCount)
        {
            elements.Add(null);
        }
    }

    private sealed record RawSegment(string Name, IReadOnlyList<string?> Elements);
}

internal sealed record InternalLoadTender204Document(
    InternalGsSegment? FunctionalGroupHeader,
    InternalStSegment? TransactionSetHeader,
    InternalB2Segment? BeginningSegment,
    InternalB2ASegment? SetPurposeSegment,
    IReadOnlyList<InternalG62Segment> DateSegments,
    IReadOnlyList<InternalN1Segment> Parties,
    IReadOnlyList<InternalStopLoop> StopLoops,
    InternalSeSegment? TransactionSetTrailer,
    InternalGeSegment? FunctionalGroupTrailer,
    InternalIeaSegment? InterchangeTrailer);

internal sealed record InternalGsSegment(
    string? FunctionalIdentifierCode,
    string? ApplicationSenderCode,
    string? ApplicationReceiverCode);

internal sealed record InternalStSegment(
    string? TransactionSetIdentifierCode,
    string? TransactionSetControlNumber);

internal sealed record InternalB2Segment(
    string? StandardCarrierAlphaCode,
    string? LoadNumber);

internal sealed record InternalB2ASegment(string? SetPurposeCode);

internal sealed record InternalG62Segment(
    string? DateQualifier,
    string? Date);

internal sealed record InternalN1Segment(
    string? EntityIdentifierCode,
    string? Name);

internal sealed record InternalS5Segment(
    string? SequenceNumber,
    string? StopReasonCode);

internal sealed record InternalStopLoop(
    InternalS5Segment Stop,
    IReadOnlyList<InternalN1Segment> Parties);

internal sealed record InternalSeSegment(
    string? SegmentCount,
    string? TransactionSetControlNumber);

internal sealed record InternalGeSegment(
    string? NumberOfTransactionSetsIncluded,
    string? GroupControlNumber);

internal sealed record InternalIeaSegment(
    string? NumberOfIncludedGroups,
    string? InterchangeControlNumber);
