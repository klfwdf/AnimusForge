using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace TaleWorlds.Library;

public static class MachineId
{
	private static string MachineIdString;

	public static void Initialize()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			MachineIdString = "nonwindows";
		}
		else
		{
			MachineIdString = ProcessId();
		}
	}

	public static int AsInteger()
	{
		if (!string.IsNullOrEmpty(MachineIdString))
		{
			return BitConverter.ToInt32(Encoding.ASCII.GetBytes(MachineIdString), 0);
		}
		return 0;
	}

	private static string ProcessId()
	{
		return string.Concat(string.Concat("" + GetMotherboardIdentifier(), GetCpuIdentifier()), GetDiskIdentifier());
	}

	private static string GetMotherboardIdentifier()
	{
		string text = "";
		try
		{
			using ManagementObjectCollection managementObjectCollection = new ManagementClass("win32_baseboard").GetInstances();
			foreach (ManagementObject item in managementObjectCollection)
			{
				string text2 = (item["SerialNumber"] as string).Trim(new char[1] { ' ' });
				text += text2.Replace("-", "");
			}
			return text;
		}
		catch (Exception)
		{
			return "";
		}
	}

	private static string GetCpuIdentifier()
	{
		string text = "";
		try
		{
			using ManagementObjectCollection managementObjectCollection = new ManagementClass("win32_processor").GetInstances();
			foreach (ManagementObject item in managementObjectCollection)
			{
				if (item["ProcessorId"] is string text2)
				{
					string text3 = text2.Trim(new char[1] { ' ' });
					text += text3.Replace("-", "");
				}
			}
			return text;
		}
		catch (Exception)
		{
			return "";
		}
	}

	private static string GetDiskIdentifier()
	{
		string text = "";
		try
		{
			using ManagementObjectCollection managementObjectCollection = new ManagementClass("win32_diskdrive").GetInstances();
			foreach (ManagementObject item in managementObjectCollection)
			{
				if (string.Compare(item["InterfaceType"] as string, "IDE", StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					string text2 = (item["SerialNumber"] as string).Trim(new char[1] { ' ' });
					text += text2.Replace("-", "");
				}
			}
			return text;
		}
		catch (Exception)
		{
			return "";
		}
	}
}
