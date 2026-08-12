using Serilog;
using Serilog.Sinks.Spectre;

namespace BookTakeout.ConsoleApplication.Configuration;

public static class SerilogLogging
{
	public static void Configure()
	{
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel
			.Information()
			.WriteTo
			.Spectre(outputTemplate: "{Message:lj}{NewLine}{Exception}")
			.CreateLogger();
	}
}
