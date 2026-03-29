namespace TigerClaw.Models;

/// <summary>Stable codes for <see cref="TaskResponse.ErrorCode"/> and workflow/skill results.</summary>
public static class TigerClawErrorCodes
{
    public const string PrerequisiteMissing = "PREREQUISITE_MISSING";
    public const string CapabilityNotMet = "CAPABILITY_NOT_MET";
    public const string EmailImapConnectFailed = "EMAIL_IMAP_CONNECT_FAILED";
    public const string EmailImapAuthFailed = "EMAIL_IMAP_AUTH_FAILED";
    public const string UnsupportedResource = "UNSUPPORTED_PREREQUISITE_RESOURCE";
    public const string WorkflowNotFound = "WORKFLOW_NOT_FOUND";
    public const string SkillNotFound = "SKILL_NOT_FOUND";
}
