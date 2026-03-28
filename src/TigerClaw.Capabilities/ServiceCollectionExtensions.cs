using Microsoft.Extensions.DependencyInjection;

namespace TigerClaw.Capabilities;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTigerClawCapabilities(this IServiceCollection services)
    {
        services.AddSingleton<Bins.AnybinLoader>();
        services.AddSingleton<ResourceSnapshotBuilder>();
        services.AddSingleton<PreflightCheck>();
        return services;
    }
}
