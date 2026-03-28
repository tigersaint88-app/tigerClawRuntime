using TigerClaw.Core;
using TigerClaw.Infrastructure.Options;

namespace TigerClaw.Capabilities;

/// <summary>
/// Builds effective capability set: observed snapshot + user grants, minus policy blocks.
/// </summary>
public static class CapabilityResolver
{
    public static async Task<HashSet<string>> BuildObservedAsync(
        ResourceSnapshot snapshot,
        string? userId,
        IPreferenceService? preferences,
        CancellationToken cancellationToken = default)
    {
        var observed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        observed.Add(snapshot.OsCapabilityId);
        if (snapshot.DesktopInteractive)
            observed.Add(CapabilityIds.DesktopInteractive);
        foreach (var x in snapshot.BinaryCapabilityIds) observed.Add(x);
        foreach (var x in snapshot.AnyBinCapabilityIds) observed.Add(x);
        if (snapshot.LlmEndpointReachable)
            observed.Add(CapabilityIds.LlmEndpointReachable);

        if (preferences != null)
        {
            var account = await preferences.GetAsync("email.default_account_id", userId, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(account))
            {
                var host = await preferences.GetAsync($"email.accounts.{account}.host", userId, cancellationToken).ConfigureAwait(false);
                var user = await preferences.GetAsync($"email.accounts.{account}.username", userId, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(user))
                    observed.Add(CapabilityIds.EmailRead);
            }
        }

        return observed;
    }

    public static HashSet<string> PolicyBlockSet(CapabilityPolicyOptions policy)
    {
        var s = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var x in policy.BlockedCapabilities ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(x)) s.Add(x.Trim());
        foreach (var x in policy.UserBlockedCapabilities ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(x)) s.Add(x.Trim());
        return s;
    }
}
