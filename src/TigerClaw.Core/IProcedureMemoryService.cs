using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Manages procedure memory (successful task paths).
/// </summary>
public interface IProcedureMemoryService
{
    Task SaveAsync(ProcedureRecord record, CancellationToken cancellationToken = default);
    Task<ProcedureRecord?> SearchByTaskTypeAsync(string taskType, string? userId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcedureRecord>> ListAllAsync(string? userId = null, CancellationToken cancellationToken = default);
}
