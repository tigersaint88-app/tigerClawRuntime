using TigerClaw.Models;

namespace TigerClaw.Memory;

/// <summary>
/// Semantic memory - stub in V1. Returns empty results.
/// TODO: Implement with vector embeddings in V1.5.
/// </summary>
public class SemanticMemoryService : Core.ISemanticMemoryService
{
    public Task<IReadOnlyList<MemoryRecord>> SearchAsync(string query, int topK = 5, string? userId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<MemoryRecord>>(Array.Empty<MemoryRecord>());
    }
}
