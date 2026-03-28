using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Manages user preferences (profile memory).
/// </summary>
public interface IPreferenceService
{
    Task<string?> GetAsync(string key, string? userId = null, CancellationToken cancellationToken = default);
    Task UpsertAsync(string key, string value, string? userId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserPreference>> ListAllAsync(string? userId = null, CancellationToken cancellationToken = default);
}
