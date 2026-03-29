using TigerClaw.Core;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Capabilities.Tests;

public class PreflightCheckTests
{
    sealed class MemPrefs : IPreferenceService
    {
        private readonly Dictionary<(string key, string uid), string> _d = new();

        public MemPrefs(string uid, params (string k, string v)[] pairs)
        {
            foreach (var (k, v) in pairs)
                _d[(k, uid)] = v;
        }

        public Task<string?> GetAsync(string key, string? userId = null, CancellationToken cancellationToken = default)
        {
            _d.TryGetValue((key, userId ?? ""), out var v);
            return Task.FromResult<string?>(v);
        }

        public Task UpsertAsync(string key, string value, string? userId = null, CancellationToken cancellationToken = default)
        {
            _d[(key, userId ?? "")] = value;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UserPreference>> ListAllAsync(string? userId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<UserPreference>>(Array.Empty<UserPreference>());

        public Task<int> DeleteKeyPrefixAsync(string keyPrefix, string? userId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }

    static ResourceSnapshot MinimalSnapshot()
    {
        return new ResourceSnapshot(
            "windows",
            CapabilityIds.OsFamily("windows"),
            true,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { CapabilityIds.Bin("git") },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            false,
            null,
            new Dictionary<string, string>());
    }

    [Fact]
    public async Task Policy_block_denies_even_when_observed()
    {
        var skill = new SkillDefinition
        {
            Id = "x",
            Name = "X",
            Prerequisites = new SkillPrerequisites
            {
                Capabilities = new CapabilityRequirements { AllOf = new[] { CapabilityIds.Bin("git") } }
            }
        };

        var snap = MinimalSnapshot();
        var policy = new CapabilityPolicyOptions { BlockedCapabilities = new[] { CapabilityIds.Bin("git") } };
        var pre = new PreflightCheck();
        var r = await pre.RunAsync(skill, null, snap, policy, "u", new MemPrefs("u"), new CapabilityProviderRegistry());
        Assert.False(r.Allowed);
    }

    [Fact]
    public async Task Tag_only_passes()
    {
        var skill = new SkillDefinition { Id = "t", Name = "T" };
        var pre = new PreflightCheck();
        var r = await pre.RunAsync(skill, null, MinimalSnapshot(), new CapabilityPolicyOptions(), "u", null, new CapabilityProviderRegistry());
        Assert.True(r.Allowed);
    }
}
