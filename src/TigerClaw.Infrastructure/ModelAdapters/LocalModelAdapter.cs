using TigerClaw.Models;

namespace TigerClaw.Infrastructure.ModelAdapters;

/// <summary>
/// Local/stub model adapter. Returns placeholder responses without calling real LLM.
/// </summary>
public class LocalModelAdapter : Core.IModelAdapter
{
    public Task<ModelResponse> CompleteAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ModelResponse
        {
            Content = "[Local stub] No LLM call made. Configure remote adapter for real completion.",
            PromptTokens = 0,
            CompletionTokens = 0,
            Model = "local-stub"
        });
    }
}
