namespace Playwright.SDK
{
  public interface IDriver : IDisposable
  {
    public Task<IBrowserInstance> OpenBrowser(string executablePath, BrowserType type, bool headless = true);
  }
}
