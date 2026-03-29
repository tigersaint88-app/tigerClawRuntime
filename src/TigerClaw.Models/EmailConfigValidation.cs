namespace TigerClaw.Models;

/// <summary>Shared rules: example/placeholder hosts are not valid email configuration (avoid pointless IMAP connection attempts).</summary>
public static class EmailConfigValidation
{
    public static bool IsPlaceholderHost(string? host)
    {
        var h = (host ?? "").Trim();
        if (h.Length == 0) return true;
        return h.Equals("imap.example.com", StringComparison.OrdinalIgnoreCase)
               || h.EndsWith(".example.com", StringComparison.OrdinalIgnoreCase)
               || h.EndsWith(".example.org", StringComparison.OrdinalIgnoreCase)
               || h.EndsWith(".test", StringComparison.OrdinalIgnoreCase);
    }
}
