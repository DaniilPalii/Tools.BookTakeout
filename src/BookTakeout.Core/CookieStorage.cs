using System.Net;
using System.Text.Json;

namespace BookTakeout.Core;

public class CookieStorage(string profileDirectoryName)
{
	private string FilePath
		=> Path.Combine(
			Environment.GetFolderPath(
				Environment.SpecialFolder.LocalApplicationData,
				Environment.SpecialFolderOption.Create),
			profileDirectoryName,
			"Cookies");

	public async Task SaveCookiesAsync(List<Cookie> cookies)
	{
		await using var fileStream = new StreamWriter(FilePath, append: false);
		await JsonSerializer.SerializeAsync(fileStream.BaseStream, cookies);
	}

	public async Task<List<Cookie>> LoadCookiesAsync()
	{
		if (!File.Exists(FilePath))
			return [];

		using var fileStream = new StreamReader(FilePath);

		return await JsonSerializer.DeserializeAsync<List<Cookie>>(fileStream.BaseStream)
			?? [];
	}
}