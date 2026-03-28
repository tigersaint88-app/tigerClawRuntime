using Microsoft.Extensions.DependencyInjection;
using TigerClaw.Memory;

namespace TigerClaw.Memory;

/// <summary>
/// DI registration for Memory.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTigerClawMemory(this IServiceCollection services)
    {
        services.AddSingleton<PreferenceService>();
        services.AddSingleton<AliasService>();
        services.AddSingleton<ProcedureMemoryService>();
        services.AddSingleton<SemanticMemoryService>();
        services.AddSingleton<Core.IPreferenceService>(sp => sp.GetRequiredService<PreferenceService>());
        services.AddSingleton<Core.IAliasService>(sp => sp.GetRequiredService<AliasService>());
        services.AddSingleton<Core.IProcedureMemoryService>(sp => sp.GetRequiredService<ProcedureMemoryService>());
        services.AddSingleton<Core.ISemanticMemoryService>(sp => sp.GetRequiredService<SemanticMemoryService>());
        services.AddSingleton<Core.IMemoryStore, MemoryStore>();
        return services;
    }
}
