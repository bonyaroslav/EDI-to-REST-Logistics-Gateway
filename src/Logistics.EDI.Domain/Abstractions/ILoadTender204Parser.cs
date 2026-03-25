using Logistics.EDI.Domain.Models;

namespace Logistics.EDI.Domain.Abstractions;

public interface ILoadTender204Parser
{
    ParsedLoadTenderDocument Parse(string rawEdi);
}
