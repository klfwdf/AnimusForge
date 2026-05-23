using System;
using System.IO;
using System.Xml;

namespace TaleWorlds.Library;

public class ParameterLoader
{
	public static ParameterContainer LoadParametersFromClientProfile(string configurationName)
	{
		ParameterContainer parameterContainer = new ParameterContainer();
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(VirtualFolders.GetFileContent(BasePath.Name + "Parameters/ClientProfile.xml"));
		string innerText = xmlDocument.ChildNodes[0].Attributes["Value"].InnerText;
		LoadParametersInto("ClientProfiles/" + innerText + "/" + configurationName + ".xml", parameterContainer);
		return parameterContainer;
	}

	public static void LoadParametersInto(string fileFullName, ParameterContainer parameters)
	{
		XmlDocument xmlDocument = new XmlDocument();
		string filePath = BasePath.Name + "Parameters/" + fileFullName;
		xmlDocument.LoadXml(VirtualFolders.GetFileContent(filePath));
		foreach (XmlNode childNode in xmlDocument.FirstChild.ChildNodes)
		{
			if (!(childNode.Name == "Parameters"))
			{
				continue;
			}
			string text = childNode.Attributes?["Platforms"]?.InnerText;
			if (!string.IsNullOrWhiteSpace(text) && text.Split(new char[1] { ',' }).FindIndex((string p) => p.Trim().Equals(string.Concat(ApplicationPlatform.CurrentPlatform))) < 0)
			{
				continue;
			}
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				if (childNode2.NodeType != XmlNodeType.Comment)
				{
					string innerText = childNode2.Attributes["Name"].InnerText;
					string text2 = "";
					text2 = (TryGetFromFile(childNode2, out var value) ? value : (TryGetFromEnvironment(childNode2, out var value2) ? value2 : ((childNode2.Attributes["DefaultValue"] == null) ? childNode2.Attributes["Value"].InnerText : childNode2.Attributes["DefaultValue"].InnerText)));
					parameters.AddParameter(innerText, text2, overwriteIfExists: true);
				}
			}
		}
	}

	private static bool TryGetFromFile(XmlNode node, out string value)
	{
		value = "";
		if (node.Attributes?["LoadFromFile"] != null && node.Attributes["LoadFromFile"].InnerText.ToLower() == "true")
		{
			string innerText = node.Attributes["File"].InnerText;
			if (File.Exists(innerText))
			{
				string text = File.ReadAllText(innerText);
				value = text;
				return true;
			}
		}
		return false;
	}

	private static bool TryGetFromEnvironment(XmlNode node, out string value)
	{
		value = "";
		if (node.Attributes?["GetFromEnvironment"] != null && node.Attributes["GetFromEnvironment"].InnerText.ToLower() == "true")
		{
			string innerText = node.Attributes["Name"].InnerText;
			string environmentVariable = Environment.GetEnvironmentVariable(innerText);
			if (string.IsNullOrEmpty(environmentVariable))
			{
				environmentVariable = Environment.GetEnvironmentVariable(GetAltEnvironmentVariableName(innerText));
			}
			if (!string.IsNullOrEmpty(environmentVariable))
			{
				value = environmentVariable;
				return true;
			}
		}
		return false;
	}

	private static string GetAltEnvironmentVariableName(string name)
	{
		return name.Replace(".", "_").Replace(":", "__");
	}
}
