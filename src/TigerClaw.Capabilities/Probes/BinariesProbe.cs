namespace TigerClaw.Capabilities.Probes;

/// <summary>
/// Resolves binaries on PATH into <see cref="CapabilityIds.Bin"/> capability ids.
/// </summary>
public static class BinariesProbe
{
    public static HashSet<string> Probe(IEnumerable<string> extraNamesToCheck)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var sep = Path.PathSeparator;
        var dirs = path.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var exts = new List<string> { "" };
        var pathext = Environment.GetEnvironmentVariable("PATHEXT");
        if (!string.IsNullOrEmpty(pathext))
            exts.AddRange(pathext.Split(';', StringSplitOptions.RemoveEmptyEntries));

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in extraNamesToCheck)
        {
            if (!string.IsNullOrWhiteSpace(n)) names.Add(n.Trim());
        }

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var name in names)
            {
                foreach (var ext in exts)
                {
                    var fileName = name + ext;
                    if (string.IsNullOrEmpty(fileName)) continue;
                    var full = Path.Combine(dir, fileName);
                    if (File.Exists(full))
                    {
                        set.Add(CapabilityIds.Bin(name));
                        break;
                    }
                }
            }
        }

        return set;
    }
}
