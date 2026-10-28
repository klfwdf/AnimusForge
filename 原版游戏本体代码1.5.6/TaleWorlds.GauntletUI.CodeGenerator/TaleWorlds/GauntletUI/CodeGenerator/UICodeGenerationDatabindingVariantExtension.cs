using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.CodeGeneration;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class UICodeGenerationDatabindingVariantExtension : UICodeGenerationVariantExtension
{
	private WidgetTemplateGenerateContext _widgetTemplateGenerateContext;

	private Type _dataSourceType;

	private List<WidgetCodeGenerationInfoDatabindingExtension> _extensions;

	private Dictionary<BindingPath, List<WidgetCodeGenerationInfoDatabindingExtension>> _extensionsWithPath;

	private List<BindingPathTargetDetails> _bindingPathTargetDetailsList;

	public override PrefabExtension GetPrefabExtension()
	{
		return new PrefabDatabindingExtension();
	}

	public override void AddExtensionVariables(ClassCode classCode)
	{
	}

	public override void Initialize(WidgetTemplateGenerateContext widgetTemplateGenerateContext)
	{
		_widgetTemplateGenerateContext = widgetTemplateGenerateContext;
		_dataSourceType = (Type)_widgetTemplateGenerateContext.Data["DataSourceType"];
	}

	public override Type GetAttributeType(WidgetAttributeTemplate widgetAttributeTemplate)
	{
		WidgetAttributeKeyType keyType = widgetAttributeTemplate.KeyType;
		WidgetAttributeValueType valueType = widgetAttributeTemplate.ValueType;
		if (keyType is WidgetAttributeKeyTypeDataSource)
		{
			return typeof(BindingPath);
		}
		if (valueType is WidgetAttributeValueTypeBinding)
		{
			return typeof(BindingPath);
		}
		if (valueType is WidgetAttributeValueTypeBindingPath)
		{
			return typeof(BindingPath);
		}
		return null;
	}

	private static string GetDatasourceVariableNameOfPath(BindingPath bindingPath)
	{
		string text = bindingPath.Path.Replace("\\", "_");
		return "_datasource_" + text;
	}

	private string GetViewModelCodeWriteableTypeAtPath(BindingPath bindingPath)
	{
		Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(_dataSourceType, bindingPath);
		string text = "";
		if (typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath))
		{
			Type type = viewModelTypeAtPath.GetGenericArguments()[0];
			return "TaleWorlds.Library.MBBindingList<" + type.FullName + ">";
		}
		if (viewModelTypeAtPath.IsGenericType)
		{
			return GetGenericTypeCodeFileName(viewModelTypeAtPath);
		}
		return viewModelTypeAtPath.FullName;
	}

	private void FillDataSourceVariables(BindingPathTargetDetails bindingPathTargetDetails, ClassCode classCode)
	{
		BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
		VariableCode variableCode = new VariableCode();
		variableCode.Name = GetDatasourceVariableNameOfPath(bindingPath);
		variableCode.AccessModifier = VariableCodeAccessModifier.Private;
		variableCode.Type = GetViewModelCodeWriteableTypeAtPath(bindingPath);
		classCode.AddVariable(variableCode);
		foreach (BindingPathTargetDetails child in bindingPathTargetDetails.Children)
		{
			FillDataSourceVariables(child, classCode);
		}
	}

	private void CreateSetDataSourceVariables(ClassCode classCode)
	{
		BindingPathTargetDetails rootBindingPathTargetDetails = GetRootBindingPathTargetDetails();
		FillDataSourceVariables(rootBindingPathTargetDetails, classCode);
	}

	private void CreateSetDataSourceMethod(ClassCode classCode)
	{
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "SetDataSource";
		string viewModelCodeWriteableTypeAtPath = GetViewModelCodeWriteableTypeAtPath(new BindingPath("Root"));
		string datasourceVariableNameOfPath = GetDatasourceVariableNameOfPath(new BindingPath("Root"));
		methodCode.MethodSignature = "(" + viewModelCodeWriteableTypeAtPath + " dataSource)";
		if (_widgetTemplateGenerateContext.CheckIfInheritsAnotherPrefab())
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
			methodCode.AddLine("base.SetDataSource(dataSource);");
		}
		else if (_widgetTemplateGenerateContext.ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
		}
		methodCode.AddLine("RefreshDataSource" + datasourceVariableNameOfPath + "(dataSource);");
		classCode.AddMethod(methodCode);
	}

	private static string GetGenericTypeCodeFileName(Type type)
	{
		string text = type.FullName.Split('`')[0] + "<";
		for (int i = 0; i < type.GenericTypeArguments.Length; i++)
		{
			Type type2 = type.GenericTypeArguments[i];
			text += type2.FullName;
			if (i + 1 < type.GenericTypeArguments.Length)
			{
				text += ", ";
			}
		}
		return text + ">";
	}

	private void CreateDestroyDataSourceyMethod(ClassCode classCode)
	{
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "DestroyDataSource";
		if (_widgetTemplateGenerateContext.CheckIfInheritsAnotherPrefab())
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
			methodCode.AddLine("base.DestroyDataSource();");
		}
		else if (_widgetTemplateGenerateContext.ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
		}
		BindingPathTargetDetails rootBindingPathTargetDetails = GetRootBindingPathTargetDetails();
		FillRefreshDataSourceMethodClearSection(rootBindingPathTargetDetails, methodCode, forDestroy: true);
		classCode.AddMethod(methodCode);
	}

	private void CreateRefreshBindingWithChildrenMethod(ClassCode classCode)
	{
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "RefreshBindingWithChildren";
		methodCode.AddLine("var dataSource = _datasource_Root;");
		methodCode.AddLine("this.SetDataSource(null);");
		methodCode.AddLine("this.SetDataSource(dataSource);");
		classCode.AddMethod(methodCode);
	}

	private void FillRefreshDataSourceMethodClearSection(BindingPathTargetDetails bindingPathTargetDetails, MethodCode methodCode, bool forDestroy)
	{
		BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
		string path = bindingPath.Path;
		Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(_dataSourceType, bindingPath);
		bool flag = typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath);
		string datasourceVariableNameOfPath = GetDatasourceVariableNameOfPath(bindingPath);
		methodCode.AddLine("if (" + datasourceVariableNameOfPath + " != null)");
		methodCode.AddLine("{");
		foreach (WidgetCodeGenerationInfoDatabindingExtension widgetDatabindingInformation in bindingPathTargetDetails.WidgetDatabindingInformations)
		{
			bool num = widgetDatabindingInformation.WidgetCodeGenerationInfo.WidgetFactory.IsBuiltinType(widgetDatabindingInformation.WidgetCodeGenerationInfo.WidgetTemplate.Type);
			bool isRoot = widgetDatabindingInformation.IsRoot;
			if (!num && !isRoot)
			{
				if (forDestroy)
				{
					methodCode.AddLine(widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName + ".DestroyDataSource();");
				}
				else
				{
					methodCode.AddLine(widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName + ".SetDataSource(null);");
				}
			}
			if (widgetDatabindingInformation.CheckIfRequiresDataComponentForWidget())
			{
				methodCode.AddLine("{");
				methodCode.AddLine("//Requires component data to be cleared");
				methodCode.AddLine("var widgetComponent = " + widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName + ".GetComponent<TaleWorlds.GauntletUI.Data.GeneratedWidgetData>();");
				methodCode.AddLine("widgetComponent.Data = null;");
				methodCode.AddLine("}");
			}
		}
		if (flag)
		{
			methodCode.AddLine(datasourceVariableNameOfPath + ".ListChanged -= OnList" + datasourceVariableNameOfPath + "Changed;");
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetDatabindingInformation2 in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				if (widgetDatabindingInformation2.ItemTemplateCodeGenerationInfo != null)
				{
					methodCode.AddLine("//Binding path list: " + path);
					string variableName = widgetDatabindingInformation2.WidgetCodeGenerationInfo.VariableName;
					methodCode.AddLine("for (var i = " + variableName + ".ChildCount - 1; i >= 0; i--)");
					methodCode.AddLine("{");
					AddBindingListItemBeforeDeletionSection(methodCode, widgetDatabindingInformation2, "i", forDestroy);
					AddBindingListItemDeletionSection(methodCode, widgetDatabindingInformation2, "i", forDestroy);
					methodCode.AddLine("}");
				}
				if (widgetDatabindingInformation2.BindCommandInfos.Values.Count > 0)
				{
					string text = "EventListenerOf" + widgetDatabindingInformation2.WidgetCodeGenerationInfo.VariableName;
					methodCode.AddLine(widgetDatabindingInformation2.WidgetCodeGenerationInfo.VariableName + ".EventFire -= " + text + ";");
				}
			}
		}
		else
		{
			methodCode.AddLine("//Binding path: " + path);
			methodCode.AddLine(datasourceVariableNameOfPath + ".PropertyChanged -= ViewModelPropertyChangedListenerOf" + datasourceVariableNameOfPath + ";");
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, string.Empty, add: false);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Bool", add: false);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Int", add: false);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Float", add: false);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "UInt", add: false);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Color", add: false);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Double", add: false);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Vec2", add: false);
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetDatabindingInformation3 in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				if (widgetDatabindingInformation3.BindCommandInfos.Values.Count > 0)
				{
					string text2 = "EventListenerOf" + widgetDatabindingInformation3.WidgetCodeGenerationInfo.VariableName;
					methodCode.AddLine(widgetDatabindingInformation3.WidgetCodeGenerationInfo.VariableName + ".EventFire -= " + text2 + ";");
				}
				if (widgetDatabindingInformation3.BindDataInfos.Values.Count > 0)
				{
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation3, string.Empty, add: false);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation3, "bool", add: false);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation3, "float", add: false);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation3, "Vec2", add: false);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation3, "Vector2", add: false);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation3, "double", add: false);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation3, "int", add: false);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation3, "uint", add: false);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation3, "Color", add: false);
				}
			}
		}
		foreach (BindingPathTargetDetails child in bindingPathTargetDetails.Children)
		{
			FillRefreshDataSourceMethodClearSection(child, methodCode, forDestroy);
		}
		methodCode.AddLine(datasourceVariableNameOfPath + " = null;");
		methodCode.AddLine("}");
	}

	private static void AddDataSourcePropertyChangedMethod(MethodCode methodCode, string dataSourceVariableName, string typeModifier, bool add)
	{
		methodCode.AddLine(dataSourceVariableName + ".PropertyChangedWith" + typeModifier + "Value " + (add ? "+" : "-") + "=ViewModelPropertyChangedWith" + typeModifier + "ValueListenerOf" + dataSourceVariableName + ";");
	}

	private void AddBindingListItemCreationItemMethodsSection(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string variableName, string dataSourceVariableName, string childIndexVariableName)
	{
		methodCode.AddLine("var widgetComponent = new TaleWorlds.GauntletUI.Data.GeneratedWidgetData(" + variableName + ");");
		methodCode.AddLine("var dataSource = " + dataSourceVariableName + "[" + childIndexVariableName + "];");
		methodCode.AddLine("widgetComponent.Data = dataSource;");
		methodCode.AddLine(variableName + ".AddComponent(widgetComponent);");
		methodCode.AddLine(extension.WidgetCodeGenerationInfo.VariableName + ".AddChildAtIndex(" + variableName + ", " + childIndexVariableName + ");");
		methodCode.AddLine(variableName + ".CreateWidgets();");
		methodCode.AddLine(variableName + ".SetIds();");
		methodCode.AddLine(variableName + ".SetAttributes();");
		methodCode.AddLine(variableName + ".SetDataSource(dataSource);");
	}

	private void AddBindingListItemCreationSection(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string dataSourceVariableName, string childIndexVariableName)
	{
		if (extension.FirstItemTemplateCodeGenerationInfo != null)
		{
			methodCode.AddLine("//Got first item template");
			methodCode.AddLine("if (" + extension.WidgetCodeGenerationInfo.VariableName + ".ChildCount == 0)");
			methodCode.AddLine("{");
			methodCode.AddLine("var item = new " + extension.FirstItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
			AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
			methodCode.AddLine("}");
		}
		if (extension.LastItemTemplateCodeGenerationInfo != null)
		{
			methodCode.AddLine("//Got last item template");
			string text = ((extension.FirstItemTemplateCodeGenerationInfo != null) ? "else " : "");
			methodCode.AddLine(text + "if (" + extension.WidgetCodeGenerationInfo.VariableName + ".ChildCount == " + childIndexVariableName + " && " + extension.WidgetCodeGenerationInfo.VariableName + ".ChildCount > 0)");
			methodCode.AddLine("{");
			methodCode.AddLine("//Change current last item into default item template");
			methodCode.AddLine("{");
			methodCode.AddLine("var currentLastItem = " + extension.WidgetCodeGenerationInfo.VariableName + ".GetChild(" + extension.WidgetCodeGenerationInfo.VariableName + ".ChildCount - 1) as " + extension.LastItemTemplateCodeGenerationInfo.ClassName + ";");
			methodCode.AddLine("if (currentLastItem != null)");
			methodCode.AddLine("{");
			methodCode.AddLine(extension.WidgetCodeGenerationInfo.VariableName + ".OnBeforeRemovedChild(currentLastItem);");
			methodCode.AddLine("currentLastItem.SetDataSource(null);");
			methodCode.AddLine("var newPreviousItem = new " + extension.ItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
			AddBindingListItemCreationItemMethodsSection(methodCode, extension, "newPreviousItem", dataSourceVariableName, extension.WidgetCodeGenerationInfo.VariableName + ".ChildCount - 1");
			methodCode.AddLine(extension.WidgetCodeGenerationInfo.VariableName + ".RemoveChild(currentLastItem);");
			methodCode.AddLine("}");
			methodCode.AddLine("}");
			methodCode.AddLine("//Add new last item");
			methodCode.AddLine("{");
			methodCode.AddLine("var item = new " + extension.LastItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
			AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
			methodCode.AddLine("}");
			methodCode.AddLine("}");
		}
		if (extension.FirstItemTemplateCodeGenerationInfo != null || extension.LastItemTemplateCodeGenerationInfo != null)
		{
			methodCode.AddLine("else");
		}
		methodCode.AddLine("{");
		methodCode.AddLine("var item = new " + extension.ItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
		AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
		methodCode.AddLine("}");
	}

	private void AddBindingListItemCreationSectionForPopulate(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string dataSourceVariableName, string childIndexVariableName)
	{
		if (extension.FirstItemTemplateCodeGenerationInfo != null)
		{
			methodCode.AddLine("//Got first item template");
			methodCode.AddLine("if (" + childIndexVariableName + " == 0)");
			methodCode.AddLine("{");
			methodCode.AddLine("var item = new " + extension.FirstItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
			AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
			methodCode.AddLine("}");
		}
		if (extension.LastItemTemplateCodeGenerationInfo != null)
		{
			methodCode.AddLine("//Got last item template");
			string text = ((extension.FirstItemTemplateCodeGenerationInfo != null) ? "else " : "");
			methodCode.AddLine(text + "if (" + childIndexVariableName + " == " + dataSourceVariableName + ".Count - 1)");
			methodCode.AddLine("{");
			methodCode.AddLine("var item = new " + extension.LastItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
			AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
			methodCode.AddLine("}");
		}
		if (extension.FirstItemTemplateCodeGenerationInfo != null || extension.LastItemTemplateCodeGenerationInfo != null)
		{
			methodCode.AddLine("else");
		}
		methodCode.AddLine("{");
		methodCode.AddLine("var item = new " + extension.ItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
		AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
		methodCode.AddLine("}");
	}

	private void AddBindingListItemDeletionSection(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string childIndexVariableName, bool forDestroy)
	{
		string variableName = extension.WidgetCodeGenerationInfo.VariableName;
		methodCode.AddLine("{");
		methodCode.AddLine("var widget = " + variableName + ".GetChild(" + childIndexVariableName + ");");
		if (extension.FirstItemTemplateCodeGenerationInfo != null || extension.LastItemTemplateCodeGenerationInfo != null)
		{
			if (extension.FirstItemTemplateCodeGenerationInfo != null)
			{
				methodCode.AddLine("if (widget is " + extension.FirstItemTemplateCodeGenerationInfo.ClassName + ")");
				methodCode.AddLine("{");
				methodCode.AddLine("var targetWidget = (" + extension.FirstItemTemplateCodeGenerationInfo.ClassName + ")widget;");
				if (forDestroy)
				{
					methodCode.AddLine("targetWidget.DestroyDataSource();");
				}
				else
				{
					methodCode.AddLine("targetWidget.SetDataSource(null);");
				}
				methodCode.AddLine("}");
			}
			if (extension.LastItemTemplateCodeGenerationInfo != null)
			{
				string text = ((extension.FirstItemTemplateCodeGenerationInfo != null) ? "else " : "");
				methodCode.AddLine(text + "if (widget is " + extension.LastItemTemplateCodeGenerationInfo.ClassName + ")");
				methodCode.AddLine("{");
				methodCode.AddLine("var targetWidget = (" + extension.LastItemTemplateCodeGenerationInfo.ClassName + ")widget;");
				if (forDestroy)
				{
					methodCode.AddLine("targetWidget.DestroyDataSource();");
				}
				else
				{
					methodCode.AddLine("targetWidget.SetDataSource(null);");
				}
				methodCode.AddLine("}");
			}
			methodCode.AddLine("else");
			methodCode.AddLine("{");
			methodCode.AddLine("var targetWidget = (" + extension.ItemTemplateCodeGenerationInfo.ClassName + ")widget;");
			if (forDestroy)
			{
				methodCode.AddLine("targetWidget.DestroyDataSource();");
			}
			else
			{
				methodCode.AddLine("targetWidget.SetDataSource(null);");
			}
			methodCode.AddLine("}");
		}
		else
		{
			methodCode.AddLine("var targetWidget = (" + extension.ItemTemplateCodeGenerationInfo.ClassName + ")widget;");
			if (forDestroy)
			{
				methodCode.AddLine("targetWidget.DestroyDataSource();");
			}
			else
			{
				methodCode.AddLine("targetWidget.SetDataSource(null);");
			}
		}
		if (!forDestroy)
		{
			methodCode.AddLine(variableName + ".RemoveChild(widget);");
		}
		methodCode.AddLine("}");
	}

	private void AddBindingListItemBeforeDeletionSection(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string childIndexVariableName, bool forDestroy)
	{
		string variableName = extension.WidgetCodeGenerationInfo.VariableName;
		methodCode.AddLine("{");
		methodCode.AddLine("var widget = " + variableName + ".GetChild(" + childIndexVariableName + ");");
		if (extension.FirstItemTemplateCodeGenerationInfo != null || extension.LastItemTemplateCodeGenerationInfo != null)
		{
			if (extension.FirstItemTemplateCodeGenerationInfo != null)
			{
				methodCode.AddLine("if (widget is " + extension.FirstItemTemplateCodeGenerationInfo.ClassName + ")");
				methodCode.AddLine("{");
				methodCode.AddLine("var targetWidget = (" + extension.FirstItemTemplateCodeGenerationInfo.ClassName + ")widget;");
				methodCode.AddLine("targetWidget.OnBeforeRemovedChild(widget);");
				methodCode.AddLine("}");
			}
			if (extension.LastItemTemplateCodeGenerationInfo != null)
			{
				string text = ((extension.FirstItemTemplateCodeGenerationInfo != null) ? "else " : "");
				methodCode.AddLine(text + "if (widget is " + extension.LastItemTemplateCodeGenerationInfo.ClassName + ")");
				methodCode.AddLine("{");
				methodCode.AddLine("var targetWidget = (" + extension.LastItemTemplateCodeGenerationInfo.ClassName + ")widget;");
				methodCode.AddLine("targetWidget.OnBeforeRemovedChild(widget);");
				methodCode.AddLine("}");
			}
			methodCode.AddLine("else");
			methodCode.AddLine("{");
			methodCode.AddLine("var targetWidget = (" + extension.ItemTemplateCodeGenerationInfo.ClassName + ")widget;");
			methodCode.AddLine("targetWidget.OnBeforeRemovedChild(widget);");
			methodCode.AddLine("}");
		}
		else
		{
			methodCode.AddLine("var targetWidget = (" + extension.ItemTemplateCodeGenerationInfo.ClassName + ")widget;");
			methodCode.AddLine("targetWidget.OnBeforeRemovedChild(widget);");
		}
		methodCode.AddLine("}");
	}

	private void FillRefreshDataSourceMethodAssignSection(BindingPathTargetDetails bindingPathTargetDetails, MethodCode methodCode)
	{
		BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
		string path = bindingPath.Path;
		Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(_dataSourceType, bindingPath);
		bool flag = typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath);
		bool isRoot = bindingPathTargetDetails.IsRoot;
		string datasourceVariableNameOfPath = GetDatasourceVariableNameOfPath(bindingPath);
		if (!isRoot)
		{
			string datasourceVariableNameOfPath2 = GetDatasourceVariableNameOfPath(bindingPath.ParentPath);
			methodCode.AddLine(datasourceVariableNameOfPath + " = " + datasourceVariableNameOfPath2 + "." + bindingPath.LastNode + ";");
		}
		methodCode.AddLine("if (" + datasourceVariableNameOfPath + " != null)");
		methodCode.AddLine("{");
		if (flag)
		{
			methodCode.AddLine(datasourceVariableNameOfPath + ".ListChanged += OnList" + datasourceVariableNameOfPath + "Changed;");
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetDatabindingInformation in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				if (widgetDatabindingInformation.ItemTemplateCodeGenerationInfo != null)
				{
					methodCode.AddLine("//Binding path list: " + path);
					methodCode.AddLine("for (var i = 0; i < " + datasourceVariableNameOfPath + ".Count; i++)");
					methodCode.AddLine("{");
					AddBindingListItemCreationSectionForPopulate(methodCode, widgetDatabindingInformation, datasourceVariableNameOfPath, "i");
					methodCode.AddLine("}");
				}
				if (widgetDatabindingInformation.BindCommandInfos.Values.Count > 0)
				{
					string text = "EventListenerOf" + widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName;
					methodCode.AddLine(widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName + ".EventFire += " + text + ";");
				}
			}
		}
		else
		{
			methodCode.AddLine("//Binding path: " + path);
			methodCode.AddLine(datasourceVariableNameOfPath + ".PropertyChanged += ViewModelPropertyChangedListenerOf" + datasourceVariableNameOfPath + ";");
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, string.Empty, add: true);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Bool", add: true);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Int", add: true);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Float", add: true);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "UInt", add: true);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Color", add: true);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Double", add: true);
			AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Vec2", add: true);
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetDatabindingInformation2 in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				foreach (GeneratedBindDataInfo value in widgetDatabindingInformation2.BindDataInfos.Values)
				{
					if (value.ViewModelPropertType == null)
					{
						methodCode.AddLine("//Couldn't find property in ViewModel");
						methodCode.AddLine("//" + widgetDatabindingInformation2.WidgetCodeGenerationInfo.VariableName + "." + value.Property + " = " + datasourceVariableNameOfPath + "." + value.Path + ";");
					}
					else
					{
						if (value.RequiresConversion)
						{
							methodCode.AddLine("//Requires conversion");
						}
						string[] lines = CreateAssignmentLines(datasourceVariableNameOfPath + "." + value.Path, value.ViewModelPropertType, widgetDatabindingInformation2.WidgetCodeGenerationInfo.VariableName + "." + value.Property, value.WidgetPropertyType);
						methodCode.AddLines(lines);
					}
				}
				if (widgetDatabindingInformation2.CheckIfRequiresDataComponentForWidget())
				{
					methodCode.AddLine("{");
					methodCode.AddLine("//Requires component data assignment");
					methodCode.AddLine("var widgetComponent = " + widgetDatabindingInformation2.WidgetCodeGenerationInfo.VariableName + ".GetComponent<TaleWorlds.GauntletUI.Data.GeneratedWidgetData>();");
					methodCode.AddLine("widgetComponent.Data = " + datasourceVariableNameOfPath + ";");
					methodCode.AddLine("}");
				}
				if (widgetDatabindingInformation2.BindCommandInfos.Values.Count > 0)
				{
					string text2 = "EventListenerOf" + widgetDatabindingInformation2.WidgetCodeGenerationInfo.VariableName;
					methodCode.AddLine(widgetDatabindingInformation2.WidgetCodeGenerationInfo.VariableName + ".EventFire += " + text2 + ";");
				}
				if (widgetDatabindingInformation2.BindDataInfos.Values.Count > 0)
				{
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation2, string.Empty, add: true);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation2, "bool", add: true);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation2, "float", add: true);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation2, "Vec2", add: true);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation2, "Vector2", add: true);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation2, "double", add: true);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation2, "int", add: true);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation2, "uint", add: true);
					AddPropertyChangedMethod(methodCode, widgetDatabindingInformation2, "Color", add: true);
				}
			}
		}
		foreach (BindingPathTargetDetails child in bindingPathTargetDetails.Children)
		{
			FillRefreshDataSourceMethodAssignSection(child, methodCode);
		}
		foreach (WidgetCodeGenerationInfoDatabindingExtension widgetDatabindingInformation3 in bindingPathTargetDetails.WidgetDatabindingInformations)
		{
			bool num = widgetDatabindingInformation3.WidgetCodeGenerationInfo.WidgetFactory.IsBuiltinType(widgetDatabindingInformation3.WidgetCodeGenerationInfo.WidgetTemplate.Type);
			bool isRoot2 = widgetDatabindingInformation3.IsRoot;
			if (!num && !isRoot2)
			{
				methodCode.AddLine(widgetDatabindingInformation3.WidgetCodeGenerationInfo.VariableName + ".SetDataSource(" + datasourceVariableNameOfPath + ");");
			}
		}
		methodCode.AddLine("}");
	}

	private void FillRefreshDataSourceMethod(BindingPathTargetDetails bindingPathTargetDetails, MethodCode methodCode)
	{
		string datasourceVariableNameOfPath = GetDatasourceVariableNameOfPath(bindingPathTargetDetails.BindingPath);
		methodCode.AddLine("//Clear Section");
		FillRefreshDataSourceMethodClearSection(bindingPathTargetDetails, methodCode, forDestroy: false);
		methodCode.AddLine("");
		methodCode.AddLine(datasourceVariableNameOfPath + " = newDataSource; ");
		methodCode.AddLine("");
		methodCode.AddLine("//Assign Section");
		FillRefreshDataSourceMethodAssignSection(bindingPathTargetDetails, methodCode);
	}

	private void CreateRefreshDataSourceMethod(ClassCode classCode)
	{
		foreach (BindingPathTargetDetails bindingPathTargetDetails in _bindingPathTargetDetailsList)
		{
			BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
			string datasourceVariableNameOfPath = GetDatasourceVariableNameOfPath(bindingPath);
			string viewModelCodeWriteableTypeAtPath = GetViewModelCodeWriteableTypeAtPath(bindingPath);
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "RefreshDataSource" + datasourceVariableNameOfPath;
			methodCode.MethodSignature = "(" + viewModelCodeWriteableTypeAtPath + " newDataSource)";
			methodCode.AccessModifier = MethodCodeAccessModifier.Private;
			FillRefreshDataSourceMethod(bindingPathTargetDetails, methodCode);
			classCode.AddMethod(methodCode);
		}
	}

	private string[] CreateAssignmentLines(string sourceVariable, Type sourceType, string targetVariable, Type targetType)
	{
		List<string> list = new List<string>();
		if (sourceType != targetType)
		{
			if (sourceType.IsEnum)
			{
				if (targetType == typeof(int))
				{
					string item = targetVariable + " = (" + WidgetTemplateGenerateContext.GetCodeFileType(targetType) + ")" + sourceVariable + ";";
					list.Add(item);
				}
			}
			else if (sourceType == typeof(string))
			{
				if (targetType == typeof(Sprite))
				{
					string item2 = targetVariable + " = this.Context.SpriteData.GetSprite(" + sourceVariable + ");";
					list.Add("if (" + sourceVariable + " != null)");
					list.Add("{");
					list.Add(item2);
					list.Add("}");
				}
				else if (targetType == typeof(Color))
				{
					string item3 = targetVariable + " = TaleWorlds.Library.Color.ConvertStringToColor(" + sourceVariable + ");";
					list.Add("if (" + sourceVariable + " != null)");
					list.Add("{");
					list.Add(item3);
					list.Add("}");
				}
				else if (targetType == typeof(Brush))
				{
					string item4 = targetVariable + " = this.Context.BrushFactory.GetBrush(" + sourceVariable + ");";
					list.Add("if (" + sourceVariable + " != null)");
					list.Add("{");
					list.Add(item4);
					list.Add("}");
				}
			}
			else if (sourceType == typeof(Sprite))
			{
				if (targetType == typeof(string))
				{
					string item5 = targetVariable + " = " + sourceVariable + ".Name;";
					list.Add(item5);
				}
			}
			else if (sourceType == typeof(Color) && targetType == typeof(string))
			{
				string item6 = targetVariable + " = " + sourceVariable + ".ToString();";
				list.Add(item6);
			}
		}
		else
		{
			string item7 = targetVariable + " = " + sourceVariable + ";";
			list.Add(item7);
		}
		return list.ToArray();
	}

	private void CreateEventMethods(ClassCode classCode)
	{
		Dictionary<WidgetCodeGenerationInfoDatabindingExtension, MethodCode> dictionary = new Dictionary<WidgetCodeGenerationInfoDatabindingExtension, MethodCode>();
		foreach (KeyValuePair<BindingPath, List<WidgetCodeGenerationInfoDatabindingExtension>> item in _extensionsWithPath)
		{
			BindingPath key = item.Key;
			foreach (WidgetCodeGenerationInfoDatabindingExtension item2 in item.Value)
			{
				foreach (GeneratedBindCommandInfo value in item2.BindCommandInfos.Values)
				{
					BindingPath bindingPath = new BindingPath(value.Path);
					string lastNode = bindingPath.LastNode;
					BindingPath bindingPath2 = key;
					if (bindingPath.Nodes.Length > 1)
					{
						bindingPath2 = key.Append(bindingPath.ParentPath).Simplify();
					}
					string datasourceVariableNameOfPath = GetDatasourceVariableNameOfPath(bindingPath2);
					if (!dictionary.ContainsKey(item2))
					{
						MethodCode methodCode = new MethodCode();
						dictionary.Add(item2, methodCode);
						methodCode.Name = "EventListenerOf" + item2.WidgetCodeGenerationInfo.VariableName;
						methodCode.MethodSignature = "(TaleWorlds.GauntletUI.BaseTypes.Widget widget, System.String commandName, System.Object[] args)";
						methodCode.AccessModifier = MethodCodeAccessModifier.Private;
						classCode.AddMethod(methodCode);
					}
					MethodCode methodCode2 = dictionary[item2];
					methodCode2.AddLine("if (commandName == \"" + value.Command + "\")");
					methodCode2.AddLine("{");
					if (value.Method == null)
					{
						methodCode2.AddLine("//Couldn't find method " + lastNode + " for action");
						methodCode2.AddLine("//" + datasourceVariableNameOfPath + "." + lastNode + "();");
					}
					else
					{
						for (int i = 0; i < value.ParameterCount; i++)
						{
							if (i + 1 != value.ParameterCount || !value.GotParameter)
							{
								GeneratedBindCommandParameterInfo generatedBindCommandParameterInfo = value.MethodParameters[i];
								string fullName = generatedBindCommandParameterInfo.Type.FullName;
								if (generatedBindCommandParameterInfo.IsViewModel)
								{
									methodCode2.AddLine(fullName + " arg" + i + " = null;");
									methodCode2.AddLine("{");
									methodCode2.AddLine("var arg" + i + "_widget = (TaleWorlds.GauntletUI.BaseTypes.Widget)args[" + i + "];");
									methodCode2.AddLine("var arg" + i + "_widget_data = arg" + i + "_widget.GetComponent<TaleWorlds.GauntletUI.Data.GeneratedWidgetData>();");
									methodCode2.AddLine("if (arg" + i + "_widget_data != null)");
									methodCode2.AddLine("{");
									methodCode2.AddLine("arg" + i + " = (" + fullName + ")arg" + i + "_widget_data.Data;");
									methodCode2.AddLine("}");
									methodCode2.AddLine("}");
								}
								else
								{
									methodCode2.AddLine("var arg" + i + " = (" + fullName + ")args[" + i + "];");
								}
							}
						}
						if (value.GotParameter)
						{
							GeneratedBindCommandParameterInfo generatedBindCommandParameterInfo2 = value.MethodParameters[value.ParameterCount - 1];
							methodCode2.AddLine("//GotParameter " + value.Parameter);
							if (generatedBindCommandParameterInfo2.Type == typeof(int))
							{
								methodCode2.AddLine("var arg" + (value.ParameterCount - 1) + " = " + value.Parameter + ";");
							}
							else if (generatedBindCommandParameterInfo2.Type == typeof(float))
							{
								methodCode2.AddLine("var arg" + (value.ParameterCount - 1) + " = " + value.Parameter + "f;");
							}
							else
							{
								methodCode2.AddLine("var arg" + (value.ParameterCount - 1) + " = \"" + value.Parameter + "\";");
							}
						}
						if (value.ParameterCount > 0)
						{
							string text = datasourceVariableNameOfPath + "." + lastNode + "(";
							for (int j = 0; j < value.ParameterCount; j++)
							{
								text = text + "arg" + j;
								if (j + 1 < value.ParameterCount)
								{
									text += ", ";
								}
							}
							text += ");";
							methodCode2.AddLine(text);
						}
						else
						{
							methodCode2.AddLine(datasourceVariableNameOfPath + "." + lastNode + "();");
						}
					}
					methodCode2.AddLine("}");
				}
			}
		}
	}

	private void CreateWidgetPropertyChangedMethods(ClassCode classCode)
	{
		Dictionary<WidgetCodeGenerationInfoDatabindingExtension, MethodCode> dictionary = new Dictionary<WidgetCodeGenerationInfoDatabindingExtension, MethodCode>();
		foreach (BindingPathTargetDetails bindingPathTargetDetails in _bindingPathTargetDetailsList)
		{
			BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
			string datasourceVariableNameOfPath = GetDatasourceVariableNameOfPath(bindingPath);
			Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(_dataSourceType, bindingPath);
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetDatabindingInformation in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				foreach (GeneratedBindDataInfo value in widgetDatabindingInformation.BindDataInfos.Values)
				{
					if (!dictionary.ContainsKey(widgetDatabindingInformation))
					{
						AddWidgetPropertyChangedWithValueVariant(classCode, widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName, string.Empty);
						AddWidgetPropertyChangedWithValueVariant(classCode, widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName, "bool");
						AddWidgetPropertyChangedWithValueVariant(classCode, widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName, "float");
						AddWidgetPropertyChangedWithValueVariant(classCode, widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName, "Vec2");
						AddWidgetPropertyChangedWithValueVariant(classCode, widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName, "Vector2");
						AddWidgetPropertyChangedWithValueVariant(classCode, widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName, "double");
						AddWidgetPropertyChangedWithValueVariant(classCode, widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName, "int");
						AddWidgetPropertyChangedWithValueVariant(classCode, widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName, "uint");
						AddWidgetPropertyChangedWithValueVariant(classCode, widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName, "Color");
						string variableName = widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName;
						MethodCode methodCode = new MethodCode
						{
							Name = "HandleWidgetPropertyChangeOf" + variableName,
							MethodSignature = "(System.String propertyName)",
							AccessModifier = MethodCodeAccessModifier.Private
						};
						dictionary.Add(widgetDatabindingInformation, methodCode);
						classCode.AddMethod(methodCode);
					}
					MethodCode methodCode2 = dictionary[widgetDatabindingInformation];
					methodCode2.AddLine("if (propertyName == \"" + value.Property + "\")");
					methodCode2.AddLine("{");
					if (value.ViewModelPropertType == null)
					{
						methodCode2.AddLine("//Couldn't find property in ViewModel");
						methodCode2.AddLine("//" + datasourceVariableNameOfPath + "." + value.Path + " = " + widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName + "." + value.Property + ";");
					}
					else if (VariableCollection.GetPropertyInfo(viewModelTypeAtPath, value.Path).GetSetMethod() == null)
					{
						methodCode2.AddLine("//Property in ViewModel does not have a set method");
						methodCode2.AddLine("//" + datasourceVariableNameOfPath + "." + value.Path + " = " + widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName + "." + value.Property + ";");
					}
					else
					{
						string[] lines = CreateAssignmentLines(widgetDatabindingInformation.WidgetCodeGenerationInfo.VariableName + "." + value.Property, value.WidgetPropertyType, datasourceVariableNameOfPath + "." + value.Path, value.ViewModelPropertType);
						methodCode2.AddLines(lines);
					}
					methodCode2.AddLine("return;");
					methodCode2.AddLine("}");
				}
			}
		}
	}

	private void CreateViewModelPropertyChangedMethods(ClassCode classCode)
	{
		foreach (BindingPathTargetDetails bindingPathTargetDetails in _bindingPathTargetDetailsList)
		{
			BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
			string datasourceVariableNameOfPath = GetDatasourceVariableNameOfPath(bindingPath);
			Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(_dataSourceType, bindingPath);
			if (typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath))
			{
				continue;
			}
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "ViewModelPropertyChangedListenerOf" + datasourceVariableNameOfPath;
			methodCode.MethodSignature = "(System.Object sender, System.ComponentModel.PropertyChangedEventArgs e)";
			methodCode.AccessModifier = MethodCodeAccessModifier.Private;
			methodCode.AddLine("HandleViewModelPropertyChangeOf" + datasourceVariableNameOfPath + "(e.PropertyName);");
			classCode.AddMethod(methodCode);
			AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, string.Empty);
			AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Bool");
			AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Int");
			AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Float");
			AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "UInt");
			AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Color");
			AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Double");
			AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Vec2");
			MethodCode methodCode2 = new MethodCode();
			methodCode2.Name = "HandleViewModelPropertyChangeOf" + datasourceVariableNameOfPath;
			methodCode2.MethodSignature = "(System.String propertyName)";
			methodCode2.AccessModifier = MethodCodeAccessModifier.Private;
			classCode.AddMethod(methodCode2);
			methodCode2.AddLine("//DataSource property section");
			foreach (BindingPathTargetDetails child in bindingPathTargetDetails.Children)
			{
				string datasourceVariableNameOfPath2 = GetDatasourceVariableNameOfPath(child.BindingPath);
				string lastNode = child.BindingPath.LastNode;
				methodCode2.AddLine("if (propertyName == \"" + lastNode + "\")");
				methodCode2.AddLine("{");
				methodCode2.AddLine("RefreshDataSource" + datasourceVariableNameOfPath2 + "(" + datasourceVariableNameOfPath + "." + lastNode + ");");
				methodCode2.AddLine("return;");
				methodCode2.AddLine("}");
			}
			Dictionary<string, List<WidgetCodeGenerationInfoDatabindingExtension>> dictionary = new Dictionary<string, List<WidgetCodeGenerationInfoDatabindingExtension>>();
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetDatabindingInformation in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				foreach (GeneratedBindDataInfo value2 in widgetDatabindingInformation.BindDataInfos.Values)
				{
					if (!dictionary.ContainsKey(value2.Path))
					{
						dictionary.Add(value2.Path, new List<WidgetCodeGenerationInfoDatabindingExtension>());
					}
					if (!dictionary[value2.Path].Contains(widgetDatabindingInformation))
					{
						dictionary[value2.Path].Add(widgetDatabindingInformation);
					}
				}
			}
			methodCode2.AddLine("//Primitive property section");
			foreach (KeyValuePair<string, List<WidgetCodeGenerationInfoDatabindingExtension>> item in dictionary)
			{
				string key = item.Key;
				List<WidgetCodeGenerationInfoDatabindingExtension> value = item.Value;
				methodCode2.AddLine("if (propertyName == \"" + key + "\")");
				methodCode2.AddLine("{");
				foreach (WidgetCodeGenerationInfoDatabindingExtension item2 in value)
				{
					foreach (GeneratedBindDataInfo value3 in item2.BindDataInfos.Values)
					{
						if (!(value3.Path == key))
						{
							continue;
						}
						if (value3.ViewModelPropertType == null)
						{
							methodCode2.AddLine("//Couldn't find property in ViewModel");
							methodCode2.AddLine("//" + item2.WidgetCodeGenerationInfo.VariableName + "." + value3.Property + " = " + datasourceVariableNameOfPath + "." + value3.Path + ";");
						}
						else
						{
							if (value3.RequiresConversion)
							{
								methodCode2.AddLine("//Requires conversion");
							}
							string[] lines = CreateAssignmentLines(datasourceVariableNameOfPath + "." + value3.Path, value3.ViewModelPropertType, item2.WidgetCodeGenerationInfo.VariableName + "." + value3.Property, value3.WidgetPropertyType);
							methodCode2.AddLines(lines);
						}
					}
				}
				methodCode2.AddLine("return;");
				methodCode2.AddLine("}");
			}
		}
	}

	private static void AddPropertyChangedWithValueVariant(ClassCode classCode, string dataSourceVariableName, string typeVariant)
	{
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "ViewModelPropertyChangedWith" + typeVariant + "ValueListenerOf" + dataSourceVariableName;
		methodCode.MethodSignature = "(System.Object sender, TaleWorlds.Library.PropertyChangedWith" + typeVariant + "ValueEventArgs e)";
		methodCode.AccessModifier = MethodCodeAccessModifier.Private;
		methodCode.AddLine("HandleViewModelPropertyChangeOf" + dataSourceVariableName + "(e.PropertyName);");
		classCode.AddMethod(methodCode);
	}

	private void CreateListChangedMethods(ClassCode classCode)
	{
		foreach (KeyValuePair<BindingPath, List<WidgetCodeGenerationInfoDatabindingExtension>> item in _extensionsWithPath)
		{
			List<WidgetCodeGenerationInfoDatabindingExtension> value = item.Value;
			BindingPath key = item.Key;
			string datasourceVariableNameOfPath = GetDatasourceVariableNameOfPath(key);
			Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(_dataSourceType, key);
			if (!typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath))
			{
				continue;
			}
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "OnList" + datasourceVariableNameOfPath + "Changed";
			methodCode.MethodSignature = "(System.Object sender, TaleWorlds.Library.ListChangedEventArgs e)";
			methodCode.AddLine("switch (e.ListChangedType)");
			methodCode.AddLine("{");
			methodCode.AddLine("case TaleWorlds.Library.ListChangedType.Reset:");
			methodCode.AddLine("{");
			foreach (WidgetCodeGenerationInfoDatabindingExtension item2 in value)
			{
				if (item2.ItemTemplateCodeGenerationInfo != null)
				{
					methodCode.AddLine("for (var i = " + item2.WidgetCodeGenerationInfo.VariableName + ".ChildCount - 1; i >= 0; i--)");
					methodCode.AddLine("{");
					AddBindingListItemBeforeDeletionSection(methodCode, item2, "i", forDestroy: false);
					AddBindingListItemDeletionSection(methodCode, item2, "i", forDestroy: false);
					methodCode.AddLine("}");
				}
			}
			methodCode.AddLine("}");
			methodCode.AddLine("break;");
			methodCode.AddLine("case TaleWorlds.Library.ListChangedType.Sorted:");
			methodCode.AddLine("{");
			methodCode.AddLine("for (int i = 0; i < " + datasourceVariableNameOfPath + ".Count; i++)");
			methodCode.AddLine("{");
			methodCode.AddLine("var bindingObject = " + datasourceVariableNameOfPath + "[i];");
			foreach (WidgetCodeGenerationInfoDatabindingExtension item3 in value)
			{
				if (item3.ItemTemplateCodeGenerationInfo != null)
				{
					methodCode.AddLine("{");
					methodCode.AddLine("var target = " + item3.WidgetCodeGenerationInfo.VariableName + ".FindChild(widget => widget.GetComponent<TaleWorlds.GauntletUI.Data.GeneratedWidgetData>().Data == bindingObject);");
					methodCode.AddLine("target.SetSiblingIndex(i);");
					methodCode.AddLine("}");
				}
			}
			methodCode.AddLine("}");
			methodCode.AddLine("}");
			methodCode.AddLine("break;");
			methodCode.AddLine("case TaleWorlds.Library.ListChangedType.ItemAdded:");
			methodCode.AddLine("{");
			foreach (WidgetCodeGenerationInfoDatabindingExtension item4 in value)
			{
				if (item4.ItemTemplateCodeGenerationInfo != null)
				{
					AddBindingListItemCreationSection(methodCode, item4, datasourceVariableNameOfPath, "e.NewIndex");
				}
			}
			methodCode.AddLine("}");
			methodCode.AddLine("break;");
			methodCode.AddLine("case TaleWorlds.Library.ListChangedType.ItemBeforeDeleted:");
			methodCode.AddLine("{");
			foreach (WidgetCodeGenerationInfoDatabindingExtension item5 in value)
			{
				if (item5.ItemTemplateCodeGenerationInfo != null)
				{
					methodCode.AddLine("{");
					AddBindingListItemBeforeDeletionSection(methodCode, item5, "e.NewIndex", forDestroy: false);
					methodCode.AddLine("}");
				}
			}
			methodCode.AddLine("}");
			methodCode.AddLine("break;");
			methodCode.AddLine("case TaleWorlds.Library.ListChangedType.ItemDeleted:");
			methodCode.AddLine("{");
			foreach (WidgetCodeGenerationInfoDatabindingExtension item6 in value)
			{
				if (item6.ItemTemplateCodeGenerationInfo != null)
				{
					methodCode.AddLine("{");
					AddBindingListItemDeletionSection(methodCode, item6, "e.NewIndex", forDestroy: false);
					methodCode.AddLine("}");
				}
			}
			methodCode.AddLine("}");
			methodCode.AddLine("break;");
			methodCode.AddLine("case TaleWorlds.Library.ListChangedType.ItemChanged:");
			methodCode.AddLine("{");
			methodCode.AddLine("");
			methodCode.AddLine("}");
			methodCode.AddLine("break;");
			methodCode.AddLine("}");
			classCode.AddMethod(methodCode);
		}
	}

	private BindingPathTargetDetails GetRootBindingPathTargetDetails()
	{
		foreach (BindingPathTargetDetails bindingPathTargetDetails in _bindingPathTargetDetailsList)
		{
			if (bindingPathTargetDetails.IsRoot)
			{
				return bindingPathTargetDetails;
			}
		}
		return null;
	}

	private BindingPathTargetDetails GetBindingPathTargetDetails(BindingPath bindingPath)
	{
		foreach (BindingPathTargetDetails bindingPathTargetDetails in _bindingPathTargetDetailsList)
		{
			if (bindingPathTargetDetails.BindingPath == bindingPath)
			{
				return bindingPathTargetDetails;
			}
		}
		return null;
	}

	private void FindBindingPathTargetDetails()
	{
		_bindingPathTargetDetailsList = new List<BindingPathTargetDetails>();
		foreach (WidgetCodeGenerationInfo widgetCodeGenerationInformation in _widgetTemplateGenerateContext.WidgetCodeGenerationInformations)
		{
			BindingPath fullBindingPath = ((WidgetCodeGenerationInfoDatabindingExtension)widgetCodeGenerationInformation.Extension).FullBindingPath;
			if (GetBindingPathTargetDetails(fullBindingPath) == null)
			{
				BindingPathTargetDetails item = new BindingPathTargetDetails(fullBindingPath);
				_bindingPathTargetDetailsList.Add(item);
			}
		}
		for (int i = 0; i < _bindingPathTargetDetailsList.Count; i++)
		{
			BindingPathTargetDetails bindingPathTargetDetails = _bindingPathTargetDetailsList[i];
			if (!bindingPathTargetDetails.IsRoot)
			{
				BindingPath parentPath = bindingPathTargetDetails.BindingPath.ParentPath;
				BindingPathTargetDetails bindingPathTargetDetails2 = GetBindingPathTargetDetails(parentPath);
				if (bindingPathTargetDetails2 == null)
				{
					bindingPathTargetDetails2 = new BindingPathTargetDetails(parentPath);
					_bindingPathTargetDetailsList.Add(bindingPathTargetDetails2);
				}
				bindingPathTargetDetails.SetParent(bindingPathTargetDetails2);
			}
		}
	}

	public override void DoExtraCodeGeneration(ClassCode classCode)
	{
		FindBindingPathTargetDetails();
		_extensions = new List<WidgetCodeGenerationInfoDatabindingExtension>();
		_extensionsWithPath = new Dictionary<BindingPath, List<WidgetCodeGenerationInfoDatabindingExtension>>();
		foreach (WidgetCodeGenerationInfo widgetCodeGenerationInformation in _widgetTemplateGenerateContext.WidgetCodeGenerationInformations)
		{
			WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension = (WidgetCodeGenerationInfoDatabindingExtension)widgetCodeGenerationInformation.Extension;
			_extensions.Add(widgetCodeGenerationInfoDatabindingExtension);
			BindingPath fullBindingPath = widgetCodeGenerationInfoDatabindingExtension.FullBindingPath;
			GetBindingPathTargetDetails(fullBindingPath).WidgetDatabindingInformations.Add(widgetCodeGenerationInfoDatabindingExtension);
			if (!_extensionsWithPath.ContainsKey(fullBindingPath))
			{
				_extensionsWithPath.Add(fullBindingPath, new List<WidgetCodeGenerationInfoDatabindingExtension>());
			}
			_extensionsWithPath[fullBindingPath].Add(widgetCodeGenerationInfoDatabindingExtension);
		}
		if (_widgetTemplateGenerateContext.ContextType == WidgetTemplateGenerateContextType.RootPrefab)
		{
			classCode.InheritedInterfaces.Add("TaleWorlds.GauntletUI.Data.IGeneratedGauntletMovieRoot");
			CreateRefreshBindingWithChildrenMethod(classCode);
		}
		CreateDestroyDataSourceyMethod(classCode);
		CreateSetDataSourceVariables(classCode);
		CreateSetDataSourceMethod(classCode);
		CreateEventMethods(classCode);
		CreateWidgetPropertyChangedMethods(classCode);
		CreateViewModelPropertyChangedMethods(classCode);
		CreateListChangedMethods(classCode);
		CreateRefreshDataSourceMethod(classCode);
	}

	public override void AddExtrasToCreatorMethod(MethodCode methodCode)
	{
		methodCode.AddLine("var movie = new TaleWorlds.GauntletUI.Data.GeneratedGauntletMovie(\"" + _widgetTemplateGenerateContext.PrefabName + "\", widget);");
		methodCode.AddLine("var dataSource = data[\"DataSource\"];");
		methodCode.AddLine("widget.SetDataSource((" + _dataSourceType.FullName + ")dataSource);");
		methodCode.AddLine("result.AddData(\"Movie\", movie);");
	}

	public override WidgetCodeGenerationInfoExtension CreateWidgetCodeGenerationInfoExtension(WidgetCodeGenerationInfo widgetCodeGenerationInfo)
	{
		return new WidgetCodeGenerationInfoDatabindingExtension(widgetCodeGenerationInfo);
	}

	private static void AddWidgetPropertyChangedWithValueVariant(ClassCode classCode, string widgetName, string typeVariant)
	{
		MethodCode methodCode = new MethodCode();
		methodCode.Name = typeVariant + "PropertyChangedListenerOf" + widgetName;
		if (string.IsNullOrEmpty(typeVariant))
		{
			methodCode.MethodSignature = "(TaleWorlds.GauntletUI.PropertyOwnerObject propertyOwnerObject, System.String propertyName, System.Object e)";
		}
		else
		{
			methodCode.MethodSignature = "(TaleWorlds.GauntletUI.PropertyOwnerObject propertyOwnerObject, System.String propertyName, " + typeVariant + " e)";
		}
		methodCode.AccessModifier = MethodCodeAccessModifier.Private;
		methodCode.AddLine("HandleWidgetPropertyChangeOf" + widgetName + "(propertyName);");
		classCode.AddMethod(methodCode);
	}

	private static void AddPropertyChangedMethod(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string typeName, bool add)
	{
		methodCode.AddLine(extension.WidgetCodeGenerationInfo.VariableName + "." + typeName + "PropertyChanged " + (add ? "+" : "-") + "= " + typeName + "PropertyChangedListenerOf" + extension.WidgetCodeGenerationInfo.VariableName + ";");
	}
}
