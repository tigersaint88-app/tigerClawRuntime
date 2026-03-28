namespace TigerClaw.Memory;

/// <summary>
/// Aggregated memory store facade.
/// </summary>
public class MemoryStore : Core.IMemoryStore
{
    public MemoryStore(Core.IPreferenceService preferences, Core.IAliasService aliases, Core.IProcedureMemoryService procedures)
    {
        Preferences = preferences;
        Aliases = aliases;
        Procedures = procedures;
    }

    public Core.IPreferenceService Preferences { get; }
    public Core.IAliasService Aliases { get; }
    public Core.IProcedureMemoryService Procedures { get; }
}
