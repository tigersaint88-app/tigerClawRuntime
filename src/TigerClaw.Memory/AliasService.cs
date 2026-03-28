using TigerClaw.Infrastructure.Repositories;
using TigerClaw.Models;

namespace TigerClaw.Memory;

/// <summary>
/// Manages alias mappings.
/// </summary>
public class AliasService : Core.IAliasService
{
    private readonly AliasRepository _repo;

    public AliasService(AliasRepository repo)
    {
        _repo = repo;
    }

    public Task<string?> ResolveAsync(string alias, string? userId = null, CancellationToken cancellationToken = default)
        => _repo.GetAsync(alias, userId, cancellationToken);

    public Task UpsertAsync(string alias, string resolvedValue, string? userId = null, CancellationToken cancellationToken = default)
        => _repo.UpsertAsync(alias, resolvedValue, userId, cancellationToken);

    public Task<IReadOnlyList<AliasRecord>> ListAllAsync(string? userId = null, CancellationToken cancellationToken = default)
        => _repo.ListAllAsync(userId, cancellationToken);
}
