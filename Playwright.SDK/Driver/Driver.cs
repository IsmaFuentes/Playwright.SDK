using Microsoft.Playwright;

namespace Playwright.SDK
{
  public sealed class Driver : IDriver
  {
    private readonly IPlaywright _instance;

    public Driver(IPlaywright playwrightInstance)
    {
      _instance = playwrightInstance;
    }

    public void Dispose() 
    {
      _instance.Dispose();
    }

    /// <summary>
    /// Asynchronously launches a new browser instance of the specified type using the provided executable path and
    /// options.
    /// </summary>
    /// <remarks>Ensure that the executable path points to a valid browser binary and that the specified
    /// browser type is supported. Headless mode allows the browser to run without a user interface, which is useful for
    /// automated testing scenarios.</remarks>
    /// <param name="executablePath">The full file path to the browser executable to launch. Must refer to a valid and supported browser binary.</param>
    /// <param name="type">The type of browser to launch, specified as a value from the BrowserType enumeration.</param>
    /// <param name="headless">Indicates whether the browser should run in headless mode. The default value is <see langword="false"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IBrowserInstance representing the
    /// launched browser.</returns>
    /// <exception cref="Exception">Thrown if an invalid browser type is specified in <paramref name="type"/>.</exception>
    public async Task<IBrowserInstance> OpenBrowser(string executablePath, BrowserType type, bool headless = false)
    {
      var options = new BrowserTypeLaunchOptions 
      { 
        Headless = headless,
        ExecutablePath = executablePath 
      };

      IBrowser? browser = type switch
      {
        BrowserType.Chromium  => await _instance.Chromium.LaunchAsync(options),
        BrowserType.Firefox   => await _instance.Firefox.LaunchAsync(options),
        _                     => throw new Exception("Invalid browser type!")
      };

      return await BrowserInstance.CreateAsync(browser);
    }
  }
}
