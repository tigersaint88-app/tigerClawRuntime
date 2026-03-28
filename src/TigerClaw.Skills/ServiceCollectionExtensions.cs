using Microsoft.Extensions.DependencyInjection;

namespace TigerClaw.Skills;

/// <summary>
/// DI registration for Skills.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTigerClawSkills(this IServiceCollection services, bool isInteractive = false)
    {
        services.AddSingleton<BrowserAutomationSession>();

        services.AddSingleton<FileReadSkill>();
        services.AddSingleton<FileWriteSkill>();
        services.AddSingleton<ShellRunSkill>();
        services.AddSingleton<TextSummarizeSkill>();
        services.AddSingleton<MemorySavePreferenceSkill>();
        services.AddSingleton<WorkflowRunNamedSkill>();
        services.AddSingleton<BrowserOpenUrlSkill>();
        services.AddSingleton<BrowserTypeSkill>();
        services.AddSingleton<BrowserClickSkill>();
        services.AddSingleton<BrowserWaitForSkill>();
        services.AddSingleton<BrowserExtractTextSkill>();
        services.AddSingleton<BrowserScreenshotSkill>();
        services.AddSingleton<EmailFetchUnreadSkill>();

        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<FileReadSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<FileWriteSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<ShellRunSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<TextSummarizeSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<MemorySavePreferenceSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<WorkflowRunNamedSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<BrowserOpenUrlSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<BrowserTypeSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<BrowserClickSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<BrowserWaitForSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<BrowserExtractTextSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<BrowserScreenshotSkill>());
        services.AddSingleton<Core.ISkill>(sp => sp.GetRequiredService<EmailFetchUnreadSkill>());

        services.AddSingleton<Core.ISkill>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HumanWaitForContinueSkill>>();
            return new HumanWaitForContinueSkill(logger, () => isInteractive);
        });

        services.AddSingleton<Core.ISkillRegistry, JsonSkillRegistry>();
        return services;
    }
}
