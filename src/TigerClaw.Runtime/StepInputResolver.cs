using System.Text.Json;
using System.Text.RegularExpressions;
using TigerClaw.Models;

namespace TigerClaw.Runtime;

/// <summary>
/// Resolves variable placeholders in step inputs (e.g. {{input.x}}, {{context.prev.summary}}).
/// </summary>
public static class StepInputResolver
{
    private static readonly Regex VarRegex = new(@"\{\{(.+?)\}\}", RegexOptions.Compiled);

    public static IReadOnlyDictionary<string, object?> Resolve(
        IReadOnlyDictionary<string, object?> template,
        IReadOnlyDictionary<string, object?> variables,
        IReadOnlyDictionary<string, StepExecutionResult>? stepResults)
    {
        var result = new Dictionary<string, object?>();
        foreach (var kv in template ?? new Dictionary<string, object?>())
        {
            var value = ResolveValue(kv.Value, variables, stepResults);
            result[kv.Key] = value;
        }
        return result;
    }

    private static object? ResolveValue(object? value, IReadOnlyDictionary<string, object?> variables, IReadOnlyDictionary<string, StepExecutionResult>? stepResults)
    {
        // System.Text.Json deserializes JSON string values as JsonElement, not string — unwrap first.
        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => ResolveString(je.GetString() ?? "", variables, stepResults),
                JsonValueKind.Null => null,
                JsonValueKind.Object => Resolve(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText())
                    ?? new Dictionary<string, object?>(),
                    variables,
                    stepResults),
                JsonValueKind.Array => value,
                _ => je.GetRawText()
            };
        }

        if (value is string s)
            return ResolveString(s, variables, stepResults);

        if (value is Dictionary<string, object?> dict)
            return Resolve(dict, variables, stepResults);

        return value;
    }

    private static string ResolveString(string s, IReadOnlyDictionary<string, object?> variables, IReadOnlyDictionary<string, StepExecutionResult>? stepResults)
    {
        return VarRegex.Replace(s, m =>
            {
                var path = m.Groups[1].Value.Trim().Split('.');
                if (path.Length == 1 && variables.TryGetValue(path[0], out var v1))
                    return v1?.ToString() ?? "";
                if (path.Length >= 2 && path[0] == "input")
                {
                    if (variables.TryGetValue(path[1], out var v2))
                        return v2?.ToString() ?? "";
                    if (variables.TryGetValue("input", out var inputObj) && inputObj is IReadOnlyDictionary<string, object?> inputDict && inputDict.TryGetValue(path[1], out var v3))
                        return v3?.ToString() ?? "";
                }
                if (path.Length >= 2 && path[0] == "context" && path[1] == "previous" && path.Length >= 3 && stepResults != null)
                {
                    var prev = stepResults.Values.LastOrDefault();
                    if (prev?.Output != null && path[2] == "summary")
                        return prev.Output?.ToString() ?? "";
                }
                if (path.Length >= 2 && path[0] == "step" && stepResults != null && stepResults.TryGetValue(path[1], out var stepRes) && stepRes.Output != null)
                    return stepRes.Output is string st ? st : JsonSerializer.Serialize(stepRes.Output);
                return m.Value;
            });
    }
}
