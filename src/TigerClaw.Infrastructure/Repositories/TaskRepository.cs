using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Infrastructure.Repositories;

/// <summary>
/// SQLite repository for task and step records.
/// </summary>
public class TaskRepository
{
    private readonly string _connectionString;

    public TaskRepository(IOptions<TigerClawOptions> options)
    {
        _connectionString = GetConnectionString(options.Value.Database);
    }

    public async Task SaveTaskAsync(string taskId, string workflowId, string userId, string? inputText, string status, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var now = DateTime.UtcNow.ToString("O");
        await conn.ExecuteAsync(
            "INSERT OR REPLACE INTO tasks (id, workflow_id, user_id, input_text, status, created_at_utc) VALUES (@id, @wf, @uid, @input, @status, @now)",
            new { id = taskId, wf = workflowId, uid = userId, input = inputText, status, now });
    }

    public async Task SaveStepAsync(string taskId, StepExecutionResult step, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var output = step.Output != null ? System.Text.Json.JsonSerializer.Serialize(step.Output) : null;
        var completed = step.CompletedAtUtc?.ToString("O");
        await conn.ExecuteAsync(
            "INSERT INTO task_steps (task_id, step_id, status, message, output, artifact_path, completed_at_utc, retry_count) VALUES (@taskId, @stepId, @status, @msg, @output, @artifact, @completed, @retry)",
            new { taskId, stepId = step.StepId, status = step.Status, msg = step.Message, output, artifact = step.ArtifactPath, completed, retry = step.RetryCount ?? 0 });
    }

    public async Task UpdateTaskStatusAsync(string taskId, string status, string? completedAt = null, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(
            "UPDATE tasks SET status = @status, completed_at_utc = @completed WHERE id = @id",
            new { status, completed = completedAt ?? DateTime.UtcNow.ToString("O"), id = taskId });
    }

    private static string GetConnectionString(DatabaseOptions opts)
    {
        var cs = opts.ConnectionString;
        if (cs.Contains("Data Source=") && !Path.IsPathRooted(cs.Replace("Data Source=", "").Split(';')[0].Trim()))
        {
            var dataDir = Path.Combine(Directory.GetCurrentDirectory(), opts.DataDirectory);
            Directory.CreateDirectory(dataDir);
            return $"Data Source={Path.Combine(dataDir, "tigerclaw.db")}";
        }
        return cs;
    }
}
