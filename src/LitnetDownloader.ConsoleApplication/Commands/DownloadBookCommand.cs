using System.ComponentModel;
using LitnetDownloader.Core;
using LitnetDownloader.Core.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LitnetDownloader.ConsoleApplication.Commands;

public class DownloadBookCommandSettings : CommandSettings
{
	[CommandArgument(position: 0, template: "<book-url>")]
	[Description("URL of the book to download")]
	public required string BookUrl { get; init; }

	[CommandOption("-f|--forceLogin")]
	[Description("Prompt login even if previous login is saved")]
	[DefaultValue(false)]
	public bool ForceLogin { get; init; } = false;
}

public class DownloadBookCommand(BookDownloader bookDownloader)
	: AsyncCommand<DownloadBookCommandSettings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		DownloadBookCommandSettings settings,
		CancellationToken cancellationToken)
	{
		if (!BookUrl.TryGetSlug(settings.BookUrl, out var bookSlug))
		{
			AnsiConsole.MarkupLine("[red]Invalid book URL.[/]");
			return 1;
		}

		await bookDownloader.AuthenticateAsync(cancellationToken, settings.ForceLogin);

		var epubDocument = await bookDownloader.DownloadAsEpubAsync(
			bookSlug,
			cancellationToken,
			chapterRange: ..1);

		epubDocument.Series ??= AnsiConsole.Prompt(
			new TextPrompt<string?>("Enter series name")
				.AllowEmpty()
				.DefaultValue(null));

		var filePath = epubDocument.WriteToFile();
		AnsiConsole.MarkupLine($"[green]Book saved to file:[/]\n\t\"{filePath}\"");
		AnsiConsole.MarkupLine("Done");
		return 0;
	}
}