using Microsoft.Playwright;

namespace TigerClaw.Skills;

/// <summary>
/// Shared Playwright browser session used by browser skills.
/// </summary>
public sealed class BrowserAutomationSession : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    public async Task<IPage> GetPageAsync(bool headless, CancellationToken cancellationToken = default)
    {
        if (_page != null)
        {
            return _page;
        }

        _playwright ??= await Playwright.CreateAsync();
        _browser ??= await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless
        });
        _context ??= await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        return _page;
    }

    public async ValueTask DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}
