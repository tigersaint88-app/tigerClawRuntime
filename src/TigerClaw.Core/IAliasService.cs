using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Manages alias mappings.
/// </summary>
public interface IAliasService
{
    Task<string?> ResolveAsync(string alias, string? userId = null, CancellationToken cancellationToken = default);
    Task UpsertAsync(string alias, string resolvedValue, string? userId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AliasRecord>> ListAllAsync(string? userId = null, CancellationToken cancellationToken = default);
}
