using AngleSharp.Html.Dom;

namespace BookTakeout.Core.WebPages;

public interface IWebPage<out TPage>
	where TPage : IWebPage<TPage>
{
	static abstract TPage Parse(IHtmlDocument htmlDocument);
}
