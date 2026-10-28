using System;
using System.IO;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public sealed class ManagedParameters : IManagedParametersInitializer
{
	private readonly float[] _managedParametersArray = new float[72];

	public static ManagedParameters Instance { get; } = new ManagedParameters();

	public static float GetParameter(ManagedParametersEnum managedParameterType)
	{
		return Instance._managedParametersArray[(int)managedParameterType];
	}

	public static void SetParameter(ManagedParametersEnum managedParameterType, float newValue)
	{
		Instance._managedParametersArray[(int)managedParameterType] = newValue;
	}

	public void Initialize(string relativeXmlPath)
	{
		XmlDocument mergedXmlForManaged = MBObjectManager.GetMergedXmlForManaged("CoreParameters", skipValidation: true);
		LoadFromXml(mergedXmlForManaged);
	}

	private ManagedParameters()
	{
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

	private void LoadFromXml(XmlNode doc)
	{
		Debug.Print("loading managed_core_parameters.xml");
		if (doc.ChildNodes.Count < 1)
		{
			throw new TWXmlLoadException("Incorrect XML document format.");
		}
		for (XmlNode xmlNode = (doc.SelectSingleNode(".//managed_core_parameters") ?? throw new TWXmlLoadException("Incorrect XML document format.")).ChildNodes[0]; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (xmlNode.Name == "managed_core_parameter" && xmlNode.NodeType != XmlNodeType.Comment && Enum.TryParse<ManagedParametersEnum>(xmlNode.Attributes["id"].Value, ignoreCase: true, out var result))
			{
				_managedParametersArray[(int)result] = float.Parse(xmlNode.Attributes["value"].Value);
			}
		}
	}

	public float GetManagedParameter(ManagedParametersEnum managedParameterEnum)
	{
		return _managedParametersArray[(int)managedParameterEnum];
	}
}
