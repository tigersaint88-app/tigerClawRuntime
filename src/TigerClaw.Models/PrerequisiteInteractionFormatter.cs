using System.Text;

namespace TigerClaw.Models;

/// <summary>Builds a default multi-line message for clients when <see cref="TaskResponse.WaitingHuman"/> is true.</summary>
public static class PrerequisiteInteractionFormatter
{
    public static string Format(IReadOnlyList<PrerequisiteIssue> issues, string? summaryLine)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(summaryLine))
            sb.AppendLine(summaryLine.TrimEnd());
        else
            sb.AppendLine("以下预设条件未满足。请通过 preferences（或环境变量）补齐后，重新运行同一工作流。");

        foreach (var i in issues)
        {
            var line = i.InteractionHint ?? i.Message;
            var keyPart = string.IsNullOrWhiteSpace(i.Key) ? "" : $" [{i.Key}]";
            sb.AppendLine($"- ({i.Kind}){keyPart} {line}");
        }

        return sb.ToString().TrimEnd();
    }
}
