namespace TigerClaw.Models;

/// <summary>
/// Heuristics for <see cref="PrerequisiteIssue.MaskKeyInUi"/> so UIs can redact preference/env keys until the user reveals them.
/// </summary>
public static class PrerequisiteSensitive
{
    /// <summary>Returns true when <paramref name="preferenceKey"/> should be treated as secret in interactive UIs.</summary>
    public static bool ShouldMaskPreferenceKey(string? preferenceKey)
    {
        if (string.IsNullOrWhiteSpace(preferenceKey)) return false;
        var k = preferenceKey.Trim();

        if (k.EndsWith(".password", StringComparison.OrdinalIgnoreCase)) return true;
        if (k.Contains(".password.", StringComparison.OrdinalIgnoreCase)) return true;
        if (k.EndsWith(".secret", StringComparison.OrdinalIgnoreCase)) return true;
        if (k.EndsWith(".clientSecret", StringComparison.OrdinalIgnoreCase)) return true;

        if (k.Contains("apikey", StringComparison.OrdinalIgnoreCase)) return true;
        if (k.Contains("api_key", StringComparison.OrdinalIgnoreCase)) return true;
        if (k.Contains("private_key", StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    /// <summary>Returns true when an environment variable name should be masked in UIs.</summary>
    public static bool ShouldMaskEnvName(string? envName)
    {
        if (string.IsNullOrWhiteSpace(envName)) return false;
        var e = envName.Trim();
        if (e.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase)) return true;
        if (e.Contains("SECRET", StringComparison.OrdinalIgnoreCase)) return true;
        if (e.Contains("API_KEY", StringComparison.OrdinalIgnoreCase)) return true;
        if (e.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) && !e.Contains("MAX", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}
