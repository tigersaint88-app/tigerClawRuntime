using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Semantic memory for document/embedding search. Stub in V1.
/// </summary>
public interface ISemanticMemoryService
{
    Task<IReadOnlyList<MemoryRecord>> SearchAsync(string query, int topK = 5, string? userId = null, CancellationToken cancellationToken = default);
}
