using System.Text;

namespace BookTakeout.Console.Configuration;

public static class ConsoleEncoding
{
	public static void Configure()
	{
		System.Console.InputEncoding = Encoding.UTF8;
		System.Console.OutputEncoding = Encoding.UTF8;
	}
}
