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

    /// <summary>Deletes all preferences whose key starts with <paramref name="keyPrefix"/> for the user (e.g. <c>email.</c>).</summary>
    Task<int> DeleteKeyPrefixAsync(string keyPrefix, string? userId = null, CancellationToken cancellationToken = default);
}
