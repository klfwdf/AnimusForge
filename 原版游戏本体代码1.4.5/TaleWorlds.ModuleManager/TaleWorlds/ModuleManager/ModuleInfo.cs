using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.ModuleManager;

public class ModuleInfo
{
	private const int ModuleDefaultChangeSet = 0;

	public readonly List<SubModuleInfo> SubModules;

	public readonly List<DependedModule> DependedModules;

	public readonly List<DependedModule> ModulesToLoadAfterThis;

	public readonly List<DependedModule> IncompatibleModules;

	public bool IsSelected { get; set; }

	public string Id { get; private set; }

	public string Name { get; private set; }

	public bool IsOfficial => Type != ModuleType.Community;

	public bool IsDefault { get; private set; }

	public bool IsRequiredOfficial => Type == ModuleType.Official;

	public bool IsActive { get; private set; }

	public ApplicationVersion Version { get; private set; }

	public ApplicationVersion RequiredBaseVersion { get; private set; }

	public ModuleCategory Category { get; private set; }

	public string FolderPath { get; private set; }

	public ModuleType Type { get; private set; }

	public bool HasMultiplayerCategory
	{
		get
		{
			if (Category != ModuleCategory.Multiplayer)
			{
				return Category == ModuleCategory.MultiplayerOptional;
			}
			return true;
		}
	}

	public bool IsNative => Id.Equals("Native", StringComparison.OrdinalIgnoreCase);

	public ModuleInfo()
	{
		DependedModules = new List<DependedModule>();
		SubModules = new List<SubModuleInfo>();
		ModulesToLoadAfterThis = new List<DependedModule>();
		IncompatibleModules = new List<DependedModule>();
		IsActive = true;
	}

	public void LoadWithFullPath(string fullPath)
	{
		SubModules.Clear();
		DependedModules.Clear();
		ModulesToLoadAfterThis.Clear();
		IncompatibleModules.Clear();
		FolderPath = fullPath;
		string text = FolderPath + "/SubModule.xml";
		Debug.Print("LoadWithFullPath  subModulePath = " + text);
		StreamReader txtReader = new StreamReader(text);
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(txtReader);
		XmlNode xmlNode = xmlDocument.SelectSingleNode("Module");
		Name = xmlNode.SelectSingleNode("Name").Attributes["value"].InnerText;
		Id = xmlNode.SelectSingleNode("Id").Attributes["value"].InnerText;
		if (!Id.Contains(';'.ToString()))
		{
			Id.Contains(':'.ToString());
		}
		Version = ApplicationVersion.FromString(xmlNode.SelectSingleNode("Version").Attributes["value"].InnerText);
		if (xmlNode.SelectSingleNode("RequiredBaseVersion") != null)
		{
			RequiredBaseVersion = ApplicationVersion.FromString(xmlNode.SelectSingleNode("RequiredBaseVersion").Attributes["value"].InnerText);
		}
		IsDefault = xmlNode.SelectSingleNode("DefaultModule")?.Attributes["value"].InnerText.Equals("true") ?? false;
		XmlNode xmlNode2 = xmlNode.SelectSingleNode("ModuleType");
		if (xmlNode2 != null && Enum.TryParse<ModuleType>(xmlNode2.Attributes["value"].InnerText, out var result))
		{
			Type = result;
		}
		IsSelected = IsNative;
		Category = ModuleCategory.Singleplayer;
		XmlNode xmlNode3 = xmlNode.SelectSingleNode("ModuleCategory");
		if (xmlNode3 != null && Enum.TryParse<ModuleCategory>(xmlNode3.Attributes["value"].InnerText, out var result2))
		{
			Category = result2;
		}
		XmlNodeList xmlNodeList = xmlNode.SelectSingleNode("DependedModules")?.SelectNodes("DependedModule");
		if (xmlNodeList != null)
		{
			for (int i = 0; i < xmlNodeList.Count; i++)
			{
				string innerText = xmlNodeList[i].Attributes["Id"].InnerText;
				ApplicationVersion version = ApplicationVersion.Empty;
				bool isOptional = false;
				if (xmlNodeList[i].Attributes["DependentVersion"] != null)
				{
					try
					{
						version = ApplicationVersion.FromString(xmlNodeList[i].Attributes["DependentVersion"].InnerText);
					}
					catch
					{
						_ = "Couldn't parse dependent version of " + innerText + " for " + Id + ". Using default version.";
					}
				}
				if (bool.TryParse(xmlNodeList[i].Attributes["Optional"]?.InnerText, out var result3))
				{
					isOptional = result3;
				}
				DependedModules.Add(new DependedModule(innerText, version, isOptional));
			}
		}
		XmlNodeList xmlNodeList2 = xmlNode.SelectSingleNode("ModulesToLoadAfterThis")?.SelectNodes("Module");
		if (xmlNodeList2 != null)
		{
			for (int j = 0; j < xmlNodeList2.Count; j++)
			{
				string innerText2 = xmlNodeList2[j].Attributes["Id"].InnerText;
				ModulesToLoadAfterThis.Add(new DependedModule(innerText2, ApplicationVersion.Empty));
			}
		}
		XmlNodeList xmlNodeList3 = xmlNode.SelectSingleNode("IncompatibleModules")?.SelectNodes("Module");
		if (xmlNodeList3 != null)
		{
			for (int k = 0; k < xmlNodeList3.Count; k++)
			{
				string innerText3 = xmlNodeList3[k].Attributes["Id"].InnerText;
				IncompatibleModules.Add(new DependedModule(innerText3, ApplicationVersion.Empty));
			}
		}
		XmlNodeList xmlNodeList4 = xmlNode.SelectSingleNode("SubModules")?.SelectNodes("SubModule");
		if (xmlNodeList4 == null)
		{
			return;
		}
		for (int l = 0; l < xmlNodeList4.Count; l++)
		{
			SubModuleInfo subModuleInfo = new SubModuleInfo();
			try
			{
				subModuleInfo.LoadFrom(xmlNodeList4[l], FolderPath, IsOfficial);
			}
			catch
			{
				_ = $"Cannot load a submodule {l} under {FolderPath}";
			}
			SubModules.Add(subModuleInfo);
		}
	}

	public void ActivateModule()
	{
		IsActive = true;
	}

	public void DeactivateModule()
	{
		IsActive = false;
	}

	public void UpdateVersionChangeSet()
	{
		Version = new ApplicationVersion(Version.ApplicationVersionType, Version.Major, Version.Minor, Version.Revision, 114896);
	}
}
