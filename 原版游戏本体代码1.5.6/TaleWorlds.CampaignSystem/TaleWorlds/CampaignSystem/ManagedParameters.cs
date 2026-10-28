using System;
using System.IO;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public sealed class ManagedParameters : IManagedParametersInitializer
{
	private readonly bool[] _managedParametersArray = new bool[2];

	public static ManagedParameters Instance { get; } = new ManagedParameters();

	public void Initialize(string relativeXmlPath)
	{
		XmlDocument doc = LoadXmlFile(relativeXmlPath);
		LoadFromXml(doc);
	}

	private void LoadFromXml(XmlNode doc)
	{
		XmlNode xmlNode = null;
		if (doc.ChildNodes[1].ChildNodes[0].Name == "managed_campaign_parameters")
		{
			xmlNode = doc.ChildNodes[1].ChildNodes[0].ChildNodes[0];
		}
		while (xmlNode != null)
		{
			if (xmlNode.Name == "managed_campaign_parameter" && xmlNode.NodeType != XmlNodeType.Comment && Enum.TryParse<ManagedParametersEnum>(xmlNode.Attributes["id"].Value, ignoreCase: true, out var result))
			{
				_managedParametersArray[(int)result] = bool.Parse(xmlNode.Attributes["value"].Value);
			}
			xmlNode = xmlNode.NextSibling;
		}
	}

	private static XmlDocument LoadXmlFile(string path)
	{
		Debug.Print("opening " + path);
		XmlDocument xmlDocument = new XmlDocument();
		StreamReader streamReader = new StreamReader(path);
		string xml = streamReader.ReadToEnd();
		xmlDocument.LoadXml(xml);
		streamReader.Close();
		return xmlDocument;
	}

	public bool GetManagedParameter(ManagedParametersEnum _managedParametersEnum)
	{
		return _managedParametersArray[(int)_managedParametersEnum];
	}

	public bool SetManagedParameter(ManagedParametersEnum _managedParametersEnum, bool value)
	{
		bool result;
		_managedParametersArray[(int)_managedParametersEnum] = (result = value);
		return result;
	}
}
