using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace LitnetDownloader.Core;

public partial class LitnetBrowserClient(
	ILogger<LitnetBrowserClient> logger)
{
	private const string LoginUrl = "https://litnet.com/auth/login?classic=1&link=https%3A%2F%2Flitnet.com%2F";

	public async Task<List<System.Net.Cookie>> AuthenticateAsync()
	{
		LogOpeningBrowserForInteractiveLogin();

		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Firefox.LaunchAsync(options: new() { Headless = false });
		var page = await browser.NewPageAsync();
		await page.GotoAsync(
			LoginUrl,
			options: new()
			{
				Timeout = TimeSpan.FromMinutes(15).Milliseconds,
			});

		await page.GetByText("Обо мне").WaitForAsync(new() { Timeout = TimeSpan.FromMinutes(15).Milliseconds });
		LogLogInConfirmed();

		var playwrightCookies = await page.Context.CookiesAsync();
		LogGotCookies(playwrightCookies.Count);

		await browser.CloseAsync();

		return playwrightCookies
			.Where(cookie => cookie.Domain is ".litnet.com" or "litnet.com")
			.Select(
				playwrightCookie => new System.Net.Cookie(playwrightCookie.Name, playwrightCookie.Value)
				{
					Domain = playwrightCookie.Domain,
					Path = playwrightCookie.Path,
					Secure = playwrightCookie.Secure,
				})
			.ToList();
	}

	[LoggerMessage(LogLevel.Information, "Opening browser for interactive login")]
	partial void LogOpeningBrowserForInteractiveLogin();

	[LoggerMessage(LogLevel.Information, "Log in confirmed")]
	partial void LogLogInConfirmed();

	[LoggerMessage(LogLevel.Information, "Got {CookiesCount} cookies")]
	partial void LogGotCookies(int cookiesCount);
}