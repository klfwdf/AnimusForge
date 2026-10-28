using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Missions.Objects;

internal static class NavalModuleDataHelper
{
	public static List<XmlNode> GetMergedEntries(string xmlId, string elementName = null)
	{
		List<XmlNode> list = new List<XmlNode>();
		if (!IsXmlIdDeclaredByAnyActiveModule(xmlId))
		{
			return list;
		}
		XmlDocument mergedXmlForManaged;
		try
		{
			mergedXmlForManaged = MBObjectManager.GetMergedXmlForManaged(xmlId, skipValidation: false);
		}
		catch (Exception)
		{
			return list;
		}
		XmlElement xmlElement = mergedXmlForManaged?.DocumentElement;
		if (xmlElement == null)
		{
			return list;
		}
		foreach (XmlNode childNode in xmlElement.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Element && (elementName == null || !(childNode.Name != elementName)))
			{
				list.Add(childNode);
			}
		}
		return list;
	}

	private static bool IsXmlIdDeclaredByAnyActiveModule(string xmlId)
	{
		foreach (MbObjectXmlInformation xmlInformation in XmlResource.XmlInformationList)
		{
			if (xmlInformation.Id == xmlId && ModuleHelper.IsModuleActive(xmlInformation.ModuleName))
			{
				return true;
			}
		}
		return false;
	}

	public static XmlNode FindMergedEntryByAttribute(string xmlId, string elementName, string attributeName, string attributeValue)
	{
		if (string.IsNullOrEmpty(attributeValue))
		{
			return null;
		}
		foreach (XmlNode mergedEntry in GetMergedEntries(xmlId, elementName))
		{
			XmlAttribute xmlAttribute = mergedEntry.Attributes?[attributeName];
			if (xmlAttribute != null && xmlAttribute.Value == attributeValue)
			{
				return mergedEntry;
			}
		}
		return null;
	}

	public static string FindModuleDataFile(string relativeFilePath)
	{
		List<ModuleInfo> activeModules = ModuleHelper.GetActiveModules();
		for (int num = activeModules.Count - 1; num >= 0; num--)
		{
			string text = ModuleHelper.GetModuleFullPath(activeModules[num].Id) + "ModuleData/" + relativeFilePath;
			if (File.Exists(text))
			{
				return text;
			}
		}
		return null;
	}
}
