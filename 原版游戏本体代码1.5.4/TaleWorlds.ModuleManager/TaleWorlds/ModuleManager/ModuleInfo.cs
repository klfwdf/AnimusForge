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

	private static string GetRequiredValueAttribute(XmlNode moduleNode, string elementName, string subModulePath)
	{
		XmlNode xmlNode = moduleNode.SelectSingleNode(elementName);
		if (xmlNode == null)
		{
			throw new Exception("Required element <" + elementName + "> not found under <Module> in '" + subModulePath + "'.");
		}
		XmlAttribute xmlAttribute = xmlNode.Attributes?["value"];
		if (xmlAttribute == null)
		{
			throw new Exception("Element <" + elementName + "> in '" + subModulePath + "' is missing the required 'value' attribute.");
		}
		return xmlAttribute.InnerText;
	}

	private static string GetValueAttribute(XmlNode node, string elementName, string subModulePath)
	{
		XmlAttribute xmlAttribute = node.Attributes?["value"];
		if (xmlAttribute == null)
		{
			throw new Exception("Element <" + elementName + "> in '" + subModulePath + "' is missing the required 'value' attribute.");
		}
		return xmlAttribute.InnerText;
	}

	private static string GetRequiredAttribute(XmlNode node, string attributeName, string elementDescription, string subModulePath)
	{
		XmlAttribute xmlAttribute = node.Attributes?[attributeName];
		if (xmlAttribute == null)
		{
			throw new Exception(elementDescription + " in '" + subModulePath + "' is missing the required '" + attributeName + "' attribute.");
		}
		return xmlAttribute.InnerText;
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
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			using StreamReader txtReader = new StreamReader(text);
			xmlDocument.Load(txtReader);
		}
		catch (XmlException ex)
		{
			throw new Exception($"Malformed XML in '{text}' at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}", ex);
		}
		XmlNode xmlNode = xmlDocument.SelectSingleNode("Module");
		if (xmlNode == null)
		{
			throw new Exception("Root element <Module> not found in '" + text + "'. The file may be empty or have an unexpected root element.");
		}
		Name = GetRequiredValueAttribute(xmlNode, "Name", text);
		Id = GetRequiredValueAttribute(xmlNode, "Id", text);
		if (!Id.Contains(';'.ToString()))
		{
			Id.Contains(':'.ToString());
		}
		Version = ApplicationVersion.FromString(GetRequiredValueAttribute(xmlNode, "Version", text));
		XmlNode xmlNode2 = xmlNode.SelectSingleNode("RequiredBaseVersion");
		if (xmlNode2 != null)
		{
			RequiredBaseVersion = ApplicationVersion.FromString(GetValueAttribute(xmlNode2, "RequiredBaseVersion", text));
		}
		XmlNode xmlNode3 = xmlNode.SelectSingleNode("DefaultModule");
		IsDefault = xmlNode3 != null && GetValueAttribute(xmlNode3, "DefaultModule", text).Equals("true");
		XmlNode xmlNode4 = xmlNode.SelectSingleNode("ModuleType");
		if (xmlNode4 != null && Enum.TryParse<ModuleType>(GetValueAttribute(xmlNode4, "ModuleType", text), out var result))
		{
			Type = result;
		}
		IsSelected = IsNative;
		Category = ModuleCategory.Singleplayer;
		XmlNode xmlNode5 = xmlNode.SelectSingleNode("ModuleCategory");
		if (xmlNode5 != null && Enum.TryParse<ModuleCategory>(GetValueAttribute(xmlNode5, "ModuleCategory", text), out var result2))
		{
			Category = result2;
		}
		XmlNodeList xmlNodeList = xmlNode.SelectSingleNode("DependedModules")?.SelectNodes("DependedModule");
		if (xmlNodeList != null)
		{
			for (int i = 0; i < xmlNodeList.Count; i++)
			{
				string requiredAttribute = GetRequiredAttribute(xmlNodeList[i], "Id", "A <DependedModule> element", text);
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
						_ = "Couldn't parse dependent version of " + requiredAttribute + " for " + Id + ". Using default version.";
					}
				}
				if (bool.TryParse(xmlNodeList[i].Attributes["Optional"]?.InnerText, out var result3))
				{
					isOptional = result3;
				}
				DependedModules.Add(new DependedModule(requiredAttribute, version, isOptional));
			}
		}
		XmlNodeList xmlNodeList2 = xmlNode.SelectSingleNode("ModulesToLoadAfterThis")?.SelectNodes("Module");
		if (xmlNodeList2 != null)
		{
			for (int j = 0; j < xmlNodeList2.Count; j++)
			{
				string requiredAttribute2 = GetRequiredAttribute(xmlNodeList2[j], "Id", "A <ModulesToLoadAfterThis> <Module> element", text);
				ModulesToLoadAfterThis.Add(new DependedModule(requiredAttribute2, ApplicationVersion.Empty));
			}
		}
		XmlNodeList xmlNodeList3 = xmlNode.SelectSingleNode("IncompatibleModules")?.SelectNodes("Module");
		if (xmlNodeList3 != null)
		{
			for (int k = 0; k < xmlNodeList3.Count; k++)
			{
				string requiredAttribute3 = GetRequiredAttribute(xmlNodeList3[k], "Id", "An <IncompatibleModules> <Module> element", text);
				IncompatibleModules.Add(new DependedModule(requiredAttribute3, ApplicationVersion.Empty));
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
		Version = new ApplicationVersion(Version.ApplicationVersionType, Version.Major, Version.Minor, Version.Revision, 123627);
	}
}
