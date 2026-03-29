using System.Net.Sockets;
using System.Text;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using TigerClaw.Core;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Fetches unread (not-seen) messages over IMAP using preferences under <c>email.accounts.{accountId}.*</c>.
/// Secrets: <c>email.accounts.{id}.password</c> or <c>email.auth_profiles.{authProfile}.password</c>.
/// Set <c>TIGERCLAW_EMAIL_DRY_RUN=true</c> to skip the network (tests/CI).
/// </summary>
public class EmailFetchUnreadSkill : Core.ISkill
{
    static EmailFetchUnreadSkill()
    {
        // GB2312/GBK/GB18030 等不在 .NET Core 默认编码集中；未注册时 MimeKit 解码主题/正文会乱码。
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public const string DryRunEnvVar = "TIGERCLAW_EMAIL_DRY_RUN";

    public string Id => "email.fetch_unread";

    private readonly IPreferenceService _preferences;
    private readonly ILogger<EmailFetchUnreadSkill> _logger;

    public EmailFetchUnreadSkill(IPreferenceService preferences, ILogger<EmailFetchUnreadSkill> logger)
    {
        _preferences = preferences;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> inputs,
        TaskExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var maxMessages = ReadIntInput(inputs, "maxMessages", 50);
        var folderName = ReadStringInput(inputs, "folder") ?? "INBOX";
        var maxPlainChars = ReadBodyCap(inputs, "maxPlainChars", 32_768);
        var maxHtmlChars = ReadBodyCap(inputs, "maxHtmlChars", 65_536);
        var digestBodyCap = ReadBodyCap(inputs, "digestBodyCharsPerMail", 4_096);

        var accountId = await ResolveAccountIdAsync(context, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            const string msg = "缺少邮箱账号：请设置 preference email.default_account_id，或在任务变量中传入 accountId。";
            return WaitingForConfig(TigerClawErrorCodes.PrerequisiteMissing, msg, new PrerequisiteIssue
            {
                Kind = "resource",
                Key = "email.default_account_id",
                Code = "missing_email_account_id",
                Message = msg,
                InteractionHint = "请设置「email.default_account_id」或通过工作流变量提供 accountId，保存后重新运行工作流。"
            });
        }

        var host = await _preferences.GetAsync($"email.accounts.{accountId}.host", context.UserId, cancellationToken).ConfigureAwait(false);
        var portStr = await _preferences.GetAsync($"email.accounts.{accountId}.port", context.UserId, cancellationToken).ConfigureAwait(false);
        var username = await _preferences.GetAsync($"email.accounts.{accountId}.username", context.UserId, cancellationToken).ConfigureAwait(false);
        var authProfile = await _preferences.GetAsync($"email.accounts.{accountId}.authProfile", context.UserId, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
        {
            var issues = new List<PrerequisiteIssue>();
            if (string.IsNullOrWhiteSpace(host))
                issues.Add(MissingPref($"email.accounts.{accountId}.host", "缺少 IMAP 主机名。"));
            if (string.IsNullOrWhiteSpace(username))
                issues.Add(MissingPref($"email.accounts.{accountId}.username", "缺少 IMAP 用户名。"));
            var msg = "邮箱尚未配置或信息不完整：请先设置 IMAP 主机与用户名（preferences 或演示页）。";
            return WaitingForConfig(TigerClawErrorCodes.PrerequisiteMissing, msg, issues.Count > 0 ? issues.ToArray() : Array.Empty<PrerequisiteIssue>());
        }

        if (!int.TryParse((portStr ?? "993").Trim(), out var port) || port <= 0 || port > 65535)
        {
            var msg = $"IMAP 端口无效：{portStr}";
            return WaitingForConfig(TigerClawErrorCodes.PrerequisiteMissing, msg, MissingPref($"email.accounts.{accountId}.port", msg));
        }

        var password = await ResolvePasswordAsync(accountId, authProfile, context.UserId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(password))
        {
            var msg = "缺少 IMAP 密码：请设置 email.accounts.{账号}.password 或 email.auth_profiles.{profile}.password。";
            return WaitingForConfig(TigerClawErrorCodes.PrerequisiteMissing, msg,
                MissingPref($"email.accounts.{accountId}.password", "请填写该账号密码或应用专用密码（也可写在 email.auth_profiles 下）。"));
        }

        if (IsDryRun())
        {
            _logger.LogInformation("email.fetch_unread dry-run account={Account} taskId={TaskId}", accountId, context.TaskId);
            return new SkillExecutionResult
            {
                Success = true,
                Output = new
                {
                    dryRun = true,
                    accountId,
                    host,
                    port,
                    count = 0,
                    emails = Array.Empty<object>(),
                    digestText = "[dry-run] Skipped IMAP; no mail bodies to summarize.",
                    message = "TIGERCLAW_EMAIL_DRY_RUN is set; skipped IMAP connection."
                }
            };
        }

        if (EmailConfigValidation.IsPlaceholderHost(host))
        {
            var hostKey = $"email.accounts.{accountId}.host";
            var msg = "邮箱尚未完成有效配置：不能使用示例/占位域名作为 IMAP 主机（已跳过连接）。请修改「" + hostKey + "」为真实服务商地址，或先清除邮件配置后重新填写。";
            return WaitingForConfig(TigerClawErrorCodes.PrerequisiteMissing, msg, new PrerequisiteIssue
            {
                Kind = "preference",
                Key = hostKey,
                Code = "placeholder_imap_host",
                Message = msg,
                InteractionHint = "请删除示例域名（如 imap.example.com），改为真实 IMAP 主机名，保存后重试。"
            });
        }

        var useSsl = await ReadUseSslAsync(accountId, port, context.UserId, cancellationToken).ConfigureAwait(false);

        var phase = "connect";
        try
        {
            using var client = new ImapClient();
            client.Timeout = 60_000;

            var socket = useSsl
                ? (port == 993 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable)
                : SecureSocketOptions.None;

            await client.ConnectAsync(host, port, socket, cancellationToken).ConfigureAwait(false);
            phase = "auth";
            await client.AuthenticateAsync(username, password, cancellationToken).ConfigureAwait(false);
            phase = "imap";

            var folder = await client.GetFolderAsync(folderName, cancellationToken).ConfigureAwait(false);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);

            var uids = await folder.SearchAsync(SearchQuery.NotSeen, cancellationToken).ConfigureAwait(false);
            var list = new List<object>();
            var digestChunks = new List<string>();
            var take = Math.Min(maxMessages, uids.Count);
            var start = Math.Max(0, uids.Count - take);
            for (var i = start; i < uids.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var msg = await folder.GetMessageAsync(uids[i], cancellationToken).ConfigureAwait(false);
                var subject = msg.Subject ?? "";
                var sender = msg.From.Mailboxes.FirstOrDefault()?.Address ?? "";
                var date = msg.Date.UtcDateTime.ToString("O");
                var plainFull = msg.TextBody ?? "";
                if (string.IsNullOrEmpty(plainFull) && msg.HtmlBody != null)
                    plainFull = StripHtml(msg.HtmlBody);
                var plainBody = TruncateBody(plainFull, maxPlainChars);
                var htmlBody = TruncateBody(msg.HtmlBody ?? "", maxHtmlChars);
                var snippet = SnippetFromParts(plainFull, msg.HtmlBody, 800);
                var digestPlain = TruncateBody(string.IsNullOrEmpty(msg.TextBody) && msg.HtmlBody != null ? StripHtml(msg.HtmlBody) : msg.TextBody ?? "", digestBodyCap);
                digestChunks.Add($"Subject: {subject}\nFrom: {sender}\nDate: {date}\n\n{digestPlain}");
                list.Add(new
                {
                    subject,
                    sender,
                    date,
                    bodySnippet = snippet,
                    textBody = plainBody,
                    htmlBody,
                    attachments = ListAttachments(msg)
                });
            }

            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            var digestText = digestChunks.Count == 0
                ? "(无未读邮件)"
                : string.Join("\n\n---\n\n", digestChunks);
            digestText = TruncateBody(digestText, 120_000);

            _logger.LogInformation("email.fetch_unread fetched {Count} unseen (cap {Cap}) account={Account} taskId={TaskId}", list.Count, maxMessages, accountId, context.TaskId);
            return new SkillExecutionResult
            {
                Success = true,
                Output = new { count = list.Count, unseenTotal = uids.Count, digestText, emails = list }
            };
        }
        catch (Exception ex)
        {
            if (phase == "connect" && IsDnsOrTcpFailure(ex))
            {
                var hostKey = $"email.accounts.{accountId}.host";
                var msg = $"无法连接 IMAP 服务器（{host}）：{ex.Message}";
                _logger.LogInformation(
                    "email.fetch_unread host unreachable host={Host} account={Account} taskId={TaskId}: {Reason}",
                    host, accountId, context.TaskId, ex.Message);
                return WaitingForConfig(TigerClawErrorCodes.EmailImapConnectFailed, msg, new PrerequisiteIssue
                {
                    Kind = "network",
                    Key = hostKey,
                    Code = "imap_connect_failed",
                    Message = msg,
                    InteractionHint = EmailConfigValidation.IsPlaceholderHost(host)
                        ? $"「{host}」为示例占位主机。请将「{hostKey}」改为真实 IMAP 地址，保存后重新运行工作流。"
                        : $"请确认「{hostKey}」为可解析、可访问的真实 IMAP 主机，并检查端口与 useSsl；修改 preferences 后重新运行工作流。"
                });
            }

            if (phase == "auth")
            {
                var msg = $"IMAP 认证失败：{ex.Message}";
                _logger.LogInformation(
                    "email.fetch_unread auth failed account={Account} taskId={TaskId}: {Reason}",
                    accountId, context.TaskId, ex.Message);
                return WaitingForConfig(TigerClawErrorCodes.EmailImapAuthFailed, msg,
                    MissingPref($"email.accounts.{accountId}.password", "请核对用户名与密码（或应用专用密码），更新 email.accounts.{id}.password 或 email.auth_profiles.{profile}.password 后重试。"));
            }

            _logger.LogWarning(ex, "email.fetch_unread failed account={Account} phase={Phase} taskId={TaskId}", accountId, phase, context.TaskId);
            return new SkillExecutionResult
            {
                Success = false,
                Message = $"IMAP error: {ex.Message}",
                ErrorCode = TigerClawErrorCodes.EmailImapConnectFailed
            };
        }
    }

    private static bool IsDnsOrTcpFailure(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            if (e is SocketException se)
            {
                if (se.SocketErrorCode is SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain
                    or SocketError.NetworkUnreachable or SocketError.ConnectionRefused or SocketError.TimedOut)
                    return true;
            }
        }

        return ex is SocketException;
    }

    private static PrerequisiteIssue MissingPref(string key, string message) => new()
    {
        Kind = "preference",
        Key = key,
        Code = "missing_or_invalid_preference",
        Message = message,
        InteractionHint = $"请通过 POST /memory/preferences 写入「{key}」，然后重新运行同一工作流。",
        MaskKeyInUi = PrerequisiteSensitive.ShouldMaskPreferenceKey(key)
    };

    private static SkillExecutionResult WaitingForConfig(string errorCode, string message, params PrerequisiteIssue[] issues)
    {
        var list = issues.Length > 0 ? issues : Array.Empty<PrerequisiteIssue>();
        var interaction = PrerequisiteInteractionFormatter.Format(list, message);
        return new SkillExecutionResult
        {
            Success = false,
            WaitingHuman = true,
            ErrorCode = errorCode,
            Message = message,
            Issues = list,
            Output = new { errorCode, issues = list, interactionMessage = interaction }
        };
    }

    private static bool IsDryRun()
    {
        var v = Environment.GetEnvironmentVariable(DryRunEnvVar);
        return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || v == "1";
    }

    private async Task<string?> ResolveAccountIdAsync(TaskExecutionContext context, CancellationToken cancellationToken)
    {
        if (context.Variables.TryGetValue("accountId", out var v) && v != null)
        {
            var s = v.ToString();
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
        }

        return await _preferences.GetAsync("email.default_account_id", context.UserId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> ResolvePasswordAsync(string accountId, string? authProfile, string? userId, CancellationToken cancellationToken)
    {
        var direct = await _preferences.GetAsync($"email.accounts.{accountId}.password", userId, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(direct)) return direct;

        if (!string.IsNullOrWhiteSpace(authProfile))
        {
            var profilePass = await _preferences.GetAsync($"email.auth_profiles.{authProfile.Trim()}.password", userId, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(profilePass)) return profilePass;
        }

        return null;
    }

    private async Task<bool> ReadUseSslAsync(string accountId, int port, string? userId, CancellationToken cancellationToken)
    {
        var raw = await _preferences.GetAsync($"email.accounts.{accountId}.useSsl", userId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
            return port != 143;

        return !string.Equals(raw.Trim(), "false", StringComparison.OrdinalIgnoreCase) && raw.Trim() != "0";
    }

    private static int ReadIntInput(IReadOnlyDictionary<string, object?> inputs, string key, int defaultValue)
    {
        if (!inputs.TryGetValue(key, out var v) || v == null) return defaultValue;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), out var n) && n > 0) return Math.Min(n, 500);
        return defaultValue;
    }

    private static string? ReadStringInput(IReadOnlyDictionary<string, object?> inputs, string key)
    {
        if (!inputs.TryGetValue(key, out var v) || v == null) return null;
        var s = v.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static int ReadBodyCap(IReadOnlyDictionary<string, object?> inputs, string key, int defaultValue)
    {
        if (!inputs.TryGetValue(key, out var v) || v == null) return defaultValue;
        if (v is int i) return Math.Clamp(i, 0, 512_000);
        if (int.TryParse(v.ToString(), out var n)) return Math.Clamp(n, 0, 512_000);
        return defaultValue;
    }

    private static string TruncateBody(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("\r\n", "\n");
        if (maxLen <= 0 || text.Length <= maxLen) return text;
        return text[..maxLen] + "…";
    }

    private static string SnippetFromParts(string? plain, string? html, int maxLen)
    {
        var text = plain;
        if (string.IsNullOrEmpty(text))
            text = html != null ? StripHtml(html) : "";
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("\r\n", "\n").Trim();
        return text.Length <= maxLen ? text : text[..maxLen] + "…";
    }

    private static object[] ListAttachments(MimeMessage msg)
    {
        var list = new List<object>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in msg.BodyParts.OfType<MimePart>())
        {
            var mime = part.ContentType?.MimeType ?? "";
            if ((mime.Equals("text/plain", StringComparison.OrdinalIgnoreCase) || mime.Equals("text/html", StringComparison.OrdinalIgnoreCase))
                && string.IsNullOrEmpty(part.FileName)
                && part.ContentDisposition?.IsAttachment != true)
                continue;

            var cd = part.ContentDisposition;
            var isInline = string.Equals(cd?.Disposition, "inline", StringComparison.OrdinalIgnoreCase);
            var isAttach = cd?.IsAttachment == true
                || !string.IsNullOrEmpty(part.FileName)
                || (isInline && mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

            if (!isAttach && string.IsNullOrEmpty(part.FileName)) continue;

            long size = TryPartOctets(part);
            var name = part.FileName ?? part.ContentType?.Name
                ?? (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "(inline image)" : "unnamed");
            var cid = part.ContentId;
            var cidNorm = string.IsNullOrEmpty(cid) ? "" : cid.Trim().TrimStart('<').TrimEnd('>');
            var dedupe = name + "|" + cidNorm + "|" + size + "|" + mime;
            if (!seen.Add(dedupe)) continue;

            list.Add(new
            {
                fileName = name,
                mimeType = string.IsNullOrEmpty(mime) ? "application/octet-stream" : mime,
                sizeBytes = size,
                isInline,
                contentId = string.IsNullOrEmpty(cidNorm) ? null : cidNorm
            });
        }

        return list.ToArray();
    }

    private static long TryPartOctets(MimePart part)
    {
        try
        {
            if (part.Content?.Stream is { CanSeek: true } s)
                return s.Length;
            using var stream = part.Content?.Open();
            if (stream is { CanSeek: true } s2)
                return s2.Length;
        }
        catch
        {
            /* ignore */
        }

        return 0;
    }

    private static string StripHtml(string html)
    {
        var sb = new StringBuilder(html.Length);
        var inTag = false;
        foreach (var c in html)
        {
            if (c == '<') inTag = true;
            else if (c == '>') inTag = false;
            else if (!inTag) sb.Append(c);
        }
        return sb.ToString();
    }
}
