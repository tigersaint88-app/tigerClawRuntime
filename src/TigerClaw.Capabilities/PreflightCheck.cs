using TigerClaw.Capabilities.Resolver;
using TigerClaw.Core;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Capabilities;

public sealed record PreflightResult(
    bool Allowed,
    IReadOnlyList<PrerequisiteDiagnostic> Diagnostics,
    string? SelectedPreferredCapabilityId,
    IReadOnlySet<string> ObservedCapabilities,
    IReadOnlySet<string> EffectiveCapabilities);

/// <summary>
/// Capability preflight before skill execution: merge requirements, resolve effective set, evaluate.
/// </summary>
public sealed class PreflightCheck
{
    public async Task<PreflightResult> RunAsync(
        SkillDefinition? skill,
        WorkflowStepDefinition? step,
        ResourceSnapshot snapshot,
        CapabilityPolicyOptions policy,
        string? userId,
        IPreferenceService? preferences,
        CapabilityProviderRegistry registry,
        CancellationToken cancellationToken = default)
    {
        RegisterManifestProviders(skill, registry);

        var observed = await CapabilityResolver.BuildObservedAsync(snapshot, userId, preferences, cancellationToken).ConfigureAwait(false);
        var blocked = CapabilityResolver.PolicyBlockSet(policy);
        var effective = new HashSet<string>(observed, StringComparer.OrdinalIgnoreCase);
        foreach (var b in blocked) effective.Remove(b);

        var merged = SkillPrerequisitesMerge.Merge(skill, step);
        var hasExpr = merged.AllOf.Count > 0 || merged.AnyOf.Count > 0 || merged.NoneOf.Count > 0 || merged.Prefer.Count > 0;
        if (!hasExpr)
        {
            var preferredIdle = ProviderSelector.SelectPreferred(merged.Prefer, effective);
            return new PreflightResult(true, Array.Empty<PrerequisiteDiagnostic>(), preferredIdle, observed, effective);
        }

        var eval = PrerequisiteEvaluator.Evaluate(merged, effective, observed, blocked);
        var preferred = ProviderSelector.SelectPreferred(merged.Prefer, effective);

        return new PreflightResult(eval.Allowed, eval.Diagnostics, preferred, observed, effective);
    }

    static void RegisterManifestProviders(SkillDefinition? skill, CapabilityProviderRegistry registry)
    {
        if (skill == null) return;
        foreach (var b in skill.Bins ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(b)) continue;
            var name = b.Trim();
            registry.Register(new CapabilityProviderDescriptor(
                ProviderId: $"manifest:bin:{name}",
                Source: $"skill:{skill.Id}",
                ProvidesCapabilityIds: new[] { CapabilityIds.Bin(name) }));
        }

        foreach (var a in skill.AnyBins ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(a)) continue;
            var id = a.Trim();
            registry.Register(new CapabilityProviderDescriptor(
                ProviderId: $"manifest:anybin:{id}",
                Source: $"skill:{skill.Id}",
                ProvidesCapabilityIds: new[] { CapabilityIds.AnyBin(id) }));
        }
    }
}
