using BookTakeout.Core.Serialization.Cookies;
using MemoryPack;

namespace BookTakeout.Console.Configuration;

public static class MemoryPackSerialization
{
	public static void Configure()
	{
		MemoryPackFormatterProvider.Register(new CookieMemoryPackFormatter());
	}
}
