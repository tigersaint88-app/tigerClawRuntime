using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Unified interface for local/remote LLM calls.
/// </summary>
public interface IModelAdapter
{
    Task<ModelResponse> CompleteAsync(ModelRequest request, CancellationToken cancellationToken = default);
}
