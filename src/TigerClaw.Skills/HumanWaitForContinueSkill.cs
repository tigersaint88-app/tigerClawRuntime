using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Human checkpoint - waits for user to continue. In API mode returns WaitingHuman.
/// </summary>
public class HumanWaitForContinueSkill : Core.ISkill
{
    public string Id => "human.wait_for_continue";
    private readonly ILogger<HumanWaitForContinueSkill> _logger;
    private readonly Func<bool> _isInteractive;

    public HumanWaitForContinueSkill(ILogger<HumanWaitForContinueSkill> logger, Func<bool>? isInteractive = null)
    {
        _logger = logger;
        _isInteractive = isInteractive ?? (() => false);
    }

    public Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var instruction = inputs.TryGetValue("instruction", out var i) ? i?.ToString() : "Press Enter to continue...";
        _logger.LogInformation("Human checkpoint: {Instruction} taskId={TaskId}", instruction, context.TaskId);

        if (_isInteractive())
        {
            Console.WriteLine(instruction);
            Console.ReadLine();
            return Task.FromResult(new SkillExecutionResult { Success = true });
        }

        return Task.FromResult(new SkillExecutionResult
        {
            Success = false,
            WaitingHuman = true,
            Message = instruction
        });
    }
}
