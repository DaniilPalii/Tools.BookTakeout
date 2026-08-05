using System.ComponentModel;
using System.Linq;
using LitnetDownloader.Core;
using LitnetDownloader.Core.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LitnetDownloader.ConsoleApplication.Commands;

public class DownloadBookCommandSettings : CommandSettings
{
	[CommandArgument(0, "[book-urls]")]
	[Description("URLs of the books to download (space or comma separated)")]
	public string[]? BookUrls { get; set; }

	[CommandOption("-f|--forceLogin")]
	[Description("Prompt login even if previous login is saved")]
	[DefaultValue(false)]
	public bool ForceLogin { get; init; } = false;

	[CommandOption("-c|--chaptersCount")]
	[Description("The number of chapters. All if value not provided.")]
	public int? ChaptersCount { get; set; }

	[CommandOption("-i|--interactive")]
	[Description("Interactive prompt")]
	[DefaultValue(false)]
	public bool Interactive { get; set; } = false;

	[CommandOption("-d|--directory")]
	[Description("The directory where the books will be saved.")]
	public string? Directory { get; set; }
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
				AnsiConsole.MarkupLine($"[red]Error:[/] {nameof(DownloadBookCommandSettings.BookUrls)} is required in non-interactive mode.");
				return 1;
			}

			var input = AnsiConsole.Prompt(
				new TextPrompt<string>("Enter the [green]URL(s) of the book(s)[/] (separate multiple with spaces or commas):")
					.PromptStyle("cyan")
					.Validate(value =>
					{
						var parts = value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);

						if (parts.Any(p => !BookUrl.TryGetSlug(p, out _)))
							return ValidationResult.Error("[red]Please enter valid HTTP/HTTPS URL(s)[/]");

						return ValidationResult.Success();
					}));

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
					AnsiConsole.MarkupLine($"[yellow]Warning:[/] Invalid book URL skipped: {url}");
			}
		}

		if (slugs.Count == 0)
		{
			AnsiConsole.MarkupLine("[red]No valid book URLs provided.[/]");
			return 1;
		}

		await bookDownloader.AuthenticateAsync(cancellationToken, settings.ForceLogin);
		var downloadsPath = settings.Directory ?? OsLocations.GetDownloadsPath();

		foreach (var bookSlug in slugs)
		{
			AnsiConsole.MarkupLine($"[cyan]Downloading book:[/] {bookSlug}");
			try
			{
				var epubDocument = await bookDownloader.DownloadAsEpubAsync(
					bookSlug,
					cancellationToken,
					chapterRange: settings.ChaptersCount.HasValue ? ..settings.ChaptersCount.Value : null);

				epubDocument.Series ??= AnsiConsole.Prompt(
					new TextPrompt<string?>($"Enter series name for book [green]{epubDocument.Title}[/] (optional):")
						.AllowEmpty()
						.DefaultValue(null));

				var filePath = epubDocument.WriteToFile(location: downloadsPath);
				AnsiConsole.MarkupLine($"[green]Book saved to file:[/]\n\t\"{filePath}\"");
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine($"[red]Failed to download book {bookSlug}:[/] {ex.Message}");
			}
		}

		AnsiConsole.MarkupLine("Done");

		if (settings.Interactive)
		{
			AnsiConsole.MarkupLine("Press any key to exit...");
			Console.ReadKey(intercept: true);
		}

		return 0;
	}
}