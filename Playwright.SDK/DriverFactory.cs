namespace Playwright.SDK
{
  public static class DriverFactory
  {
    public static async Task<IDriver> CreateDriver()
    {
      var instance = await Microsoft.Playwright.Playwright.CreateAsync();

      return new Driver(instance);
    }
  }
}
