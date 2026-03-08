using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace TaleWorlds.Library
{
	// Token: 0x02000060 RID: 96
	public static class MachineId
	{
		// Token: 0x060002B9 RID: 697 RVA: 0x000083D2 File Offset: 0x000065D2
		public static void Initialize()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				MachineId.MachineIdString = "nonwindows";
				return;
			}
			MachineId.MachineIdString = MachineId.ProcessId();
		}

		// Token: 0x060002BA RID: 698 RVA: 0x000083F5 File Offset: 0x000065F5
		public static int AsInteger()
		{
			if (!string.IsNullOrEmpty(MachineId.MachineIdString))
			{
				return BitConverter.ToInt32(Encoding.ASCII.GetBytes(MachineId.MachineIdString), 0);
			}
			return 0;
		}

		// Token: 0x060002BB RID: 699 RVA: 0x0000841A File Offset: 0x0000661A
		private static string ProcessId()
		{
			return "" + MachineId.GetMotherboardIdentifier() + MachineId.GetCpuIdentifier() + MachineId.GetDiskIdentifier();
		}

		// Token: 0x060002BC RID: 700 RVA: 0x00008440 File Offset: 0x00006640
		private static string GetMotherboardIdentifier()
		{
			string text = "";
			try
			{
				using (ManagementObjectCollection instances = new ManagementClass("win32_baseboard").GetInstances())
				{
					foreach (ManagementBaseObject managementBaseObject in instances)
					{
						string text2 = (((ManagementObject)managementBaseObject)["SerialNumber"] as string).Trim(new char[] { ' ' });
						text += text2.Replace("-", "");
					}
				}
			}
			catch (Exception)
			{
				return "";
			}
			return text;
		}

		// Token: 0x060002BD RID: 701 RVA: 0x00008508 File Offset: 0x00006708
		private static string GetCpuIdentifier()
		{
			string text = "";
			try
			{
				using (ManagementObjectCollection instances = new ManagementClass("win32_processor").GetInstances())
				{
					foreach (ManagementBaseObject managementBaseObject in instances)
					{
						string text2 = ((ManagementObject)managementBaseObject)["ProcessorId"] as string;
						if (text2 != null)
						{
							string text3 = text2.Trim(new char[] { ' ' });
							text += text3.Replace("-", "");
						}
					}
				}
			}
			catch (Exception)
			{
				return "";
			}
			return text;
		}

		// Token: 0x060002BE RID: 702 RVA: 0x000085D8 File Offset: 0x000067D8
		private static string GetDiskIdentifier()
		{
			string text = "";
			try
			{
				using (ManagementObjectCollection instances = new ManagementClass("win32_diskdrive").GetInstances())
				{
					foreach (ManagementBaseObject managementBaseObject in instances)
					{
						ManagementObject managementObject = (ManagementObject)managementBaseObject;
						if (string.Compare(managementObject["InterfaceType"] as string, "IDE", StringComparison.InvariantCultureIgnoreCase) == 0)
						{
							string text2 = (managementObject["SerialNumber"] as string).Trim(new char[] { ' ' });
							text += text2.Replace("-", "");
						}
					}
				}
			}
			catch (Exception)
			{
				return "";
			}
			return text;
		}

		// Token: 0x0400011E RID: 286
		private static string MachineIdString;
	}
}
