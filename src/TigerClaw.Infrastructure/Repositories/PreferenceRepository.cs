using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Infrastructure.Repositories;

/// <summary>
/// SQLite repository for user preferences.
/// </summary>
public class PreferenceRepository
{
    private readonly string _connectionString;
    private readonly ILogger<PreferenceRepository> _logger;

    public PreferenceRepository(IOptions<TigerClawOptions> options, ILogger<PreferenceRepository> logger)
    {
        _connectionString = ResolveConnectionString(options.Value.Database);
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key, string? userId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var uid = userId ?? "";
        var row = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT value FROM preferences WHERE key = @key AND user_id = @uid",
            new { key, uid });
        return row;
    }

    public async Task UpsertAsync(string key, string value, string? userId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var uid = userId ?? "";
        var now = DateTime.UtcNow.ToString("O");
        await conn.ExecuteAsync(
            @"INSERT INTO preferences (key, value, user_id, updated_at_utc) VALUES (@key, @value, @uid, @now)
              ON CONFLICT(key, user_id) DO UPDATE SET value = @value, updated_at_utc = @now",
            new { key, value, uid, now });
        _logger.LogDebug("Preference upserted: {Key}", key);
    }

    public async Task<IReadOnlyList<UserPreference>> ListAllAsync(string? userId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var uid = userId ?? "";
        var rows = await conn.QueryAsync<(string key, string value, string? user_id, string updated_at)>(
            "SELECT key, value, user_id, updated_at_utc FROM preferences WHERE user_id = @uid",
            new { uid });
        return rows.Select(r => new UserPreference
        {
            Key = r.key,
            Value = r.value,
            UserId = r.user_id,
            UpdatedAtUtc = DateTime.Parse(r.updated_at)
        }).ToList();
    }

    public async Task<int> DeleteKeyPrefixAsync(string keyPrefix, string? userId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var uid = userId ?? "";
        var pattern = keyPrefix.Contains('%', StringComparison.Ordinal) ? keyPrefix : keyPrefix + "%";
        return await conn.ExecuteAsync(
            "DELETE FROM preferences WHERE user_id = @uid AND key LIKE @pat",
            new { uid, pat = pattern });
    }

    private static string ResolveConnectionString(DatabaseOptions opts)
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
