using System;
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

	private sealed class LineInfoXmlElement : XmlElement, IXmlLineInfo
	{
		private readonly int _lineNumber;

		private readonly int _linePosition;

		private readonly bool _hasLineInfo;

		public int LineNumber => _lineNumber;

		public int LinePosition => _linePosition;

		internal LineInfoXmlElement(string prefix, string localName, string namespaceUri, XmlDocument doc, IXmlLineInfo lineInfo)
			: base(prefix, localName, namespaceUri, doc)
		{
			if (lineInfo != null && lineInfo.HasLineInfo())
			{
				_hasLineInfo = true;
				_lineNumber = lineInfo.LineNumber;
				_linePosition = lineInfo.LinePosition;
			}
		}

		public bool HasLineInfo()
		{
			return _hasLineInfo;
		}
	}

	private sealed class LineInfoXmlDocument : XmlDocument
	{
		private IXmlLineInfo _currentLineInfo;

		public override void Load(XmlReader reader)
		{
			_currentLineInfo = reader as IXmlLineInfo;
			try
			{
				base.Load(reader);
			}
			finally
			{
				_currentLineInfo = null;
			}
		}

		public override XmlElement CreateElement(string prefix, string localName, string namespaceUri)
		{
			return new LineInfoXmlElement(prefix, localName, namespaceUri, this, _currentLineInfo);
		}
	}

	public static List<MbObjectXmlInformation> XmlInformationList = new List<MbObjectXmlInformation>();

	public static List<MbObjectXmlInformation> MbprojXmls = new List<MbObjectXmlInformation>();

	public static Dictionary<string, Dictionary<string, XsdElement>> XsdElementDictionary = new Dictionary<string, Dictionary<string, XsdElement>>();

	public static XNamespace XsNamespace = "http://www.w3.org/2001/XMLSchema";

	private static XmlDocument LoadXmlDocumentWithLineInfo(string filePath)
	{
		XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
		xmlReaderSettings.IgnoreComments = true;
		xmlReaderSettings.IgnoreWhitespace = true;
		xmlReaderSettings.DtdProcessing = DtdProcessing.Parse;
		xmlReaderSettings.CheckCharacters = false;
		xmlReaderSettings.XmlResolver = new XmlUrlResolver();
		xmlReaderSettings.MaxCharactersFromEntities = 0L;
		LineInfoXmlDocument lineInfoXmlDocument = new LineInfoXmlDocument();
		using StreamReader input = new StreamReader(filePath);
		using XmlReader reader = XmlReader.Create(input, xmlReaderSettings);
		lineInfoXmlDocument.Load(reader);
		return lineInfoXmlDocument;
	}

	private static string GetLineHint(XmlNode node)
	{
		if (!(node is IXmlLineInfo xmlLineInfo) || !xmlLineInfo.HasLineInfo())
		{
			return "";
		}
		return $" (line {xmlLineInfo.LineNumber})";
	}

	private static string GetLineHint(XObject node)
	{
		if (node == null || !((IXmlLineInfo)node).HasLineInfo())
		{
			return "";
		}
		return $" (line {((IXmlLineInfo)node).LineNumber})";
	}

	public static void ReadXsdFileAndExtractInformation(string xsdFilePath)
	{
		try
		{
			XDocument xDocument = XDocument.Load(xsdFilePath, LoadOptions.SetLineInfo);
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
				string text = GetFullXPathOfElement(item2) + "/" + item2.Element(XsNamespace + "selector")?.Attribute("xpath")?.Value;
				foreach (XElement item3 in item2.Elements(XsNamespace + "field"))
				{
					string text2 = item3?.Attribute("xpath")?.Value;
					if (string.IsNullOrEmpty(text2))
					{
						throw new Exception("An <xs:field> element" + GetLineHint(item3) + " in '" + xsdFilePath + "' is missing the required 'xpath' attribute.");
					}
					if (!XsdElementDictionary[xsdFilePath].ContainsKey(text))
					{
						throw new Exception("The selector xpath '" + text + "' of an <xs:unique>/<xs:key> element" + GetLineHint(item2) + " in '" + xsdFilePath + "' does not match any declared element.");
					}
					XsdElementDictionary[xsdFilePath][text].UniqueAttributes.Add(text2.Substring(1));
				}
			}
		}
		catch (XmlException ex)
		{
			throw new Exception($"Malformed XSD schema in '{xsdFilePath}' at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}", ex);
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
		try
		{
			XmlNodeList xmlNodeList = (LoadXmlDocumentWithLineInfo(mbprojPath).SelectSingleNode("base") ?? throw new Exception("Root element <base> not found in '" + mbprojPath + "'. The file may be empty or have an unexpected root element.")).SelectNodes("file");
			if (xmlNodeList == null)
			{
				return;
			}
			foreach (XmlNode item2 in xmlNodeList)
			{
				string lineHint = GetLineHint(item2);
				if (item2.Attributes["id"] == null)
				{
					throw new Exception("A <file> element" + lineHint + " in '" + mbprojPath + "' is missing the required 'id' attribute.");
				}
				if (item2.Attributes["name"] == null)
				{
					throw new Exception("A <file> element" + lineHint + " in '" + mbprojPath + "' is missing the required 'name' attribute.");
				}
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
		catch (XmlException ex)
		{
			throw new Exception($"Malformed XML in '{mbprojPath}' at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}", ex);
		}
	}

	public static void GetXmlListAndApply(string moduleName)
	{
		string path = ModuleHelper.GetPath(moduleName);
		try
		{
			XmlNodeList xmlNodeList = (LoadXmlDocumentWithLineInfo(path).SelectSingleNode("Module") ?? throw new Exception("Root element <Module> not found in '" + path + "'. The file may be empty or have an unexpected root element.")).SelectNodes("Xmls/XmlNode");
			if (xmlNodeList == null)
			{
				return;
			}
			foreach (XmlNode item2 in xmlNodeList)
			{
				string lineHint = GetLineHint(item2);
				XmlNode xmlNode2 = item2.SelectSingleNode("XmlName");
				if (xmlNode2 == null)
				{
					throw new Exception("An <XmlNode> element" + lineHint + " in '" + path + "' is missing the required <XmlName> child element.");
				}
				string lineHint2 = GetLineHint(xmlNode2);
				if (xmlNode2.Attributes["id"] == null)
				{
					throw new Exception("An <XmlName> element" + lineHint2 + " in '" + path + "' is missing the required 'id' attribute.");
				}
				if (xmlNode2.Attributes["path"] == null)
				{
					throw new Exception("An <XmlName> element" + lineHint2 + " in '" + path + "' is missing the required 'path' attribute.");
				}
				string innerText = xmlNode2.Attributes["id"].InnerText;
				string innerText2 = xmlNode2.Attributes["path"].InnerText;
				string xsdPath = ModuleHelper.GetXsdPath(innerText);
				if (File.Exists(xsdPath))
				{
					ReadXsdFileAndExtractInformation(xsdPath);
				}
				List<string> list = new List<string>();
				XmlNode xmlNode3 = item2.SelectSingleNode("IncludedGameTypes");
				if (xmlNode3 != null)
				{
					foreach (XmlNode childNode in xmlNode3.ChildNodes)
					{
						if (childNode.NodeType == XmlNodeType.Element)
						{
							string lineHint3 = GetLineHint(childNode);
							if (childNode.Attributes["value"] == null)
							{
								throw new Exception("An <IncludedGameTypes> child element" + lineHint3 + " in '" + path + "' is missing the required 'value' attribute.");
							}
							list.Add(childNode.Attributes["value"].InnerText);
						}
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
		catch (XmlException ex)
		{
			throw new Exception($"Malformed XML in '{path}' at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}", ex);
		}
	}
}
