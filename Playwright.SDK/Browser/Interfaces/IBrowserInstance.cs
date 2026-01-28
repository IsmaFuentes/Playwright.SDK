using Microsoft.Playwright;

namespace Playwright.SDK
{
  public interface IBrowserInstance : IAsyncDisposable
  {
    public Task<bool> Navigate(string url);
    public Task WaitForSelector(string selector, int timeout = 5000);
    public Task Click(string selector, int timeout = 5000);
    public Task<List<T>> QuerySelectorAll<T>(string selector, Func<ILocator, Task<T>> mapNode);
    public Task EvaluateFunction(string script, object? arguments = null);
    public Task<T> EvaluateFunction<T>(string script, object? arguments = null);
    public Task<IEnumerable<dynamic>> ExtractElements(string selector, Dictionary<string, string> ruleSet);
  }
}
