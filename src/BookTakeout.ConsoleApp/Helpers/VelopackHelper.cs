using BookTakeout.Resources.Text;
using Velopack;
using Velopack.Sources;

namespace BookTakeout.ConsoleApp.Helpers;

public static class VelopackHelper
{
	public static async Task UpdateApplicationAsync()
	{
		var updateManager = new UpdateManager(
			new GithubSource(
				repoUrl: "https://github.com/DaniilPalii/Tools.BookTakeout",
				accessToken: null,
				prerelease: false));

		var newVersion = await updateManager.CheckForUpdatesAsync();
		if (newVersion is null)
			return;

		Console.WriteLine(Messages.NewVersionAvailableDownloading);
		await updateManager.DownloadUpdatesAsync(newVersion);
		updateManager.ApplyUpdatesAndRestart(newVersion);
	}
}
