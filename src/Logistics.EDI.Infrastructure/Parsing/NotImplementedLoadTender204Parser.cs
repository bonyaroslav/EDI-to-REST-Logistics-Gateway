using Logistics.EDI.Domain.Abstractions;
using Logistics.EDI.Domain.Models;

namespace Logistics.EDI.Infrastructure.Parsing;

public sealed class NotImplementedLoadTender204Parser : ILoadTender204Parser
{
    public ParsedLoadTenderDocument Parse(string rawEdi)
    {
        throw new NotImplementedException("204 parsing is not implemented yet.");
    }
}
