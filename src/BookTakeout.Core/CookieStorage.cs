using System.Net;
using BookTakeout.Core.Helpers;
using MemoryPack;

namespace BookTakeout.Core;

public class CookieStorage(string profileDirectoryName)
{
	public async Task SaveCookiesAsync(List<Cookie> cookies)
	{
		Directory.CreateDirectory(DirectoryPath);
		await using var fileStream = new StreamWriter(FilePath, append: false);
		await MemoryPackSerializer.SerializeAsync(fileStream.BaseStream, cookies);
	}

	public async Task<List<Cookie>> LoadCookiesAsync()
	{
		if (!File.Exists(FilePath))
			return [];

		using var fileStream = new StreamReader(FilePath);

		return await MemoryPackSerializer.DeserializeAsync<List<Cookie>>(fileStream.BaseStream)
			?? [];
	}

	private string DirectoryPath
		=> Path.Combine(
			OsLocations.GetApplicationDataPath(),
			profileDirectoryName);

	private string FilePath
		=> Path.Combine(
			DirectoryPath,
			"Cookies");
}
