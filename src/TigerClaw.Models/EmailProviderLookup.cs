namespace TigerClaw.Models;

/// <summary>
/// V1 built-in IMAP knowledge base: common domains → host/port/SSL (not live web search; no API keys).
/// Extend this table for new providers; keep notes short for UI.
/// </summary>
public record EmailProviderHint(string ImapHost, int Port, bool UseSsl, string Note);

public static class EmailProviderLookup
{
    public static EmailProviderHint? Lookup(string emailOrDomain)
    {
        var s = (emailOrDomain ?? "").Trim();
        if (string.IsNullOrEmpty(s)) return null;
        var domain = s.Contains('@', StringComparison.Ordinal)
            ? s.Split('@', 2)[^1].Trim().ToLowerInvariant()
            : s.ToLowerInvariant();
        return Map.TryGetValue(domain, out var h) ? h : null;
    }

    private static readonly Dictionary<string, EmailProviderHint> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // —— 国内常用 ——
        ["sina.com"] = new EmailProviderHint("imap.sina.com", 993, true, "请在新浪邮箱网页版开启 IMAP/SMTP。"),
        ["sina.cn"] = new EmailProviderHint("imap.sina.com", 993, true, "同上。"),
        ["163.com"] = new EmailProviderHint("imap.163.com", 993, true, "请在 163 邮箱设置中开启 IMAP。"),
        ["126.com"] = new EmailProviderHint("imap.126.com", 993, true, "请在 126 邮箱设置中开启 IMAP。"),
        ["yeah.net"] = new EmailProviderHint("imap.yeah.net", 993, true, "网易 yeah 邮箱；需在网页端开启 IMAP。"),
        ["188.com"] = new EmailProviderHint("imap.188.com", 993, true, "网易 188；需在设置中开启 IMAP。"),
        ["vip.163.com"] = new EmailProviderHint("imap.vip.163.com", 993, true, "网易 VIP；需在设置中开启 IMAP。"),
        ["vip.126.com"] = new EmailProviderHint("imap.vip.126.com", 993, true, "网易 VIP；需在设置中开启 IMAP。"),
        ["qq.com"] = new EmailProviderHint("imap.qq.com", 993, true, "需生成授权码作为密码。"),
        ["foxmail.com"] = new EmailProviderHint("imap.qq.com", 993, true, "Foxmail 常走 QQ IMAP。"),
        ["139.com"] = new EmailProviderHint("imap.139.com", 993, true, "移动 139 邮箱；需在设置中开启 IMAP。"),
        ["aliyun.com"] = new EmailProviderHint("imap.aliyun.com", 993, true, "阿里邮箱；需在控制台开启 IMAP。"),
        ["sohu.com"] = new EmailProviderHint("imap.sohu.com", 993, true, "需在搜狐邮箱设置中开启 IMAP。"),
        ["tom.com"] = new EmailProviderHint("imap.tom.com", 993, true, "若无法连接请确认该域名邮箱仍提供服务。"),
        ["21cn.com"] = new EmailProviderHint("imap.21cn.com", 993, true, "电信 21CN；需在邮箱中开启 IMAP。"),

        // —— Google / Microsoft / Yahoo ——
        ["gmail.com"] = new EmailProviderHint("imap.gmail.com", 993, true, "通常需在 Google 账号中开启「应用专用密码」。"),
        ["googlemail.com"] = new EmailProviderHint("imap.gmail.com", 993, true, "同 Gmail。"),
        ["outlook.com"] = new EmailProviderHint("outlook.office365.com", 993, true, "Microsoft 365 / Outlook。"),
        ["hotmail.com"] = new EmailProviderHint("outlook.office365.com", 993, true, "同上。"),
        ["live.com"] = new EmailProviderHint("outlook.office365.com", 993, true, "同上。"),
        ["msn.com"] = new EmailProviderHint("outlook.office365.com", 993, true, "同上。"),
        ["hotmail.co.uk"] = new EmailProviderHint("outlook.office365.com", 993, true, "同上。"),
        ["outlook.jp"] = new EmailProviderHint("outlook.office365.com", 993, true, "同上。"),
        ["yahoo.com"] = new EmailProviderHint("imap.mail.yahoo.com", 993, true, "需在 Yahoo 安全设置中允许应用。"),
        ["yahoo.co.jp"] = new EmailProviderHint("imap.mail.yahoo.co.jp", 993, true, "Yahoo Japan；按页面说明开启第三方客户端。"),

        // —— Apple / 其他国际 ——
        ["icloud.com"] = new EmailProviderHint("imap.mail.me.com", 993, true, "Apple ID 需开启「应用专用密码」。"),
        ["me.com"] = new EmailProviderHint("imap.mail.me.com", 993, true, "同上。"),
        ["mac.com"] = new EmailProviderHint("imap.mail.me.com", 993, true, "同上。"),
        ["yandex.com"] = new EmailProviderHint("imap.yandex.com", 993, true, "需在 Yandex 邮箱设置中允许 IMAP。"),
        ["yandex.ru"] = new EmailProviderHint("imap.yandex.com", 993, true, "同上。"),
        ["mail.ru"] = new EmailProviderHint("imap.mail.ru", 993, true, "按 Mail.ru 安全设置开启 IMAP。"),
        ["zoho.com"] = new EmailProviderHint("imap.zoho.com", 993, true, "Zoho Mail；区域不同主机可能为 imap.zoho.eu 等。"),
        ["gmx.com"] = new EmailProviderHint("imap.gmx.com", 993, true, "GMX；需在网页端允许第三方客户端。"),
        ["gmx.de"] = new EmailProviderHint("imap.gmx.com", 993, true, "同上。"),
        ["gmx.net"] = new EmailProviderHint("imap.gmx.com", 993, true, "同上。"),
        ["web.de"] = new EmailProviderHint("imap.web.de", 993, true, "需在网页端允许 IMAP。"),
        ["naver.com"] = new EmailProviderHint("imap.naver.com", 993, true, "Naver；需按说明生成应用密码。"),
        ["daum.net"] = new EmailProviderHint("imap.daum.net", 993, true, "Daum；若失败请确认账号仍支持 IMAP。"),
        // Proton Mail 无标准公网 IMAP；需 Proton Bridge，不写入固定主机以免误导。
    };
}
