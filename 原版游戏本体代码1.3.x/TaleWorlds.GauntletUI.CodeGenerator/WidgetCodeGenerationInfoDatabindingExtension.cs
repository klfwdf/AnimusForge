using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.CodeGeneration;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000008 RID: 8
	public class WidgetCodeGenerationInfoDatabindingExtension : WidgetCodeGenerationInfoExtension
	{
		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000049 RID: 73 RVA: 0x00002AA4 File Offset: 0x00000CA4
		public BindingPath FullBindingPath
		{
			get
			{
				if (this._usesParentsDatabinding)
				{
					return this.Parent.FullBindingPath;
				}
				if (this.IsRoot)
				{
					return this._bindingPath;
				}
				return this.Parent.FullBindingPath.Append(this._bindingPath).Simplify();
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x0600004A RID: 74 RVA: 0x00002AE4 File Offset: 0x00000CE4
		private Type RootDataSourceType
		{
			get
			{
				if (!this.IsRoot)
				{
					return this.Parent.RootDataSourceType;
				}
				if (this._rootUsesSubPath)
				{
					return this._actualRootDataSourceType;
				}
				return this._dataSourceType;
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600004B RID: 75 RVA: 0x00002B0F File Offset: 0x00000D0F
		public bool IsBindingList
		{
			get
			{
				return typeof(IMBBindingList).IsAssignableFrom(this._dataSourceType);
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600004C RID: 76 RVA: 0x00002B26 File Offset: 0x00000D26
		// (set) Token: 0x0600004D RID: 77 RVA: 0x00002B2E File Offset: 0x00000D2E
		public WidgetCodeGenerationInfo WidgetCodeGenerationInfo { get; private set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600004E RID: 78 RVA: 0x00002B37 File Offset: 0x00000D37
		public WidgetCodeGenerationInfoDatabindingExtension Parent
		{
			get
			{
				if (this.WidgetCodeGenerationInfo.Parent != null)
				{
					return (WidgetCodeGenerationInfoDatabindingExtension)this.WidgetCodeGenerationInfo.Parent.Extension;
				}
				return null;
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600004F RID: 79 RVA: 0x00002B5D File Offset: 0x00000D5D
		// (set) Token: 0x06000050 RID: 80 RVA: 0x00002B65 File Offset: 0x00000D65
		public WidgetTemplateGenerateContext FirstItemTemplateCodeGenerationInfo { get; private set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000051 RID: 81 RVA: 0x00002B6E File Offset: 0x00000D6E
		// (set) Token: 0x06000052 RID: 82 RVA: 0x00002B76 File Offset: 0x00000D76
		public WidgetTemplateGenerateContext LastItemTemplateCodeGenerationInfo { get; private set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000053 RID: 83 RVA: 0x00002B7F File Offset: 0x00000D7F
		// (set) Token: 0x06000054 RID: 84 RVA: 0x00002B87 File Offset: 0x00000D87
		public WidgetTemplateGenerateContext ItemTemplateCodeGenerationInfo { get; private set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000055 RID: 85 RVA: 0x00002B90 File Offset: 0x00000D90
		public bool IsRoot
		{
			get
			{
				return this.Parent == null;
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000056 RID: 86 RVA: 0x00002B9B File Offset: 0x00000D9B
		// (set) Token: 0x06000057 RID: 87 RVA: 0x00002BA3 File Offset: 0x00000DA3
		public Dictionary<string, GeneratedBindDataInfo> BindDataInfos { get; private set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000058 RID: 88 RVA: 0x00002BAC File Offset: 0x00000DAC
		// (set) Token: 0x06000059 RID: 89 RVA: 0x00002BB4 File Offset: 0x00000DB4
		public Dictionary<string, GeneratedBindCommandInfo> BindCommandInfos { get; private set; }

		// Token: 0x0600005A RID: 90 RVA: 0x00002BC0 File Offset: 0x00000DC0
		public WidgetCodeGenerationInfoDatabindingExtension(WidgetCodeGenerationInfo widgetCodeGenerationInfo)
		{
			this.WidgetCodeGenerationInfo = widgetCodeGenerationInfo;
			this._widgetTemplateGenerateContext = this.WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext;
			this._widgetTemplate = this.WidgetCodeGenerationInfo.WidgetTemplate;
			this._codeGenerationContext = this._widgetTemplateGenerateContext.UICodeGenerationContext;
			this.BindDataInfos = new Dictionary<string, GeneratedBindDataInfo>();
			this.BindCommandInfos = new Dictionary<string, GeneratedBindCommandInfo>();
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00002C24 File Offset: 0x00000E24
		public override void Initialize()
		{
			if (this.IsRoot)
			{
				this._dataSourceType = this._widgetTemplateGenerateContext.Data["DataSourceType"] as Type;
				this._actualRootDataSourceType = this._dataSourceType;
				this._bindingPath = new BindingPath("Root");
				this._usesParentsDatabinding = false;
			}
			else
			{
				this._dataSourceType = this.Parent._dataSourceType;
				this._bindingPath = this.Parent._bindingPath;
				this._usesParentsDatabinding = true;
			}
			this.ReadAttributes();
			this.InitializeCommandBindings();
			this.InitializeDataBindings();
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002CB9 File Offset: 0x00000EB9
		public override bool TryGetVariantPropertiesForNewDependency(out UICodeGenerationVariantExtension variantExtension, out Dictionary<string, object> data)
		{
			variantExtension = new UICodeGenerationDatabindingVariantExtension();
			data = new Dictionary<string, object>();
			data.Add("DataSourceType", this._dataSourceType);
			return true;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00002CDC File Offset: 0x00000EDC
		public bool CheckIfRequiresDataComponentForWidget()
		{
			foreach (KeyValuePair<Type, Dictionary<string, WidgetAttributeTemplate>> keyValuePair in this._widgetTemplate.Attributes)
			{
				foreach (KeyValuePair<string, WidgetAttributeTemplate> keyValuePair2 in keyValuePair.Value)
				{
					WidgetAttributeKeyType keyType = keyValuePair2.Value.KeyType;
					WidgetAttributeValueType valueType = keyValuePair2.Value.ValueType;
					string key = keyValuePair2.Value.Key;
					string value = keyValuePair2.Value.Value;
					if (keyType is WidgetAttributeKeyTypeAttribute && key == "AcceptDrag")
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00002DC4 File Offset: 0x00000FC4
		public override void OnFillCreateWidgetMethod(MethodCode methodCode)
		{
			if (this.CheckIfRequiresDataComponentForWidget())
			{
				methodCode.AddLine("//Requires data component");
				methodCode.AddLine("{");
				methodCode.AddLine("var widgetComponent = new TaleWorlds.GauntletUI.Data.GeneratedWidgetData(" + this.WidgetCodeGenerationInfo.VariableName + ");");
				methodCode.AddLine(this.WidgetCodeGenerationInfo.VariableName + ".AddComponent(widgetComponent);");
				methodCode.AddLine("}");
			}
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00002E38 File Offset: 0x00001038
		private void CheckDependency()
		{
			ItemTemplateUsage extensionData = this._widgetTemplate.GetExtensionData<ItemTemplateUsage>();
			if (extensionData != null)
			{
				Type value = this._dataSourceType.GetGenericArguments()[0];
				WidgetTemplate defaultItemTemplate = extensionData.DefaultItemTemplate;
				PrefabDependencyContext prefabDependencyContext = this.WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext.PrefabDependencyContext;
				VariableCollection variableCollection = this.WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext.VariableCollection;
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("DataSourceType", value);
				this.ItemTemplateCodeGenerationInfo = WidgetTemplateGenerateContext.CreateAsCustomWidgetTemplate(this._codeGenerationContext, prefabDependencyContext, defaultItemTemplate, "ItemTemplate", variableCollection, new UICodeGenerationDatabindingVariantExtension(), dictionary);
				if (extensionData.FirstItemTemplate != null)
				{
					Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
					dictionary2.Add("DataSourceType", value);
					this.FirstItemTemplateCodeGenerationInfo = WidgetTemplateGenerateContext.CreateAsCustomWidgetTemplate(this._codeGenerationContext, prefabDependencyContext, extensionData.FirstItemTemplate, "FirstItemTemplate", variableCollection, new UICodeGenerationDatabindingVariantExtension(), dictionary2);
				}
				if (extensionData.LastItemTemplate != null)
				{
					Dictionary<string, object> dictionary3 = new Dictionary<string, object>();
					dictionary3.Add("DataSourceType", value);
					this.LastItemTemplateCodeGenerationInfo = WidgetTemplateGenerateContext.CreateAsCustomWidgetTemplate(this._codeGenerationContext, prefabDependencyContext, extensionData.LastItemTemplate, "LastItemTemplate", variableCollection, new UICodeGenerationDatabindingVariantExtension(), dictionary3);
				}
			}
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00002F48 File Offset: 0x00001148
		private void ReadAttributes()
		{
			VariableCollection variableCollection = this._widgetTemplateGenerateContext.VariableCollection;
			foreach (KeyValuePair<Type, Dictionary<string, WidgetAttributeTemplate>> keyValuePair in this._widgetTemplate.Attributes)
			{
				foreach (KeyValuePair<string, WidgetAttributeTemplate> keyValuePair2 in keyValuePair.Value)
				{
					WidgetAttributeKeyType keyType = keyValuePair2.Value.KeyType;
					WidgetAttributeValueType valueType = keyValuePair2.Value.ValueType;
					string key = keyValuePair2.Value.Key;
					string value = keyValuePair2.Value.Value;
					if (keyType is WidgetAttributeKeyTypeDataSource)
					{
						if (valueType is WidgetAttributeValueTypeBindingPath)
						{
							this.AssignBindingPathFromValue(value);
						}
						else if (valueType is WidgetAttributeValueTypeParameter)
						{
							string parameterValue = variableCollection.GetParameterValue(value);
							if (!string.IsNullOrEmpty(parameterValue))
							{
								this.AssignBindingPathFromValue(parameterValue);
							}
						}
					}
					else if (keyType is WidgetAttributeKeyTypeAttribute || keyType is WidgetAttributeKeyTypeId)
					{
						if (valueType is WidgetAttributeValueTypeBinding)
						{
							GeneratedBindDataInfo generatedBindDataInfo = new GeneratedBindDataInfo(key, value);
							this.BindDataInfos.Add(generatedBindDataInfo.Property, generatedBindDataInfo);
						}
						else if (valueType is WidgetAttributeValueTypeParameter)
						{
							string key2 = value;
							if (variableCollection.GivenParameters.ContainsKey(key2) && variableCollection.GivenParameters[key2].ValueType is WidgetAttributeValueTypeBinding)
							{
								string value2 = variableCollection.GivenParameters[key2].Value;
								GeneratedBindDataInfo generatedBindDataInfo2 = new GeneratedBindDataInfo(key, value2);
								this.BindDataInfos.Add(generatedBindDataInfo2.Property, generatedBindDataInfo2);
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
						this.BindCommandInfos.Add(generatedBindCommandInfo.Command, generatedBindCommandInfo);
					}
					else if (keyType is WidgetAttributeKeyTypeCommandParameter)
					{
						GeneratedBindCommandInfo generatedBindCommandInfo2 = this.BindCommandInfos[key];
						generatedBindCommandInfo2.GotParameter = true;
						if (valueType is WidgetAttributeValueTypeDefault)
						{
							generatedBindCommandInfo2.Parameter = value;
						}
						else if (valueType is WidgetAttributeValueTypeParameter)
						{
							string parameterValue2 = this.WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext.VariableCollection.GetParameterValue(value);
							generatedBindCommandInfo2.Parameter = parameterValue2;
						}
					}
				}
			}
			this.CheckDependency();
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00003204 File Offset: 0x00001404
		private void InitializeCommandBindings()
		{
			foreach (GeneratedBindCommandInfo generatedBindCommandInfo in this.BindCommandInfos.Values)
			{
				BindingPath bindingPath = new BindingPath(generatedBindCommandInfo.Path);
				BindingPath path = this.FullBindingPath;
				if (bindingPath.Nodes.Length > 1)
				{
					path = this.FullBindingPath.Append(bindingPath.ParentPath).Simplify();
				}
				MethodInfo method = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(this.RootDataSourceType, path).GetMethod(bindingPath.LastNode, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				int parameterCount = 0;
				if (method != null)
				{
					ParameterInfo[] parameters = method.GetParameters();
					parameterCount = parameters.Length;
					for (int i = 0; i < parameters.Length; i++)
					{
						GeneratedBindCommandParameterInfo generatedBindCommandParameterInfo = new GeneratedBindCommandParameterInfo();
						Type parameterType = parameters[i].ParameterType;
						generatedBindCommandParameterInfo.Type = parameterType;
						if (typeof(ViewModel).IsAssignableFrom(parameterType) || typeof(IMBBindingList).IsAssignableFrom(parameterType))
						{
							generatedBindCommandParameterInfo.IsViewModel = true;
						}
						generatedBindCommandInfo.MethodParameters.Add(generatedBindCommandParameterInfo);
					}
				}
				generatedBindCommandInfo.Method = method;
				generatedBindCommandInfo.ParameterCount = parameterCount;
			}
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00003350 File Offset: 0x00001550
		private void InitializeDataBindings()
		{
			foreach (GeneratedBindDataInfo generatedBindDataInfo in this.BindDataInfos.Values)
			{
				PropertyInfo property = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(this.RootDataSourceType, this.FullBindingPath).GetProperty(generatedBindDataInfo.Path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property != null)
				{
					Type propertyType = property.PropertyType;
					generatedBindDataInfo.ViewModelPropertType = propertyType;
				}
				string type = this.WidgetCodeGenerationInfo.WidgetTemplate.Type;
				PropertyInfo propertyInfo = VariableCollection.GetPropertyInfo(this.WidgetCodeGenerationInfo.RootWidgetTemplateGenerateContext.GetWidgetTypeWithinPrefabRoots(type), generatedBindDataInfo.Property);
				if (propertyInfo != null)
				{
					Type propertyType2 = propertyInfo.PropertyType;
					generatedBindDataInfo.WidgetPropertyType = propertyType2;
				}
				if (property != null && propertyInfo != null && !generatedBindDataInfo.WidgetPropertyType.IsAssignableFrom(generatedBindDataInfo.ViewModelPropertType))
				{
					generatedBindDataInfo.RequiresConversion = true;
				}
			}
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00003458 File Offset: 0x00001658
		private void AssignBindingPathFromValue(string value)
		{
			BindingPath bindingPath = new BindingPath(value);
			BindingPath path = this.FullBindingPath.Append(bindingPath).Simplify();
			Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(this.RootDataSourceType, path);
			this._usesParentsDatabinding = false;
			this._dataSourceType = viewModelTypeAtPath;
			if (this.IsRoot)
			{
				this._bindingPath = new BindingPath("Root").Append(bindingPath);
				this._rootUsesSubPath = true;
				return;
			}
			this._bindingPath = bindingPath;
		}

		// Token: 0x06000064 RID: 100 RVA: 0x000034C6 File Offset: 0x000016C6
		public override void OnFillSetAttributesMethod(MethodCode methodCode)
		{
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000034C8 File Offset: 0x000016C8
		public static Type GetViewModelTypeAtPath(Type type, BindingPath path)
		{
			BindingPath subPath = path.SubPath;
			if (subPath != null)
			{
				PropertyInfo property = WidgetCodeGenerationInfoDatabindingExtension.GetProperty(type, subPath.FirstNode);
				if (property != null)
				{
					Type returnType = property.GetGetMethod().ReturnType;
					if (typeof(ViewModel).IsAssignableFrom(returnType))
					{
						return WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(returnType, subPath);
					}
					if (typeof(IMBBindingList).IsAssignableFrom(returnType))
					{
						return WidgetCodeGenerationInfoDatabindingExtension.GetChildTypeAtPath(returnType, subPath);
					}
				}
				return null;
			}
			return type;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00003540 File Offset: 0x00001740
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
				return WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(type, subPath);
			}
			if (typeof(IMBBindingList).IsAssignableFrom(type))
			{
				return WidgetCodeGenerationInfoDatabindingExtension.GetChildTypeAtPath(type, subPath);
			}
			return null;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x000035A0 File Offset: 0x000017A0
		private static PropertyInfo GetProperty(Type type, string name)
		{
			foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (propertyInfo.Name == name)
				{
					return propertyInfo;
				}
			}
			return null;
		}

		// Token: 0x0400001B RID: 27
		private Type _actualRootDataSourceType;

		// Token: 0x0400001C RID: 28
		private Type _dataSourceType;

		// Token: 0x0400001D RID: 29
		private BindingPath _bindingPath;

		// Token: 0x0400001E RID: 30
		private bool _usesParentsDatabinding;

		// Token: 0x0400001F RID: 31
		private bool _rootUsesSubPath;

		// Token: 0x04000021 RID: 33
		private WidgetTemplateGenerateContext _widgetTemplateGenerateContext;

		// Token: 0x04000022 RID: 34
		private UICodeGenerationContext _codeGenerationContext;

		// Token: 0x04000023 RID: 35
		private WidgetTemplate _widgetTemplate;
	}
}
