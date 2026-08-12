using System.Text;

namespace BookTakeout.ConsoleApplication.Configuration;

public static class ConsoleEncoding
{
	public static void Configure()
	{
		Console.InputEncoding = Encoding.UTF8;
		Console.OutputEncoding = Encoding.UTF8;
	}
}
