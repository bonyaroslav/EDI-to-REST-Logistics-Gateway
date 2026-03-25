using Logistics.EDI.Domain.Models;

namespace Logistics.EDI.Domain.Abstractions;

/// <summary>
/// Parses a raw ASC X12 204 payload into the application-safe document model.
/// Infrastructure may omit missing values with <see langword="null"/>, but it must not
/// expose parser-specific types outside the boundary.
/// </summary>
public interface ILoadTender204Parser
{
    ParsedLoadTenderDocument Parse(string rawEdi);
}
