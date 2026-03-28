using TigerClaw.Models;

namespace TigerClaw.Capabilities.Resolver;

public sealed record PrerequisiteEvaluationResult(
    bool Allowed,
    IReadOnlyList<PrerequisiteDiagnostic> Diagnostics);

public sealed record PrerequisiteDiagnostic
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? CapabilityId { get; init; }
}

/// <summary>
/// Pure, deterministic evaluation of capability requirements against an effective set.
/// </summary>
public static class PrerequisiteEvaluator
{
    public static PrerequisiteEvaluationResult Evaluate(
        CapabilityRequirements req,
        IReadOnlySet<string> effectiveCapabilities,
        IReadOnlySet<string> observedBeforePolicy,
        IReadOnlySet<string> policyBlocked)
    {
        var diagnostics = new List<PrerequisiteDiagnostic>();
        var effective = new HashSet<string>(effectiveCapabilities, StringComparer.OrdinalIgnoreCase);
        var observed = new HashSet<string>(observedBeforePolicy, StringComparer.OrdinalIgnoreCase);
        var blocked = new HashSet<string>(policyBlocked, StringComparer.OrdinalIgnoreCase);

        foreach (var id in req.AllOf)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (!effective.Contains(id))
            {
                if (observed.Contains(id) && blocked.Contains(id))
                    diagnostics.Add(new PrerequisiteDiagnostic
                    {
                        Code = "blocked_by_policy",
                        Message = $"Capability {id} is blocked by policy or user deny list.",
                        CapabilityId = id
                    });
                else
                    diagnostics.Add(new PrerequisiteDiagnostic
                    {
                        Code = "missing_all_of",
                        Message = $"Missing required capability: {id}",
                        CapabilityId = id
                    });
            }
        }

        if (req.AnyOf.Count > 0)
        {
            var anyHit = req.AnyOf.Any(id => !string.IsNullOrWhiteSpace(id) && effective.Contains(id!));
            if (!anyHit)
            {
                diagnostics.Add(new PrerequisiteDiagnostic
                {
                    Code = "missing_any_of",
                    Message = $"None of anyOf satisfied: {string.Join(", ", req.AnyOf)}",
                    CapabilityId = req.AnyOf.FirstOrDefault()
                });
            }
        }

        foreach (var id in req.NoneOf)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (effective.Contains(id))
            {
                diagnostics.Add(new PrerequisiteDiagnostic
                {
                    Code = "none_of_violation",
                    Message = $"Forbidden capability present: {id}",
                    CapabilityId = id
                });
            }
        }

        return new PrerequisiteEvaluationResult(diagnostics.Count == 0, diagnostics);
    }
}
