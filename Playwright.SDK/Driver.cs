using Microsoft.Playwright;

namespace Playwright.SDK
{
  public sealed class Driver : IDriver
  {
    private readonly IPlaywright _playwrightInstance;

    public Driver(IPlaywright playwrightInstance)
    {
      _playwrightInstance = playwrightInstance;
    }

    public void Dispose() 
    {
      _playwrightInstance.Dispose();
    }

    public async Task<IBrowserInstance> OpenBrowser(string executablePath, BrowserType type, bool headless = true)
    {
      var options = new BrowserTypeLaunchOptions 
      { 
        Headless = headless,
        ExecutablePath = executablePath 
      };

      IBrowser? browser = type switch
      {
        BrowserType.Chromium  => await _playwrightInstance.Chromium.LaunchAsync(options),
        BrowserType.Firefox   => await _playwrightInstance.Firefox.LaunchAsync(options),
        _                     => throw new Exception("invalid browser type")
      };

      return await BrowserInstance.CreateAsync(browser);
    }
  }
}
