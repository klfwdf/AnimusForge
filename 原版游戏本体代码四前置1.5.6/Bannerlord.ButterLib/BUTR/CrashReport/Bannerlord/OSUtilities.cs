using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace BUTR.CrashReport.Bannerlord;

internal static class OSUtilities
{
	private struct RTL_OSVERSIONINFOEX
	{
		internal uint dwOSVersionInfoSize;

		internal uint dwMajorVersion;

		internal uint dwMinorVersion;

		internal uint dwBuildNumber;

		internal uint dwPlatformId;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		internal string szCSDVersion;
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

	[DllImport("ntdll.dll", SetLastError = true)]
	private static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

	public static bool IsWine()
	{
		IntPtr moduleHandle = GetModuleHandle("ntdll.dll");
		if (moduleHandle != IntPtr.Zero)
		{
			return GetProcAddress(moduleHandle, "wine_get_version") != IntPtr.Zero;
		}
		return false;
	}

	public static string GetOSVersionWindows()
	{
		string text = string.Empty;
		RTL_OSVERSIONINFOEX lpVersionInformation = default(RTL_OSVERSIONINFOEX);
		lpVersionInformation.dwOSVersionInfoSize = (uint)Marshal.SizeOf(lpVersionInformation);
		if (RtlGetVersion(out lpVersionInformation) == 0)
		{
			text = $"{lpVersionInformation.dwMajorVersion}.{lpVersionInformation.dwMinorVersion}.{lpVersionInformation.dwBuildNumber}";
		}
		return "Windows" + " " + text;
	}

	public static string GetOSVersionLinux()
	{
		string text = string.Empty;
		string text2 = string.Empty;
		string text3 = string.Empty;
		try
		{
			if (File.Exists("/usr/lib/os-release"))
			{
				text3 = "/usr/lib/os-release";
			}
		}
		catch
		{
		}
		try
		{
			if (File.Exists("/etc/os-release"))
			{
				text3 = "/etc/os-release";
			}
		}
		catch
		{
		}
		try
		{
			if (File.Exists("/etc/lsb-release"))
			{
				text3 = "/etc/lsb-release";
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text3))
		{
			return "UNKNOWN LINUX";
		}
		try
		{
			using FileStream stream = new FileStream(text3, FileMode.Open, FileAccess.Read);
			using StreamReader streamReader = new StreamReader(stream);
			string text4;
			while ((text4 = streamReader.ReadLine()) != null)
			{
				if (text4.StartsWith("NAME="))
				{
					text = text4.Split('=')[1].Trim('"');
				}
				if (text4.StartsWith("DISTRIB_ID="))
				{
					text = text4.Split('=')[1].Trim('"');
				}
				if (text4.StartsWith("VERSION_ID="))
				{
					text2 = text4.Split('=')[1].Trim('"');
				}
				if (text4.StartsWith("DISTRIB_RELEASE="))
				{
					text2 = text4.Split('=')[1].Trim('"');
				}
			}
		}
		catch
		{
			return "UNKNOWN LINUX";
		}
		return text + " " + text2;
	}

	public static string? GetOSXVersion()
	{
		try
		{
			if (!File.Exists("/System/Library/CoreServices/SystemVersion.plist"))
			{
				return null;
			}
		}
		catch
		{
		}
		try
		{
			using FileStream stream = new FileStream("/System/Library/CoreServices/SystemVersion.plist", FileMode.Open, FileAccess.Read);
			using StreamReader textReader = new StreamReader(stream);
			Dictionary<string, string> dictionary = XDocument.Load(textReader).Descendants("dict").SelectMany((XElement d) => d.Elements("key").Zip(from e in d.Elements()
				where e.Name != "key"
				select e, (XElement k, XElement v) => new
			{
				Key = k,
				Value = v
			}))
				.ToDictionary(i => i.Key.Value, i => i.Value.Value);
			string obj2 = (dictionary.ContainsKey("ProductName") ? dictionary["ProductName"] : null);
			string text = (dictionary.ContainsKey("ProductVersion") ? dictionary["ProductVersion"] : null);
			return obj2 + " " + text;
		}
		catch
		{
			return null;
		}
	}
}
