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
            },
            stop =>
            {
                Assert.Equal(2, stop.Sequence);
                Assert.Equal("CU", stop.TypeCode);
                Assert.Equal("DESTINATION DC", stop.Name);
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
    public void Parse_MissingSe_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MissingSe));

        Assert.Equal("Mandatory segment 'SE' is missing or malformed.", exception.Message);
    }

    [Fact]
    public void Parse_MissingGe_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MissingGe));

        Assert.Equal("Mandatory segment 'GE' is missing or malformed.", exception.Message);
    }

    [Fact]
    public void Parse_MissingIea_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MissingIea));

        Assert.Equal("Mandatory segment 'IEA' is missing or malformed.", exception.Message);
    }

    [Fact]
    public void Parse_MismatchedTransactionSetControlNumbers_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MismatchedStAndSe));

        Assert.Equal("ST02 and SE02 control numbers must match.", exception.Message);
    }

    [Fact]
    public void Parse_MultipleTransactionSetsInFunctionalGroup_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MultipleTransactionSetsInGroup));

        Assert.Equal("GE01 must be '1' because only one transaction set is supported.", exception.Message);
    }

    [Fact]
    public void Parse_MultipleFunctionalGroupsInInterchange_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MultipleFunctionalGroupsInInterchange));

        Assert.Equal("IEA01 must be '1' because only one functional group is supported.", exception.Message);
    }

    [Fact]
    public void Parse_MalformedPayload_ThrowsPredictableValidationException()
    {
        EdiValidationException exception = Assert.Throws<EdiValidationException>(() => _parser.Parse(SamplePayloads.MalformedPayload));

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
        public static string Valid204 => SampleFile.Read("valid-original-tender.edi");
        public static string MissingGs => SampleFile.Read("missing-gs.edi");
        public static string MissingSt => string.Concat(
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~",
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~",
            "B2**XXXX*9999999**PO~",
            "B2A*00~",
            "SE*4*0001~",
            "GE*1*1~",
            "IEA*1*000000001~");
        public static string MissingB2 => string.Concat(
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~",
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~",
            "ST*204*0001~",
            "B2A*00~",
            "SE*4*0001~",
            "GE*1*1~",
            "IEA*1*000000001~");
        public static string MissingB2A => string.Concat(
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~",
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~",
            "ST*204*0001~",
            "B2**XXXX*9999999**PO~",
            "SE*4*0001~",
            "GE*1*1~",
            "IEA*1*000000001~");
        public static string MissingSe => string.Concat(
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~",
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~",
            "ST*204*0001~",
            "B2**XXXX*9999999**PO~",
            "B2A*00~",
            "GE*1*1~",
            "IEA*1*000000001~");
        public static string MissingGe => string.Concat(
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~",
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~",
            "ST*204*0001~",
            "B2**XXXX*9999999**PO~",
            "B2A*00~",
            "SE*5*0001~",
            "IEA*1*000000001~");
        public static string MissingIea => string.Concat(
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~",
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~",
            "ST*204*0001~",
            "B2**XXXX*9999999**PO~",
            "B2A*00~",
            "SE*5*0001~",
            "GE*1*1~");
        public static string MismatchedStAndSe => string.Concat(
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~",
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~",
            "ST*204*0001~",
            "B2**XXXX*9999999**PO~",
            "B2A*00~",
            "SE*5*9999~",
            "GE*1*1~",
            "IEA*1*000000001~");
        public static string MultipleTransactionSetsInGroup => string.Concat(
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~",
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~",
            "ST*204*0001~",
            "B2**XXXX*9999999**PO~",
            "B2A*00~",
            "SE*5*0001~",
            "GE*2*1~",
            "IEA*1*000000001~");
        public static string MultipleFunctionalGroupsInInterchange => string.Concat(
            "ISA*00*          *00*          *ZZ*SENDERID       *ZZ*RECEIVERID     *250116*1230*U*00401*000000001*0*P*>~",
            "GS*SM*SENDERID*RECEIVERID*20250116*1230*1*X*004010~",
            "ST*204*0001~",
            "B2**XXXX*9999999**PO~",
            "B2A*00~",
            "SE*5*0001~",
            "GE*1*1~",
            "IEA*2*000000001~");
        public static string Non204 => SampleFile.Read("unsupported-transaction-990.edi");
        public static string MalformedPayload => SampleFile.Read("malformed-payload.edi");
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
