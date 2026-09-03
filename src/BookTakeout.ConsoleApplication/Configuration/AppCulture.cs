using System.Globalization;

namespace BookTakeout.ConsoleApplication.Configuration;

public static class AppCulture
{
	public static void Set(string cultureCode)
	{
		var culture = new CultureInfo(cultureCode);

		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}
}
