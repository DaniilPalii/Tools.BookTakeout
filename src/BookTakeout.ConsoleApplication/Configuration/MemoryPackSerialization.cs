using System.Net;
using BookTakeout.Core.Serialization.Cookies;
using MemoryPack;

namespace BookTakeout.ConsoleApplication.Configuration;

public static class MemoryPackSerialization
{
	public static void Configure()
	{
		MemoryPackFormatterProvider.Register<Cookie>(new CookieMemoryPackFormatter());
	}
}