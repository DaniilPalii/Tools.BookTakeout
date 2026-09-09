using System.Reflection;

namespace BookTakeout.Console.Helpers;

public static class VersionHelper
{
	public static string GetInformationalVersion()
	{
		return Assembly
			.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			!.InformationalVersion;
	}
}
