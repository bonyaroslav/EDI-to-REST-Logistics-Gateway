using Logistics.EDI.Application.Contracts;

namespace Logistics.EDI.Application.Abstractions;

/// <summary>
/// Application entry point for translating a raw 204 payload into the locked API response.
/// </summary>
public interface ILoadTender204TranslationService
{
    LoadTenderResponse Translate(string rawEdi);
}
