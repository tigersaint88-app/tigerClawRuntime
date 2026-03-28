using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Aggregated memory store facade.
/// </summary>
public interface IMemoryStore
{
    IPreferenceService Preferences { get; }
    IAliasService Aliases { get; }
    IProcedureMemoryService Procedures { get; }
}
