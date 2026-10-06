using System.Collections.Generic;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class VisualStateTemplate
{
	private Dictionary<string, string> _attributes;

	public string State { get; set; }

	public VisualStateTemplate()
	{
		_attributes = new Dictionary<string, string>();
	}

	public void SetAttribute(string name, string value)
	{
		if (_attributes.ContainsKey(name))
		{
			_attributes[name] = value;
		}
		else
		{
			_attributes.Add(name, value);
		}
	}

	public Dictionary<string, string> GetAttributes()
	{
		return _attributes;
	}

	public void ClearAttribute(string name)
	{
		if (_attributes.ContainsKey(name))
		{
			_attributes.Remove(name);
		}
	}

	public VisualState CreateVisualState(BrushFactory brushFactory, SpriteData spriteData, Dictionary<string, VisualDefinitionTemplate> visualDefinitionTemplates, Dictionary<string, ConstantDefinition> constants, Dictionary<string, WidgetAttributeTemplate> parameters, Dictionary<string, string> defaultParameters)
	{
		VisualState visualState = new VisualState(State);
		foreach (KeyValuePair<string, string> attribute in _attributes)
		{
			string key = attribute.Key;
			string actualValueOf = ConstantDefinition.GetActualValueOf(attribute.Value, brushFactory, spriteData, constants, parameters, defaultParameters);
			WidgetExtensions.SetWidgetAttributeFromString(visualState, key, actualValueOf, brushFactory, spriteData, visualDefinitionTemplates, constants, parameters, null, defaultParameters);
		}
		return visualState;
	}
}
