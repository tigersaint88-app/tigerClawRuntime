using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Resolves workflow templates from intent or name.
/// </summary>
public interface IWorkflowTemplateResolver
{
    Task<string?> ResolveWorkflowIdAsync(string intent, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
}
