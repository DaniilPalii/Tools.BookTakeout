using AngleSharp.Html.Dom;
using BookTakeout.Core.Exceptions;

namespace BookTakeout.Core.WebPages;

internal record AccountWebPage(
	string UserName)
	: IWebPage<AccountWebPage>
{
	public static AccountWebPage Parse(IHtmlDocument htmlDocument)
	{
		return new(
			UserName: GetUserName(htmlDocument));
	}

	private static string GetUserName(IHtmlDocument htmlDocument)
	{
		return htmlDocument.QuerySelector(".u_name")?.TextContent.Trim()
			?? throw new NoDataException("User name not found");
	}
}
