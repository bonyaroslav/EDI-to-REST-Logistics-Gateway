using Logistics.EDI.Application.Contracts;

namespace Logistics.EDI.Application.Abstractions;

public interface ILoadTender204TranslationService
{
    LoadTenderResponse Translate(string rawEdi);
}
