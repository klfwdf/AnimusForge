using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TaleWorlds.Library
{
	// Token: 0x020000A6 RID: 166
	public class VirtualFolders
	{
		// Token: 0x06000657 RID: 1623 RVA: 0x00016268 File Offset: 0x00014468
		public static string GetFileContent(string filePath, Type type = null)
		{
			if (VirtualFolders._useVirtualFolders)
			{
				if (type == null)
				{
					type = typeof(VirtualFolders);
				}
				return VirtualFolders.GetVirtualFileContent(filePath, type);
			}
			if (filePath.Contains("__MODULE_NAME__"))
			{
				string text = "__MODULE_NAME__";
				string pattern = Regex.Escape(text) + "(.*?)" + Regex.Escape(text);
				string value = Regex.Match(filePath, pattern).Groups[1].Value;
				filePath = filePath.Replace(text + value + text, VirtualFolders.PlatformDLCPaths[value]);
			}
			if (!File.Exists(filePath))
			{
				return "";
			}
			return File.ReadAllText(filePath);
		}

		// Token: 0x06000658 RID: 1624 RVA: 0x0001630C File Offset: 0x0001450C
		private static string GetVirtualFileContent(string filePath, Type type)
		{
			string fileName = Path.GetFileName(filePath);
			string directoryName = Path.GetDirectoryName(filePath);
			Type type2 = VirtualFolders.GetNestedDirectory(directoryName, type);
			if (type2 == null)
			{
				type2 = type;
				string[] array = directoryName.Split(new char[] { Path.DirectorySeparatorChar });
				int num = 0;
				while (type2 != null && num != array.Length)
				{
					if (!string.IsNullOrEmpty(array[num]))
					{
						type2 = VirtualFolders.GetNestedDirectory(array[num], type2);
					}
					num++;
				}
			}
			if (type2 != null)
			{
				FieldInfo[] fields = type2.GetFields();
				for (int i = 0; i < fields.Length; i++)
				{
					VirtualFileAttribute[] array2 = (VirtualFileAttribute[])fields[i].GetCustomAttributesSafe(typeof(VirtualFileAttribute), false);
					if (array2[0].Name == fileName)
					{
						return array2[0].Content;
					}
				}
			}
			return "";
		}

		// Token: 0x06000659 RID: 1625 RVA: 0x000163E4 File Offset: 0x000145E4
		private static Type GetNestedDirectory(string name, Type type)
		{
			foreach (Type type2 in type.GetNestedTypes())
			{
				if (((VirtualDirectoryAttribute[])type2.GetCustomAttributesSafe(typeof(VirtualDirectoryAttribute), false))[0].Name == name)
				{
					return type2;
				}
			}
			return null;
		}

		// Token: 0x040001DF RID: 479
		private static readonly bool _useVirtualFolders = true;

		// Token: 0x040001E0 RID: 480
		public static Dictionary<string, string> PlatformDLCPaths = new Dictionary<string, string>();

		// Token: 0x020000F1 RID: 241
		[VirtualDirectory("..")]
		public class Win64_Shipping_Client
		{
			// Token: 0x020000FC RID: 252
			[VirtualDirectory("..")]
			public class bin
			{
				// Token: 0x020000FD RID: 253
				[VirtualDirectory("Parameters")]
				public class Parameters
				{
					// Token: 0x04000343 RID: 835
					[VirtualFile("Environment", "hgwh7Ih7fFcugIZK29wc_bHxAy0TYsB0cs.8VHf2dliJMlaQaSuCjCLibEwtxpfVgvT5uX8Edxag8gk0JdfM5K8PHb.qRgADEOBZjE7Vz_efvqwzALUbOhWiJxP_WHhD6nqLY7NshnwnIyRv91cqcSfLqIxAaQLG2S94JrG1Ii0-")]
					public string Environment;

					// Token: 0x04000344 RID: 836
					[VirtualFile("Version.xml", "<Version>\t<Singleplayer Value=\"v1.3.13.106033\"/></Version>")]
					public string Version;

					// Token: 0x04000345 RID: 837
					[VirtualFile("ClientProfile.xml", "<ClientProfile Value=\"Azure.Discovery\"/>")]
					public string ClientProfile;

					// Token: 0x020000FE RID: 254
					[VirtualDirectory("ClientProfiles")]
					public class ClientProfiles
					{
						// Token: 0x020000FF RID: 255
						[VirtualDirectory("Azure.Discovery")]
						public class AzureDiscovery
						{
							// Token: 0x04000346 RID: 838
							[VirtualFile("LobbyClient.xml", "<Configuration>\t<SessionProvider Type=\"ThreadedRest\" />\t<Clients>\t\t<Client Type=\"LobbyClient\" />\t</Clients>\t<Parameters>\t\t<Parameter Name=\"LobbyClient.ServiceDiscovery.Address\" Value=\"https://bannerlord-service-discovery.bannerlord-services-3.net/\" />\t\t<Parameter Name=\"LobbyClient.Address\" Value=\"service://bannerlord.lobby/\" />\t</Parameters></Configuration>")]
							public string LobbyClient;
						}
					}
				}
			}
		}
	}
}
