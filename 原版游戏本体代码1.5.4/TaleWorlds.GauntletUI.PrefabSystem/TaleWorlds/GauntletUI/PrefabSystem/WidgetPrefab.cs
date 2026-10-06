using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class WidgetPrefab
{
	public Dictionary<string, VisualDefinitionTemplate> VisualDefinitionTemplates { get; set; }

	public Dictionary<string, ConstantDefinition> Constants { get; set; }

	public Dictionary<string, string> Parameters { get; set; }

	public Dictionary<string, XmlElement> CustomElements { get; set; }

	public WidgetTemplate RootTemplate { get; private set; }

	public WidgetPrefab()
	{
		VisualDefinitionTemplates = new Dictionary<string, VisualDefinitionTemplate>();
		Constants = new Dictionary<string, ConstantDefinition>();
		Parameters = new Dictionary<string, string>();
		CustomElements = new Dictionary<string, XmlElement>();
	}

	private static Dictionary<string, VisualDefinitionTemplate> LoadVisualDefinitions(XmlNode visualDefinitionsNode)
	{
		Dictionary<string, VisualDefinitionTemplate> dictionary = new Dictionary<string, VisualDefinitionTemplate>();
		foreach (XmlNode childNode in visualDefinitionsNode.ChildNodes)
		{
			VisualDefinitionTemplate visualDefinitionTemplate = new VisualDefinitionTemplate();
			visualDefinitionTemplate.Name = childNode.Attributes["Name"].Value;
			XmlAttribute xmlAttribute = childNode.Attributes["TransitionDuration"];
			if (xmlAttribute != null)
			{
				visualDefinitionTemplate.TransitionDuration = Convert.ToSingle(xmlAttribute.Value, CultureInfo.InvariantCulture);
			}
			XmlAttribute xmlAttribute2 = childNode.Attributes["EaseType"];
			if (xmlAttribute2 != null && Enum.TryParse<AnimationInterpolation.Type>(xmlAttribute2.Value, out var result))
			{
				visualDefinitionTemplate.EaseType = result;
			}
			XmlAttribute xmlAttribute3 = childNode.Attributes["EaseFunction"];
			if (xmlAttribute3 != null && Enum.TryParse<AnimationInterpolation.Function>(xmlAttribute3.Value, out var result2))
			{
				visualDefinitionTemplate.EaseFunction = result2;
			}
			XmlAttribute xmlAttribute4 = childNode.Attributes["DelayOnBegin"];
			if (xmlAttribute4 != null)
			{
				visualDefinitionTemplate.DelayOnBegin = Convert.ToSingle(xmlAttribute4.Value, CultureInfo.InvariantCulture);
			}
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				VisualStateTemplate visualStateTemplate = new VisualStateTemplate();
				foreach (XmlAttribute attribute in childNode2.Attributes)
				{
					string name = attribute.Name;
					string value = attribute.Value;
					if (name == "State")
					{
						visualStateTemplate.State = value;
					}
					else
					{
						visualStateTemplate.SetAttribute(name, value);
					}
				}
				visualDefinitionTemplate.AddVisualState(visualStateTemplate);
			}
			dictionary.Add(visualDefinitionTemplate.Name, visualDefinitionTemplate);
		}
		return dictionary;
	}

	private static void SaveVisualDefinitionsTo(XmlNode visualDefinitionsNode, Dictionary<string, VisualDefinitionTemplate> visualDefinitionTemplates)
	{
		foreach (VisualDefinitionTemplate value in visualDefinitionTemplates.Values)
		{
			value.Save(visualDefinitionsNode);
		}
	}

	private static Dictionary<string, string> LoadParameters(XmlNode constantsNode)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (XmlNode childNode in constantsNode.ChildNodes)
		{
			string value = childNode.Attributes["Name"].Value;
			string value2 = childNode.Attributes["DefaultValue"].Value;
			dictionary.Add(value, value2);
		}
		return dictionary;
	}

	private static Dictionary<string, XmlElement> LoadCustomElements(XmlNode customElementsNode)
	{
		Dictionary<string, XmlElement> dictionary = new Dictionary<string, XmlElement>();
		foreach (XmlNode childNode in customElementsNode.ChildNodes)
		{
			string value = childNode.Attributes["Name"].Value;
			XmlElement value2 = childNode.FirstChild as XmlElement;
			dictionary.Add(value, value2);
		}
		return dictionary;
	}

	private static void SaveParametersTo(XmlNode parametersNode, Dictionary<string, string> parameters)
	{
		foreach (KeyValuePair<string, string> parameter in parameters)
		{
			XmlNode xmlNode = parametersNode.OwnerDocument.CreateElement("Parameter");
			XmlAttribute xmlAttribute = parametersNode.OwnerDocument.CreateAttribute(parameter.Key);
			xmlAttribute.InnerText = parameter.Value;
			xmlNode.Attributes.Append(xmlAttribute);
			parametersNode.AppendChild(xmlNode);
		}
	}

	private static Dictionary<string, ConstantDefinition> LoadConstants(XmlNode constantsNode)
	{
		Dictionary<string, ConstantDefinition> dictionary = new Dictionary<string, ConstantDefinition>();
		foreach (XmlNode childNode in constantsNode.ChildNodes)
		{
			XmlAttribute xmlAttribute = childNode.Attributes["Name"];
			XmlAttribute xmlAttribute2 = childNode.Attributes["Value"];
			XmlAttribute xmlAttribute3 = childNode.Attributes["Prefix"];
			XmlAttribute xmlAttribute4 = childNode.Attributes["Suffix"];
			XmlAttribute xmlAttribute5 = childNode.Attributes["BrushName"];
			XmlAttribute xmlAttribute6 = childNode.Attributes["BrushLayer"];
			XmlAttribute xmlAttribute7 = childNode.Attributes["BrushValueType"];
			XmlAttribute xmlAttribute8 = childNode.Attributes["SpriteName"];
			XmlAttribute xmlAttribute9 = childNode.Attributes["SpriteValueType"];
			XmlAttribute xmlAttribute10 = childNode.Attributes["Additive"];
			XmlAttribute xmlAttribute11 = childNode.Attributes["MultiplyResult"];
			XmlAttribute xmlAttribute12 = childNode.Attributes["BooleanCheck"];
			XmlAttribute xmlAttribute13 = childNode.Attributes["OnTrue"];
			XmlAttribute xmlAttribute14 = childNode.Attributes["OnFalse"];
			ConstantDefinition constantDefinition = null;
			if (xmlAttribute != null)
			{
				string value = xmlAttribute.Value;
				if (xmlAttribute2 != null)
				{
					string value2 = xmlAttribute2.Value;
					constantDefinition = new ConstantDefinition(value);
					constantDefinition.Type = ConstantDefinitionType.Constant;
					constantDefinition.Value = value2;
				}
				else if (xmlAttribute12 != null && xmlAttribute13 != null && xmlAttribute14 != null)
				{
					string value3 = xmlAttribute12.Value;
					string value4 = xmlAttribute13.Value;
					string value5 = xmlAttribute14.Value;
					constantDefinition = new ConstantDefinition(value);
					constantDefinition.Value = value3;
					constantDefinition.OnTrueValue = value4;
					constantDefinition.OnFalseValue = value5;
					constantDefinition.Type = ConstantDefinitionType.BooleanCheck;
				}
				else if (xmlAttribute5 != null && xmlAttribute6 != null && xmlAttribute7 != null)
				{
					string value6 = xmlAttribute5.Value;
					string value7 = xmlAttribute6.Value;
					string value8 = xmlAttribute7.Value;
					if (value8 == "Width" || value8 == "Height")
					{
						constantDefinition = new ConstantDefinition(value);
						constantDefinition.BrushName = value6;
						constantDefinition.LayerName = value7;
						constantDefinition.Type = ((value8 == "Width") ? ConstantDefinitionType.BrushLayerWidth : ConstantDefinitionType.BrushLayerHeight);
					}
				}
				else if (xmlAttribute8 != null && xmlAttribute9 != null)
				{
					string value9 = xmlAttribute8.Value;
					string value10 = xmlAttribute9.Value;
					if (value10 == "Width" || value10 == "Height")
					{
						constantDefinition = new ConstantDefinition(value);
						constantDefinition.SpriteName = value9;
						constantDefinition.Type = ((value10 == "Width") ? ConstantDefinitionType.SpriteWidth : ConstantDefinitionType.SpriteHeight);
					}
				}
				if (constantDefinition != null && xmlAttribute3 != null)
				{
					string value11 = xmlAttribute3.Value;
					constantDefinition.Prefix = value11;
				}
				if (constantDefinition != null && xmlAttribute4 != null)
				{
					string value12 = xmlAttribute4.Value;
					constantDefinition.Suffix = value12;
				}
				if (constantDefinition != null && xmlAttribute10 != null)
				{
					string value13 = xmlAttribute10.Value;
					constantDefinition.Additive = value13;
				}
				if (constantDefinition != null && xmlAttribute11 != null)
				{
					string value14 = xmlAttribute11.Value;
					constantDefinition.MultiplyResult = Convert.ToSingle(value14, CultureInfo.InvariantCulture);
				}
			}
			if (constantDefinition != null)
			{
				dictionary.Add(constantDefinition.Name, constantDefinition);
			}
		}
		return dictionary;
	}

	private static void SaveConstantsTo(XmlNode constantsNode, Dictionary<string, ConstantDefinition> constants)
	{
		foreach (ConstantDefinition value in constants.Values)
		{
			XmlNode xmlNode = constantsNode.OwnerDocument.CreateElement("Constant");
			XmlAttribute xmlAttribute = constantsNode.OwnerDocument.CreateAttribute("Name");
			xmlAttribute.InnerText = value.Name;
			xmlNode.Attributes.Append(xmlAttribute);
			switch (value.Type)
			{
			case ConstantDefinitionType.Constant:
			{
				XmlAttribute xmlAttribute15 = constantsNode.OwnerDocument.CreateAttribute("Value");
				xmlAttribute15.InnerText = value.Value;
				xmlNode.Attributes.Append(xmlAttribute15);
				break;
			}
			case ConstantDefinitionType.BooleanCheck:
			{
				XmlAttribute xmlAttribute12 = constantsNode.OwnerDocument.CreateAttribute("BooleanCheck");
				xmlAttribute12.InnerText = value.Value;
				xmlNode.Attributes.Append(xmlAttribute12);
				XmlAttribute xmlAttribute13 = constantsNode.OwnerDocument.CreateAttribute("OnTrue");
				xmlAttribute13.InnerText = value.OnTrueValue;
				xmlNode.Attributes.Append(xmlAttribute13);
				XmlAttribute xmlAttribute14 = constantsNode.OwnerDocument.CreateAttribute("OnFalse");
				xmlAttribute14.InnerText = value.OnFalseValue;
				xmlNode.Attributes.Append(xmlAttribute14);
				break;
			}
			case ConstantDefinitionType.BrushLayerWidth:
			{
				XmlAttribute xmlAttribute9 = constantsNode.OwnerDocument.CreateAttribute("BrushName");
				xmlAttribute9.InnerText = value.BrushName;
				xmlNode.Attributes.Append(xmlAttribute9);
				XmlAttribute xmlAttribute10 = constantsNode.OwnerDocument.CreateAttribute("BrushLayer");
				xmlAttribute10.InnerText = value.LayerName;
				xmlNode.Attributes.Append(xmlAttribute10);
				XmlAttribute xmlAttribute11 = constantsNode.OwnerDocument.CreateAttribute("BrushValueType");
				xmlAttribute11.InnerText = "Width";
				xmlNode.Attributes.Append(xmlAttribute11);
				break;
			}
			case ConstantDefinitionType.BrushLayerHeight:
			{
				XmlAttribute xmlAttribute6 = constantsNode.OwnerDocument.CreateAttribute("BrushName");
				xmlAttribute6.InnerText = value.BrushName;
				xmlNode.Attributes.Append(xmlAttribute6);
				XmlAttribute xmlAttribute7 = constantsNode.OwnerDocument.CreateAttribute("BrushLayer");
				xmlAttribute7.InnerText = value.LayerName;
				xmlNode.Attributes.Append(xmlAttribute7);
				XmlAttribute xmlAttribute8 = constantsNode.OwnerDocument.CreateAttribute("BrushValueType");
				xmlAttribute8.InnerText = "Height";
				xmlNode.Attributes.Append(xmlAttribute8);
				break;
			}
			case ConstantDefinitionType.SpriteWidth:
			{
				XmlAttribute xmlAttribute4 = constantsNode.OwnerDocument.CreateAttribute("SpriteName");
				xmlAttribute4.InnerText = value.SpriteName;
				xmlNode.Attributes.Append(xmlAttribute4);
				XmlAttribute xmlAttribute5 = constantsNode.OwnerDocument.CreateAttribute("SpriteValueType");
				xmlAttribute5.InnerText = "Width";
				xmlNode.Attributes.Append(xmlAttribute5);
				break;
			}
			case ConstantDefinitionType.SpriteHeight:
			{
				XmlAttribute xmlAttribute2 = constantsNode.OwnerDocument.CreateAttribute("SpriteName");
				xmlAttribute2.InnerText = value.SpriteName;
				xmlNode.Attributes.Append(xmlAttribute2);
				XmlAttribute xmlAttribute3 = constantsNode.OwnerDocument.CreateAttribute("SpriteValueType");
				xmlAttribute3.InnerText = "Height";
				xmlNode.Attributes.Append(xmlAttribute3);
				break;
			}
			default:
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI.PrefabSystem\\WidgetPrefab.cs", "SaveConstantsTo", 362);
				break;
			}
			if (!string.IsNullOrEmpty(value.Additive))
			{
				XmlAttribute xmlAttribute16 = constantsNode.OwnerDocument.CreateAttribute("Additive");
				xmlAttribute16.InnerText = value.Additive;
				xmlNode.Attributes.Append(xmlAttribute16);
			}
			if (value.MultiplyResult != 1f)
			{
				XmlAttribute xmlAttribute17 = constantsNode.OwnerDocument.CreateAttribute("MultiplyResult");
				xmlAttribute17.InnerText = value.MultiplyResult.ToString();
				xmlNode.Attributes.Append(xmlAttribute17);
			}
			constantsNode.AppendChild(xmlNode);
		}
	}

	public static WidgetPrefab LoadFrom(PrefabExtensionContext prefabExtensionContext, WidgetAttributeContext widgetAttributeContext, string path)
	{
		path = Path.GetFullPath(path);
		XmlDocument xmlDocument = new XmlDocument();
		XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
		xmlReaderSettings.IgnoreComments = true;
		using (XmlReader reader = XmlReader.Create(new StreamReader(path), xmlReaderSettings))
		{
			xmlDocument.Load(reader);
		}
		WidgetPrefab widgetPrefab = new WidgetPrefab();
		WidgetTemplate widgetTemplate = null;
		XmlNode xmlNode = xmlDocument.SelectSingleNode("Prefab");
		if (xmlNode != null)
		{
			XmlNode xmlNode2 = xmlNode.SelectSingleNode("Parameters");
			XmlNode xmlNode3 = xmlNode.SelectSingleNode("Constants");
			XmlNode xmlNode4 = xmlNode.SelectSingleNode("Variables");
			XmlNode xmlNode5 = xmlNode.SelectSingleNode("VisualDefinitions");
			XmlNode xmlNode6 = xmlNode.SelectSingleNode("CustomElements");
			XmlNode firstChild = xmlNode.SelectSingleNode("Window").FirstChild;
			widgetTemplate = WidgetTemplate.LoadFrom(prefabExtensionContext, widgetAttributeContext, firstChild);
			if (xmlNode2 != null)
			{
				widgetPrefab.Parameters = LoadParameters(xmlNode2);
			}
			if (xmlNode3 != null)
			{
				widgetPrefab.Constants = LoadConstants(xmlNode3);
			}
			if (xmlNode6 != null)
			{
				widgetPrefab.CustomElements = LoadCustomElements(xmlNode6);
			}
			if (xmlNode5 != null)
			{
				widgetPrefab.VisualDefinitionTemplates = LoadVisualDefinitions(xmlNode5);
			}
		}
		else
		{
			XmlNode firstChild2 = xmlDocument.SelectSingleNode("Window").FirstChild;
			widgetTemplate = WidgetTemplate.LoadFrom(prefabExtensionContext, widgetAttributeContext, firstChild2);
		}
		widgetTemplate.SetRootTemplate(widgetPrefab);
		widgetPrefab.RootTemplate = widgetTemplate;
		foreach (PrefabExtension prefabExtension in prefabExtensionContext.PrefabExtensions)
		{
			prefabExtension.OnLoadingFinished(widgetPrefab);
		}
		return widgetPrefab;
	}

	public XmlDocument Save(PrefabExtensionContext prefabExtensionContext)
	{
		XmlDocument xmlDocument = new XmlDocument();
		XmlNode xmlNode = xmlDocument.CreateElement("Prefab");
		XmlNode xmlNode2 = xmlDocument.CreateElement("Parameters");
		SaveParametersTo(xmlNode2, Parameters);
		xmlNode.AppendChild(xmlNode2);
		XmlNode xmlNode3 = xmlDocument.CreateElement("Constants");
		SaveConstantsTo(xmlNode3, Constants);
		xmlNode.AppendChild(xmlNode3);
		XmlNode xmlNode4 = xmlDocument.CreateElement("VisualDefinitions");
		SaveVisualDefinitionsTo(xmlNode4, VisualDefinitionTemplates);
		xmlNode.AppendChild(xmlNode4);
		XmlNode xmlNode5 = xmlDocument.CreateElement("Window");
		RootTemplate.Save(prefabExtensionContext, xmlNode5);
		xmlNode.AppendChild(xmlNode5);
		xmlDocument.AppendChild(xmlNode);
		return xmlDocument;
	}

	public WidgetInstantiationResult Instantiate(WidgetCreationData widgetCreationData)
	{
		return RootTemplate.Instantiate(widgetCreationData, new Dictionary<string, WidgetAttributeTemplate>());
	}

	public WidgetInstantiationResult Instantiate(WidgetCreationData widgetCreationData, Dictionary<string, WidgetAttributeTemplate> parameters)
	{
		return RootTemplate.Instantiate(widgetCreationData, parameters);
	}

	public void OnRelease()
	{
		RootTemplate.OnRelease();
	}

	public ConstantDefinition GetConstantValue(string name)
	{
		Constants.TryGetValue(name, out var value);
		return value;
	}

	public string GetParameterDefaultValue(string name)
	{
		Parameters.TryGetValue(name, out var value);
		return value;
	}
}
