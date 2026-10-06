using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.CodeGeneration;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class WidgetCodeGenerationInfoDatabindingExtension : WidgetCodeGenerationInfoExtension
{
	private Type _actualRootDataSourceType;

	private Type _dataSourceType;

	private BindingPath _bindingPath;

	private bool _usesParentsDatabinding;

	private bool _rootUsesSubPath;

	private WidgetTemplateGenerateContext _widgetTemplateGenerateContext;

	private UICodeGenerationContext _codeGenerationContext;

	private WidgetTemplate _widgetTemplate;

	public BindingPath FullBindingPath
	{
		get
		{
			if (_usesParentsDatabinding)
			{
				return Parent.FullBindingPath;
			}
			if (IsRoot)
			{
				return _bindingPath;
			}
			return Parent.FullBindingPath.Append(_bindingPath).Simplify();
		}
	}

	private Type RootDataSourceType
	{
		get
		{
			if (IsRoot)
			{
				if (_rootUsesSubPath)
				{
					return _actualRootDataSourceType;
				}
				return _dataSourceType;
			}
			return Parent.RootDataSourceType;
		}
	}

	public bool IsBindingList => typeof(IMBBindingList).IsAssignableFrom(_dataSourceType);

	public WidgetCodeGenerationInfo WidgetCodeGenerationInfo { get; private set; }

	public WidgetCodeGenerationInfoDatabindingExtension Parent
	{
		get
		{
			if (WidgetCodeGenerationInfo.Parent != null)
			{
				return (WidgetCodeGenerationInfoDatabindingExtension)WidgetCodeGenerationInfo.Parent.Extension;
			}
			return null;
		}
	}

	public WidgetTemplateGenerateContext FirstItemTemplateCodeGenerationInfo { get; private set; }

	public WidgetTemplateGenerateContext LastItemTemplateCodeGenerationInfo { get; private set; }

	public WidgetTemplateGenerateContext ItemTemplateCodeGenerationInfo { get; private set; }

	public bool IsRoot => Parent == null;

	public Dictionary<string, GeneratedBindDataInfo> BindDataInfos { get; private set; }

	public Dictionary<string, GeneratedBindCommandInfo> BindCommandInfos { get; private set; }

	public WidgetCodeGenerationInfoDatabindingExtension(WidgetCodeGenerationInfo widgetCodeGenerationInfo)
	{
		WidgetCodeGenerationInfo = widgetCodeGenerationInfo;
		_widgetTemplateGenerateContext = WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext;
		_widgetTemplate = WidgetCodeGenerationInfo.WidgetTemplate;
		_codeGenerationContext = _widgetTemplateGenerateContext.UICodeGenerationContext;
		BindDataInfos = new Dictionary<string, GeneratedBindDataInfo>();
		BindCommandInfos = new Dictionary<string, GeneratedBindCommandInfo>();
	}

	public override void Initialize()
	{
		if (IsRoot)
		{
			_dataSourceType = _widgetTemplateGenerateContext.Data["DataSourceType"] as Type;
			_actualRootDataSourceType = _dataSourceType;
			_bindingPath = new BindingPath("Root");
			_usesParentsDatabinding = false;
		}
		else
		{
			_dataSourceType = Parent._dataSourceType;
			_bindingPath = Parent._bindingPath;
			_usesParentsDatabinding = true;
		}
		ReadAttributes();
		InitializeCommandBindings();
		InitializeDataBindings();
	}

	public override bool TryGetVariantPropertiesForNewDependency(out UICodeGenerationVariantExtension variantExtension, out Dictionary<string, object> data)
	{
		variantExtension = new UICodeGenerationDatabindingVariantExtension();
		data = new Dictionary<string, object>();
		data.Add("DataSourceType", _dataSourceType);
		return true;
	}

	public bool CheckIfRequiresDataComponentForWidget()
	{
		foreach (KeyValuePair<Type, Dictionary<string, WidgetAttributeTemplate>> attribute in _widgetTemplate.Attributes)
		{
			foreach (KeyValuePair<string, WidgetAttributeTemplate> item in attribute.Value)
			{
				WidgetAttributeKeyType keyType = item.Value.KeyType;
				_ = item.Value.ValueType;
				string key = item.Value.Key;
				_ = item.Value.Value;
				if (keyType is WidgetAttributeKeyTypeAttribute && key == "AcceptDrag")
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void OnFillCreateWidgetMethod(MethodCode methodCode)
	{
		if (CheckIfRequiresDataComponentForWidget())
		{
			methodCode.AddLine("//Requires data component");
			methodCode.AddLine("{");
			methodCode.AddLine("var widgetComponent = new TaleWorlds.GauntletUI.Data.GeneratedWidgetData(" + WidgetCodeGenerationInfo.VariableName + ");");
			methodCode.AddLine(WidgetCodeGenerationInfo.VariableName + ".AddComponent(widgetComponent);");
			methodCode.AddLine("}");
		}
	}

	private void CheckDependency()
	{
		ItemTemplateUsage extensionData = _widgetTemplate.GetExtensionData<ItemTemplateUsage>();
		if (extensionData != null)
		{
			Type value = _dataSourceType.GetGenericArguments()[0];
			WidgetTemplate defaultItemTemplate = extensionData.DefaultItemTemplate;
			PrefabDependencyContext prefabDependencyContext = WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext.PrefabDependencyContext;
			VariableCollection variableCollection = WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext.VariableCollection;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary.Add("DataSourceType", value);
			ItemTemplateCodeGenerationInfo = WidgetTemplateGenerateContext.CreateAsCustomWidgetTemplate(_codeGenerationContext, prefabDependencyContext, defaultItemTemplate, "ItemTemplate", variableCollection, new UICodeGenerationDatabindingVariantExtension(), dictionary);
			if (extensionData.FirstItemTemplate != null)
			{
				Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
				dictionary2.Add("DataSourceType", value);
				FirstItemTemplateCodeGenerationInfo = WidgetTemplateGenerateContext.CreateAsCustomWidgetTemplate(_codeGenerationContext, prefabDependencyContext, extensionData.FirstItemTemplate, "FirstItemTemplate", variableCollection, new UICodeGenerationDatabindingVariantExtension(), dictionary2);
			}
			if (extensionData.LastItemTemplate != null)
			{
				Dictionary<string, object> dictionary3 = new Dictionary<string, object>();
				dictionary3.Add("DataSourceType", value);
				LastItemTemplateCodeGenerationInfo = WidgetTemplateGenerateContext.CreateAsCustomWidgetTemplate(_codeGenerationContext, prefabDependencyContext, extensionData.LastItemTemplate, "LastItemTemplate", variableCollection, new UICodeGenerationDatabindingVariantExtension(), dictionary3);
			}
		}
	}

	private void ReadAttributes()
	{
		VariableCollection variableCollection = _widgetTemplateGenerateContext.VariableCollection;
		foreach (KeyValuePair<Type, Dictionary<string, WidgetAttributeTemplate>> attribute in _widgetTemplate.Attributes)
		{
			foreach (KeyValuePair<string, WidgetAttributeTemplate> item in attribute.Value)
			{
				WidgetAttributeKeyType keyType = item.Value.KeyType;
				WidgetAttributeValueType valueType = item.Value.ValueType;
				string key = item.Value.Key;
				string value = item.Value.Value;
				if (keyType is WidgetAttributeKeyTypeDataSource)
				{
					if (valueType is WidgetAttributeValueTypeBindingPath)
					{
						AssignBindingPathFromValue(value);
					}
					else if (valueType is WidgetAttributeValueTypeParameter)
					{
						string parameterValue = variableCollection.GetParameterValue(value);
						if (!string.IsNullOrEmpty(parameterValue))
						{
							AssignBindingPathFromValue(parameterValue);
						}
					}
				}
				else if (keyType is WidgetAttributeKeyTypeAttribute || keyType is WidgetAttributeKeyTypeId)
				{
					if (valueType is WidgetAttributeValueTypeBinding)
					{
						GeneratedBindDataInfo generatedBindDataInfo = new GeneratedBindDataInfo(key, value);
						BindDataInfos.Add(generatedBindDataInfo.Property, generatedBindDataInfo);
					}
					else if (valueType is WidgetAttributeValueTypeParameter)
					{
						string key2 = value;
						if (variableCollection.GivenParameters.ContainsKey(key2) && variableCollection.GivenParameters[key2].ValueType is WidgetAttributeValueTypeBinding)
						{
							string value2 = variableCollection.GivenParameters[key2].Value;
							GeneratedBindDataInfo generatedBindDataInfo2 = new GeneratedBindDataInfo(key, value2);
							BindDataInfos.Add(generatedBindDataInfo2.Property, generatedBindDataInfo2);
						}
					}
				}
				else if (keyType is WidgetAttributeKeyTypeCommand)
				{
					string path = value;
					if (valueType is WidgetAttributeValueTypeParameter && variableCollection.GivenParameters.ContainsKey(value))
					{
						path = variableCollection.GivenParameters[value].Value;
					}
					GeneratedBindCommandInfo generatedBindCommandInfo = new GeneratedBindCommandInfo(key, path);
					BindCommandInfos.Add(generatedBindCommandInfo.Command, generatedBindCommandInfo);
				}
				else if (keyType is WidgetAttributeKeyTypeCommandParameter)
				{
					GeneratedBindCommandInfo generatedBindCommandInfo2 = BindCommandInfos[key];
					generatedBindCommandInfo2.GotParameter = true;
					if (valueType is WidgetAttributeValueTypeDefault)
					{
						generatedBindCommandInfo2.Parameter = value;
					}
					else if (valueType is WidgetAttributeValueTypeParameter)
					{
						string parameterValue2 = WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext.VariableCollection.GetParameterValue(value);
						generatedBindCommandInfo2.Parameter = parameterValue2;
					}
				}
			}
		}
		CheckDependency();
	}

	private void InitializeCommandBindings()
	{
		foreach (GeneratedBindCommandInfo value in BindCommandInfos.Values)
		{
			BindingPath bindingPath = new BindingPath(value.Path);
			BindingPath path = FullBindingPath;
			if (bindingPath.Nodes.Length > 1)
			{
				path = FullBindingPath.Append(bindingPath.ParentPath).Simplify();
			}
			MethodInfo method = GetViewModelTypeAtPath(RootDataSourceType, path).GetMethod(bindingPath.LastNode, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			int parameterCount = 0;
			if (method != null)
			{
				ParameterInfo[] parameters = method.GetParameters();
				parameterCount = parameters.Length;
				for (int i = 0; i < parameters.Length; i++)
				{
					GeneratedBindCommandParameterInfo generatedBindCommandParameterInfo = new GeneratedBindCommandParameterInfo();
					Type c = (generatedBindCommandParameterInfo.Type = parameters[i].ParameterType);
					if (typeof(ViewModel).IsAssignableFrom(c) || typeof(IMBBindingList).IsAssignableFrom(c))
					{
						generatedBindCommandParameterInfo.IsViewModel = true;
					}
					value.MethodParameters.Add(generatedBindCommandParameterInfo);
				}
			}
			value.Method = method;
			value.ParameterCount = parameterCount;
		}
	}

	private void InitializeDataBindings()
	{
		foreach (GeneratedBindDataInfo value in BindDataInfos.Values)
		{
			PropertyInfo property = GetViewModelTypeAtPath(RootDataSourceType, FullBindingPath).GetProperty(value.Path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				Type propertyType = property.PropertyType;
				value.ViewModelPropertType = propertyType;
			}
			string type = WidgetCodeGenerationInfo.WidgetTemplate.Type;
			PropertyInfo propertyInfo = VariableCollection.GetPropertyInfo(WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext.GetWidgetTypeWithinPrefabRoots(type), value.Property);
			if (propertyInfo != null)
			{
				Type propertyType2 = propertyInfo.PropertyType;
				value.WidgetPropertyType = propertyType2;
			}
			if (property != null && propertyInfo != null && !value.WidgetPropertyType.IsAssignableFrom(value.ViewModelPropertType))
			{
				value.RequiresConversion = true;
			}
		}
	}

	private void AssignBindingPathFromValue(string value)
	{
		BindingPath bindingPath = new BindingPath(value);
		BindingPath path = FullBindingPath.Append(bindingPath).Simplify();
		Type viewModelTypeAtPath = GetViewModelTypeAtPath(RootDataSourceType, path);
		_usesParentsDatabinding = false;
		_dataSourceType = viewModelTypeAtPath;
		if (IsRoot)
		{
			_bindingPath = new BindingPath("Root").Append(bindingPath);
			_rootUsesSubPath = true;
		}
		else
		{
			_bindingPath = bindingPath;
		}
	}

	public override void OnFillSetAttributesMethod(MethodCode methodCode)
	{
	}

	public static Type GetViewModelTypeAtPath(Type type, BindingPath path)
	{
		BindingPath subPath = path.SubPath;
		if (subPath != null)
		{
			PropertyInfo property = GetProperty(type, subPath.FirstNode);
			if (property != null)
			{
				Type returnType = property.GetGetMethod().ReturnType;
				if (typeof(ViewModel).IsAssignableFrom(returnType))
				{
					return GetViewModelTypeAtPath(returnType, subPath);
				}
				if (typeof(IMBBindingList).IsAssignableFrom(returnType))
				{
					return GetChildTypeAtPath(returnType, subPath);
				}
			}
			return null;
		}
		return type;
	}

	private static Type GetChildTypeAtPath(Type bindingListType, BindingPath path)
	{
		BindingPath subPath = path.SubPath;
		if (subPath == null)
		{
			return bindingListType;
		}
		Type type = bindingListType.GetGenericArguments()[0];
		if (typeof(ViewModel).IsAssignableFrom(type))
		{
			return GetViewModelTypeAtPath(type, subPath);
		}
		if (typeof(IMBBindingList).IsAssignableFrom(type))
		{
			return GetChildTypeAtPath(type, subPath);
		}
		return null;
	}

	private static PropertyInfo GetProperty(Type type, string name)
	{
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.Name == name)
			{
				return propertyInfo;
			}
		}
		return null;
	}
}
