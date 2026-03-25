using Logistics.EDI.Domain.Exceptions;
using Logistics.EDI.Domain.Models;
using Logistics.EDI.Infrastructure.Parsing;

namespace Logistics.EDI.Infrastructure.Tests;

public sealed class LoadTender204ParserTests
{
    private readonly LoadTender204Parser _parser = new();

    [Fact]
    public void Parse_MapsValidMinimal204IntoCanonicalDocument()
    {
        ParsedLoadTenderDocument document = _parser.Parse(SamplePayloads.Valid204);

        Assert.Equal("0001", document.TransactionId);
        Assert.Equal("9999999", document.LoadNumber);
        Assert.Equal("XXXX", document.CarrierAlphaCode);
        Assert.Equal("00", document.SetPurposeCode);
        Assert.Equal(new DateOnly(2025, 1, 16), document.EstimatedDeliveryDate);
        Assert.Equal("DIGIS LOGISTICS", document.ShipperName);
        Assert.Collection(
            document.Stops,
            stop =>
            {
                Assert.Equal(1, stop.Sequence);
                Assert.Equal("CL", stop.TypeCode);
                Assert.Equal("DIGIS LOGISTICS", stop.Name);
                Assert.Null(stop.ScheduledDateTime);
            },
            stop =>
            {
                Assert.Equal(2, stop.Sequence);
                Assert.Equal("CU", stop.TypeCode);
                Assert.Equal("DESTINATION DC", stop.Name);
                Assert.Null(stop.ScheduledDateTime);
            });
    }

    [Fact]
    public void Parse_MissingGs_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MissingGs));

        Assert.Equal("Mandatory segment 'GS' is missing or malformed.", exception.Message);
    }

    [Fact]
    public void Parse_MissingSt_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MissingSt));

        Assert.Equal("Mandatory segment 'ST' is missing or malformed.", exception.Message);
    }

    [Fact]
    public void Parse_MissingB2_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MissingB2));

        Assert.Equal("Mandatory segment 'B2' is missing or malformed.", exception.Message);
    }

    [Fact]
    public void Parse_MissingB2A_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MissingB2A));

        Assert.Equal("Mandatory segment 'B2A' is missing or malformed.", exception.Message);
    }

    [Fact]
    public void Parse_MalformedPayload_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse("NOT-EDI"));

        Assert.Equal("EDI payload is malformed or not a supported X12 document.", exception.Message);
    }

    [Fact]
    public void Parse_UnsupportedTransactionSet_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.Non204));

        Assert.Equal("Only ASC X12 204 transactions are supported.", exception.Message);
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

        public const string MissingGs =
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~" +
            "ST*204*0001~" +
            "B2**XXXX*9999999**PO~" +
            "B2A*00~" +
            "SE*4*0001~" +
            "IEA*1*000000001~";

        public const string MissingSt =
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~" +
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~" +
            "B2**XXXX*9999999**PO~" +
            "B2A*00~" +
            "SE*4*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        public const string MissingB2 =
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~" +
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~" +
            "ST*204*0001~" +
            "B2A*00~" +
            "SE*4*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        public const string MissingB2A =
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~" +
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~" +
            "ST*204*0001~" +
            "B2**XXXX*9999999**PO~" +
            "SE*4*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        public const string Non204 =
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~" +
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~" +
            "ST*990*0001~" +
            "B2**XXXX*9999999**PO~" +
            "B2A*00~" +
            "SE*5*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";
    }
}
