using Aco228.Common.LocalStorage;
using Aco228.Common.Models;
using Microsoft.Playwright;
using Soenneker.Playwrights.Extensions.Stealth;

namespace Aco228.FacebookAdLibrary.Browser;

public interface IFacebookAdLibraryBrowser : ITransient, IAsyncDisposable
{
    IPage Page { get; }
    Task Launch(string? userDataDir = null, bool openAsHeadless = false);
}

public class FacebookAdLibraryBrowser : IFacebookAdLibraryBrowser
{
    private IPlaywright _playwright;
    public IPage Page { get; private set; }
    private IBrowserContext? _context;
    
    public async Task Launch(string? userDataDir = null, bool openAsHeadless = false)
    {
        if (string.IsNullOrEmpty(userDataDir))
            userDataDir = StorageManager.Instance.GetFolder("FbAdLibrary").GetFolder("user-dir").GetDirectoryInfo().FullName;
        
        int num = Program.Main(new string[3]
        {
            "install",
            "--with-deps",
            "Chromium".ToLowerInvariant()
        });
        
        if (num != 0)
            throw new Exception($"Playwright exited with code {num}");
        
        _playwright = await Playwright.CreateAsync();

        _context = await _playwright.Chromium.LaunchPersistentContextAsync(userDataDir, options: new()
        {
            BypassCSP = true,
            IgnoreHTTPSErrors = true,
            UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
            
            Headless = openAsHeadless,
            Args = new []
            { 
                // "--auto-open-devtools-for-tabs",
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-web-security",
                "--disable-features=IsolateOrigins,site-per-process",
                "--incognito",
                "--disable-infobars",
                "--disable-site-isolation-trials",
                "--ignore-certificate-errors",
            },
        });

        var pages = _context.Pages;
        if(pages.Any())
            Page = pages.First();
        else
            Page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_playwright is IAsyncDisposable playwrightAsyncDisposable)
            await playwrightAsyncDisposable.DisposeAsync();
        else
            _playwright.Dispose();
    }
}