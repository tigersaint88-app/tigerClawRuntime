using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;

namespace TigerClaw.Infrastructure.Database;

/// <summary>
/// Initializes SQLite database and schema.
/// </summary>
public class DatabaseInitializer
{
    private readonly DatabaseOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IOptions<TigerClawOptions> options, ILogger<DatabaseInitializer> logger)
    {
        _options = options.Value.Database;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var baseDir = Path.GetDirectoryName(_options.ConnectionString.Replace("Data Source=", "").Split(';')[0].Trim());
        if (!string.IsNullOrEmpty(baseDir) && !string.IsNullOrEmpty(_options.DataDirectory))
        {
            var dataPath = Path.Combine(baseDir, _options.DataDirectory);
            if (!Path.IsPathRooted(dataPath))
            {
                dataPath = Path.Combine(Directory.GetCurrentDirectory(), dataPath);
            }
            Directory.CreateDirectory(dataPath);
            _logger.LogInformation("Data directory ensured: {Path}", dataPath);
        }

        var sqlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "scripts", "bootstrap.sql");
        if (!File.Exists(sqlPath))
        {
            sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "scripts", "bootstrap.sql");
        }
        if (!File.Exists(sqlPath))
        {
            _logger.LogWarning("Bootstrap SQL not found at {Path}, using embedded schema", sqlPath);
        }

        await using var connection = new SqliteConnection(ResolveConnectionString());
        await connection.OpenAsync(cancellationToken);

        var sql = File.Exists(sqlPath)
            ? await File.ReadAllTextAsync(sqlPath, cancellationToken)
            : GetEmbeddedSchema();

        foreach (var raw in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var statement = string.Join("\n", raw.Split('\n').Where(l => !l.TrimStart().StartsWith("--"))).Trim();
            if (string.IsNullOrWhiteSpace(statement)) continue;
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = statement + ";";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("Database initialized successfully");
    }

    private string ResolveConnectionString()
    {
        var cs = _options.ConnectionString;
        if (!Path.IsPathRooted(cs.Replace("Data Source=", "").Split(';')[0].Trim()))
        {
            var dataDir = Path.Combine(Directory.GetCurrentDirectory(), _options.DataDirectory);
            Directory.CreateDirectory(dataDir);
            var dbPath = Path.Combine(dataDir, "tigerclaw.db");
            return $"Data Source={dbPath}";
        }
        return cs;
    }

    private static string GetEmbeddedSchema()
    {
        return """
            CREATE TABLE IF NOT EXISTS preferences (
                key TEXT NOT NULL, value TEXT NOT NULL, user_id TEXT NOT NULL DEFAULT '',
                updated_at_utc TEXT NOT NULL, PRIMARY KEY (key, user_id)
            );
            CREATE TABLE IF NOT EXISTS aliases (
                alias TEXT NOT NULL, resolved_value TEXT NOT NULL, user_id TEXT NOT NULL DEFAULT '',
                updated_at_utc TEXT NOT NULL, PRIMARY KEY (alias, user_id)
            );
            CREATE TABLE IF NOT EXISTS procedures (
                id INTEGER PRIMARY KEY AUTOINCREMENT, task_type TEXT NOT NULL,
                steps_summary TEXT NOT NULL, user_id TEXT, created_at_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS tasks (
                id TEXT PRIMARY KEY, workflow_id TEXT NOT NULL, user_id TEXT NOT NULL,
                input_text TEXT, status TEXT NOT NULL, created_at_utc TEXT NOT NULL, completed_at_utc TEXT
            );
            CREATE TABLE IF NOT EXISTS task_steps (
                id INTEGER PRIMARY KEY AUTOINCREMENT, task_id TEXT NOT NULL, step_id TEXT NOT NULL,
                status TEXT NOT NULL, message TEXT, output TEXT, artifact_path TEXT,
                completed_at_utc TEXT, retry_count INTEGER, FOREIGN KEY (task_id) REFERENCES tasks(id)
            );
            CREATE TABLE IF NOT EXISTS audit_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT, task_id TEXT, step_id TEXT,
                event_type TEXT NOT NULL, message TEXT, payload TEXT, created_at_utc TEXT NOT NULL
            );
            """;
    }
}
