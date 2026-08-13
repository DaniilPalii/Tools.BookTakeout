using AngleSharp.Html.Dom;
using BookTakeout.Core.Exceptions;
using BookTakeout.Core.Values;

namespace BookTakeout.Core.WebPages;

internal record BookReaderWebPage(
	ChapterInfo[] Chapters)
	: IWebPage<BookReaderWebPage>
{
	public static BookReaderWebPage Parse(IHtmlDocument htmlDocument)
	{
		return new(
			Chapters: GetChapters(htmlDocument));
	}

	private static ChapterInfo[] GetChapters(IHtmlDocument htmlDocument)
	{
		var chapterIndex = 1;
		var chapters
			= htmlDocument
				.QuerySelector(selectors: "select[name='chapter']")
				?.QuerySelectorAll(selectors: "option")
				.Select(
					selector: option =>
						new ChapterInfo(
							Index: chapterIndex++,
							Id: option.GetAttribute("value") ?? throw new NoDataException("Chapter option without value"),
							Title: option.TextContent))
				.ToArray()
			?? throw new NoDataException(message: "No chapter list found");

		return chapters;
	}
}
