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

    /// <summary>
    /// Navigates the browser page to the specified URL and waits until the DOM content is fully loaded.
    /// </summary>
    /// <remarks>This method uses the browser's GotoAsync operation with a wait condition of DOMContentLoaded.
    /// The method returns false if the navigation fails or the response is not successful.</remarks>
    /// <param name="url">The URL to navigate to. This must be a valid, absolute URL string.</param>
    /// <returns>true if the navigation completes successfully and the response status is OK; otherwise, false.</returns>
    public async Task<bool> Navigate(string url)
    {
      var response = await _browserPage.GotoAsync(url, new PageGotoOptions() { WaitUntil = WaitUntilState.DOMContentLoaded });

      return response?.Ok ?? false;
    }

    /// <summary>
    /// Waits asynchronously for an element matching the specified selector to appear in the DOM within the given
    /// timeout period.
    /// </summary>
    /// <remarks>Throws an exception if the selector does not appear within the specified timeout period. The
    /// wait is performed in strict mode, requiring an exact match for the selector.</remarks>
    /// <param name="selector">The CSS selector used to identify the element to wait for. Cannot be null or empty.</param>
    /// <param name="timeout">The maximum time, in milliseconds, to wait for the selector to appear. The default value is 5000 milliseconds.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    public async Task WaitForSelector(string selector, int timeout = 5000)
    {
      var options = new PageWaitForSelectorOptions()
      {
        Strict = true,
        Timeout = timeout
      };

      await _browserPage.WaitForSelectorAsync(selector, options);
    }

    /// <summary>
    /// Clicks the specified element on the current browser page, waiting for the element to become available within the
    /// given timeout period.
    /// </summary>
    /// <remarks>An exception is thrown if the element specified by the selector is not found within the
    /// timeout period.</remarks>
    /// <param name="selector">The CSS selector that identifies the element to click. The selector must match an existing element on the page.</param>
    /// <param name="timeout">The maximum time, in milliseconds, to wait for the element to become available before the operation times out.
    /// The default value is 5000 milliseconds.</param>
    /// <returns>A task that represents the asynchronous click operation.</returns>
    public async Task Click(string selector, int timeout = 5000)
    {
      var options = new PageClickOptions()
      {
        Strict = true,
        Timeout = timeout
      };

      await _browserPage.ClickAsync(selector, options);
    }

    /// <summary>
    /// Retrieves all elements that match the specified CSS selector and asynchronously maps each element to a value
    /// using the provided mapping function.
    /// </summary>
    /// <remarks>This method is useful for scenarios where you need to process multiple elements on a page
    /// asynchronously. The mapping function is invoked for each matched element in the order they appear in the
    /// DOM.</remarks>
    /// <typeparam name="T">The type of the value returned by the mapping function for each matched element.</typeparam>
    /// <param name="selector">The CSS selector used to identify elements to retrieve from the page.</param>
    /// <param name="mapNode">A function that receives an element locator and returns a task representing the mapped value for that element.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a list of values produced by applying the
    /// mapping function to each matched element.</returns>
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

    /// <summary>
    /// Executes the specified JavaScript code within the context of the current browser page.
    /// </summary>
    /// <remarks>Use this method to interact with the page's DOM or JavaScript environment by executing custom
    /// scripts. The script is evaluated in the context of the currently loaded page.</remarks>
    /// <param name="script">The JavaScript code to evaluate in the browser context. This parameter cannot be null or empty.</param>
    /// <param name="arguments">An optional object containing arguments to pass to the script during execution. May be null if no arguments are
    /// required.</param>
    /// <returns>A task that represents the asynchronous operation of evaluating the script.</returns>
    public async Task EvaluateFunction(string script, object? arguments = null)
    {
      await _browserPage.EvaluateAsync(script, arguments);
    }

    /// <summary>
    /// Evaluates the specified JavaScript code within the context of the current browser page and returns the result as
    /// the specified type.
    /// </summary>
    /// <remarks>This method is asynchronous and executes the script in the context of the currently active
    /// browser page. If the script execution fails or the arguments are invalid, an exception may be thrown.</remarks>
    /// <typeparam name="T">The type to which the result of the evaluated script is cast.</typeparam>
    /// <param name="script">The JavaScript code to execute in the browser context. This code should return a value compatible with the
    /// specified type parameter.</param>
    /// <param name="arguments">An optional object containing arguments to pass to the script during execution. May be null if no arguments are
    /// required.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the value returned by the script,
    /// cast to the specified type.</returns>
    public async Task<T> EvaluateFunction<T>(string script, object? arguments = null)
    {
      return await _browserPage.EvaluateAsync<T>(script, arguments);
    }

    /// <summary>
    /// Asynchronously extracts elements from the browser page that match the specified CSS selector and applies
    /// extraction rules to retrieve relevant inner text.
    /// </summary>
    /// <remarks>Elements that are hidden or do not contain visible text are skipped. The method decodes HTML
    /// entities and normalizes whitespace in the extracted text. Only the first visible inner text for each rule is
    /// included in the result.</remarks>
    /// <param name="selector">The CSS selector used to locate elements on the browser page. Cannot be null or empty.</param>
    /// <param name="ruleSet">A dictionary containing key-value pairs where each key represents a field name and each value is a CSS selector
    /// used to extract additional data from the located elements. Cannot be null.</param>
    /// <returns>A collection of dynamic objects, each representing an extracted element with properties defined by the ruleSet.
    /// The collection is empty if no matching elements are found.</returns>
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
                else
                {
                  dict[rule.Key] = string.Empty;
                }
              }
            }

            results.Add(item);
          }

        }
      }


      return results;
    }
  }
}
