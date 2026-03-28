namespace TigerClaw.Capabilities.Resolver;

/// <summary>
/// Deterministic selection of preferred capability id among available candidates.
/// </summary>
public static class ProviderSelector
{
    /// <summary>First entry in <paramref name="preferOrder"/> that exists in <paramref name="available"/> wins.</summary>
    public static string? SelectPreferred(IReadOnlyList<string> preferOrder, IReadOnlySet<string> available)
    {
        var set = new HashSet<string>(available, StringComparer.OrdinalIgnoreCase);
        foreach (var p in preferOrder)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            if (set.Contains(p)) return p;
        }
        return null;
    }

    /// <summary>
    /// Deterministic tie-break: sort provider ids by ordinal case-insensitive, then pick first available in prefer order.
    /// </summary>
    public static IReadOnlyList<string> OrderProviders(IEnumerable<string> providerIds)
    {
        return providerIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
