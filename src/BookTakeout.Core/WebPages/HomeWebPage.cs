using AngleSharp.Html.Dom;
using BookTakeout.Core.Exceptions;

namespace BookTakeout.Core.WebPages;

internal record HomeWebPage(
	bool IsLoggedIn,
	string CsrfToken)
	: IWebPage<HomeWebPage>
{
	public static HomeWebPage Parse(IHtmlDocument htmlDocument)
	{
		return new(
			IsLoggedIn: GetIsLoggedIn(htmlDocument),
			CsrfToken: GetCsrfToken(htmlDocument));
	}

	private static string GetCsrfToken(IHtmlDocument htmlDocument)
	{
		return htmlDocument.QuerySelector("meta[name='csrf-token']")?.GetAttribute("content")
			?? throw new NoDataException("CSRF token not found");
	}

	private static bool GetIsLoggedIn(IHtmlDocument htmlDocument)
	{
		return !htmlDocument.QuerySelector(".ln_topbar")?.TextContent.Contains("Регистрация")
			?? throw new NoDataException("Top bar not found");
	}
}
