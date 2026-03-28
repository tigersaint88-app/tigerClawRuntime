using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Infrastructure.Repositories;

/// <summary>
/// SQLite repository for alias mappings.
/// </summary>
public class AliasRepository
{
    private readonly string _connectionString;
    private readonly ILogger<AliasRepository> _logger;

    public AliasRepository(IOptions<TigerClawOptions> options, ILogger<AliasRepository> logger)
    {
        _connectionString = GetConnectionString(options.Value.Database);
        _logger = logger;
    }

    public async Task<string?> GetAsync(string alias, string? userId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var uid = userId ?? "";
        var row = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT resolved_value FROM aliases WHERE alias = @alias AND user_id = @uid",
            new { alias, uid });
        return row;
    }

    public async Task UpsertAsync(string alias, string resolvedValue, string? userId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var uid = userId ?? "";
        var now = DateTime.UtcNow.ToString("O");
        await conn.ExecuteAsync(
            @"INSERT INTO aliases (alias, resolved_value, user_id, updated_at_utc) VALUES (@alias, @resolved, @uid, @now)
              ON CONFLICT(alias, user_id) DO UPDATE SET resolved_value = @resolved, updated_at_utc = @now",
            new { alias, resolved = resolvedValue, uid, now });
        _logger.LogDebug("Alias upserted: {Alias}", alias);
    }

    public async Task<IReadOnlyList<AliasRecord>> ListAllAsync(string? userId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var uid = userId ?? "";
        var rows = await conn.QueryAsync<(string alias, string resolved, string? user_id, string updated)>(
            "SELECT alias, resolved_value, user_id, updated_at_utc FROM aliases WHERE user_id = @uid",
            new { uid });
        return rows.Select(r => new AliasRecord
        {
            Alias = r.alias,
            ResolvedValue = r.resolved,
            UserId = r.user_id,
            UpdatedAtUtc = DateTime.Parse(r.updated)
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
