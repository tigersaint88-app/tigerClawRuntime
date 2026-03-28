namespace TigerClaw.Capabilities;

/// <summary>
/// Declares a capability provider discovered from skill manifest (bins / anybins registration).
/// </summary>
public sealed record CapabilityProviderDescriptor(
    string ProviderId,
    string Source,
    IReadOnlyList<string> ProvidesCapabilityIds);

/// <summary>
/// Registry of manifest-registered providers (bins/anybins). Thread-safe for read-heavy use.
/// </summary>
public sealed class CapabilityProviderRegistry
{
    private readonly List<CapabilityProviderDescriptor> _providers = new();

    public void Register(CapabilityProviderDescriptor descriptor)
    {
        lock (_providers)
            _providers.Add(descriptor);
    }

    public IReadOnlyList<CapabilityProviderDescriptor> List() => _providers.ToArray();
}
