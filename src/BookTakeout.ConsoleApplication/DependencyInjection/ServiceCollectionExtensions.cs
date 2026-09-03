using System.Net;
using BookTakeout.Core;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BookTakeout.ConsoleApplication.DependencyInjection;

public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		public void AddAppLogger()
		{
			services.AddLogging(loggingBuilder =>
			{
				loggingBuilder.AddSerilog(dispose: true);
			});
		}

		public void AddAppHttpClient()
		{
			services.AddSingleton<CookieContainer>();

			services
				.AddHttpClient<LitnetHttpClient>(LitnetHttpClient.ConfigureClient)
				.ConfigurePrimaryHttpMessageHandler(provider =>
				{
					var cookieContainer = provider.GetRequiredService<CookieContainer>();
					return LitnetHttpClient.CreateHandler(cookieContainer);
				})
				.RemoveAllLoggers();
		}

		public void AddAppServices()
		{
			services.AddTransient<BookDownloader>();
			services.AddTransient<LitnetBrowserClient>();

			services.AddTransient<CookieStorage>(_ => new(profileDirectoryName: "Litnet"));
		}
	}
}
