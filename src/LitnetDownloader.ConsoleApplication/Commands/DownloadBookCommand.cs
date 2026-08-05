using System.ComponentModel;
using LitnetDownloader.Core;
using LitnetDownloader.Core.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LitnetDownloader.ConsoleApplication.Commands;

public class DownloadBookCommandSettings : CommandSettings
{
	[CommandArgument(0, "[book-url]")]
	[Description("URL of the book to download")]
	public string? BookUrl { get; set; }

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
}

public class DownloadBookCommand(BookDownloader bookDownloader)
	: AsyncCommand<DownloadBookCommandSettings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		DownloadBookCommandSettings settings,
		CancellationToken cancellationToken)
	{
		string bookSlug = null!;

		if (string.IsNullOrWhiteSpace(settings.BookUrl))
		{
			settings.Interactive = true;

			if (!AnsiConsole.Profile.Capabilities.Interactive)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] {nameof(BookUrl)} is required in non-interactive mode.");
				return 1;
			}

			settings.BookUrl = AnsiConsole.Prompt(
				new TextPrompt<string>("Enter the [green]URL of the book[/]:")
					.PromptStyle("cyan")
					.Validate(url => BookUrl.TryGetSlug(url, out bookSlug)
						? ValidationResult.Success()
						: ValidationResult.Error("[red]Please enter a valid HTTP/HTTPS URL[/]")));
		}
		else
		{
			if (!BookUrl.TryGetSlug(settings.BookUrl, out bookSlug))
			{
				AnsiConsole.MarkupLine("[red]Invalid book URL.[/]");
				return 1;
			}
		}

		await bookDownloader.AuthenticateAsync(cancellationToken, settings.ForceLogin);

		var epubDocument = await bookDownloader.DownloadAsEpubAsync(
			bookSlug,
			cancellationToken,
			chapterRange: settings.ChaptersCount.HasValue ? ..settings.ChaptersCount.Value : null);

		epubDocument.Series ??= AnsiConsole.Prompt(
			new TextPrompt<string?>("Enter series name")
				.AllowEmpty()
				.DefaultValue(null));

		var filePath = epubDocument.WriteToFile();
		AnsiConsole.MarkupLine($"[green]Book saved to file:[/]\n\t\"{filePath}\"");
		AnsiConsole.MarkupLine("Done");

		if (settings.Interactive)
		{
			AnsiConsole.MarkupLine("Press any key to exit...");
			Console.ReadKey(intercept: true);
		}

		return 0;
	}
}