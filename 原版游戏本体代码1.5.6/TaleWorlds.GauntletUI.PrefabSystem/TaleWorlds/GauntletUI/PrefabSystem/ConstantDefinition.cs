using System;
using System.Collections.Generic;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class ConstantDefinition
{
	public string Name { get; private set; }

	public string Value { get; set; }

	public string SpriteName { get; set; }

	public string BrushName { get; set; }

	public string LayerName { get; set; }

	public string Additive { get; set; }

	public string Prefix { get; set; }

	public string Suffix { get; set; }

	public float MultiplyResult { get; set; }

	public string OnTrueValue { get; set; }

	public string OnFalseValue { get; set; }

	public ConstantDefinitionType Type { get; set; }

	public ConstantDefinition(string name)
	{
		Name = name;
		MultiplyResult = 1f;
	}

	public string GetValue(BrushFactory brushFactory, SpriteData spriteData, Dictionary<string, ConstantDefinition> constants, Dictionary<string, WidgetAttributeTemplate> parameters, Dictionary<string, string> defaultParameters)
	{
		string text = "";
		if (Type == ConstantDefinitionType.Constant)
		{
			string text2 = GetActualValueOf(Value, brushFactory, spriteData, constants, parameters, defaultParameters);
			if (!string.IsNullOrEmpty(Additive))
			{
				int num = Convert.ToInt32(GetActualValueOf(Additive, brushFactory, spriteData, constants, parameters, defaultParameters));
				text2 = (Convert.ToInt32(decimal.Parse(text2)) + num).ToString();
			}
			text = text2;
		}
		else if (Type == ConstantDefinitionType.BooleanCheck)
		{
			text = ((!(GetActualValueOf(Value, brushFactory, spriteData, constants, parameters, defaultParameters) == "true")) ? GetActualValueOf(OnFalseValue, brushFactory, spriteData, constants, parameters, defaultParameters) : GetActualValueOf(OnTrueValue, brushFactory, spriteData, constants, parameters, defaultParameters));
		}
		else if (Type == ConstantDefinitionType.BrushLayerWidth || Type == ConstantDefinitionType.BrushLayerHeight)
		{
			string actualValueOf = GetActualValueOf(BrushName, brushFactory, spriteData, constants, parameters, defaultParameters);
			Brush brush = brushFactory.GetBrush(actualValueOf);
			if (brush != null)
			{
				BrushLayer layer = brush.GetLayer(LayerName);
				if (layer != null)
				{
					Sprite sprite = layer.Sprite;
					if (sprite != null)
					{
						int num2 = 0;
						if (!string.IsNullOrEmpty(Additive))
						{
							num2 = Convert.ToInt32(GetActualValueOf(Additive, brushFactory, spriteData, constants, parameters, defaultParameters));
						}
						if (Type == ConstantDefinitionType.BrushLayerWidth)
						{
							float extendLeft = layer.ExtendLeft;
							float extendRight = layer.ExtendRight;
							text = ((int)((float)sprite.Width - extendLeft - extendRight + (float)num2)).ToString();
						}
						else if (Type == ConstantDefinitionType.BrushLayerHeight)
						{
							float extendTop = layer.ExtendTop;
							float extendBottom = layer.ExtendBottom;
							text = ((int)((float)sprite.Height - extendTop - extendBottom + (float)num2)).ToString();
						}
					}
				}
			}
		}
		else if (Type == ConstantDefinitionType.SpriteWidth || Type == ConstantDefinitionType.SpriteHeight)
		{
			Sprite sprite2 = spriteData.GetSprite(SpriteName);
			if (sprite2 != null)
			{
				int num3 = 0;
				if (!string.IsNullOrEmpty(Additive))
				{
					num3 = Convert.ToInt32(GetActualValueOf(Additive, brushFactory, spriteData, constants, parameters, defaultParameters));
				}
				if (Type == ConstantDefinitionType.SpriteWidth)
				{
					text = (sprite2.Width + num3).ToString();
				}
				else if (Type == ConstantDefinitionType.SpriteHeight)
				{
					text = (sprite2.Height + num3).ToString();
				}
			}
		}
		if (MultiplyResult != 1f)
		{
			text = ((float)Convert.ToInt32(text) * MultiplyResult).ToString();
		}
		if (!string.IsNullOrEmpty(Prefix))
		{
			text = Prefix + text;
		}
		if (!string.IsNullOrEmpty(Suffix))
		{
			text += Suffix;
		}
		return text;
	}

	public static string GetActualValueOf(string value, BrushFactory brushFactory, SpriteData spriteData, Dictionary<string, ConstantDefinition> constants, Dictionary<string, WidgetAttributeTemplate> parameters, Dictionary<string, string> defaultParameters)
	{
		string result = "";
		if (value.StartsWith("!"))
		{
			string key = value.Substring("!".Length);
			if (constants.ContainsKey(key))
			{
				result = constants[key].GetValue(brushFactory, spriteData, constants, parameters, defaultParameters);
			}
		}
		else if (value.StartsWith("*"))
		{
			string key2 = value.Substring("*".Length);
			if (parameters.ContainsKey(key2))
			{
				result = parameters[key2].Value;
			}
			else if (defaultParameters != null && defaultParameters.ContainsKey(key2))
			{
				result = defaultParameters[key2];
			}
		}
		else
		{
			result = value;
		}
		return result;
	}
}
