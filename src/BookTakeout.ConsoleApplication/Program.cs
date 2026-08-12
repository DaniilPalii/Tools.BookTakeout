using BookTakeout.ConsoleApplication.Commands;
using BookTakeout.ConsoleApplication.Configuration;
using BookTakeout.ConsoleApplication.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console.Cli;

ConsoleEncoding.Configure();
SerilogLogging.Configure();
MemoryPackSerialization.Configure();

try
{
	var services = new ServiceCollection();
	services.AddAppLogger();
	services.AddAppHttpClient();
	services.AddAppServices();

	var typeRegistrar = new TypeRegistrar(services);
	var app = new CommandApp<DownloadBookCommand>(typeRegistrar);

	var returnCode = await app.RunAsync(args);
	return returnCode;
}
catch (Exception ex)
{
	Log.Fatal(ex, messageTemplate: "Application terminated unexpectedly");
	return 1;
}
finally
{
	Log.CloseAndFlush();
}
