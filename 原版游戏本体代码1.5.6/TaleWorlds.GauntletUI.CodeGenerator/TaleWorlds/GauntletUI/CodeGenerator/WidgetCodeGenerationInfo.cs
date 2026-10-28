using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.CodeGeneration;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class WidgetCodeGenerationInfo
{
	public WidgetTemplateGenerateContext RootWidgetTemplateGenerateContext { get; private set; }

	public WidgetTemplate WidgetTemplate { get; private set; }

	public WidgetFactory WidgetFactory { get; private set; }

	public string VariableName { get; private set; }

	public WidgetCodeGenerationInfo Parent { get; private set; }

	public WidgetCodeGenerationInfoExtension Extension { get; private set; }

	public WidgetTemplateGenerateContext ChildWidgetTemplateGenerateContext { get; private set; }

	public bool IsRoot => Parent == null;

	public string Id => WidgetTemplate.Id;

	public List<WidgetCodeGenerationInfo> Children { get; private set; }

	public WidgetCodeGenerationInfo(WidgetTemplateGenerateContext widgetTemplateGenerateContext, WidgetTemplate widgetTemplate, string variableName, WidgetCodeGenerationInfo parent)
	{
		RootWidgetTemplateGenerateContext = widgetTemplateGenerateContext;
		WidgetFactory = widgetTemplateGenerateContext.WidgetFactory;
		WidgetTemplate = widgetTemplate;
		VariableName = variableName;
		Parent = parent;
		Children = new List<WidgetCodeGenerationInfo>();
	}

	public WidgetCodeGenerationInfoChildSearchResult FindChild(BindingPath path)
	{
		string firstNode = path.FirstNode;
		BindingPath subPath = path.SubPath;
		if (firstNode == "..")
		{
			if (Parent != null)
			{
				return Parent.FindChild(subPath);
			}
			return new WidgetCodeGenerationInfoChildSearchResult
			{
				RemainingPath = path,
				ReachedWidget = this
			};
		}
		if (firstNode == ".")
		{
			return new WidgetCodeGenerationInfoChildSearchResult
			{
				FoundWidget = this
			};
		}
		foreach (WidgetCodeGenerationInfo child in Children)
		{
			if (!string.IsNullOrEmpty(child.Id) && child.Id == firstNode)
			{
				if (subPath == null)
				{
					return new WidgetCodeGenerationInfoChildSearchResult
					{
						FoundWidget = child
					};
				}
				return child.FindChild(subPath);
			}
		}
		return new WidgetCodeGenerationInfoChildSearchResult
		{
			RemainingPath = path,
			ReachedWidget = this
		};
	}

	public Dictionary<string, WidgetAttributeTemplate> GetPassedParametersToChild()
	{
		Dictionary<string, WidgetAttributeTemplate> dictionary = new Dictionary<string, WidgetAttributeTemplate>();
		foreach (KeyValuePair<Type, Dictionary<string, WidgetAttributeTemplate>> attribute in WidgetTemplate.Attributes)
		{
			foreach (KeyValuePair<string, WidgetAttributeTemplate> item in attribute.Value)
			{
				WidgetAttributeKeyType keyType = item.Value.KeyType;
				WidgetAttributeValueType valueType = item.Value.ValueType;
				string key = item.Value.Key;
				_ = item.Value.Value;
				if (keyType is WidgetAttributeKeyTypeParameter)
				{
					WidgetAttributeTemplate value = item.Value;
					if (valueType is WidgetAttributeValueTypeParameter && RootWidgetTemplateGenerateContext.VariableCollection.GivenParameters.TryGetValue(key, out var value2))
					{
						value = value2;
					}
					dictionary.Add(key, value);
				}
			}
		}
		return dictionary;
	}

	public void CheckDependendType()
	{
		if (RootWidgetTemplateGenerateContext.IsBuiltinType(WidgetTemplate))
		{
			return;
		}
		PrefabDependencyContext prefabDependencyContext = RootWidgetTemplateGenerateContext.PrefabDependencyContext;
		UICodeGenerationVariantExtension variantExtension = null;
		Dictionary<string, object> data = new Dictionary<string, object>();
		Dictionary<string, WidgetAttributeTemplate> passedParametersToChild = GetPassedParametersToChild();
		if (Extension != null)
		{
			Extension.TryGetVariantPropertiesForNewDependency(out variantExtension, out data);
		}
		if (!prefabDependencyContext.ContainsDependency(WidgetTemplate.Type, passedParametersToChild, data, IsRoot))
		{
			if (IsRoot)
			{
				ChildWidgetTemplateGenerateContext = WidgetTemplateGenerateContext.CreateAsInheritedDependendPrefab(RootWidgetTemplateGenerateContext.UICodeGenerationContext, prefabDependencyContext, WidgetTemplate.Type, "InheritedPrefab", variantExtension, data, passedParametersToChild);
			}
			else
			{
				ChildWidgetTemplateGenerateContext = WidgetTemplateGenerateContext.CreateAsDependendPrefab(RootWidgetTemplateGenerateContext.UICodeGenerationContext, prefabDependencyContext, WidgetTemplate.Type, "DependendPrefab", variantExtension, data, passedParametersToChild);
			}
		}
		else
		{
			PrefabDependency dependendPrefab = prefabDependencyContext.GetDependendPrefab(WidgetTemplate.Type, passedParametersToChild, data, IsRoot);
			ChildWidgetTemplateGenerateContext = dependendPrefab.WidgetTemplateGenerateContext;
		}
	}

	public string GetUseableTypeName()
	{
		if (WidgetFactory.IsBuiltinType(WidgetTemplate.Type))
		{
			return WidgetFactory.GetBuiltinType(WidgetTemplate.Type).FullName;
		}
		return ChildWidgetTemplateGenerateContext.ClassName;
	}

	public VariableCode CreateVariableCode()
	{
		VariableCode obj = new VariableCode
		{
			Name = VariableName,
			AccessModifier = VariableCodeAccessModifier.Private
		};
		string useableTypeName = GetUseableTypeName();
		obj.Type = useableTypeName;
		return obj;
	}

	internal void AddChild(WidgetCodeGenerationInfo widgetCodeGenerationInfo)
	{
		Children.Add(widgetCodeGenerationInfo);
	}

	internal void FillCreateWidgetsMethod(MethodCode methodCode)
	{
		string text = ((Parent != null) ? Parent.VariableName : null);
		bool flag = WidgetFactory.IsBuiltinType(WidgetTemplate.Type);
		if (IsRoot)
		{
			methodCode.AddLine(VariableName + " = this;");
		}
		else
		{
			string useableTypeName = GetUseableTypeName();
			methodCode.AddLine(VariableName + " = new " + useableTypeName + "(this.Context);");
			if (Extension != null)
			{
				Extension.OnFillCreateWidgetMethod(methodCode);
			}
			if (Parent.ChildWidgetTemplateGenerateContext != null && Parent.ChildWidgetTemplateGenerateContext.GotLogicalChildrenLocation)
			{
				methodCode.AddLine(text + ".AddChildToLogicalLocation(" + VariableName + ");");
			}
			else
			{
				methodCode.AddLine(text + ".AddChild(" + VariableName + ");");
			}
		}
		if (!IsRoot && !flag)
		{
			methodCode.AddLine(VariableName + ".CreateWidgets();");
		}
	}

	internal void FillSetAttributesMethod(MethodCode methodCode)
	{
		VariableCollection variableCollection = RootWidgetTemplateGenerateContext.VariableCollection;
		string text = (IsRoot ? "this" : VariableName);
		if (!WidgetFactory.IsBuiltinType(WidgetTemplate.Type) && !IsRoot)
		{
			methodCode.AddLine(text + ".SetAttributes();");
		}
		foreach (KeyValuePair<Type, Dictionary<string, WidgetAttributeTemplate>> attribute in WidgetTemplate.Attributes)
		{
			foreach (KeyValuePair<string, WidgetAttributeTemplate> item in attribute.Value)
			{
				WidgetAttributeKeyType keyType = item.Value.KeyType;
				WidgetAttributeValueType valueType = item.Value.ValueType;
				bool flag = false;
				string key = item.Value.Key;
				string value = item.Value.Value;
				if (keyType is WidgetAttributeKeyTypeId)
				{
					flag = true;
				}
				else if (keyType is WidgetAttributeKeyTypeAttribute)
				{
					if (valueType is WidgetAttributeValueTypeDefault)
					{
						AddDefaultAttributeSet(methodCode, text, key, value);
						flag = true;
					}
					else if (valueType is WidgetAttributeValueTypeConstant)
					{
						string constantValue = variableCollection.GetConstantValue(value);
						methodCode.AddLine("//From constant " + value + ":" + constantValue);
						AddDefaultAttributeSet(methodCode, text, key, constantValue);
						flag = true;
					}
					else if (valueType is WidgetAttributeValueTypeParameter)
					{
						string text2 = variableCollection.GetParameterDefaultValue(value);
						bool flag2 = true;
						if (variableCollection.GivenParameters.TryGetValue(value, out var value2))
						{
							if (value2.ValueType is WidgetAttributeValueTypeDefault)
							{
								text2 = value2.Value;
							}
							else if (value2.ValueType is WidgetAttributeValueTypeConstant)
							{
								methodCode.AddLine("//parameter below has something different then default value type, " + value2.ValueType);
							}
							else if (value2.ValueType is WidgetAttributeValueTypeParameter)
							{
								methodCode.AddLine("//parameter below has something different then default value type, " + value2.ValueType);
							}
							else
							{
								flag2 = false;
								methodCode.AddLine("//parameter below has something different then default value type, " + value2.ValueType);
							}
						}
						if (flag2)
						{
							methodCode.AddLine("//From parameter " + value + ":" + text2);
							AddDefaultAttributeSet(methodCode, text, key, text2);
							flag = true;
						}
					}
				}
				if (!flag)
				{
					methodCode.AddLine("//");
					methodCode.AddLine("//" + item.Value.KeyType);
					methodCode.AddLine("//" + item.Value.ValueType);
					methodCode.AddLine("//" + item.Value.Key + " " + item.Value.Value);
					methodCode.AddLine("//");
				}
			}
		}
		methodCode.AddLine("");
		if (Extension != null)
		{
			Extension.OnFillSetAttributesMethod(methodCode);
		}
	}

	private Type GetAttributeType(string propertyName)
	{
		PropertyInfo propertyInfo = RootWidgetTemplateGenerateContext.VariableCollection.GetPropertyInfo(WidgetTemplate, propertyName);
		if (propertyInfo != null)
		{
			return propertyInfo.PropertyType;
		}
		return null;
	}

	private void AddDefaultAttributeSet(MethodCode methodCode, string targetWidgetName, string propertyName, string value)
	{
		Type attributeType = GetAttributeType(propertyName);
		if (!(attributeType != null))
		{
			return;
		}
		if (attributeType == typeof(int))
		{
			methodCode.AddLine(targetWidgetName + "." + propertyName + " = " + value + ";");
		}
		else if (attributeType == typeof(float))
		{
			methodCode.AddLine(targetWidgetName + "." + propertyName + " = " + value + "f;");
		}
		else if (attributeType == typeof(bool))
		{
			methodCode.AddLine(targetWidgetName + "." + propertyName + " = " + value.ToLower() + ";");
		}
		else if (attributeType == typeof(string))
		{
			methodCode.AddLine(targetWidgetName + "." + propertyName + " = @\"" + value + "\";");
		}
		else if (attributeType == typeof(Brush))
		{
			methodCode.AddLine(targetWidgetName + "." + propertyName + " = this.Context.GetBrush(@\"" + value + "\");");
		}
		else if (attributeType == typeof(Sprite))
		{
			methodCode.AddLine(targetWidgetName + "." + propertyName + " = this.Context.SpriteData.GetSprite(@\"" + value + "\");");
		}
		else if (attributeType.IsEnum)
		{
			methodCode.AddLine(targetWidgetName + "." + propertyName + " = " + WidgetTemplateGenerateContext.GetCodeFileType(attributeType) + "." + value + ";");
		}
		else if (attributeType == typeof(Color))
		{
			Color color = Color.ConvertStringToColor(value);
			methodCode.AddLine(targetWidgetName + "." + propertyName + " = new TaleWorlds.Library.Color(" + color.Red + "f, " + color.Green + "f, " + color.Blue + "f, " + color.Alpha + "f);");
		}
		else if (attributeType == typeof(XmlElement))
		{
			methodCode.AddLine("//XmlElement - " + targetWidgetName + "." + propertyName + " = \"" + value + "\";");
		}
		else if (typeof(Widget).IsAssignableFrom(attributeType))
		{
			WidgetCodeGenerationInfoChildSearchResult widgetCodeGenerationInfoChildSearchResult = FindChild(new BindingPath(value));
			if (widgetCodeGenerationInfoChildSearchResult != null && widgetCodeGenerationInfoChildSearchResult.FoundWidget != null)
			{
				methodCode.AddLine("//Found widget during generation - " + value);
				methodCode.AddLine(targetWidgetName + "." + propertyName + " = " + widgetCodeGenerationInfoChildSearchResult.FoundWidget.VariableName + ";");
			}
			else if (widgetCodeGenerationInfoChildSearchResult != null && widgetCodeGenerationInfoChildSearchResult.ReachedWidget != null)
			{
				methodCode.AddLine("//Found widget partial path during generation - " + value);
				if (widgetCodeGenerationInfoChildSearchResult.RemainingPath.Nodes.Length == 1)
				{
					methodCode.AddLine(targetWidgetName + "." + propertyName + " = " + widgetCodeGenerationInfoChildSearchResult.ReachedWidget.VariableName + ".FindChild(@\"" + widgetCodeGenerationInfoChildSearchResult.RemainingPath.Path + "\") as " + attributeType.FullName + ";");
				}
				else
				{
					methodCode.AddLine(targetWidgetName + "." + propertyName + " = " + widgetCodeGenerationInfoChildSearchResult.ReachedWidget.VariableName + ".FindChild(new TaleWorlds.Library.BindingPath(@\"" + widgetCodeGenerationInfoChildSearchResult.RemainingPath.Path + "\")) as " + attributeType.FullName + ";");
				}
			}
			else
			{
				methodCode.AddLine(targetWidgetName + "." + propertyName + " = " + targetWidgetName + ".FindChild(new TaleWorlds.Library.BindingPath(@\"" + value + "\")) as " + attributeType.FullName + ";");
			}
		}
		else if (attributeType == typeof(VisualDefinition))
		{
			string text = "CreateVisualDefinition" + WidgetTemplateGenerateContext.GetUseableName(value);
			methodCode.AddLine(targetWidgetName + "." + propertyName + " = " + text + "();");
		}
	}

	internal void FillSetIdsMethod(MethodCode methodCode)
	{
		string text = (IsRoot ? "this" : VariableName);
		if (!WidgetFactory.IsBuiltinType(WidgetTemplate.Type) && !IsRoot)
		{
			methodCode.AddLine(text + ".SetIds();");
		}
		foreach (KeyValuePair<Type, Dictionary<string, WidgetAttributeTemplate>> attribute in WidgetTemplate.Attributes)
		{
			foreach (KeyValuePair<string, WidgetAttributeTemplate> item in attribute.Value)
			{
				WidgetAttributeKeyType keyType = item.Value.KeyType;
				WidgetAttributeValueType valueType = item.Value.ValueType;
				_ = item.Value.Key;
				string value = item.Value.Value;
				if (keyType is WidgetAttributeKeyTypeId && valueType is WidgetAttributeValueTypeDefault)
				{
					methodCode.AddLine(text + ".Id = \"" + value + "\";");
				}
			}
		}
	}

	internal void AddExtension(WidgetCodeGenerationInfoExtension extension)
	{
		Extension = extension;
	}
}
