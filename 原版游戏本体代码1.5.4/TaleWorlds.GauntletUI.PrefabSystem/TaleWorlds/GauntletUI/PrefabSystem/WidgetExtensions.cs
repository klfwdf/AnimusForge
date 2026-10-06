using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public static class WidgetExtensions
{
	private static void GetObjectAndProperty(object parent, string name, int nameStartIndex, out object targetObject, out PropertyInfo targetPropertyInfo)
	{
		int num = name.IndexOf('.', nameStartIndex);
		PropertyInfo property = parent.GetType().GetProperty((num >= 0) ? name.Substring(nameStartIndex, num) : ((nameStartIndex > 0) ? name.Substring(nameStartIndex) : name), BindingFlags.Instance | BindingFlags.Public);
		if (property != null)
		{
			if (num < 0)
			{
				targetObject = parent;
				targetPropertyInfo = property;
			}
			else
			{
				GetObjectAndProperty(property.GetGetMethod().Invoke(parent, new object[0]), name, num + 1, out targetObject, out targetPropertyInfo);
			}
		}
		else
		{
			targetPropertyInfo = null;
			targetObject = null;
		}
	}

	public static void SetWidgetAttributeFromString(object target, string name, string value, BrushFactory brushFactory, SpriteData spriteData, Dictionary<string, VisualDefinitionTemplate> visualDefinitionTemplates, Dictionary<string, ConstantDefinition> constants, Dictionary<string, WidgetAttributeTemplate> parameters, Dictionary<string, XmlElement> customElements, Dictionary<string, string> defaultParameters)
	{
		try
		{
			SetWidgetAttributeFromStringAux(target, name, value, brushFactory, spriteData, visualDefinitionTemplates, constants, parameters, customElements, defaultParameters);
		}
		catch (Exception ex)
		{
			Debug.FailedAssert($"Failed to set attribute from string.\nTarget:{target}\nName:{name}\nValue:{value}\n{ex.Message}", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI.PrefabSystem\\WidgetExtensions.cs", "SetWidgetAttributeFromString", 54);
		}
	}

	private static void SetWidgetAttributeFromStringAux(object target, string name, string value, BrushFactory brushFactory, SpriteData spriteData, Dictionary<string, VisualDefinitionTemplate> visualDefinitionTemplates, Dictionary<string, ConstantDefinition> constants, Dictionary<string, WidgetAttributeTemplate> parameters, Dictionary<string, XmlElement> customElements, Dictionary<string, string> defaultParameters)
	{
		GetObjectAndProperty(target, name, 0, out var targetObject, out var targetPropertyInfo);
		if (targetPropertyInfo == null)
		{
			return;
		}
		if (targetPropertyInfo.PropertyType == typeof(int))
		{
			int num = Convert.ToInt32(value);
			targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { num });
		}
		else if (targetPropertyInfo.PropertyType == typeof(float))
		{
			float num2 = Convert.ToSingle(value, CultureInfo.InvariantCulture);
			targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { num2 });
		}
		else if (targetPropertyInfo.PropertyType == typeof(bool))
		{
			bool flag = value == "true";
			targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { flag });
		}
		else if (targetPropertyInfo.PropertyType == typeof(string))
		{
			targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { value });
		}
		else if (targetPropertyInfo.PropertyType == typeof(Brush))
		{
			if (brushFactory != null)
			{
				Brush brush = brushFactory.GetBrush(value);
				targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { brush });
			}
		}
		else if (targetPropertyInfo.PropertyType == typeof(Sprite))
		{
			if (spriteData != null)
			{
				Sprite sprite = spriteData.GetSprite(value);
				targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { sprite });
			}
		}
		else if (targetPropertyInfo.PropertyType.IsEnum)
		{
			object obj = Enum.Parse(targetPropertyInfo.PropertyType, value);
			targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { obj });
		}
		else if (targetPropertyInfo.PropertyType == typeof(Color))
		{
			Color color = Color.ConvertStringToColor(value);
			targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { color });
		}
		else if (targetPropertyInfo.PropertyType == typeof(XmlElement))
		{
			if (customElements != null && customElements.ContainsKey(value))
			{
				XmlElement xmlElement = customElements[value];
				targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { xmlElement });
			}
		}
		else if (typeof(Widget).IsAssignableFrom(targetPropertyInfo.PropertyType))
		{
			if (target is Widget widget)
			{
				BindingPath path = new BindingPath(value);
				Widget widget2 = widget.FindChild(path);
				targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { widget2 });
			}
		}
		else if (targetPropertyInfo.PropertyType == typeof(VisualDefinition) && visualDefinitionTemplates != null)
		{
			VisualDefinition visualDefinition = visualDefinitionTemplates[value].CreateVisualDefinition(brushFactory, spriteData, visualDefinitionTemplates, constants, parameters, defaultParameters);
			targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { visualDefinition });
		}
	}

	public static Type GetWidgetAttributeType(object target, string name)
	{
		GetObjectAndProperty(target, name, 0, out var _, out var targetPropertyInfo);
		if (targetPropertyInfo != null)
		{
			return targetPropertyInfo.PropertyType;
		}
		return null;
	}

	public static void SetWidgetAttribute(UIContext context, object target, string name, object value)
	{
		GetObjectAndProperty(target, name, 0, out var targetObject, out var targetPropertyInfo);
		if (targetPropertyInfo != null)
		{
			object obj = ConvertObject(context, value, targetPropertyInfo.PropertyType);
			targetPropertyInfo.GetSetMethod().Invoke(targetObject, new object[1] { obj });
		}
	}

	private static object ConvertObject(UIContext context, object input, Type targetType)
	{
		object result = input;
		if (input != null && input.GetType() == typeof(string))
		{
			if (targetType == typeof(Sprite))
			{
				result = context.SpriteData.GetSprite((string)input);
			}
			else if (targetType == typeof(Brush))
			{
				result = context.GetBrush((string)input);
			}
			else if (targetType == typeof(int))
			{
				result = Convert.ToInt32(input);
			}
			else if (targetType == typeof(Color))
			{
				result = Color.ConvertStringToColor((string)input);
			}
		}
		return result;
	}
}
