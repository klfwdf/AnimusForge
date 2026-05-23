using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TaleWorlds.Library;

public class VirtualFolders
{
	[VirtualDirectory("..")]
	public class Win64_Shipping_Client
	{
		[VirtualDirectory("..")]
		public class bin
		{
			[VirtualDirectory("Parameters")]
			public class Parameters
			{
				[VirtualDirectory("ClientProfiles")]
				public class ClientProfiles
				{
					[VirtualDirectory("Azure.Discovery")]
					public class AzureDiscovery
					{
						[VirtualFile("LobbyClient.xml", "<Configuration>\t<SessionProvider Type=\"ThreadedRest\" />\t<Clients>\t\t<Client Type=\"LobbyClient\" />\t</Clients>\t<Parameters>\t\t<Parameter Name=\"LobbyClient.ServiceDiscovery.Address\" Value=\"https://bannerlord-service-discovery.bannerlord-services-3.net/\" />\t\t<Parameter Name=\"LobbyClient.Address\" Value=\"service://bannerlord.lobby/\" />\t</Parameters></Configuration>")]
						public string LobbyClient;
					}
				}

				[VirtualFile("Environment", "XPyuUglbM_U0ADFMQZVuPkTk28KsdlXhFHxwrRLYUTfaTf6ptRQwbXZmaCeD1kuHwnp5rlHGuVOYLyWqe2WW4MGa7sgtLbNOGkzhvvEpeIffKc.fh7dk3KcP26MEZ1urJb0mWRMjfkNAcmJxKxTg77tvThz6TDJXlFBSYoO9BpE-")]
				public string Environment;

				[VirtualFile("Version.xml", "<Version>\t<Singleplayer Value=\"v1.4.5.114896\"/></Version> ")]
				public string Version;

				[VirtualFile("ClientProfile.xml", "<ClientProfile Value=\"Azure.Discovery\"/>")]
				public string ClientProfile;
			}
		}
	}

	private static readonly bool _useVirtualFolders = true;

	public static Dictionary<string, string> PlatformDLCPaths = new Dictionary<string, string>();

	public static string GetFileContent(string filePath, Type type = null)
	{
		if (!_useVirtualFolders)
		{
			if (filePath.Contains("__MODULE_NAME__"))
			{
				string text = "__MODULE_NAME__";
				string pattern = Regex.Escape(text) + "(.*?)" + Regex.Escape(text);
				string value = Regex.Match(filePath, pattern).Groups[1].Value;
				filePath = filePath.Replace(text + value + text, PlatformDLCPaths[value]);
			}
			if (!File.Exists(filePath))
			{
				return "";
			}
			return File.ReadAllText(filePath);
		}
		if (type == null)
		{
			type = typeof(VirtualFolders);
		}
		return GetVirtualFileContent(filePath, type);
	}

	private static string GetVirtualFileContent(string filePath, Type type)
	{
		string fileName = Path.GetFileName(filePath);
		string directoryName = Path.GetDirectoryName(filePath);
		Type type2 = type;
		type2 = GetNestedDirectory(directoryName, type2);
		if (type2 == null)
		{
			type2 = type;
			string[] array = directoryName.Split(new char[1] { Path.DirectorySeparatorChar });
			int num = 0;
			while (type2 != null && num != array.Length)
			{
				if (!string.IsNullOrEmpty(array[num]))
				{
					type2 = GetNestedDirectory(array[num], type2);
				}
				num++;
			}
		}
		if (type2 != null)
		{
			FieldInfo[] fields = type2.GetFields();
			for (int i = 0; i < fields.Length; i++)
			{
				VirtualFileAttribute[] array2 = (VirtualFileAttribute[])fields[i].GetCustomAttributesSafe(typeof(VirtualFileAttribute), inherit: false);
				if (array2[0].Name == fileName)
				{
					return array2[0].Content;
				}
			}
		}
		return "";
	}

	private static Type GetNestedDirectory(string name, Type type)
	{
		Type[] nestedTypes = type.GetNestedTypes();
		foreach (Type type2 in nestedTypes)
		{
			if (((VirtualDirectoryAttribute[])type2.GetCustomAttributesSafe(typeof(VirtualDirectoryAttribute), inherit: false))[0].Name == name)
			{
				return type2;
			}
		}
		return null;
	}
}
