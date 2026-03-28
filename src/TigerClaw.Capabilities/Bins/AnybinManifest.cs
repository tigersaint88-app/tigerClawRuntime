namespace TigerClaw.Capabilities.Bins;

public sealed record AnybinManifestEntry
{
    public required string Id { get; init; }
    public required IReadOnlyList<string> Candidates { get; init; }
}

public sealed record AnybinManifest
{
    public IReadOnlyList<AnybinManifestEntry> AnyBins { get; init; } = Array.Empty<AnybinManifestEntry>();
}
