using System.Reflection;
using BookTakeout.Console.Commands;
using BookTakeout.Console.Configuration;
using BookTakeout.Console.DependencyInjection;
using BookTakeout.Resources.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console.Cli;

ConsoleEncoding.Configure();
SerilogLogging.Configure();
MemoryPackSerialization.Configure();

var appVersion = Assembly
	.GetExecutingAssembly()
	.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
	!.InformationalVersion;

Console.WriteLine(Messages.ConsoleLogoVersionX, appVersion);

try
{
	var builder = Host.CreateApplicationBuilder(args);
	builder.Logging.ClearProviders();

	if (builder.Configuration["Culture"] is { } cultureCode)
		AppCulture.Set(cultureCode);

	builder.Services.AddAppLogger();
	builder.Services.AddAppHttpClient();
	builder.Services.AddAppServices();

	var typeRegistrar = new TypeRegistrar(builder);
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
