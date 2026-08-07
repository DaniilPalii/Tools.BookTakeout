using System.Net;
using System.Text;
using BookTakeout.ConsoleApplication.Commands;
using BookTakeout.ConsoleApplication.DependencyInjection;
using BookTakeout.Core;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.Spectre;
using Spectre.Console.Cli;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Information()
	.WriteTo.Spectre(outputTemplate: "{Message:lj}{NewLine}{Exception}")
	.CreateLogger();

try
{
	var services = new ServiceCollection();

	services.AddLogging(loggingBuilder =>
	{
		loggingBuilder.AddSerilog(dispose: true);
	});

	services.AddSingleton<CookieContainer>();
	services
		.AddHttpClient<LitnetHttpClient>(LitnetHttpClient.ConfigureClient)
		.ConfigurePrimaryHttpMessageHandler(provider =>
		{
			var cookieContainer = provider.GetRequiredService<CookieContainer>();
			return LitnetHttpClient.CreateHandler(cookieContainer);
		})
		.RemoveAllLoggers();

	services.AddTransient<BookDownloader>();
	services.AddTransient<LitnetBrowserClient>();

	var typeRegistrar = new TypeRegistrar(services);
	var app = new CommandApp<DownloadBookCommand>(typeRegistrar);

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