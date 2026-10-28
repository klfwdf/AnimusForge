using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.Diamond.ClientApplication;

public class ClientApplicationConfiguration
{
	private static string _defaultConfigurationCategory = "";

	public string Name { get; set; }

	public string InheritFrom { get; set; }

	public string[] Clients { get; set; }

	public SessionProviderType SessionProviderType { get; set; }

	public ParameterContainer Parameters { get; set; }

	public ClientApplicationConfiguration()
	{
		Name = "NewlyCreated";
		InheritFrom = "";
		Clients = new string[0];
		Parameters = new ParameterContainer();
	}

	private void FillFromBase(ClientApplicationConfiguration baseConfiguration)
	{
		SessionProviderType = baseConfiguration.SessionProviderType;
		Parameters = baseConfiguration.Parameters.Clone();
	}

	public static string GetDefaultConfigurationFromFile()
	{
		XmlDocument xmlDocument = new XmlDocument();
		string fileContent = VirtualFolders.GetFileContent(BasePath.Name + "Parameters/ClientProfile.xml");
		if (fileContent == "")
		{
			return "";
		}
		xmlDocument.LoadXml(fileContent);
		return xmlDocument.ChildNodes[0].Attributes["Value"].InnerText;
	}

	public static void SetDefaultConfigurationCategory(string category)
	{
		_defaultConfigurationCategory = category;
	}

	public void FillFrom(string configurationName)
	{
		if (string.IsNullOrEmpty(_defaultConfigurationCategory))
		{
			_defaultConfigurationCategory = GetDefaultConfigurationFromFile();
		}
		FillFrom(_defaultConfigurationCategory, configurationName);
	}

	public void FillFrom(string configurationCategory, string configurationName)
	{
		XmlDocument xmlDocument = new XmlDocument();
		if (configurationCategory == "")
		{
			return;
		}
		string fileContent = VirtualFolders.GetFileContent(BasePath.Name + "Parameters/ClientProfiles/" + configurationCategory + "/" + configurationName + ".xml");
		if (fileContent == "")
		{
			return;
		}
		xmlDocument.LoadXml(fileContent);
		Name = Path.GetFileNameWithoutExtension(configurationName);
		XmlNode firstChild = xmlDocument.FirstChild;
		if (firstChild.Attributes != null && firstChild.Attributes["InheritFrom"] != null)
		{
			InheritFrom = firstChild.Attributes["InheritFrom"].InnerText;
			ClientApplicationConfiguration clientApplicationConfiguration = new ClientApplicationConfiguration();
			clientApplicationConfiguration.FillFrom(configurationCategory, InheritFrom);
			FillFromBase(clientApplicationConfiguration);
		}
		ParameterLoader.LoadParametersInto("ClientProfiles/" + configurationCategory + "/" + configurationName + ".xml", Parameters);
		foreach (XmlNode childNode in firstChild.ChildNodes)
		{
			if (childNode.Name == "SessionProvider")
			{
				string innerText = childNode.Attributes["Type"].InnerText;
				SessionProviderType = (SessionProviderType)Enum.Parse(typeof(SessionProviderType), innerText);
			}
			else if (childNode.Name == "Clients")
			{
				List<string> list = new List<string>();
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					string innerText2 = childNode2.Attributes["Type"].InnerText;
					list.Add(innerText2);
				}
				Clients = list.ToArray();
			}
			else
			{
				_ = childNode.Name == "Parameters";
			}
		}
	}
}
