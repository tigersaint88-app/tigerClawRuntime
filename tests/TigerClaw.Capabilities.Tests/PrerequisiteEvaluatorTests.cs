using TigerClaw.Capabilities.Resolver;
using TigerClaw.Models;

namespace TigerClaw.Capabilities.Tests;

public class PrerequisiteEvaluatorTests
{
    [Fact]
    public void AllOf_requires_each_id_in_effective()
    {
        var req = new CapabilityRequirements { AllOf = new[] { "a", "b" } };
        var effective = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a" };
        var observed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b" };
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var r = PrerequisiteEvaluator.Evaluate(req, effective, observed, blocked);
        Assert.False(r.Allowed);
        Assert.Contains(r.Diagnostics, d => d.Code == "missing_all_of");
    }

    [Fact]
    public void AnyOf_requires_at_least_one()
    {
        var req = new CapabilityRequirements { AnyOf = new[] { "x", "y" } };
        var effective = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "y" };
        var observed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "y" };
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var r = PrerequisiteEvaluator.Evaluate(req, effective, observed, blocked);
        Assert.True(r.Allowed);
    }

    [Fact]
    public void NoneOf_fails_if_present_in_effective()
    {
        var req = new CapabilityRequirements { NoneOf = new[] { "bad" } };
        var effective = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bad" };
        var observed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bad" };
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var r = PrerequisiteEvaluator.Evaluate(req, effective, observed, blocked);
        Assert.False(r.Allowed);
        Assert.Contains(r.Diagnostics, d => d.Code == "none_of_violation");
    }

    [Fact]
    public void Blocked_id_reports_blocked_by_policy()
    {
        var req = new CapabilityRequirements { AllOf = new[] { "tigerclaw.bin.git" } };
        var effective = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var observed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tigerclaw.bin.git" };
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tigerclaw.bin.git" };

        var r = PrerequisiteEvaluator.Evaluate(req, effective, observed, blocked);
        Assert.False(r.Allowed);
        Assert.Contains(r.Diagnostics, d => d.Code == "blocked_by_policy");
    }
}
