using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Bannerlord.ModuleManager;

internal record ModuleInfoExtended
{
	public string Id { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public bool IsOfficial { get; set; }

	public Bannerlord.ModuleManager.ApplicationVersion Version { get; set; } = Bannerlord.ModuleManager.ApplicationVersion.Empty;

	public bool IsSingleplayerModule { get; set; }

	public bool IsMultiplayerModule { get; set; }

	public bool IsServerModule { get; set; }

	public IReadOnlyList<Bannerlord.ModuleManager.SubModuleInfoExtended> SubModules { get; set; } = Array.Empty<Bannerlord.ModuleManager.SubModuleInfoExtended>();

	public IReadOnlyList<Bannerlord.ModuleManager.DependentModule> DependentModules { get; set; } = Array.Empty<Bannerlord.ModuleManager.DependentModule>();

	public IReadOnlyList<Bannerlord.ModuleManager.DependentModule> ModulesToLoadAfterThis { get; set; } = Array.Empty<Bannerlord.ModuleManager.DependentModule>();

	public IReadOnlyList<Bannerlord.ModuleManager.DependentModule> IncompatibleModules { get; set; } = Array.Empty<Bannerlord.ModuleManager.DependentModule>();

	public string Url { get; set; } = string.Empty;

	public string UpdateInfo { get; set; } = string.Empty;

	public IReadOnlyList<Bannerlord.ModuleManager.DependentModuleMetadata> DependentModuleMetadatas { get; set; } = Array.Empty<Bannerlord.ModuleManager.DependentModuleMetadata>();

	private static readonly string NativeModuleId = "Native";

	private static readonly string[] OfficialModuleIds = new string[7] { NativeModuleId, "SandBox", "SandBoxCore", "StoryMode", "CustomBattle", "BirthAndDeath", "Multiplayer" };

	public static Bannerlord.ModuleManager.ModuleInfoExtended? FromXml(XmlDocument? xmlDocument)
	{
		if (xmlDocument == null)
		{
			return null;
		}
		XmlNode xmlNode = xmlDocument.SelectSingleNode("Module");
		string id = xmlNode?.SelectSingleNode("Id")?.Attributes?["value"]?.InnerText ?? string.Empty;
		string name = xmlNode?.SelectSingleNode("Name")?.Attributes?["value"]?.InnerText ?? string.Empty;
		Bannerlord.ModuleManager.ApplicationVersion.TryParse(xmlNode?.SelectSingleNode("Version")?.Attributes?["value"]?.InnerText, out Bannerlord.ModuleManager.ApplicationVersion version);
		string text = xmlNode?.SelectSingleNode("ModuleType")?.Attributes?["value"].InnerText;
		bool isOfficial = (xmlNode != null && xmlNode.SelectSingleNode("Official")?.Attributes?["value"]?.InnerText.Equals("true") == true) || text == "Official" || text == "OfficialOptional";
		string text2 = xmlNode?.SelectSingleNode("ModuleCategory")?.Attributes?["value"]?.InnerText;
		bool isSingleplayerModule = (xmlNode != null && xmlNode.SelectSingleNode("SingleplayerModule")?.Attributes?["value"]?.InnerText.Equals("true") == true) || text2 == "Singleplayer" || text2 == "SingleplayerOptional";
		bool isMultiplayerModule = (xmlNode != null && xmlNode.SelectSingleNode("MultiplayerModule")?.Attributes?["value"]?.InnerText.Equals("true") == true) || text2 == "Multiplayer" || text2 == "MultiplayerOptional";
		bool isServerModule = text2 == "Server" || text2 == "ServerOptional";
		XmlNode xmlNode2 = xmlNode?.SelectSingleNode("DependedModules");
		XmlNodeList xmlNodeList = xmlNode2?.SelectNodes("DependedModule");
		List<Bannerlord.ModuleManager.DependentModule> list = new List<Bannerlord.ModuleManager.DependentModule>(xmlNodeList?.Count ?? 0);
		for (int i = 0; i < xmlNodeList?.Count; i++)
		{
			XmlAttribute xmlAttribute = xmlNodeList?[i]?.Attributes?["Id"];
			if (xmlAttribute != null)
			{
				Bannerlord.ModuleManager.ApplicationVersion.TryParse(xmlNodeList[i]?.Attributes?["DependentVersion"]?.InnerText, out Bannerlord.ModuleManager.ApplicationVersion version2);
				bool isOptional = (xmlNodeList[i]?.Attributes?["Optional"])?.InnerText.Equals("true") ?? false;
				list.Add(new Bannerlord.ModuleManager.DependentModule(xmlAttribute.InnerText, version2, isOptional));
			}
		}
		XmlNodeList xmlNodeList2 = (xmlNode?.SelectSingleNode("ModulesToLoadAfterThis"))?.SelectNodes("Module");
		List<Bannerlord.ModuleManager.DependentModule> list2 = new List<Bannerlord.ModuleManager.DependentModule>(xmlNodeList2?.Count ?? 0);
		for (int j = 0; j < xmlNodeList2?.Count; j++)
		{
			XmlAttribute xmlAttribute2 = xmlNodeList2?[j]?.Attributes?["Id"];
			if (xmlAttribute2 != null)
			{
				list2.Add(new Bannerlord.ModuleManager.DependentModule
				{
					Id = xmlAttribute2.InnerText,
					IsOptional = true
				});
			}
		}
		XmlNodeList xmlNodeList3 = (xmlNode?.SelectSingleNode("IncompatibleModules"))?.SelectNodes("Module");
		List<Bannerlord.ModuleManager.DependentModule> list3 = new List<Bannerlord.ModuleManager.DependentModule>(xmlNodeList3?.Count ?? 0);
		for (int k = 0; k < xmlNodeList3?.Count; k++)
		{
			XmlAttribute xmlAttribute3 = xmlNodeList3?[k]?.Attributes?["Id"];
			if (xmlAttribute3 != null)
			{
				list3.Add(new Bannerlord.ModuleManager.DependentModule
				{
					Id = xmlAttribute3.InnerText,
					IsOptional = true
				});
			}
		}
		XmlNodeList xmlNodeList4 = (xmlNode?.SelectSingleNode("SubModules"))?.SelectNodes("SubModule");
		List<Bannerlord.ModuleManager.SubModuleInfoExtended> list4 = new List<Bannerlord.ModuleManager.SubModuleInfoExtended>(xmlNodeList4?.Count ?? 0);
		for (int l = 0; l < xmlNodeList4?.Count; l++)
		{
			Bannerlord.ModuleManager.SubModuleInfoExtended subModuleInfoExtended = Bannerlord.ModuleManager.SubModuleInfoExtended.FromXml(xmlNodeList4?[l]);
			if ((object)subModuleInfoExtended != null)
			{
				list4.Add(subModuleInfoExtended);
			}
		}
		string url = xmlNode?.SelectSingleNode("Url")?.Attributes?["value"]?.InnerText ?? string.Empty;
		string text3 = xmlNode?.SelectSingleNode("UpdateInfo")?.Attributes?["provider"]?.InnerText ?? string.Empty;
		string text4 = xmlNode?.SelectSingleNode("UpdateInfo")?.Attributes?["value"]?.InnerText ?? string.Empty;
		XmlNodeList xmlNodeList5 = (xmlNode?.SelectSingleNode("DependedModuleMetadatas"))?.SelectNodes("DependedModuleMetadata");
		XmlNodeList xmlNodeList6 = (xmlNode?.SelectSingleNode("LoadAfterModules"))?.SelectNodes("LoadAfterModule");
		XmlNode xmlNode3 = xmlNode?.SelectSingleNode("OptionalDependModules");
		List<XmlNode> list5 = (xmlNode2?.SelectNodes("OptionalDependModule")?.Cast<XmlNode>() ?? Enumerable.Empty<XmlNode>()).Concat(xmlNode3?.SelectNodes("OptionalDependModule")?.Cast<XmlNode>() ?? Enumerable.Empty<XmlNode>()).Concat(xmlNode3?.SelectNodes("DependModule")?.Cast<XmlNode>() ?? Enumerable.Empty<XmlNode>()).ToList();
		List<Bannerlord.ModuleManager.DependentModuleMetadata> list6 = new List<Bannerlord.ModuleManager.DependentModuleMetadata>(xmlNodeList5?.Count ?? xmlNodeList6?.Count ?? list5.Count);
		for (int m = 0; m < xmlNodeList5?.Count; m++)
		{
			XmlAttribute xmlAttribute4 = xmlNodeList5?[m]?.Attributes?["id"];
			if (xmlAttribute4 != null)
			{
				XmlAttribute xmlAttribute5 = xmlNodeList5[m]?.Attributes?["order"];
				Bannerlord.ModuleManager.LoadType loadType = (Bannerlord.ModuleManager.LoadType)((xmlAttribute5 != null && Enum.TryParse<Bannerlord.ModuleManager.LoadTypeParse>(xmlAttribute5.InnerText, out var result)) ? result : ((Bannerlord.ModuleManager.LoadTypeParse)0));
				bool valueOrDefault = xmlNodeList5[m]?.Attributes?["optional"]?.InnerText.Equals("true") == true;
				bool valueOrDefault2 = xmlNodeList5[m]?.Attributes?["incompatible"]?.InnerText.Equals("true") == true;
				Bannerlord.ModuleManager.ApplicationVersion version3 = (Bannerlord.ModuleManager.ApplicationVersion.TryParse(xmlNodeList5[m]?.Attributes?["version"]?.InnerText, out Bannerlord.ModuleManager.ApplicationVersion version4) ? version4 : Bannerlord.ModuleManager.ApplicationVersion.Empty);
				Bannerlord.ModuleManager.ApplicationVersionRange versionRange = (Bannerlord.ModuleManager.ApplicationVersionRange.TryParse(xmlNodeList5[m]?.Attributes?["version"]?.InnerText ?? string.Empty, out Bannerlord.ModuleManager.ApplicationVersionRange versionRange2) ? versionRange2 : Bannerlord.ModuleManager.ApplicationVersionRange.Empty);
				list6.Add(new Bannerlord.ModuleManager.DependentModuleMetadata
				{
					Id = xmlAttribute4.InnerText,
					LoadType = loadType,
					IsOptional = valueOrDefault,
					IsIncompatible = valueOrDefault2,
					Version = version3,
					VersionRange = versionRange
				});
			}
		}
		for (int n = 0; n < xmlNodeList6?.Count; n++)
		{
			XmlAttribute xmlAttribute6 = xmlNodeList6?[n]?.Attributes?["Id"];
			if (xmlAttribute6 != null)
			{
				list6.Add(new Bannerlord.ModuleManager.DependentModuleMetadata
				{
					Id = xmlAttribute6.InnerText,
					LoadType = Bannerlord.ModuleManager.LoadType.LoadAfterThis,
					IsOptional = false,
					IsIncompatible = false,
					Version = Bannerlord.ModuleManager.ApplicationVersion.Empty,
					VersionRange = Bannerlord.ModuleManager.ApplicationVersionRange.Empty
				});
			}
		}
		for (int num = 0; num < list5.Count; num++)
		{
			XmlAttribute xmlAttribute7 = list5[num].Attributes?["Id"];
			if (xmlAttribute7 != null)
			{
				list6.Add(new Bannerlord.ModuleManager.DependentModuleMetadata
				{
					Id = xmlAttribute7.InnerText,
					LoadType = Bannerlord.ModuleManager.LoadType.None,
					IsOptional = true,
					IsIncompatible = false,
					Version = Bannerlord.ModuleManager.ApplicationVersion.Empty,
					VersionRange = Bannerlord.ModuleManager.ApplicationVersionRange.Empty
				});
			}
		}
		XmlNode xmlNode4 = xmlNode?.SelectSingleNode("RequiredGameVersion");
		string text5 = xmlNode4?.Attributes?["value"]?.InnerText ?? string.Empty;
		bool flag = xmlNode4 != null && xmlNode4.Attributes?["optional"]?.InnerText.Equals("true") == true;
		if (!string.IsNullOrWhiteSpace(text5) && Bannerlord.ModuleManager.ApplicationVersion.TryParse(text5, out Bannerlord.ModuleManager.ApplicationVersion version5))
		{
			string[] officialModuleIds = OfficialModuleIds;
			foreach (string moduleId in officialModuleIds)
			{
				bool flag2 = moduleId.Equals(NativeModuleId);
				Bannerlord.ModuleManager.DependentModuleMetadata dependentModuleMetadata = list6.Find((Bannerlord.ModuleManager.DependentModuleMetadata dmm) => dmm.Id.Equals(moduleId, StringComparison.Ordinal));
				if ((object)dependentModuleMetadata != null)
				{
					list6.Remove(dependentModuleMetadata);
				}
				list6.Add(new Bannerlord.ModuleManager.DependentModuleMetadata
				{
					Id = moduleId,
					LoadType = Bannerlord.ModuleManager.LoadType.LoadBeforeThis,
					IsOptional = (flag && !flag2),
					IsIncompatible = false,
					Version = version5,
					VersionRange = Bannerlord.ModuleManager.ApplicationVersionRange.Empty
				});
			}
		}
		return new Bannerlord.ModuleManager.ModuleInfoExtended
		{
			Id = id,
			Name = name,
			IsOfficial = isOfficial,
			Version = version,
			IsSingleplayerModule = isSingleplayerModule,
			IsMultiplayerModule = isMultiplayerModule,
			IsServerModule = isServerModule,
			SubModules = list4,
			DependentModules = list,
			ModulesToLoadAfterThis = list2,
			IncompatibleModules = list3,
			Url = url,
			UpdateInfo = ((!string.IsNullOrEmpty(text3) && !string.IsNullOrEmpty(text4)) ? (text3 + ":" + text4) : string.Empty),
			DependentModuleMetadatas = list6
		};
	}

	public ModuleInfoExtended()
	{
	}

	public ModuleInfoExtended(string id, string name, bool isOfficial, Bannerlord.ModuleManager.ApplicationVersion version, bool isSingleplayerModule, bool isMultiplayerModule, IReadOnlyList<Bannerlord.ModuleManager.SubModuleInfoExtended> subModules, IReadOnlyList<Bannerlord.ModuleManager.DependentModule> dependentModules, IReadOnlyList<Bannerlord.ModuleManager.DependentModule> modulesToLoadAfterThis, IReadOnlyList<Bannerlord.ModuleManager.DependentModule> incompatibleModules, IReadOnlyList<Bannerlord.ModuleManager.DependentModuleMetadata> dependentModuleMetadatas, string url)
	{
		Id = id;
		Name = name;
		IsOfficial = isOfficial;
		Version = version;
		IsSingleplayerModule = isSingleplayerModule;
		IsMultiplayerModule = isMultiplayerModule;
		SubModules = subModules;
		DependentModules = dependentModules;
		ModulesToLoadAfterThis = modulesToLoadAfterThis;
		IncompatibleModules = incompatibleModules;
		DependentModuleMetadatas = dependentModuleMetadatas;
		Url = url;
	}

	public bool IsNative()
	{
		return Id.Equals(NativeModuleId, StringComparison.OrdinalIgnoreCase);
	}

	public override string ToString()
	{
		return $"{Id} - {Version}";
	}

	public virtual bool Equals(Bannerlord.ModuleManager.ModuleInfoExtended? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		return Id == other.Id;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}
}
