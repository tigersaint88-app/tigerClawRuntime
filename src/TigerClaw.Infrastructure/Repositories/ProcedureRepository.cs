using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Infrastructure.Repositories;

/// <summary>
/// SQLite repository for procedure memory.
/// </summary>
public class ProcedureRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ProcedureRepository> _logger;

    public ProcedureRepository(IOptions<TigerClawOptions> options, ILogger<ProcedureRepository> logger)
    {
        _connectionString = GetConnectionString(options.Value.Database);
        _logger = logger;
    }

    public async Task SaveAsync(ProcedureRecord record, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var stepsJson = JsonSerializer.Serialize(record.StepsSummary);
        var now = DateTime.UtcNow.ToString("O");
        await conn.ExecuteAsync(
            "INSERT INTO procedures (task_type, steps_summary, user_id, created_at_utc) VALUES (@taskType, @steps, @uid, @now)",
            new { taskType = record.TaskType, steps = stepsJson, uid = record.UserId ?? "", now });
        _logger.LogDebug("Procedure saved: {TaskType}", record.TaskType);
    }

    public async Task<ProcedureRecord?> GetByTaskTypeAsync(string taskType, string? userId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var uid = userId ?? "";
        var row = await conn.QueryFirstOrDefaultAsync<(string steps, string? user_id, string created)?>(
            "SELECT steps_summary, user_id, created_at_utc FROM procedures WHERE task_type = @taskType AND COALESCE(user_id, '') = @uid ORDER BY created_at_utc DESC LIMIT 1",
            new { taskType, uid });
        if (row == null) return null;
        var steps = JsonSerializer.Deserialize<IReadOnlyList<string>>(row.Value.steps) ?? Array.Empty<string>();
        return new ProcedureRecord
        {
            TaskType = taskType,
            StepsSummary = steps,
            UserId = row.Value.user_id,
            CreatedAtUtc = DateTime.Parse(row.Value.created)
        };
    }

    public async Task<IReadOnlyList<ProcedureRecord>> ListAllAsync(string? userId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var uid = userId ?? "";
        var rows = await conn.QueryAsync<(string task_type, string steps, string? user_id, string created)>(
            "SELECT task_type, steps_summary, user_id, created_at_utc FROM procedures WHERE COALESCE(user_id, '') = @uid",
            new { uid });
        return rows.Select(r =>
        {
            var steps = JsonSerializer.Deserialize<IReadOnlyList<string>>(r.steps) ?? Array.Empty<string>();
            return new ProcedureRecord
            {
                TaskType = r.task_type,
                StepsSummary = steps,
                UserId = r.user_id,
                CreatedAtUtc = DateTime.Parse(r.created)
            };
        }).ToList();
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
