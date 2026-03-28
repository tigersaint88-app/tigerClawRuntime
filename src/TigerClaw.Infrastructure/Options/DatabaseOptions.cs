namespace TigerClaw.Infrastructure.Options;

/// <summary>
/// Database connection options.
/// </summary>
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = "Data Source=tigerclaw.db";
    public string DataDirectory { get; set; } = "data";
}
