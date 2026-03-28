namespace TigerClaw.Cli;

internal static class CliHelp
{
    public const string Global = """
        TigerClaw Runtime V1 CLI (OpenClaw-compatible)

        Usage:
          tigerclaw <group> [action] [args] [--json] [--help]

        Groups:
          run            Natural language task
          workflow       list | show <id> | run <id> [keyword...] | validate <file>
          skills         list | show <id> | exec <id>
          memory         preferences list | preference set <k> <v> | aliases list | aliases set <a> <v> | procedures list | search
          taobao         search [keyword]
          models|configure|doctor|logs   (stubs in V1)

        Global options:
          --json, -j     Machine-readable output
          --help, -h     Help for group or global

        Compatibility:
          First token may be "openclaw" (ignored).
        """;

    public const string Run = """
        tigerclaw run "<task text>" [--model=...] [--session=...] [--local-only]

        If no task text is given, reads stdin.
        """;

    public const string Workflow = """
        tigerclaw workflow list
        tigerclaw workflow show <id>
        tigerclaw workflow run <id> [keyword...]
        tigerclaw workflow validate <path-to-json>
        """;

    public const string Skills = """
        tigerclaw skills list
        tigerclaw skills show <id>
        tigerclaw skills exec <id> [--inputs='{"k":"v"}']
        """;

    public const string Memory = """
        tigerclaw memory preferences list
        tigerclaw memory preference set <key> <value>
        tigerclaw memory aliases list
        tigerclaw memory aliases set <alias> <resolved>
        tigerclaw memory procedures list
        tigerclaw memory search   (limited in V1)
        """;

    public static string ForGroup(string group) => group.ToLowerInvariant() switch
    {
        "run" => Run,
        "workflow" => Workflow,
        "skills" => Skills,
        "memory" => Memory,
        "taobao" => "tigerclaw taobao search [keyword]   (default keyword: 空调)",
        _ => Global
    };
}
