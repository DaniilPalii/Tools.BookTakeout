using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using BookTakeout.Core.Exceptions;

namespace BookTakeout.Core.Parsing;

internal record AccountWebPage(
	string UserName)
{
	public static async Task<AccountWebPage> ParseAsync(string webPageHtml, IHtmlParser htmlParser)
	{
		using var htmlDocument = await htmlParser.ParseDocumentAsync(webPageHtml);

		return new(
			UserName: GetUserName(htmlDocument));
	}

	private static string GetUserName(IHtmlDocument htmlDocument)
	{
		return htmlDocument.QuerySelector(".u_name")?.TextContent.Trim()
			?? throw new NoDataException("User name not found");
	}
}
