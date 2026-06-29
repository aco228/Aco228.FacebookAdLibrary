using Aco228.Common.LocalStorage;
using Aco228.Common.Models;
using Microsoft.Playwright;
using Soenneker.Playwrights.Extensions.Stealth;
using Soenneker.Playwrights.Extensions.Stealth.Options;

namespace Aco228.FacebookAdLibrary.Browser;

public interface IFacebookAdLibraryBrowser : ITransient, IAsyncDisposable
{
    IPage Page { get; }
    Task Launch(string? userDataDir = null, bool openAsHeadless = false);
}

public class FacebookAdLibraryBrowser : IFacebookAdLibraryBrowser
{
    private IPlaywright _playwright;
    private IBrowserContext _context;   // no IBrowser in persistent mode — the profile IS the context
    public IPage Page { get; private set; }

    private static int _installed;      // install once per process

    public async Task Launch(string? userDataDir = null, bool openAsHeadless = false)
    {
        if (string.IsNullOrEmpty(userDataDir))
            userDataDir = StorageManager.Instance.GetFolder("FbAdLibrary")
                .GetFolder("user-dir").GetDirectoryInfo().FullName;

        if (Interlocked.Exchange(ref _installed, 1) == 0)
        {
            int num = Program.Main(new[] { "install", "--with-deps", "chromium" });
            if (num != 0) throw new Exception($"Playwright install exited with code {num}");
        }

        _playwright = await Playwright.CreateAsync();

        //1) reuse the library's launch-arg hardening
        var stealthLaunch = new StealthLaunchOptions
        {
            Channel = "chromium",  // or "chrome" for a real-Chrome fingerprint
            // IncludeNoSandboxArgument / IgnoreDetectableDefaultArguments default to true
        };
        
        string[] hardenedArgs = StealthLaunchArgumentNormalizer.Normalize(
            existingArguments: new[]
            {
                // your own extras go here; they get merged + normalized
                "--ignore-certificate-errors",
            },
            isHeadlessLaunch: openAsHeadless,
            options: stealthLaunch);
        
        // 2) launch the PERSISTENT context yourself (this is what gives you user-data-dir)
        _context = await _playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new()
        {
            Headless = openAsHeadless,
            Channel = stealthLaunch.Channel,
            Args = hardenedArgs,
            // mirror what LaunchStealthChromium sets internally:
            IgnoreDefaultArgs = StealthLaunchArgumentNormalizer.DetectableDefaultArgumentsToIgnore,
        });
        
        // 3) apply the library's context-level stealth (headers, CDP hardening, init script)
        await _context.ApplyStealth(new StealthContextOptions
        {
            // e.g. NormalizeDocumentHeaders = true, InjectClientHintHeaders = true, EnableCdpDomainHardening = true
        });

        Page = _context.Pages.Count > 0 ? _context.Pages[0] : await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null) await _context.DisposeAsync();
        _playwright?.Dispose();
    }
}