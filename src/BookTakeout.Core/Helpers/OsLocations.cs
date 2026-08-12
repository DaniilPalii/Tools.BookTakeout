using System.Runtime.InteropServices;

namespace BookTakeout.Core.Helpers;

public static class OsLocations
{
	public static string GetDownloadsPath()
	{
		return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? GetWindowsDownloadsPath()
			: Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				"Downloads");
	}

	[DllImport("shell32.dll", ExactSpelling = true)]
	private static extern int SHGetKnownFolderPath(
		[MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
		uint dwFlags,
		IntPtr hToken,
		out IntPtr ppszPath);

	private static string GetWindowsDownloadsPath()
	{
		var downloadsGuid = new Guid("374DE290-123F-4565-9164-39C4925E467B");
		var pathPtr = IntPtr.Zero;

		try
		{
			var hr = SHGetKnownFolderPath(downloadsGuid, 0, IntPtr.Zero, out pathPtr);

			if (hr == 0 && pathPtr != IntPtr.Zero)
			{
				var path = Marshal.PtrToStringUni(pathPtr);
				if (!string.IsNullOrEmpty(path))
				{
					return path;
				}
			}
		}
		catch
		{
			// If the P/Invoke fails for any reason, swallow the error and use the fallback
		}
		finally
		{
			// Clean up the memory allocated by the COM API
			if (pathPtr != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(pathPtr);
			}
		}

		// Fallback for Windows if the native call fails
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
	}
}
