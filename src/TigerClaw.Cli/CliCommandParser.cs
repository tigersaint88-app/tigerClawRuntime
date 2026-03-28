namespace TigerClaw.Cli;

/// <summary>
/// Parses argv into <see cref="CliCommandRequest"/> (OpenClaw-compatible groups).
/// </summary>
public static class CliCommandParser
{
    private static readonly HashSet<string> KnownGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        "run", "workflow", "skills", "memory", "taobao",
        "models", "configure", "doctor", "logs", "help"
    };

    /// <summary>
    /// If the first token is <c>openclaw</c>, skip it (TigerClaw compatibility alias).
    /// </summary>
    public static string[] NormalizeOpenClawAlias(string[] args)
    {
        if (args.Length == 0) return args;
        if (string.Equals(args[0], "openclaw", StringComparison.OrdinalIgnoreCase))
            return args.Skip(1).ToArray();
        return args;
    }

    public static CliParseResult Parse(string[] args)
    {
        args = NormalizeOpenClawAlias(args);
        if (args.Length == 0)
            return CliParseResult.Fail("No command specified.");

        var jsonOutput = false;
        var wantHelp = false;
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var positionals = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var t = args[i];
            if (string.Equals(t, "--json", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "-j", StringComparison.OrdinalIgnoreCase))
            {
                jsonOutput = true;
                continue;
            }

            if (string.Equals(t, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "-h", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "-?", StringComparison.OrdinalIgnoreCase))
            {
                wantHelp = true;
                continue;
            }

            if (t.StartsWith("--", StringComparison.Ordinal))
            {
                var eq = t.IndexOf('=', StringComparison.Ordinal);
                if (eq > 2)
                {
                    var key = t[2..eq].Trim();
                    var val = t[(eq + 1)..];
                    options[key] = val;
                    continue;
                }

                var optKey = t[2..].Trim();
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    options[optKey] = args[i + 1];
                    i++;
                }
                else
                    options[optKey] = "true";
                continue;
            }

            positionals.Add(t);
        }

        if (positionals.Count == 0)
        {
            return CliParseResult.Ok(new CliCommandRequest
            {
                Group = wantHelp ? "help" : "",
                Help = wantHelp,
                JsonOutput = jsonOutput,
                Options = options
            });
        }

        // Legacy: "workflows ..." -> "workflow ..."
        if (string.Equals(positionals[0], "workflows", StringComparison.OrdinalIgnoreCase))
            positionals[0] = "workflow";

        var first = positionals[0];
        if (!KnownGroups.Contains(first))
        {
            var runText = string.Join(" ", positionals);
            return CliParseResult.Ok(new CliCommandRequest
            {
                Group = "run",
                Action = "",
                Positional = new[] { "run", runText },
                Options = options,
                JsonOutput = jsonOutput,
                Help = wantHelp,
                IsFallbackRun = true
            });
        }

        var group = first;
        var rest = positionals.Skip(1).ToList();

        if (string.Equals(group, "help", StringComparison.OrdinalIgnoreCase))
        {
            return CliParseResult.Ok(new CliCommandRequest
            {
                Group = "help",
                Action = rest.FirstOrDefault() ?? "",
                Positional = new[] { "help" }.Concat(rest).ToArray(),
                Options = options,
                JsonOutput = jsonOutput,
                Help = true
            });
        }

        string action;
        string target;
        IReadOnlyList<string> tail;

        switch (group.ToLowerInvariant())
        {
            case "run":
                action = "";
                target = "";
                tail = rest;
                break;
            case "workflow":
                action = rest.ElementAtOrDefault(0) ?? "";
                target = rest.ElementAtOrDefault(1) ?? "";
                tail = rest.Count > 2 ? rest.Skip(2).ToList() : new List<string>();
                break;
            case "skills":
                action = rest.ElementAtOrDefault(0) ?? "";
                target = rest.ElementAtOrDefault(1) ?? "";
                tail = rest.Count > 2 ? rest.Skip(2).ToList() : new List<string>();
                break;
            case "memory":
                action = rest.ElementAtOrDefault(0) ?? "";
                target = rest.ElementAtOrDefault(1) ?? "";
                tail = rest.Count > 2 ? rest.Skip(2).ToList() : new List<string>();
                break;
            case "taobao":
                action = rest.ElementAtOrDefault(0) ?? "";
                target = rest.ElementAtOrDefault(1) ?? "";
                tail = rest.Count > 2 ? rest.Skip(2).ToList() : new List<string>();
                break;
            default:
                action = rest.ElementAtOrDefault(0) ?? "";
                target = rest.ElementAtOrDefault(1) ?? "";
                tail = rest.Count > 2 ? rest.Skip(2).ToList() : new List<string>();
                break;
        }

        var pos = new List<string> { group };
        pos.AddRange(rest);

        return CliParseResult.Ok(new CliCommandRequest
        {
            Group = group,
            Action = action,
            Target = target,
            Positional = pos,
            Options = options,
            JsonOutput = jsonOutput,
            Help = wantHelp
        });
    }
}
