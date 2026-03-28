namespace TigerClaw.Infrastructure.Options;

/// <summary>
/// Workspace paths configuration.
/// </summary>
public class WorkspaceOptions
{
    public string RootPath { get; set; } = ".";
    public string SkillsPath { get; set; } = "skills";
    public string WorkflowsPath { get; set; } = "workflows";
    public string ArtifactsPath { get; set; } = "artifacts";
    public string LogsPath { get; set; } = "logs";
}
