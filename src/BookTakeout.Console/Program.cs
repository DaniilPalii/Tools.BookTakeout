using BookTakeout.Console.Commands;
using BookTakeout.Console.Configuration;
using BookTakeout.Console.DependencyInjection;
using BookTakeout.Console.Helpers;
using BookTakeout.Resources.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console.Cli;
using Velopack;

VelopackApp.Build().Run();

ConsoleEncoding.Configure();
SerilogLogging.Configure();
MemoryPackSerialization.Configure();

Console.WriteLine(Messages.ConsoleLogoVersionX, VersionHelper.GetInformationalVersion());
Console.WriteLine();

await VelopackHelper.UpdateApplicationAsync();

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
