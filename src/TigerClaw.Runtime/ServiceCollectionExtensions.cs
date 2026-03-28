using Microsoft.Extensions.DependencyInjection;
using TigerClaw.Capabilities;
using TigerClaw.Runtime;

namespace TigerClaw.Runtime;

/// <summary>
/// DI registration for Runtime.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTigerClawRuntime(this IServiceCollection services)
    {
        services.AddTigerClawCapabilities();
        services.AddSingleton<RuleBasedIntentRouter>();
        services.AddSingleton<Core.IIntentRouter, LlmFirstIntentRouter>();
        services.AddSingleton<Core.ITaskPlanner, TaskPlanner>();
        services.AddSingleton<Core.IWorkflowEngine, WorkflowEngine>();
        services.AddSingleton<Core.ITaskContextFactory, TaskContextFactory>();
        services.AddSingleton<Core.IRuntimeFacade>(sp =>
        {
            var router = sp.GetRequiredService<Core.IIntentRouter>();
            var planner = sp.GetRequiredService<Core.ITaskPlanner>();
            var engine = sp.GetRequiredService<Core.IWorkflowEngine>();
            var loader = sp.GetRequiredService<Core.IWorkflowLoader>();
            var registry = sp.GetRequiredService<Core.ISkillRegistry>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RuntimeFacade>>();
            var memory = sp.GetService<Core.IMemoryStore>();
            return new RuntimeFacade(router, planner, engine, loader, registry, logger, memory);
        });
        return services;
    }
}
