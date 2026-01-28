namespace Playwright.SDK
{
  public static class DriverFactory
  {
    /// <summary>
    /// Asynchronously creates and initializes a new driver instance for browser automation.
    /// </summary>
    /// <remarks>This method initializes the Playwright library and returns a driver that enables automated
    /// browser operations. The returned driver should be disposed when no longer needed to release resources.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IDriver"/> instance
    /// that can be used to control browser interactions.</returns>
    public static async Task<IDriver> CreateDriver()
    {
      var instance = await Microsoft.Playwright.Playwright.CreateAsync();

      return new Driver(instance);
    }
  }
}
