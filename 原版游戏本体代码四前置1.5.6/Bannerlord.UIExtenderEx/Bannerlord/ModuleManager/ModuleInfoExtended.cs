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

	public ApplicationVersion Version { get; set; } = ApplicationVersion.Empty;

	public bool IsSingleplayerModule { get; set; }

	public bool IsMultiplayerModule { get; set; }

	public bool IsServerModule { get; set; }

	public IReadOnlyList<SubModuleInfoExtended> SubModules { get; set; } = Array.Empty<SubModuleInfoExtended>();

	public IReadOnlyList<DependentModule> DependentModules { get; set; } = Array.Empty<DependentModule>();

	public IReadOnlyList<DependentModule> ModulesToLoadAfterThis { get; set; } = Array.Empty<DependentModule>();

	public IReadOnlyList<DependentModule> IncompatibleModules { get; set; } = Array.Empty<DependentModule>();

	public string Url { get; set; } = string.Empty;

	public string UpdateInfo { get; set; } = string.Empty;

	public IReadOnlyList<DependentModuleMetadata> DependentModuleMetadatas { get; set; } = Array.Empty<DependentModuleMetadata>();

	private static readonly string NativeModuleId = "Native";

	private static readonly string[] OfficialModuleIds = new string[7] { NativeModuleId, "SandBox", "SandBoxCore", "StoryMode", "CustomBattle", "BirthAndDeath", "Multiplayer" };

	public static ModuleInfoExtended? FromXml(XmlDocument? xmlDocument)
	{
		if (xmlDocument == null)
		{
			return null;
		}
		XmlNode xmlNode = xmlDocument.SelectSingleNode("Module");
		string id = xmlNode?.SelectSingleNode("Id")?.Attributes?["value"]?.InnerText ?? string.Empty;
		string name = xmlNode?.SelectSingleNode("Name")?.Attributes?["value"]?.InnerText ?? string.Empty;
		ApplicationVersion.TryParse(xmlNode?.SelectSingleNode("Version")?.Attributes?["value"]?.InnerText, out ApplicationVersion version);
		string text = xmlNode?.SelectSingleNode("ModuleType")?.Attributes?["value"].InnerText;
		bool isOfficial = (xmlNode != null && xmlNode.SelectSingleNode("Official")?.Attributes?["value"]?.InnerText.Equals("true") == true) || text == "Official" || text == "OfficialOptional";
		string text2 = xmlNode?.SelectSingleNode("ModuleCategory")?.Attributes?["value"]?.InnerText;
		bool isSingleplayerModule = (xmlNode != null && xmlNode.SelectSingleNode("SingleplayerModule")?.Attributes?["value"]?.InnerText.Equals("true") == true) || text2 == "Singleplayer" || text2 == "SingleplayerOptional";
		bool isMultiplayerModule = (xmlNode != null && xmlNode.SelectSingleNode("MultiplayerModule")?.Attributes?["value"]?.InnerText.Equals("true") == true) || text2 == "Multiplayer" || text2 == "MultiplayerOptional";
		bool isServerModule = text2 == "Server" || text2 == "ServerOptional";
		XmlNode xmlNode2 = xmlNode?.SelectSingleNode("DependedModules");
		XmlNodeList xmlNodeList = xmlNode2?.SelectNodes("DependedModule");
		List<DependentModule> list = new List<DependentModule>(xmlNodeList?.Count ?? 0);
		for (int i = 0; i < xmlNodeList?.Count; i++)
		{
			XmlAttribute xmlAttribute = xmlNodeList?[i]?.Attributes?["Id"];
			if (xmlAttribute != null)
			{
				ApplicationVersion.TryParse(xmlNodeList[i]?.Attributes?["DependentVersion"]?.InnerText, out ApplicationVersion version2);
				bool isOptional = (xmlNodeList[i]?.Attributes?["Optional"])?.InnerText.Equals("true") ?? false;
				list.Add(new DependentModule(xmlAttribute.InnerText, version2, isOptional));
			}
		}
		XmlNodeList xmlNodeList2 = (xmlNode?.SelectSingleNode("ModulesToLoadAfterThis"))?.SelectNodes("Module");
		List<DependentModule> list2 = new List<DependentModule>(xmlNodeList2?.Count ?? 0);
		for (int j = 0; j < xmlNodeList2?.Count; j++)
		{
			XmlAttribute xmlAttribute2 = xmlNodeList2?[j]?.Attributes?["Id"];
			if (xmlAttribute2 != null)
			{
				list2.Add(new DependentModule
				{
					Id = xmlAttribute2.InnerText,
					IsOptional = true
				});
			}
		}
		XmlNodeList xmlNodeList3 = (xmlNode?.SelectSingleNode("IncompatibleModules"))?.SelectNodes("Module");
		List<DependentModule> list3 = new List<DependentModule>(xmlNodeList3?.Count ?? 0);
		for (int k = 0; k < xmlNodeList3?.Count; k++)
		{
			XmlAttribute xmlAttribute3 = xmlNodeList3?[k]?.Attributes?["Id"];
			if (xmlAttribute3 != null)
			{
				list3.Add(new DependentModule
				{
					Id = xmlAttribute3.InnerText,
					IsOptional = true
				});
			}
		}
		XmlNodeList xmlNodeList4 = (xmlNode?.SelectSingleNode("SubModules"))?.SelectNodes("SubModule");
		List<SubModuleInfoExtended> list4 = new List<SubModuleInfoExtended>(xmlNodeList4?.Count ?? 0);
		for (int l = 0; l < xmlNodeList4?.Count; l++)
		{
			SubModuleInfoExtended subModuleInfoExtended = SubModuleInfoExtended.FromXml(xmlNodeList4?[l]);
			if ((object)subModuleInfoExtended != null)
			{
				list4.Add(subModuleInfoExtended);
			}
		}
		string url = xmlNode?.SelectSingleNode("Url")?.Attributes?["value"]?.InnerText ?? string.Empty;
		string text3 = xmlNode?.SelectSingleNode("UpdateInfo")?.Attributes?["value"]?.InnerText ?? string.Empty;
		XmlNodeList xmlNodeList5 = (xmlNode?.SelectSingleNode("DependedModuleMetadatas"))?.SelectNodes("DependedModuleMetadata");
		XmlNodeList xmlNodeList6 = (xmlNode?.SelectSingleNode("LoadAfterModules"))?.SelectNodes("LoadAfterModule");
		XmlNode xmlNode3 = xmlNode?.SelectSingleNode("OptionalDependModules");
		List<XmlNode> list5 = (xmlNode2?.SelectNodes("OptionalDependModule")?.Cast<XmlNode>() ?? Enumerable.Empty<XmlNode>()).Concat(xmlNode3?.SelectNodes("OptionalDependModule")?.Cast<XmlNode>() ?? Enumerable.Empty<XmlNode>()).Concat(xmlNode3?.SelectNodes("DependModule")?.Cast<XmlNode>() ?? Enumerable.Empty<XmlNode>()).ToList();
		List<DependentModuleMetadata> list6 = new List<DependentModuleMetadata>(xmlNodeList5?.Count ?? xmlNodeList6?.Count ?? list5.Count);
		for (int m = 0; m < xmlNodeList5?.Count; m++)
		{
			XmlAttribute xmlAttribute4 = xmlNodeList5?[m]?.Attributes?["id"];
			if (xmlAttribute4 != null)
			{
				XmlAttribute xmlAttribute5 = xmlNodeList5[m]?.Attributes?["order"];
				LoadType loadType = (LoadType)((xmlAttribute5 != null && Enum.TryParse<LoadTypeParse>(xmlAttribute5.InnerText, out var result)) ? result : ((LoadTypeParse)0));
				bool valueOrDefault = xmlNodeList5[m]?.Attributes?["optional"]?.InnerText.Equals("true") == true;
				bool valueOrDefault2 = xmlNodeList5[m]?.Attributes?["incompatible"]?.InnerText.Equals("true") == true;
				ApplicationVersion version3 = (ApplicationVersion.TryParse(xmlNodeList5[m]?.Attributes?["version"]?.InnerText, out ApplicationVersion version4) ? version4 : ApplicationVersion.Empty);
				ApplicationVersionRange versionRange = (ApplicationVersionRange.TryParse(xmlNodeList5[m]?.Attributes?["version"]?.InnerText ?? string.Empty, out ApplicationVersionRange versionRange2) ? versionRange2 : ApplicationVersionRange.Empty);
				list6.Add(new DependentModuleMetadata
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
				list6.Add(new DependentModuleMetadata
				{
					Id = xmlAttribute6.InnerText,
					LoadType = LoadType.LoadAfterThis,
					IsOptional = false,
					IsIncompatible = false,
					Version = ApplicationVersion.Empty,
					VersionRange = ApplicationVersionRange.Empty
				});
			}
		}
		for (int num = 0; num < list5.Count; num++)
		{
			XmlAttribute xmlAttribute7 = list5[num].Attributes?["Id"];
			if (xmlAttribute7 != null)
			{
				list6.Add(new DependentModuleMetadata
				{
					Id = xmlAttribute7.InnerText,
					LoadType = LoadType.None,
					IsOptional = true,
					IsIncompatible = false,
					Version = ApplicationVersion.Empty,
					VersionRange = ApplicationVersionRange.Empty
				});
			}
		}
		XmlNode xmlNode4 = xmlNode?.SelectSingleNode("RequiredGameVersion");
		string text4 = xmlNode4?.Attributes?["value"]?.InnerText ?? string.Empty;
		bool flag = xmlNode4 != null && xmlNode4.Attributes?["optional"]?.InnerText.Equals("true") == true;
		if (!string.IsNullOrWhiteSpace(text4) && ApplicationVersion.TryParse(text4, out ApplicationVersion version5))
		{
			string[] officialModuleIds = OfficialModuleIds;
			foreach (string moduleId in officialModuleIds)
			{
				bool flag2 = moduleId.Equals(NativeModuleId);
				DependentModuleMetadata dependentModuleMetadata = list6.Find((DependentModuleMetadata dmm) => moduleId.Equals(dmm.Id, StringComparison.Ordinal));
				if ((object)dependentModuleMetadata != null)
				{
					list6.Remove(dependentModuleMetadata);
				}
				list6.Add(new DependentModuleMetadata
				{
					Id = moduleId,
					LoadType = LoadType.LoadBeforeThis,
					IsOptional = (flag && !flag2),
					IsIncompatible = false,
					Version = version5,
					VersionRange = ApplicationVersionRange.Empty
				});
			}
		}
		return new ModuleInfoExtended
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
			UpdateInfo = ((!string.IsNullOrEmpty(text3)) ? text3 : string.Empty),
			DependentModuleMetadatas = list6
		};
	}

	public ModuleInfoExtended()
	{
	}

	public ModuleInfoExtended(string id, string name, bool isOfficial, ApplicationVersion version, bool isSingleplayerModule, bool isMultiplayerModule, IReadOnlyList<SubModuleInfoExtended> subModules, IReadOnlyList<DependentModule> dependentModules, IReadOnlyList<DependentModule> modulesToLoadAfterThis, IReadOnlyList<DependentModule> incompatibleModules, IReadOnlyList<DependentModuleMetadata> dependentModuleMetadatas, string url)
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
		return NativeModuleId.Equals(Id, StringComparison.OrdinalIgnoreCase);
	}

	public override string ToString()
	{
		return $"{Id} - {Version}";
	}

	public virtual bool Equals(ModuleInfoExtended? other)
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
