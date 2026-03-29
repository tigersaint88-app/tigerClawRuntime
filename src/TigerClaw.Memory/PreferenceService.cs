using TigerClaw.Infrastructure.Repositories;
using TigerClaw.Models;

namespace TigerClaw.Memory;

/// <summary>
/// Manages user preferences (profile memory).
/// </summary>
public class PreferenceService : Core.IPreferenceService
{
    private readonly PreferenceRepository _repo;

    public PreferenceService(PreferenceRepository repo)
    {
        _repo = repo;
    }

    public Task<string?> GetAsync(string key, string? userId = null, CancellationToken cancellationToken = default)
        => _repo.GetAsync(key, userId, cancellationToken);

    public Task UpsertAsync(string key, string value, string? userId = null, CancellationToken cancellationToken = default)
        => _repo.UpsertAsync(key, value, userId, cancellationToken);

    public Task<IReadOnlyList<UserPreference>> ListAllAsync(string? userId = null, CancellationToken cancellationToken = default)
        => _repo.ListAllAsync(userId, cancellationToken);

    public Task<int> DeleteKeyPrefixAsync(string keyPrefix, string? userId = null, CancellationToken cancellationToken = default)
        => _repo.DeleteKeyPrefixAsync(keyPrefix, userId, cancellationToken);
}
