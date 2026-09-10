using BookTakeout.Core.Serialization.Cookies;
using MemoryPack;

namespace BookTakeout.ConsoleApp.Configuration;

public static class MemoryPackSerialization
{
	public static void Configure()
	{
		MemoryPackFormatterProvider.Register(new CookieMemoryPackFormatter());
	}
}
