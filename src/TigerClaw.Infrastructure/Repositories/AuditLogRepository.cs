using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;

namespace TigerClaw.Infrastructure.Repositories;

/// <summary>
/// SQLite repository for audit logs.
/// </summary>
public class AuditLogRepository
{
    private readonly string _connectionString;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(IOptions<TigerClawOptions> options, ILogger<AuditLogRepository> logger)
    {
        _connectionString = GetConnectionString(options.Value.Database);
        _logger = logger;
    }

    public async Task LogAsync(string? taskId, string? stepId, string eventType, string? message = null, string? payload = null, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var now = DateTime.UtcNow.ToString("O");
        await conn.ExecuteAsync(
            "INSERT INTO audit_logs (task_id, step_id, event_type, message, payload, created_at_utc) VALUES (@taskId, @stepId, @eventType, @msg, @payload, @now)",
            new { taskId, stepId, eventType, msg = message, payload, now });
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
