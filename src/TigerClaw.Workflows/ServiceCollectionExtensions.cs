using Microsoft.Extensions.DependencyInjection;
using TigerClaw.Workflows;

namespace TigerClaw.Workflows;

/// <summary>
/// DI registration for Workflows.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTigerClawWorkflows(this IServiceCollection services)
    {
        services.AddSingleton<Core.IWorkflowLoader, WorkflowLoader>();
        services.AddSingleton<Core.IWorkflowTemplateResolver, WorkflowTemplateResolver>();
        return services;
    }
}
