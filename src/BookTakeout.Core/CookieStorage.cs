using System.Net;
using MemoryPack;

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
}