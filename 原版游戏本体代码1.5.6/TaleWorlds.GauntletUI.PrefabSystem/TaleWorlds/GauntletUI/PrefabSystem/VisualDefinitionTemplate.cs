using System.Collections.Generic;
using System.Xml;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class VisualDefinitionTemplate
{
	public string Name { get; set; }

	public float TransitionDuration { get; set; }

	public float DelayOnBegin { get; set; }

	public AnimationInterpolation.Type EaseType { get; set; }

	public AnimationInterpolation.Function EaseFunction { get; set; }

	public Dictionary<string, VisualStateTemplate> VisualStates { get; private set; }

	public VisualDefinitionTemplate()
	{
		VisualStates = new Dictionary<string, VisualStateTemplate>();
		TransitionDuration = 0.2f;
	}

	public void AddVisualState(VisualStateTemplate visualState)
	{
		VisualStates.Add(visualState.State, visualState);
	}

	public VisualDefinition CreateVisualDefinition(BrushFactory brushFactory, SpriteData spriteData, Dictionary<string, VisualDefinitionTemplate> visualDefinitionTemplates, Dictionary<string, ConstantDefinition> constants, Dictionary<string, WidgetAttributeTemplate> parameters, Dictionary<string, string> defaultParameters)
	{
		VisualDefinition visualDefinition = new VisualDefinition(Name, TransitionDuration, DelayOnBegin, EaseType, EaseFunction);
		foreach (VisualStateTemplate value in VisualStates.Values)
		{
			VisualState visualState = value.CreateVisualState(brushFactory, spriteData, visualDefinitionTemplates, constants, parameters, defaultParameters);
			visualDefinition.AddVisualState(visualState);
		}
		return visualDefinition;
	}

	internal void Save(XmlNode rootNode)
	{
		XmlDocument ownerDocument = rootNode.OwnerDocument;
		XmlNode xmlNode = ownerDocument.CreateElement("VisualDefinition");
		XmlAttribute xmlAttribute = ownerDocument.CreateAttribute("Name");
		xmlAttribute.InnerText = Name;
		xmlNode.Attributes.Append(xmlAttribute);
		XmlAttribute xmlAttribute2 = ownerDocument.CreateAttribute("TransitionDuration");
		xmlAttribute2.InnerText = TransitionDuration.ToString();
		xmlNode.Attributes.Append(xmlAttribute2);
		XmlAttribute xmlAttribute3 = ownerDocument.CreateAttribute("DelayOnBegin");
		xmlAttribute3.InnerText = DelayOnBegin.ToString();
		xmlNode.Attributes.Append(xmlAttribute3);
		XmlAttribute xmlAttribute4 = ownerDocument.CreateAttribute("EaseType");
		xmlAttribute4.InnerText = EaseType.ToString();
		xmlNode.Attributes.Append(xmlAttribute4);
		XmlAttribute xmlAttribute5 = ownerDocument.CreateAttribute("EaseFunction");
		xmlAttribute5.InnerText = EaseFunction.ToString();
		xmlNode.Attributes.Append(xmlAttribute5);
		foreach (VisualStateTemplate value in VisualStates.Values)
		{
			XmlNode xmlNode2 = ownerDocument.CreateElement("VisualState");
			XmlAttribute xmlAttribute6 = ownerDocument.CreateAttribute("State");
			xmlAttribute6.InnerText = value.State;
			xmlNode2.Attributes.Append(xmlAttribute6);
			foreach (KeyValuePair<string, string> attribute in value.GetAttributes())
			{
				XmlAttribute xmlAttribute7 = ownerDocument.CreateAttribute(attribute.Key);
				xmlAttribute7.InnerText = attribute.Value;
				xmlNode2.Attributes.Append(xmlAttribute7);
			}
			xmlNode.AppendChild(xmlNode2);
		}
		rootNode.AppendChild(xmlNode);
	}
}
