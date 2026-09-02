using System.ComponentModel;
using BookTakeout.Core;
using BookTakeout.Core.Helpers;
using BookTakeout.Core.Values;
using BookTakeout.Resources;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BookTakeout.ConsoleApplication.Commands;

public class DownloadBookCommandSettings : CommandSettings
{
	[CommandArgument(0, "[book-urls]")]
	[Description("URLs of the books to download (space or comma separated)")]
	public string[]? BookUrls { get; set; }

	[CommandOption("-r|--relogin")]
	[Description("Prompt login even if previous login is saved")]
	[DefaultValue(false)]
	public bool ForceLogin { get; set; }

	[CommandOption("-i|--interactive")]
	[Description("Interactive prompt")]
	[DefaultValue(false)]
	public bool Interactive { get; set; }

	[CommandOption("-d|--directory")]
	[Description("The directory where the books will be saved.")]
	public string? Directory { get; set; }

	[CommandOption("-f|--fromChapter")]
	[Description("The starting chapter number.")]
	public int? FromChapter { get; set; }

	[CommandOption("-t|--toChapter")]
	[Description("The ending chapter number.")]
	public int? ToChapter { get; set; }
}

public class DownloadBookCommand(BookDownloader bookDownloader)
	: AsyncCommand<DownloadBookCommandSettings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		DownloadBookCommandSettings settings,
		CancellationToken cancellationToken)
	{
		var slugs = new List<string>();

		if (settings.BookUrls == null || settings.BookUrls.Length == 0)
		{
			settings.Interactive = true;

			if (!AnsiConsole.Profile.Capabilities.Interactive)
			{
				AnsiConsole.MarkupLine(
					$"[red]{Titles.Error}:[/] {string.Format(Messages.ParameterXIsRequiredInNonInteractiveMode, Titles.BookUrls)}");

				return 1;
			}

			var input = await AnsiConsole.PromptAsync(
				new TextPrompt<string>($"{Prompts.BookUrls}:")
					.PromptStyle(new() { Foreground = Color.Cyan })
					.Validate(value =>
					{
						var parts = value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);

						return parts.Any(p => !BookUrl.TryGetSlug(p, out _))
							? ValidationResult.Error($"[red]{Messages.EnterValidUrls}[/]")
							: ValidationResult.Success();
					}),
				cancellationToken);

			var urls = input.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
			foreach (var url in urls)
			{
				if (BookUrl.TryGetSlug(url, out var slug))
					slugs.Add(slug);
			}
		}
		else
		{
			foreach (var url in settings.BookUrls)
			{
				if (string.IsNullOrWhiteSpace(url))
					continue;

				if (BookUrl.TryGetSlug(url, out var slug))
					slugs.Add(slug);
				else
					AnsiConsole.MarkupLine($"[yellow]{Titles.Warning}:[/] {string.Format(Messages.InvalidBookUrlSkippedX, url)}");
			}
		}

		if (slugs.Count == 0)
		{
			AnsiConsole.MarkupLine($"[red]{Messages.NoValidUrlsProvided}[/]");
			return 1;
		}

		var userName = await bookDownloader.AuthenticateAsync(cancellationToken, settings.ForceLogin);
		AnsiConsole.MarkupLine($"\n{string.Format(Messages.SuccessfullyAuthenticatedAsX, $"[green]{userName}[/]")}");

		var downloadsPath = settings.Directory ?? OsLocations.GetDownloadsPath();
		var chapterRange = (settings.FromChapter ?? 0)..(settings.ToChapter ?? ^0);
		var books = new (EpubDocument epubDocument, ChapterInfo[] ChaptersInfo)[slugs.Count];

		AnsiConsole.MarkupLine(
			slugs.Count > 1
				? "\n" + Messages.DownloadingInformationAboutAllBooks
				: "\n" + Messages.DownloadingInformationAboutBook);

		for (var i = 0; i < slugs.Count; i++)
		{
			var bookSlug = slugs[i];
			try
			{
				(var epubDocument, var chaptersInfo) = await bookDownloader.GetBookInfoAsync(
					bookSlug,
					cancellationToken);

				AnsiConsole.MarkupLine(
					$"""

						[green]"{epubDocument.Title}"[/]
						{Titles.Author}: {epubDocument.Author}
						{Titles.Series}: {epubDocument.Series ?? "—"}
						{Titles.Chapters}: {chaptersInfo.Length}
						""");

				epubDocument.Series ??= await AnsiConsole.PromptAsync(
					new TextPrompt<string?>($"[blue]{Prompts.SeriesNameOptional}:[/]")
						.AllowEmpty()
						.DefaultValue(null)
						.ShowDefaultValue(false),
					cancellationToken);

				books[i] = (epubDocument, chaptersInfo);
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine(
					$"[red]{string.Format(Messages.FailedToDownloadBookX, bookSlug)}:[/] {string.Format(Messages.ExceptionX, ex.Message)}");
			}
		}

		for (var i = 0; i < slugs.Count; i++)
		{
			(var epubDocument, var chaptersInfo) = books[i];
			var bookSlug = slugs[i];

			AnsiConsole.MarkupLine(
				slugs.Count > 1
					? "\n" + string.Format(Messages.DownloadingContentForBookX, $"[green]\"{epubDocument.Title}\"[/]")
					: "\n" + string.Format(Messages.DownloadingBookContent));

			try
			{
				chaptersInfo = chaptersInfo[chapterRange];

				await AnsiConsole
					.Progress()
					.Columns(
						new TaskDescriptionColumn(),
						new ProgressBarColumn(),
						new SpinnerColumn(Spinner.Known.Dots))
					.StartAsync(async progress =>
					{
						var task = progress.AddTask(
							string.Format(Messages.ChaptersXOfY, 0, chaptersInfo.Length),
							maxValue: chaptersInfo.Length);

						await bookDownloader.LoadChaptersAsync(
							bookSlug,
							epubDocument,
							chaptersInfo,
							cancellationToken,
							onChapterLoaded: _ =>
							{
								task.Increment(1);
								task.Description = string.Format(Messages.ChaptersXOfY, task.Value, task.MaxValue);
							});
					});

				var filePath = epubDocument.WriteToFile(location: downloadsPath);
				AnsiConsole.MarkupLine(string.Format(Messages.BookSavedToFileX, $"[green]{filePath}[/]"));
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine(
					$"[red]{string.Format(Messages.FailedToDownloadBookX, bookSlug)}[/] {string.Format(Messages.ExceptionX, ex.Message)}");
			}
		}

		AnsiConsole.MarkupLine("\n" + Messages.Done);

		if (settings.Interactive)
		{
			AnsiConsole.MarkupLine("\n" + Messages.PressAnyKeyToExit);
			Console.ReadKey(intercept: true);
		}

		return 0;
	}
}
