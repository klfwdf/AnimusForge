using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.ObjectSystem;

public static class XmlResource
{
	public struct XsdElement
	{
		public string XPath;

		public bool AlwaysPreferMerge;

		public List<string> UniqueAttributes;

		public XsdElement(string xPath, bool alwaysPreferMerge)
		{
			XPath = xPath;
			AlwaysPreferMerge = alwaysPreferMerge;
			UniqueAttributes = new List<string>();
		}
	}

	public static List<MbObjectXmlInformation> XmlInformationList = new List<MbObjectXmlInformation>();

	public static List<MbObjectXmlInformation> MbprojXmls = new List<MbObjectXmlInformation>();

	public static Dictionary<string, Dictionary<string, XsdElement>> XsdElementDictionary = new Dictionary<string, Dictionary<string, XsdElement>>();

	public static XNamespace XsNamespace = "http://www.w3.org/2001/XMLSchema";

	public static void ReadXsdFileAndExtractInformation(string xsdFilePath)
	{
		XDocument xDocument = XDocument.Load(xsdFilePath);
		XsdElementDictionary[xsdFilePath] = new Dictionary<string, XsdElement>();
		foreach (XElement item in xDocument.Descendants(XsNamespace + "element"))
		{
			string fullXPathOfElement = GetFullXPathOfElement(item);
			bool alwaysPreferMerge = GetAlwaysPreferMerge(item);
			XsdElement value = new XsdElement(fullXPathOfElement, alwaysPreferMerge);
			XsdElementDictionary[xsdFilePath][fullXPathOfElement] = value;
		}
		foreach (XElement item2 in xDocument.Descendants(XsNamespace + "unique").Concat(xDocument.Descendants(XsNamespace + "key")))
		{
			string key = GetFullXPathOfElement(item2) + "/" + item2.Element(XsNamespace + "selector")?.Attribute("xpath")?.Value;
			foreach (XElement item3 in item2.Elements(XsNamespace + "field"))
			{
				XsdElementDictionary[xsdFilePath][key].UniqueAttributes.Add(item3?.Attribute("xpath")?.Value.Substring(1));
			}
		}
	}

	private static bool GetAlwaysPreferMerge(XElement element)
	{
		XElement xElement = element.Element(XsNamespace + "annotation");
		if (xElement != null)
		{
			XElement xElement2 = xElement.Element(XsNamespace + "appinfo");
			if (xElement2 != null)
			{
				XElement xElement3 = xElement2.Element(XNamespace.None + "appSpecificNote");
				if (xElement3 != null && xElement3.Value.Trim() == "AlwaysPreferMerge")
				{
					return true;
				}
			}
		}
		return false;
	}

	public static string GetFullXPathOfElement(XElement element, bool isXsd = true)
	{
		if (element == null)
		{
			return null;
		}
		if (isXsd)
		{
			if (element.Name != XsNamespace + "element")
			{
				return GetFullXPathOfElement(element.Parent) ?? "";
			}
			string text = "";
			if (element.Attribute("name") != null)
			{
				text = element.Attribute("name").Value;
			}
			else if (element.Attribute("ref") != null)
			{
				text = element.Attribute("ref").Value;
			}
			if (element.Parent == null)
			{
				return text ?? "";
			}
			return GetFullXPathOfElement(element.Parent) + "/" + text;
		}
		if (element.Parent == null)
		{
			return $"/{element.Name}";
		}
		return $"{GetFullXPathOfElement(element.Parent, isXsd: false)}/{element.Name}";
	}

	public static void InitializeXmlInformationList(List<MbObjectXmlInformation> xmlInformation)
	{
		XmlInformationList = xmlInformation;
	}

	public static void GetMbprojxmls(string moduleName)
	{
		string mbprojPath = ModuleHelper.GetMbprojPath(moduleName);
		if (mbprojPath.Length <= 0 || !File.Exists(mbprojPath))
		{
			return;
		}
		StreamReader txtReader = new StreamReader(mbprojPath);
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(txtReader);
		XmlNodeList xmlNodeList = xmlDocument.SelectSingleNode("base").SelectNodes("file");
		if (xmlNodeList == null)
		{
			return;
		}
		foreach (XmlNode item2 in xmlNodeList)
		{
			string innerText = item2.Attributes["id"].InnerText;
			string innerText2 = item2.Attributes["name"].InnerText;
			string xsdPath = ModuleHelper.GetXsdPath(innerText);
			if (File.Exists(xsdPath))
			{
				ReadXsdFileAndExtractInformation(xsdPath);
			}
			MbObjectXmlInformation item = new MbObjectXmlInformation
			{
				Id = innerText,
				Name = innerText2,
				ModuleName = moduleName,
				GameTypesIncluded = new List<string>()
			};
			MbprojXmls.Add(item);
		}
	}

	public static void GetXmlListAndApply(string moduleName)
	{
		string path = ModuleHelper.GetPath(moduleName);
		new XmlReaderSettings().IgnoreComments = true;
		StreamReader txtReader = new StreamReader(path);
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(txtReader);
		XmlNodeList xmlNodeList = xmlDocument.SelectSingleNode("Module").SelectNodes("Xmls/XmlNode");
		if (xmlNodeList == null)
		{
			return;
		}
		foreach (XmlNode item2 in xmlNodeList)
		{
			XmlNode? xmlNode = item2.SelectSingleNode("XmlName");
			string innerText = xmlNode.Attributes["id"].InnerText;
			string innerText2 = xmlNode.Attributes["path"].InnerText;
			string xsdPath = ModuleHelper.GetXsdPath(innerText);
			if (File.Exists(xsdPath))
			{
				ReadXsdFileAndExtractInformation(xsdPath);
			}
			List<string> list = new List<string>();
			XmlNode xmlNode2 = item2.SelectSingleNode("IncludedGameTypes");
			if (xmlNode2 != null)
			{
				foreach (XmlNode childNode in xmlNode2.ChildNodes)
				{
					list.Add(childNode.Attributes["value"].InnerText);
				}
			}
			MbObjectXmlInformation item = new MbObjectXmlInformation
			{
				Id = innerText,
				Name = innerText2,
				ModuleName = moduleName,
				GameTypesIncluded = list
			};
			XmlInformationList.Add(item);
		}
	}
}
