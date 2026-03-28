namespace TigerClaw.Capabilities;

/// <summary>
/// Namespaced capability id helpers. Ids are lowercase dotted strings (e.g. <c>tigerclaw.os.windows</c>).
/// </summary>
public static class CapabilityIds
{
    public const string Prefix = "tigerclaw.";

    public static string Bin(string name)
    {
        var n = (name ?? "").Trim();
        if (string.IsNullOrEmpty(n)) return Prefix + "bin.unknown";
        return Prefix + "bin." + n.ToLowerInvariant();
    }

    public static string AnyBin(string id)
    {
        var n = (id ?? "").Trim();
        if (string.IsNullOrEmpty(n)) return Prefix + "anybin.unknown";
        return Prefix + "anybin." + n.ToLowerInvariant();
    }

    public static string OsFamily(string family) => Prefix + "os." + (family ?? "unknown").ToLowerInvariant();

    public const string DesktopInteractive = Prefix + "session.desktop_interactive";

    public const string LlmEndpointReachable = Prefix + "llm.endpoint.reachable";

    /// <summary>Granted when email account prerequisites appear configured for the user (see <see cref="CapabilityResolver"/>).</summary>
    public const string EmailRead = "email.read";
}
