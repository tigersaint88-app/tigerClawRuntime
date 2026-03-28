using TigerClaw.Infrastructure.Repositories;
using TigerClaw.Models;

namespace TigerClaw.Memory;

/// <summary>
/// Manages procedure memory (successful task paths).
/// </summary>
public class ProcedureMemoryService : Core.IProcedureMemoryService
{
    private readonly ProcedureRepository _repo;

    public ProcedureMemoryService(ProcedureRepository repo)
    {
        _repo = repo;
    }

    public Task SaveAsync(ProcedureRecord record, CancellationToken cancellationToken = default)
        => _repo.SaveAsync(record, cancellationToken);

    public Task<ProcedureRecord?> SearchByTaskTypeAsync(string taskType, string? userId = null, CancellationToken cancellationToken = default)
        => _repo.GetByTaskTypeAsync(taskType, userId, cancellationToken);

    public Task<IReadOnlyList<ProcedureRecord>> ListAllAsync(string? userId = null, CancellationToken cancellationToken = default)
        => _repo.ListAllAsync(userId, cancellationToken);
}
