using System.Text;
using LitnetDownloader.ConsoleApplication.Commands;
using LitnetDownloader.ConsoleApplication.DependencyInjection;
using LitnetDownloader.Core;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.Spectre;
using Spectre.Console.Cli;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.WriteTo.Spectre(outputTemplate: "{Message:lj}{NewLine}{Exception}")
	.CreateLogger();

try
{
	var services = new ServiceCollection();

	services.AddLogging(loggingBuilder =>
	{
		loggingBuilder.AddSerilog(dispose: true);
	});

	services.AddSingleton<BookDownloader>();
	services.AddSingleton<LitnetBrowserClient>();
	services.AddSingleton<LitnetHttpClient>();

	var app = new CommandApp<DownloadBookCommand>(
		registrar: new TypeRegistrar(services));

	var returnCode = await app.RunAsync(args);

	return returnCode;
}
catch (Exception ex)
{
	Log.Fatal(ex, "Application terminated unexpectedly");

	return 1;
}
finally
{
	Log.CloseAndFlush();
}