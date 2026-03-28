using Microsoft.Extensions.DependencyInjection;
using TigerClaw.Infrastructure.Audit;
using TigerClaw.Infrastructure.Database;
using TigerClaw.Infrastructure.Loaders;
using TigerClaw.Infrastructure.ModelAdapters;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Infrastructure.Repositories;

namespace TigerClaw.Infrastructure;

/// <summary>
/// DI registration for Infrastructure.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTigerClawInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<TigerClawOptions>();

        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<PreferenceRepository>();
        services.AddSingleton<AliasRepository>();
        services.AddSingleton<ProcedureRepository>();
        services.AddSingleton<TaskRepository>();
        services.AddSingleton<AuditLogRepository>();
        services.AddSingleton<SkillDefinitionLoader>();
        services.AddSingleton<WorkflowDefinitionLoader>();
        services.AddSingleton<Core.IAuditLogger, AuditLogger>();

        services.AddSingleton<LocalModelAdapter>();
        services.AddHttpClient<RemoteOpenAiCompatibleModelAdapter>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TigerClawOptions>>().Value;
            client.BaseAddress = new Uri(opts.ModelRouting.RemoteApiBaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrEmpty(opts.ModelRouting.ApiKey))
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + opts.ModelRouting.ApiKey);
        });
        services.AddSingleton<ModelRouter>();
        services.AddSingleton<Core.IModelAdapter>(sp => sp.GetRequiredService<ModelRouter>());

        return services;
    }
}
