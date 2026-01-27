using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace Playwright.SDK
{
  public sealed class BrowserInstance : IBrowserInstance
  {
    private readonly IBrowser _browser;
    private readonly IBrowserContext _context;
    private readonly IPage _browserPage;

    private BrowserInstance(IBrowser browser, IBrowserContext context, IPage page)
    {
      _browser = browser;
      _context = context;
      _browserPage = page;
    }

    public static async Task<IBrowserInstance> CreateAsync(IBrowser browser)
    {
      var context = await browser.NewContextAsync();
      var page = await context.NewPageAsync();

      return new BrowserInstance(browser, context, page);
    }

    public async ValueTask DisposeAsync()
    {
      await _context.DisposeAsync();
      await _browser.DisposeAsync();
    }

    public async Task<bool> Navigate(string url)
    {
      var response = await _browserPage.GotoAsync(url, new PageGotoOptions() { WaitUntil = WaitUntilState.DOMContentLoaded });

      return response?.Ok ?? false;
    }

    public async Task WaitForSelector(string selector, int timeout = 5000)
    {
      var options = new PageWaitForSelectorOptions()
      {
        Strict = true,
        Timeout = timeout
      };

      await _browserPage.WaitForSelectorAsync(selector, options);
    }

    public async Task Click(string selector, int timeout = 5000)
    {
      var options = new PageClickOptions()
      {
        Strict = true,
        Timeout = timeout
      };

      await _browserPage.ClickAsync(selector, options);
    }

    public async Task<List<T>> QuerySelectorAll<T>(string selector, Func<ILocator, Task<T>> mapNode)
    {
      var results = new List<T>();
      var locator = _browserPage.Locator(selector);

      if (locator is not null)
      {
        for (int i = 0; i < await locator.CountAsync(); i++)
        {
          var node = locator.Nth(i);
          T value = await mapNode(node);
          results.Add(value);
        }
      }

      return results;
    }

    public async Task EvaluateFunction(string script, object? arguments = null)
    {
      await _browserPage.EvaluateAsync(script, arguments);
    }

    public async Task<T> EvaluateFunction<T>(string script, object? arguments = null)
    {
      return await _browserPage.EvaluateAsync<T>(script, arguments);
    }

    /// <summary>
    /// Extracts structured content from a given html container
    /// </summary>
    /// <param name="selector">Target html container</param>
    /// <param name="ruleSet">Dictionary: PropertyName | Class selector</param>
    /// <returns></returns>
    public async Task<IEnumerable<dynamic>> ExtractElements(string selector, Dictionary<string, string> ruleSet)
    {
      List<System.Dynamic.ExpandoObject> results = [];

      var locator = _browserPage.Locator(selector);

      if (locator is not null)
      {
        for (int i = 0; i < await locator.CountAsync(); i++)
        {
          string? text = await locator.Nth(i).InnerTextAsync();

          var item = new System.Dynamic.ExpandoObject();
          var dict = (IDictionary<string, object>)item;

          bool skip = false;

          if (!string.IsNullOrEmpty(text))
          {
            foreach (var rule in ruleSet)
            {
              var node = locator.Nth(i).Locator(rule.Value);
              if (node is not null && await node.CountAsync() > 0)
              {
                List<string> contents = [];
                for (int index = 0; index < await node.CountAsync(); index++)
                {
                  var innerNode = node.Nth(index);
                  if (innerNode is not null && await innerNode.IsVisibleAsync())
                  {
                    var className = await innerNode.GetAttributeAsync("class");
                    if (!string.IsNullOrEmpty(className) && className.Contains("hidden"))
                      continue;

                    contents.Add(await innerNode.InnerTextAsync());
                  }
                }

                string? innerText = contents.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(innerText))
                {
                  dict[rule.Key] = Regex.Replace(System.Net.WebUtility.HtmlDecode(innerText), @"\s+", " ").Trim();
                }
              }
            }

            if (!skip)
              results.Add(item);
          }

        }
      }


      return results;
    }
  }
}
