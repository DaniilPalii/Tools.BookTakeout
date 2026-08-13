using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using BookTakeout.Core.Exceptions;

namespace BookTakeout.Core.Parsing;

internal record HomeWebPage(
	bool IsLoggedIn,
	string CsrfToken)
{
	public static async Task<HomeWebPage> ParseAsync(string webPageHtml, IHtmlParser htmlParser)
	{
		using var htmlDocument = await htmlParser.ParseDocumentAsync(webPageHtml);

		return new(
			IsLoggedIn: GetIsLoggedIn(htmlDocument),
			CsrfToken: GetCsrfToken(htmlDocument));
	}

	private static bool GetIsLoggedIn(IHtmlDocument htmlDocument)
	{
		return !htmlDocument.QuerySelector(".ln_topbar")?.TextContent.Contains("Регистрация")
			?? throw new NoDataException("Top bar not found");
	}

	private static string GetCsrfToken(IHtmlDocument htmlDocument)
	{
		return htmlDocument.QuerySelector("meta[name='csrf-token']")?.GetAttribute("content")
			?? throw new NoDataException("CSRF token not found");
	}
}
