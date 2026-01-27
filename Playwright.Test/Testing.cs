using Playwright.SDK;

namespace Playwright.Test
{
  public class Testing : IDisposable
  {
    public const string EdgeExecutablePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
    public const string ChromeExecutablePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

    private IDriver driver;
    private readonly string rootDirectory;

    public Testing()
    {
      driver = DriverFactory.CreateDriver().Result;

      rootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "results");

      if (!Directory.Exists(rootDirectory)) 
      {
        Directory.CreateDirectory(rootDirectory);
      }
    }

    public void Dispose()
    {
      driver.Dispose();
    }

    private string SerializeToJson<T>(IEnumerable<T> values)
    {
      var options = new System.Text.Json.JsonSerializerOptions()
      {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
      };

      return System.Text.Json.JsonSerializer.Serialize(values, options);
    }

    [Fact]
    public async Task TestMultiNavigation()
    {
      var browser = await driver.OpenBrowser(EdgeExecutablePath, BrowserType.Chromium, false);
      await browser.Navigate("https://supermercado.eroski.es/");
      var links = await browser.QuerySelectorAll(".nav-item > .not_clickable", item => item.GetAttributeAsync("href"));

      Assert.NotEmpty(links);

      foreach(string? url in links)
      {
        if (!string.IsNullOrEmpty(url))
        {
          bool ok = await browser.Navigate(url);
          Assert.True(ok);
        }

        await Task.Delay(1000);
      }
    }

    [Fact]
    public async Task TestWebScraping_1()
    {
      var browser = await driver.OpenBrowser(EdgeExecutablePath, BrowserType.Chromium, false);

      await browser.Navigate("https://supermercado.eroski.es/es/supermercado/2060538-limpieza/2060597-papel-cocina-y-servilletas");
      await browser.WaitForSelector("#onetrust-accept-btn-handler");
      await browser.Click("#onetrust-accept-btn-handler");

      string script = @"
        ﻿async (selector) =>
        {
          await new Promise((resolve) =>
          {
            let h = 0;
            let offset = document.body.scrollHeight * 0.01;
            let timer = setInterval(() =>
            {
              let scrollHeight = document.body.scrollHeight;
              window.scrollBy(0, offset);
              h += offset;
              if(h >= scrollHeight - window.innerHeight)
              {
                console.log('bottom');
                let loadingElement = document.querySelector(selector);
                if(loadingElement == null)
                {
                  clearInterval(timer);
                  resolve();
                } else { h -= offset; }
              }
            }, 10);
          });
        }";

      await browser.EvaluateFunction(script, ".ajax-loading");

      // Classname selectors
      var ruleSet = new Dictionary<string, string>()
      {
        { "Title", "[class*='product-title']" },
        { "Price", "[class*='price-offer-now']" }
      };

      var results = await browser.ExtractElements(".product-item-lineal", ruleSet);

      Assert.NotEmpty(results);
      var uniques = results.GroupBy(e => e.Title).Select(group => group.First());
      File.WriteAllText(Path.Combine(rootDirectory, $"result-eroski.json"), SerializeToJson(uniques));
    }

    [Fact]
    public async Task TestWebScraping_2()
    {
      var browser = await driver.OpenBrowser(EdgeExecutablePath, BrowserType.Chromium, false);

      await browser.Navigate("https://www.compraonline.bonpreuesclat.cat/categories/alimentaci%C3%B3/olis-vinagres-sals-i-esp%C3%A8cies/2822dcdb-e162-4e98-8b4d-42801e26448a");
      await browser.WaitForSelector("#onetrust-accept-btn-handler");
      await browser.Click("#onetrust-accept-btn-handler");

      string script = @"
          (offset) =>
          {
            window.scrollBy(0, offset);
            if (window.innerHeight + window.scrollY >= document.body.scrollHeight)
              return false;
            return true;
          }";

      List<dynamic> results = [];

      // Data selectors
      var ruleSet = new Dictionary<string, string>()
      {
        { "Title", "[data-test*='fop-title']" },
        { "Price", "[data-test*='fop-price']:not([data-test*='fop-price-wrapper']):not([data-test*='fop-price-per-unit'])" }
      };

      do
      {
        var elements = await browser.ExtractElements(".product-card-container", ruleSet);

        results.AddRange(elements);
      } while (await browser.EvaluateFunction<bool>(script, 300));

      Assert.NotEmpty(results);
      var uniques = results.GroupBy(e => e.Title).Select(group => group.First());
      File.WriteAllText(Path.Combine(rootDirectory, $"result-bonpreu.json"), SerializeToJson(uniques));
    }

    [Fact]
    public async Task TestWebScraping_3()
    {
      var browser = await driver.OpenBrowser(EdgeExecutablePath, BrowserType.Chromium, false);

      await browser.Navigate("https://www.dia.es/frutas/manzanas-y-peras/c/L2032");
      await browser.WaitForSelector("#onetrust-accept-btn-handler");
      await browser.Click("#onetrust-accept-btn-handler");
      await browser.WaitForSelector(".product-container");

      // Data selectors
      var ruleSet = new Dictionary<string, string>()
      {
        { "Title", "[data-test-id='search-product-card-name']" },
        { "Price", "[data-test-id*='search-product-card-unit-price']" }
      };

      var results = await browser.ExtractElements(".search-product-card", ruleSet);

      Assert.NotEmpty(results);
      var uniques = results.GroupBy(e => e.Title).Select(group => group.First());
      File.WriteAllText(Path.Combine(rootDirectory, $"result-dia.json"), SerializeToJson(uniques));
    }
  }
}
