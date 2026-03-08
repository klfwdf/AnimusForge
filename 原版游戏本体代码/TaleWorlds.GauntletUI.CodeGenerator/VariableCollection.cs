using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library.CodeGeneration;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000006 RID: 6
	public class VariableCollection
	{
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600002B RID: 43 RVA: 0x0000220F File Offset: 0x0000040F
		// (set) Token: 0x0600002C RID: 44 RVA: 0x00002217 File Offset: 0x00000417
		public BrushFactory BrushFactory { get; private set; }

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600002D RID: 45 RVA: 0x00002220 File Offset: 0x00000420
		// (set) Token: 0x0600002E RID: 46 RVA: 0x00002228 File Offset: 0x00000428
		public SpriteData SpriteData { get; private set; }

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600002F RID: 47 RVA: 0x00002231 File Offset: 0x00000431
		// (set) Token: 0x06000030 RID: 48 RVA: 0x00002239 File Offset: 0x00000439
		public WidgetFactory WidgetFactory { get; private set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000031 RID: 49 RVA: 0x00002242 File Offset: 0x00000442
		// (set) Token: 0x06000032 RID: 50 RVA: 0x0000224A File Offset: 0x0000044A
		public Dictionary<string, ConstantGenerationContext> ConstantTypes { get; private set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000033 RID: 51 RVA: 0x00002253 File Offset: 0x00000453
		// (set) Token: 0x06000034 RID: 52 RVA: 0x0000225B File Offset: 0x0000045B
		public Dictionary<string, ParameterGenerationContext> ParameterTypes { get; private set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000035 RID: 53 RVA: 0x00002264 File Offset: 0x00000464
		// (set) Token: 0x06000036 RID: 54 RVA: 0x0000226C File Offset: 0x0000046C
		public Dictionary<string, WidgetAttributeTemplate> GivenParameters { get; private set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000037 RID: 55 RVA: 0x00002275 File Offset: 0x00000475
		// (set) Token: 0x06000038 RID: 56 RVA: 0x0000227D File Offset: 0x0000047D
		public Dictionary<string, VisualDefinitionTemplate> VisualDefinitionTemplates { get; private set; }

		// Token: 0x06000039 RID: 57 RVA: 0x00002288 File Offset: 0x00000488
		public VariableCollection(WidgetFactory widgetFactory, BrushFactory brushFactory, SpriteData spriteData)
		{
			this.WidgetFactory = widgetFactory;
			this.BrushFactory = brushFactory;
			this.SpriteData = spriteData;
			this.GivenParameters = new Dictionary<string, WidgetAttributeTemplate>();
			this.ConstantTypes = new Dictionary<string, ConstantGenerationContext>();
			this.ParameterTypes = new Dictionary<string, ParameterGenerationContext>();
			this.VisualDefinitionTemplates = new Dictionary<string, VisualDefinitionTemplate>();
		}

		// Token: 0x0600003A RID: 58 RVA: 0x000022DC File Offset: 0x000004DC
		public static string GetUseableName(string name)
		{
			return name.Replace(".", "_");
		}

		// Token: 0x0600003B RID: 59 RVA: 0x000022EE File Offset: 0x000004EE
		public void SetGivenParameters(Dictionary<string, WidgetAttributeTemplate> givenParameters)
		{
			this.GivenParameters = givenParameters;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000022F8 File Offset: 0x000004F8
		public void FillFromPrefab(WidgetPrefab prefab)
		{
			foreach (ConstantDefinition constantDefinition in prefab.Constants.Values)
			{
				ConstantGenerationContext value = new ConstantGenerationContext(constantDefinition);
				this.ConstantTypes.Add(constantDefinition.Name, value);
			}
			foreach (KeyValuePair<string, string> keyValuePair in prefab.Parameters)
			{
				string key = keyValuePair.Key;
				string value2 = keyValuePair.Value;
				ParameterGenerationContext value3 = new ParameterGenerationContext(key, value2);
				this.ParameterTypes.Add(key, value3);
			}
			this.FillVisualDefinitionsFromPrefab(prefab);
		}

		// Token: 0x0600003D RID: 61 RVA: 0x000023D4 File Offset: 0x000005D4
		public void FillVisualDefinitionCreators(ClassCode classCode)
		{
			Dictionary<string, ConstantDefinition> dictionary = new Dictionary<string, ConstantDefinition>();
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			foreach (KeyValuePair<string, ConstantGenerationContext> keyValuePair in this.ConstantTypes)
			{
				dictionary.Add(keyValuePair.Key, keyValuePair.Value.ConstantDefinition);
			}
			foreach (KeyValuePair<string, ParameterGenerationContext> keyValuePair2 in this.ParameterTypes)
			{
				dictionary2.Add(keyValuePair2.Key, keyValuePair2.Value.Value);
			}
			foreach (VisualDefinitionTemplate visualDefinitionTemplate in this.VisualDefinitionTemplates.Values)
			{
				MethodCode methodCode = new MethodCode();
				methodCode.Name = "CreateVisualDefinition" + VariableCollection.GetUseableName(visualDefinitionTemplate.Name);
				methodCode.AccessModifier = MethodCodeAccessModifier.Private;
				methodCode.ReturnParameter = "TaleWorlds.GauntletUI.VisualDefinition";
				string name = visualDefinitionTemplate.Name;
				float transitionDuration = visualDefinitionTemplate.TransitionDuration;
				float delayOnBegin = visualDefinitionTemplate.DelayOnBegin;
				string text = "TaleWorlds.GauntletUI.AnimationInterpolation.Type." + visualDefinitionTemplate.EaseType.ToString();
				string text2 = "TaleWorlds.GauntletUI.AnimationInterpolation.Function." + visualDefinitionTemplate.EaseFunction.ToString();
				methodCode.AddLine(string.Concat(new object[]
				{
					"var visualDefinition = new TaleWorlds.GauntletUI.VisualDefinition(\"", name, "\", ", transitionDuration, "f, ", delayOnBegin, "f, ", text, ", ", text2,
					");"
				}));
				foreach (VisualStateTemplate visualStateTemplate in visualDefinitionTemplate.VisualStates.Values)
				{
					methodCode.AddLine("");
					methodCode.AddLine("{");
					methodCode.AddLine("var visualState = new TaleWorlds.GauntletUI.VisualState(\"" + visualStateTemplate.State + "\");");
					foreach (KeyValuePair<string, string> keyValuePair3 in visualStateTemplate.GetAttributes())
					{
						string key = keyValuePair3.Key;
						string actualValueOf = ConstantDefinition.GetActualValueOf(keyValuePair3.Value, this.BrushFactory, this.SpriteData, dictionary, this.GivenParameters, dictionary2);
						methodCode.AddLine(string.Concat(new string[] { "visualState.", key, " = ", actualValueOf, "f;" }));
					}
					methodCode.AddLine("visualDefinition.AddVisualState(visualState);");
					methodCode.AddLine("}");
				}
				methodCode.AddLine("");
				methodCode.AddLine("return visualDefinition;");
				classCode.AddMethod(methodCode);
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002774 File Offset: 0x00000974
		private void FillVisualDefinitionsFromPrefab(WidgetPrefab prefab)
		{
			foreach (VisualDefinitionTemplate visualDefinitionTemplate in prefab.VisualDefinitionTemplates.Values)
			{
				this.VisualDefinitionTemplates.Add(visualDefinitionTemplate.Name, visualDefinitionTemplate);
			}
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000027D8 File Offset: 0x000009D8
		public string GetConstantValue(string constantName)
		{
			ConstantDefinition constantDefinition = this.ConstantTypes[constantName].ConstantDefinition;
			Dictionary<string, ConstantDefinition> dictionary = new Dictionary<string, ConstantDefinition>();
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			foreach (KeyValuePair<string, ConstantGenerationContext> keyValuePair in this.ConstantTypes)
			{
				dictionary.Add(keyValuePair.Key, keyValuePair.Value.ConstantDefinition);
			}
			foreach (KeyValuePair<string, ParameterGenerationContext> keyValuePair2 in this.ParameterTypes)
			{
				dictionary2.Add(keyValuePair2.Key, keyValuePair2.Value.Value);
			}
			return constantDefinition.GetValue(this.BrushFactory, this.SpriteData, dictionary, this.GivenParameters, dictionary2);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000028CC File Offset: 0x00000ACC
		public string GetParameterDefaultValue(string parameterName)
		{
			if (this.ParameterTypes.ContainsKey(parameterName))
			{
				return this.ParameterTypes[parameterName].Value;
			}
			return "";
		}

		// Token: 0x06000041 RID: 65 RVA: 0x000028F4 File Offset: 0x00000AF4
		public string GetParameterValue(string parameterName)
		{
			if (this.GivenParameters.ContainsKey(parameterName))
			{
				WidgetAttributeTemplate widgetAttributeTemplate = this.GivenParameters[parameterName];
				if (widgetAttributeTemplate.ValueType is WidgetAttributeValueTypeDefault)
				{
					return widgetAttributeTemplate.Value;
				}
				WidgetAttributeValueTypeParameter widgetAttributeValueTypeParameter = widgetAttributeTemplate.ValueType as WidgetAttributeValueTypeParameter;
				return widgetAttributeTemplate.Value;
			}
			else
			{
				if (this.ParameterTypes.ContainsKey(parameterName))
				{
					return this.ParameterTypes[parameterName].Value;
				}
				return "";
			}
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002968 File Offset: 0x00000B68
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

		// Token: 0x06000043 RID: 67 RVA: 0x0000299C File Offset: 0x00000B9C
		public PropertyInfo GetPropertyInfo(WidgetTemplate widgetTemplate, string propertyName)
		{
			Type builtinType;
			if (this.WidgetFactory.IsBuiltinType(widgetTemplate.Type))
			{
				builtinType = this.WidgetFactory.GetBuiltinType(widgetTemplate.Type);
			}
			else
			{
				WidgetPrefab customType = this.WidgetFactory.GetCustomType(widgetTemplate.Type);
				builtinType = this.WidgetFactory.GetBuiltinType(customType.RootTemplate.Type);
			}
			PropertyInfo result;
			VariableCollection.GetPropertyInfo(builtinType, propertyName, 0, out result);
			return result;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00002A08 File Offset: 0x00000C08
		private static void GetPropertyInfo(Type type, string name, int nameStartIndex, out PropertyInfo targetPropertyInfo)
		{
			int num = name.IndexOf('.', nameStartIndex);
			string name2 = ((num >= 0) ? name.Substring(nameStartIndex, num) : ((nameStartIndex > 0) ? name.Substring(nameStartIndex) : name));
			PropertyInfo property = type.GetProperty(name2, BindingFlags.Instance | BindingFlags.Public);
			if (!(property != null))
			{
				targetPropertyInfo = null;
				return;
			}
			if (num < 0)
			{
				targetPropertyInfo = property;
				return;
			}
			VariableCollection.GetPropertyInfo(property.PropertyType, name, num + 1, out targetPropertyInfo);
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002A6C File Offset: 0x00000C6C
		public static PropertyInfo GetPropertyInfo(Type type, string name)
		{
			PropertyInfo result;
			VariableCollection.GetPropertyInfo(type, name, 0, out result);
			return result;
		}
	}
}
