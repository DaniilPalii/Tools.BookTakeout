using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Html.Parser;
using BookTakeout.Core.Exceptions;
using BookTakeout.Core.Values;
using Microsoft.Extensions.Logging;

namespace BookTakeout.Core;

public sealed partial class BookDownloader(
	LitnetHttpClient litnetHttpClient,
	ILogger<BookDownloader> logger)
{
	public Task<string> AuthenticateAsync(CancellationToken cancellationToken, bool forceRelogin = false)
		=> litnetHttpClient.AuthenticateAsync(cancellationToken, forceRelogin);

	public async Task<(EpubDocument epubDocument, ChapterInfo[] ChaptersInfo)> GetBookInfoAsync(string bookSlug, CancellationToken cancellationToken)
	{
		(var title, var author, var annotation, var series, var cover) = await litnetHttpClient.GetBookInfoWebPageAsync(bookSlug, cancellationToken);

		var epubDocument = new EpubDocument(title)
		{
			Author = author,
			Annotation = annotation,
			Identifier = bookSlug,
			Cover = cover,
			Series = series,
		};

		var chaptersInfo = await litnetHttpClient.GetBookChaptersAsync(bookSlug, cancellationToken);
		LogTotalNumberOfChapters(chaptersInfo.Length);

		return (epubDocument, chaptersInfo);
	}

	public async Task LoadChaptersAsync(
		string bookSlug,
		EpubDocument epubDocument,
		ChapterInfo[] chaptersInfo,
		CancellationToken cancellationToken,
		Action<int>? onChapterLoaded)
	{
		try
		{
			foreach (var chapter in chaptersInfo)
			{
				var chapterContent = await GetChapterContentAsync(bookSlug, chapter, epubDocument, cancellationToken);
				epubDocument.Chapters.Add(new(chapter.Title, chapterContent));
				onChapterLoaded?.Invoke(chapter.Index);

				if (cancellationToken.IsCancellationRequested)
					break;
			}
		}
		catch (NoDataException ex)
		{
			LogErrorWhileGettingChaptersSavingAvailableData(ex);
		}
	}

	private async Task<string> GetChapterContentAsync(
		string bookSlug,
		ChapterInfo chapter,
		EpubDocument epubDocument,
		CancellationToken cancellationToken)
	{
		var chapterContentBuilder = new StringBuilder();

		try
		{
			var isPageLast = false;
			var pageIndex = 1;
			while (!isPageLast && !cancellationToken.IsCancellationRequested)
			{
				(var pageContent, isPageLast) = await litnetHttpClient.GetBookPageContentAsync(bookSlug, chapter.Id, pageIndex, cancellationToken);

				pageContent = await ReplaceRemoteImagesWithLocalAsync(
					pageContent,
					epubDocument,
					imageDescription: $"Illustration for chapter {chapter.Index} page {pageIndex}",
					cancellationToken);

				chapterContentBuilder.Append(pageContent);
				pageIndex++;
			}
		}
		catch (OperationCanceledException)
		{ }

		return chapterContentBuilder.ToString();
	}

	private async Task<string> ReplaceRemoteImagesWithLocalAsync(
		string pageContent,
		EpubDocument epubDocument,
		string imageDescription,
		CancellationToken cancellationToken)
	{
		var htmlParser = new HtmlParser();
		using var htmlDocument = await htmlParser.ParseDocumentAsync(pageContent);

		foreach (var imageElement in htmlDocument.Images)
		{
			var imageSource = imageElement.GetAttribute("src");

			if (string.IsNullOrWhiteSpace(imageSource))
			{
				LogImageSourceIsEmpty();
				continue;
			}

			if (!RemoteImageSourceRegex.IsMatch(imageSource))
				continue;

			var image = await litnetHttpClient.DownloadImageAsync(imageSource, imageDescription, cancellationToken);
			var localPath = epubDocument.AddIllustration(image, imageSource);
			imageElement.SetAttribute("src", localPath);
		}

		return htmlDocument.ToHtml();
	}

	[LoggerMessage(LogLevel.Information, "Total number of chapters: {ChaptersCount}")]
	private partial void LogTotalNumberOfChapters(int chaptersCount);

	[LoggerMessage(LogLevel.Information, "Got chapter {ChapterIndex}")]
	private partial void LogGotChapter(int chapterIndex);

	[LoggerMessage(LogLevel.Error, "Error while getting chapters. Saving available data.")]
	private partial void LogErrorWhileGettingChaptersSavingAvailableData(Exception exception);

	[LoggerMessage(LogLevel.Warning, "Image source is empty")]
	private partial void LogImageSourceIsEmpty();

	[GeneratedRegex(@"^(?:(?:https?:)?\/\/)")]
	private static partial Regex RemoteImageSourceRegex { get; }
}
