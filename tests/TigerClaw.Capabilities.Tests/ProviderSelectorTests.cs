using TigerClaw.Capabilities.Resolver;

namespace TigerClaw.Capabilities.Tests;

public class ProviderSelectorTests
{
    [Fact]
    public void SelectPreferred_is_deterministic_first_match()
    {
        var prefer = new[] { "b", "a", "c" };
        var avail = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "c" };
        Assert.Equal("a", ProviderSelector.SelectPreferred(prefer, avail));
    }

    [Fact]
    public void OrderProviders_sorts_ordinal_case_insensitive()
    {
        var o = ProviderSelector.OrderProviders(new[] { "Z", "a", "M" });
        Assert.Equal(new[] { "a", "M", "Z" }, o.ToArray());
    }
}
