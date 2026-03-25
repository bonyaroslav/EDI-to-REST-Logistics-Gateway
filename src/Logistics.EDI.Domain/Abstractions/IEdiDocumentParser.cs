namespace Logistics.EDI.Domain.Abstractions;

public interface IEdiDocumentParser<TDocument>
{
    TDocument Parse(string rawEdi);
}
