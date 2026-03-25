using Logistics.EDI.Domain.Abstractions;
using Logistics.EDI.Infrastructure.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.EDI.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<ILoadTender204Parser, NotImplementedLoadTender204Parser>();

        return services;
    }
}
