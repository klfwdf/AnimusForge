using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library.CodeGeneration;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class VariableCollection
{
	public BrushFactory BrushFactory { get; private set; }

	public SpriteData SpriteData { get; private set; }

	public WidgetFactory WidgetFactory { get; private set; }

	public Dictionary<string, ConstantGenerationContext> ConstantTypes { get; private set; }

	public Dictionary<string, ParameterGenerationContext> ParameterTypes { get; private set; }

	public Dictionary<string, WidgetAttributeTemplate> GivenParameters { get; private set; }

	public Dictionary<string, VisualDefinitionTemplate> VisualDefinitionTemplates { get; private set; }

	public VariableCollection(WidgetFactory widgetFactory, BrushFactory brushFactory, SpriteData spriteData)
	{
		WidgetFactory = widgetFactory;
		BrushFactory = brushFactory;
		SpriteData = spriteData;
		GivenParameters = new Dictionary<string, WidgetAttributeTemplate>();
		ConstantTypes = new Dictionary<string, ConstantGenerationContext>();
		ParameterTypes = new Dictionary<string, ParameterGenerationContext>();
		VisualDefinitionTemplates = new Dictionary<string, VisualDefinitionTemplate>();
	}

	public static string GetUseableName(string name)
	{
		return name.Replace(".", "_");
	}

	public void SetGivenParameters(Dictionary<string, WidgetAttributeTemplate> givenParameters)
	{
		GivenParameters = givenParameters;
	}

	public void FillFromPrefab(WidgetPrefab prefab)
	{
		foreach (ConstantDefinition value4 in prefab.Constants.Values)
		{
			ConstantGenerationContext value = new ConstantGenerationContext(value4);
			ConstantTypes.Add(value4.Name, value);
		}
		foreach (KeyValuePair<string, string> parameter in prefab.Parameters)
		{
			string key = parameter.Key;
			string value2 = parameter.Value;
			ParameterGenerationContext value3 = new ParameterGenerationContext(key, value2);
			ParameterTypes.Add(key, value3);
		}
		FillVisualDefinitionsFromPrefab(prefab);
	}

	public void FillVisualDefinitionCreators(ClassCode classCode)
	{
		Dictionary<string, ConstantDefinition> dictionary = new Dictionary<string, ConstantDefinition>();
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		foreach (KeyValuePair<string, ConstantGenerationContext> constantType in ConstantTypes)
		{
			dictionary.Add(constantType.Key, constantType.Value.ConstantDefinition);
		}
		foreach (KeyValuePair<string, ParameterGenerationContext> parameterType in ParameterTypes)
		{
			dictionary2.Add(parameterType.Key, parameterType.Value.Value);
		}
		foreach (VisualDefinitionTemplate value in VisualDefinitionTemplates.Values)
		{
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "CreateVisualDefinition" + GetUseableName(value.Name);
			methodCode.AccessModifier = MethodCodeAccessModifier.Private;
			methodCode.ReturnParameter = "TaleWorlds.GauntletUI.VisualDefinition";
			string name = value.Name;
			float transitionDuration = value.TransitionDuration;
			float delayOnBegin = value.DelayOnBegin;
			string text = "TaleWorlds.GauntletUI.AnimationInterpolation.Type." + value.EaseType;
			string text2 = "TaleWorlds.GauntletUI.AnimationInterpolation.Function." + value.EaseFunction;
			methodCode.AddLine("var visualDefinition = new TaleWorlds.GauntletUI.VisualDefinition(\"" + name + "\", " + transitionDuration + "f, " + delayOnBegin + "f, " + text + ", " + text2 + ");");
			foreach (VisualStateTemplate value2 in value.VisualStates.Values)
			{
				methodCode.AddLine("");
				methodCode.AddLine("{");
				methodCode.AddLine("var visualState = new TaleWorlds.GauntletUI.VisualState(\"" + value2.State + "\");");
				foreach (KeyValuePair<string, string> attribute in value2.GetAttributes())
				{
					string key = attribute.Key;
					string actualValueOf = ConstantDefinition.GetActualValueOf(attribute.Value, BrushFactory, SpriteData, dictionary, GivenParameters, dictionary2);
					methodCode.AddLine("visualState." + key + " = " + actualValueOf + "f;");
				}
				methodCode.AddLine("visualDefinition.AddVisualState(visualState);");
				methodCode.AddLine("}");
			}
			methodCode.AddLine("");
			methodCode.AddLine("return visualDefinition;");
			classCode.AddMethod(methodCode);
		}
	}

	private void FillVisualDefinitionsFromPrefab(WidgetPrefab prefab)
	{
		foreach (VisualDefinitionTemplate value in prefab.VisualDefinitionTemplates.Values)
		{
			VisualDefinitionTemplates.Add(value.Name, value);
		}
	}

	public string GetConstantValue(string constantName)
	{
		ConstantDefinition constantDefinition = ConstantTypes[constantName].ConstantDefinition;
		Dictionary<string, ConstantDefinition> dictionary = new Dictionary<string, ConstantDefinition>();
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		foreach (KeyValuePair<string, ConstantGenerationContext> constantType in ConstantTypes)
		{
			dictionary.Add(constantType.Key, constantType.Value.ConstantDefinition);
		}
		foreach (KeyValuePair<string, ParameterGenerationContext> parameterType in ParameterTypes)
		{
			dictionary2.Add(parameterType.Key, parameterType.Value.Value);
		}
		return constantDefinition.GetValue(BrushFactory, SpriteData, dictionary, GivenParameters, dictionary2);
	}

	public string GetParameterDefaultValue(string parameterName)
	{
		if (ParameterTypes.ContainsKey(parameterName))
		{
			return ParameterTypes[parameterName].Value;
		}
		return "";
	}

	public string GetParameterValue(string parameterName)
	{
		if (GivenParameters.ContainsKey(parameterName))
		{
			WidgetAttributeTemplate widgetAttributeTemplate = GivenParameters[parameterName];
			if (widgetAttributeTemplate.ValueType is WidgetAttributeValueTypeDefault)
			{
				return widgetAttributeTemplate.Value;
			}
			_ = widgetAttributeTemplate.ValueType is WidgetAttributeValueTypeParameter;
			return widgetAttributeTemplate.Value;
		}
		if (ParameterTypes.ContainsKey(parameterName))
		{
			return ParameterTypes[parameterName].Value;
		}
		return "";
	}

	private static bool IsDigitsOnly(string str)
	{
		for (int i = 0; i < str.Length; i++)
		{
			if (!char.IsDigit(str[i]))
			{
				return false;
			}
		}
		return true;
	}

	public PropertyInfo GetPropertyInfo(WidgetTemplate widgetTemplate, string propertyName)
	{
		Type type = null;
		if (WidgetFactory.IsBuiltinType(widgetTemplate.Type))
		{
			type = WidgetFactory.GetBuiltinType(widgetTemplate.Type);
		}
		else
		{
			WidgetPrefab customType = WidgetFactory.GetCustomType(widgetTemplate.Type);
			type = WidgetFactory.GetBuiltinType(customType.RootTemplate.Type);
		}
		GetPropertyInfo(type, propertyName, 0, out var targetPropertyInfo);
		return targetPropertyInfo;
	}

	private static void GetPropertyInfo(Type type, string name, int nameStartIndex, out PropertyInfo targetPropertyInfo)
	{
		int num = name.IndexOf('.', nameStartIndex);
		string name2 = ((num >= 0) ? name.Substring(nameStartIndex, num) : ((nameStartIndex > 0) ? name.Substring(nameStartIndex) : name));
		PropertyInfo property = type.GetProperty(name2, BindingFlags.Instance | BindingFlags.Public);
		if (property != null)
		{
			if (num < 0)
			{
				targetPropertyInfo = property;
			}
			else
			{
				GetPropertyInfo(property.PropertyType, name, num + 1, out targetPropertyInfo);
			}
		}
		else
		{
			targetPropertyInfo = null;
		}
	}

	public static PropertyInfo GetPropertyInfo(Type type, string name)
	{
		GetPropertyInfo(type, name, 0, out var targetPropertyInfo);
		return targetPropertyInfo;
	}
}
