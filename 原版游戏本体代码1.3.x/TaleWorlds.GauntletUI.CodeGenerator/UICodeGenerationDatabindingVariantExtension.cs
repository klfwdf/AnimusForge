using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.CodeGeneration;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x0200000F RID: 15
	public class UICodeGenerationDatabindingVariantExtension : UICodeGenerationVariantExtension
	{
		// Token: 0x060000BF RID: 191 RVA: 0x00004978 File Offset: 0x00002B78
		public override PrefabExtension GetPrefabExtension()
		{
			return new PrefabDatabindingExtension();
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x000034C6 File Offset: 0x000016C6
		public override void AddExtensionVariables(ClassCode classCode)
		{
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x0000497F File Offset: 0x00002B7F
		public override void Initialize(WidgetTemplateGenerateContext widgetTemplateGenerateContext)
		{
			this._widgetTemplateGenerateContext = widgetTemplateGenerateContext;
			this._dataSourceType = (Type)this._widgetTemplateGenerateContext.Data["DataSourceType"];
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x000049A8 File Offset: 0x00002BA8
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

		// Token: 0x060000C3 RID: 195 RVA: 0x000049FC File Offset: 0x00002BFC
		private static string GetDatasourceVariableNameOfPath(BindingPath bindingPath)
		{
			string str = bindingPath.Path.Replace("\\", "_");
			return "_datasource_" + str;
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00004A2C File Offset: 0x00002C2C
		private string GetViewModelCodeWriteableTypeAtPath(BindingPath bindingPath)
		{
			Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(this._dataSourceType, bindingPath);
			string result;
			if (typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath))
			{
				Type type = viewModelTypeAtPath.GetGenericArguments()[0];
				result = "TaleWorlds.Library.MBBindingList<" + type.FullName + ">";
			}
			else if (viewModelTypeAtPath.IsGenericType)
			{
				result = UICodeGenerationDatabindingVariantExtension.GetGenericTypeCodeFileName(viewModelTypeAtPath);
			}
			else
			{
				result = viewModelTypeAtPath.FullName;
			}
			return result;
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00004A98 File Offset: 0x00002C98
		private void FillDataSourceVariables(BindingPathTargetDetails bindingPathTargetDetails, ClassCode classCode)
		{
			BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
			classCode.AddVariable(new VariableCode
			{
				Name = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPath),
				AccessModifier = VariableCodeAccessModifier.Private,
				Type = this.GetViewModelCodeWriteableTypeAtPath(bindingPath)
			});
			foreach (BindingPathTargetDetails bindingPathTargetDetails2 in bindingPathTargetDetails.Children)
			{
				this.FillDataSourceVariables(bindingPathTargetDetails2, classCode);
			}
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00004B20 File Offset: 0x00002D20
		private void CreateSetDataSourceVariables(ClassCode classCode)
		{
			BindingPathTargetDetails rootBindingPathTargetDetails = this.GetRootBindingPathTargetDetails();
			this.FillDataSourceVariables(rootBindingPathTargetDetails, classCode);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00004B3C File Offset: 0x00002D3C
		private void CreateSetDataSourceMethod(ClassCode classCode)
		{
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "SetDataSource";
			string viewModelCodeWriteableTypeAtPath = this.GetViewModelCodeWriteableTypeAtPath(new BindingPath("Root"));
			string datasourceVariableNameOfPath = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(new BindingPath("Root"));
			methodCode.MethodSignature = "(" + viewModelCodeWriteableTypeAtPath + " dataSource)";
			if (this._widgetTemplateGenerateContext.CheckIfInheritsAnotherPrefab())
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
				methodCode.AddLine("base.SetDataSource(dataSource);");
			}
			else if (this._widgetTemplateGenerateContext.ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
			}
			methodCode.AddLine("RefreshDataSource" + datasourceVariableNameOfPath + "(dataSource);");
			classCode.AddMethod(methodCode);
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00004BE4 File Offset: 0x00002DE4
		private static string GetGenericTypeCodeFileName(Type type)
		{
			string str = type.FullName.Split(new char[] { '`' })[0] + "<";
			for (int i = 0; i < type.GenericTypeArguments.Length; i++)
			{
				Type type2 = type.GenericTypeArguments[i];
				str += type2.FullName;
				if (i + 1 < type.GenericTypeArguments.Length)
				{
					str += ", ";
				}
			}
			return str + ">";
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00004C64 File Offset: 0x00002E64
		private void CreateDestroyDataSourceyMethod(ClassCode classCode)
		{
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "DestroyDataSource";
			if (this._widgetTemplateGenerateContext.CheckIfInheritsAnotherPrefab())
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
				methodCode.AddLine("base.DestroyDataSource();");
			}
			else if (this._widgetTemplateGenerateContext.ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
			}
			BindingPathTargetDetails rootBindingPathTargetDetails = this.GetRootBindingPathTargetDetails();
			this.FillRefreshDataSourceMethodClearSection(rootBindingPathTargetDetails, methodCode, true);
			classCode.AddMethod(methodCode);
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00004CD0 File Offset: 0x00002ED0
		private void CreateRefreshBindingWithChildrenMethod(ClassCode classCode)
		{
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "RefreshBindingWithChildren";
			methodCode.AddLine("var dataSource = _datasource_Root;");
			methodCode.AddLine("this.SetDataSource(null);");
			methodCode.AddLine("this.SetDataSource(dataSource);");
			classCode.AddMethod(methodCode);
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00004D18 File Offset: 0x00002F18
		private void FillRefreshDataSourceMethodClearSection(BindingPathTargetDetails bindingPathTargetDetails, MethodCode methodCode, bool forDestroy)
		{
			BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
			string path = bindingPath.Path;
			Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(this._dataSourceType, bindingPath);
			bool flag = typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath);
			string datasourceVariableNameOfPath = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPath);
			methodCode.AddLine("if (" + datasourceVariableNameOfPath + " != null)");
			methodCode.AddLine("{");
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				bool flag2 = widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.WidgetFactory.IsBuiltinType(widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.WidgetTemplate.Type);
				bool isRoot = widgetCodeGenerationInfoDatabindingExtension.IsRoot;
				if (!flag2 && !isRoot)
				{
					if (forDestroy)
					{
						methodCode.AddLine(widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName + ".DestroyDataSource();");
					}
					else
					{
						methodCode.AddLine(widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName + ".SetDataSource(null);");
					}
				}
				if (widgetCodeGenerationInfoDatabindingExtension.CheckIfRequiresDataComponentForWidget())
				{
					methodCode.AddLine("{");
					methodCode.AddLine("//Requires component data to be cleared");
					methodCode.AddLine("var widgetComponent = " + widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName + ".GetComponent<TaleWorlds.GauntletUI.Data.GeneratedWidgetData>();");
					methodCode.AddLine("widgetComponent.Data = null;");
					methodCode.AddLine("}");
				}
			}
			if (flag)
			{
				methodCode.AddLine(datasourceVariableNameOfPath + ".ListChanged -= OnList" + datasourceVariableNameOfPath + "Changed;");
				using (List<WidgetCodeGenerationInfoDatabindingExtension>.Enumerator enumerator = bindingPathTargetDetails.WidgetDatabindingInformations.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension2 = enumerator.Current;
						if (widgetCodeGenerationInfoDatabindingExtension2.ItemTemplateCodeGenerationInfo != null)
						{
							methodCode.AddLine("//Binding path list: " + path);
							string variableName = widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName;
							methodCode.AddLine("for (var i = " + variableName + ".ChildCount - 1; i >= 0; i--)");
							methodCode.AddLine("{");
							this.AddBindingListItemBeforeDeletionSection(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "i", forDestroy);
							this.AddBindingListItemDeletionSection(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "i", forDestroy);
							methodCode.AddLine("}");
						}
						if (widgetCodeGenerationInfoDatabindingExtension2.BindCommandInfos.Values.Count > 0)
						{
							string str = "EventListenerOf" + widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName;
							methodCode.AddLine(widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName + ".EventFire -= " + str + ";");
						}
					}
					goto IL_41E;
				}
			}
			methodCode.AddLine("//Binding path: " + path);
			methodCode.AddLine(datasourceVariableNameOfPath + ".PropertyChanged -= ViewModelPropertyChangedListenerOf" + datasourceVariableNameOfPath + ";");
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, string.Empty, false);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Bool", false);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Int", false);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Float", false);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "UInt", false);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Color", false);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Double", false);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Vec2", false);
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension3 in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				if (widgetCodeGenerationInfoDatabindingExtension3.BindCommandInfos.Values.Count > 0)
				{
					string str2 = "EventListenerOf" + widgetCodeGenerationInfoDatabindingExtension3.WidgetCodeGenerationInfo.VariableName;
					methodCode.AddLine(widgetCodeGenerationInfoDatabindingExtension3.WidgetCodeGenerationInfo.VariableName + ".EventFire -= " + str2 + ";");
				}
				if (widgetCodeGenerationInfoDatabindingExtension3.BindDataInfos.Values.Count > 0)
				{
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension3, string.Empty, false);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension3, "bool", false);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension3, "float", false);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension3, "Vec2", false);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension3, "Vector2", false);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension3, "double", false);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension3, "int", false);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension3, "uint", false);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension3, "Color", false);
				}
			}
			IL_41E:
			foreach (BindingPathTargetDetails bindingPathTargetDetails2 in bindingPathTargetDetails.Children)
			{
				this.FillRefreshDataSourceMethodClearSection(bindingPathTargetDetails2, methodCode, forDestroy);
			}
			methodCode.AddLine(datasourceVariableNameOfPath + " = null;");
			methodCode.AddLine("}");
		}

		// Token: 0x060000CC RID: 204 RVA: 0x000051D0 File Offset: 0x000033D0
		private static void AddDataSourcePropertyChangedMethod(MethodCode methodCode, string dataSourceVariableName, string typeModifier, bool add)
		{
			methodCode.AddLine(string.Concat(new string[]
			{
				dataSourceVariableName,
				".PropertyChangedWith",
				typeModifier,
				"Value ",
				add ? "+" : "-",
				"=ViewModelPropertyChangedWith",
				typeModifier,
				"ValueListenerOf",
				dataSourceVariableName,
				";"
			}));
		}

		// Token: 0x060000CD RID: 205 RVA: 0x0000523C File Offset: 0x0000343C
		private void AddBindingListItemCreationItemMethodsSection(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string variableName, string dataSourceVariableName, string childIndexVariableName)
		{
			methodCode.AddLine("var widgetComponent = new TaleWorlds.GauntletUI.Data.GeneratedWidgetData(" + variableName + ");");
			methodCode.AddLine(string.Concat(new string[] { "var dataSource = ", dataSourceVariableName, "[", childIndexVariableName, "];" }));
			methodCode.AddLine("widgetComponent.Data = dataSource;");
			methodCode.AddLine(variableName + ".AddComponent(widgetComponent);");
			methodCode.AddLine(string.Concat(new string[]
			{
				extension.WidgetCodeGenerationInfo.VariableName,
				".AddChildAtIndex(",
				variableName,
				", ",
				childIndexVariableName,
				");"
			}));
			methodCode.AddLine(variableName + ".CreateWidgets();");
			methodCode.AddLine(variableName + ".SetIds();");
			methodCode.AddLine(variableName + ".SetAttributes();");
			methodCode.AddLine(variableName + ".SetDataSource(dataSource);");
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00005334 File Offset: 0x00003534
		private void AddBindingListItemCreationSection(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string dataSourceVariableName, string childIndexVariableName)
		{
			if (extension.FirstItemTemplateCodeGenerationInfo != null)
			{
				methodCode.AddLine("//Got first item template");
				methodCode.AddLine("if (" + extension.WidgetCodeGenerationInfo.VariableName + ".ChildCount == 0)");
				methodCode.AddLine("{");
				methodCode.AddLine("var item = new " + extension.FirstItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
				this.AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
				methodCode.AddLine("}");
			}
			if (extension.LastItemTemplateCodeGenerationInfo != null)
			{
				methodCode.AddLine("//Got last item template");
				string text = ((extension.FirstItemTemplateCodeGenerationInfo != null) ? "else " : "");
				methodCode.AddLine(string.Concat(new string[]
				{
					text,
					"if (",
					extension.WidgetCodeGenerationInfo.VariableName,
					".ChildCount == ",
					childIndexVariableName,
					" && ",
					extension.WidgetCodeGenerationInfo.VariableName,
					".ChildCount > 0)"
				}));
				methodCode.AddLine("{");
				methodCode.AddLine("//Change current last item into default item template");
				methodCode.AddLine("{");
				methodCode.AddLine(string.Concat(new string[]
				{
					"var currentLastItem = ",
					extension.WidgetCodeGenerationInfo.VariableName,
					".GetChild(",
					extension.WidgetCodeGenerationInfo.VariableName,
					".ChildCount - 1) as ",
					extension.LastItemTemplateCodeGenerationInfo.ClassName,
					";"
				}));
				methodCode.AddLine("if (currentLastItem != null)");
				methodCode.AddLine("{");
				methodCode.AddLine(extension.WidgetCodeGenerationInfo.VariableName + ".OnBeforeRemovedChild(currentLastItem);");
				methodCode.AddLine("currentLastItem.SetDataSource(null);");
				methodCode.AddLine("var newPreviousItem = new " + extension.ItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
				this.AddBindingListItemCreationItemMethodsSection(methodCode, extension, "newPreviousItem", dataSourceVariableName, extension.WidgetCodeGenerationInfo.VariableName + ".ChildCount - 1");
				methodCode.AddLine(extension.WidgetCodeGenerationInfo.VariableName + ".RemoveChild(currentLastItem);");
				methodCode.AddLine("}");
				methodCode.AddLine("}");
				methodCode.AddLine("//Add new last item");
				methodCode.AddLine("{");
				methodCode.AddLine("var item = new " + extension.LastItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
				this.AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
				methodCode.AddLine("}");
				methodCode.AddLine("}");
			}
			if (extension.FirstItemTemplateCodeGenerationInfo != null || extension.LastItemTemplateCodeGenerationInfo != null)
			{
				methodCode.AddLine("else");
			}
			methodCode.AddLine("{");
			methodCode.AddLine("var item = new " + extension.ItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
			this.AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
			methodCode.AddLine("}");
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00005624 File Offset: 0x00003824
		private void AddBindingListItemCreationSectionForPopulate(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string dataSourceVariableName, string childIndexVariableName)
		{
			if (extension.FirstItemTemplateCodeGenerationInfo != null)
			{
				methodCode.AddLine("//Got first item template");
				methodCode.AddLine("if (" + childIndexVariableName + " == 0)");
				methodCode.AddLine("{");
				methodCode.AddLine("var item = new " + extension.FirstItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
				this.AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
				methodCode.AddLine("}");
			}
			if (extension.LastItemTemplateCodeGenerationInfo != null)
			{
				methodCode.AddLine("//Got last item template");
				string text = ((extension.FirstItemTemplateCodeGenerationInfo != null) ? "else " : "");
				methodCode.AddLine(string.Concat(new string[] { text, "if (", childIndexVariableName, " == ", dataSourceVariableName, ".Count - 1)" }));
				methodCode.AddLine("{");
				methodCode.AddLine("var item = new " + extension.LastItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
				this.AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
				methodCode.AddLine("}");
			}
			if (extension.FirstItemTemplateCodeGenerationInfo != null || extension.LastItemTemplateCodeGenerationInfo != null)
			{
				methodCode.AddLine("else");
			}
			methodCode.AddLine("{");
			methodCode.AddLine("var item = new " + extension.ItemTemplateCodeGenerationInfo.ClassName + "(this.Context);");
			this.AddBindingListItemCreationItemMethodsSection(methodCode, extension, "item", dataSourceVariableName, childIndexVariableName);
			methodCode.AddLine("}");
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x000057AC File Offset: 0x000039AC
		private void AddBindingListItemDeletionSection(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string childIndexVariableName, bool forDestroy)
		{
			string variableName = extension.WidgetCodeGenerationInfo.VariableName;
			methodCode.AddLine("{");
			methodCode.AddLine(string.Concat(new string[] { "var widget = ", variableName, ".GetChild(", childIndexVariableName, ");" }));
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
					string str = ((extension.FirstItemTemplateCodeGenerationInfo != null) ? "else " : "");
					methodCode.AddLine(str + "if (widget is " + extension.LastItemTemplateCodeGenerationInfo.ClassName + ")");
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

		// Token: 0x060000D1 RID: 209 RVA: 0x000059DC File Offset: 0x00003BDC
		private void AddBindingListItemBeforeDeletionSection(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string childIndexVariableName, bool forDestroy)
		{
			string variableName = extension.WidgetCodeGenerationInfo.VariableName;
			methodCode.AddLine("{");
			methodCode.AddLine(string.Concat(new string[] { "var widget = ", variableName, ".GetChild(", childIndexVariableName, ");" }));
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
					string str = ((extension.FirstItemTemplateCodeGenerationInfo != null) ? "else " : "");
					methodCode.AddLine(str + "if (widget is " + extension.LastItemTemplateCodeGenerationInfo.ClassName + ")");
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

		// Token: 0x060000D2 RID: 210 RVA: 0x00005BB0 File Offset: 0x00003DB0
		private void FillRefreshDataSourceMethodAssignSection(BindingPathTargetDetails bindingPathTargetDetails, MethodCode methodCode)
		{
			BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
			string path = bindingPath.Path;
			Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(this._dataSourceType, bindingPath);
			bool flag = typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath);
			bool isRoot = bindingPathTargetDetails.IsRoot;
			string datasourceVariableNameOfPath = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPath);
			if (!isRoot)
			{
				string datasourceVariableNameOfPath2 = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPath.ParentPath);
				methodCode.AddLine(string.Concat(new string[] { datasourceVariableNameOfPath, " = ", datasourceVariableNameOfPath2, ".", bindingPath.LastNode, ";" }));
			}
			methodCode.AddLine("if (" + datasourceVariableNameOfPath + " != null)");
			methodCode.AddLine("{");
			if (flag)
			{
				methodCode.AddLine(datasourceVariableNameOfPath + ".ListChanged += OnList" + datasourceVariableNameOfPath + "Changed;");
				using (List<WidgetCodeGenerationInfoDatabindingExtension>.Enumerator enumerator = bindingPathTargetDetails.WidgetDatabindingInformations.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension = enumerator.Current;
						if (widgetCodeGenerationInfoDatabindingExtension.ItemTemplateCodeGenerationInfo != null)
						{
							methodCode.AddLine("//Binding path list: " + path);
							methodCode.AddLine("for (var i = 0; i < " + datasourceVariableNameOfPath + ".Count; i++)");
							methodCode.AddLine("{");
							this.AddBindingListItemCreationSectionForPopulate(methodCode, widgetCodeGenerationInfoDatabindingExtension, datasourceVariableNameOfPath, "i");
							methodCode.AddLine("}");
						}
						if (widgetCodeGenerationInfoDatabindingExtension.BindCommandInfos.Values.Count > 0)
						{
							string str = "EventListenerOf" + widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName;
							methodCode.AddLine(widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName + ".EventFire += " + str + ";");
						}
					}
					goto IL_4D7;
				}
			}
			methodCode.AddLine("//Binding path: " + path);
			methodCode.AddLine(datasourceVariableNameOfPath + ".PropertyChanged += ViewModelPropertyChangedListenerOf" + datasourceVariableNameOfPath + ";");
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, string.Empty, true);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Bool", true);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Int", true);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Float", true);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "UInt", true);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Color", true);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Double", true);
			UICodeGenerationDatabindingVariantExtension.AddDataSourcePropertyChangedMethod(methodCode, datasourceVariableNameOfPath, "Vec2", true);
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension2 in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				foreach (GeneratedBindDataInfo generatedBindDataInfo in widgetCodeGenerationInfoDatabindingExtension2.BindDataInfos.Values)
				{
					if (generatedBindDataInfo.ViewModelPropertType == null)
					{
						methodCode.AddLine("//Couldn't find property in ViewModel");
						methodCode.AddLine(string.Concat(new string[]
						{
							"//",
							widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName,
							".",
							generatedBindDataInfo.Property,
							" = ",
							datasourceVariableNameOfPath,
							".",
							generatedBindDataInfo.Path,
							";"
						}));
					}
					else
					{
						if (generatedBindDataInfo.RequiresConversion)
						{
							methodCode.AddLine("//Requires conversion");
						}
						string[] lines = this.CreateAssignmentLines(datasourceVariableNameOfPath + "." + generatedBindDataInfo.Path, generatedBindDataInfo.ViewModelPropertType, widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName + "." + generatedBindDataInfo.Property, generatedBindDataInfo.WidgetPropertyType);
						methodCode.AddLines(lines);
					}
				}
				if (widgetCodeGenerationInfoDatabindingExtension2.CheckIfRequiresDataComponentForWidget())
				{
					methodCode.AddLine("{");
					methodCode.AddLine("//Requires component data assignment");
					methodCode.AddLine("var widgetComponent = " + widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName + ".GetComponent<TaleWorlds.GauntletUI.Data.GeneratedWidgetData>();");
					methodCode.AddLine("widgetComponent.Data = " + datasourceVariableNameOfPath + ";");
					methodCode.AddLine("}");
				}
				if (widgetCodeGenerationInfoDatabindingExtension2.BindCommandInfos.Values.Count > 0)
				{
					string str2 = "EventListenerOf" + widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName;
					methodCode.AddLine(widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName + ".EventFire += " + str2 + ";");
				}
				if (widgetCodeGenerationInfoDatabindingExtension2.BindDataInfos.Values.Count > 0)
				{
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension2, string.Empty, true);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "bool", true);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "float", true);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "Vec2", true);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "Vector2", true);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "double", true);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "int", true);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "uint", true);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedMethod(methodCode, widgetCodeGenerationInfoDatabindingExtension2, "Color", true);
				}
			}
			IL_4D7:
			foreach (BindingPathTargetDetails bindingPathTargetDetails2 in bindingPathTargetDetails.Children)
			{
				this.FillRefreshDataSourceMethodAssignSection(bindingPathTargetDetails2, methodCode);
			}
			foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension3 in bindingPathTargetDetails.WidgetDatabindingInformations)
			{
				bool flag2 = widgetCodeGenerationInfoDatabindingExtension3.WidgetCodeGenerationInfo.WidgetFactory.IsBuiltinType(widgetCodeGenerationInfoDatabindingExtension3.WidgetCodeGenerationInfo.WidgetTemplate.Type);
				bool isRoot2 = widgetCodeGenerationInfoDatabindingExtension3.IsRoot;
				if (!flag2 && !isRoot2)
				{
					methodCode.AddLine(widgetCodeGenerationInfoDatabindingExtension3.WidgetCodeGenerationInfo.VariableName + ".SetDataSource(" + datasourceVariableNameOfPath + ");");
				}
			}
			methodCode.AddLine("}");
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x000061DC File Offset: 0x000043DC
		private void FillRefreshDataSourceMethod(BindingPathTargetDetails bindingPathTargetDetails, MethodCode methodCode)
		{
			string datasourceVariableNameOfPath = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPathTargetDetails.BindingPath);
			methodCode.AddLine("//Clear Section");
			this.FillRefreshDataSourceMethodClearSection(bindingPathTargetDetails, methodCode, false);
			methodCode.AddLine("");
			methodCode.AddLine(datasourceVariableNameOfPath + " = newDataSource; ");
			methodCode.AddLine("");
			methodCode.AddLine("//Assign Section");
			this.FillRefreshDataSourceMethodAssignSection(bindingPathTargetDetails, methodCode);
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00006244 File Offset: 0x00004444
		private void CreateRefreshDataSourceMethod(ClassCode classCode)
		{
			foreach (BindingPathTargetDetails bindingPathTargetDetails in this._bindingPathTargetDetailsList)
			{
				BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
				string datasourceVariableNameOfPath = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPath);
				string viewModelCodeWriteableTypeAtPath = this.GetViewModelCodeWriteableTypeAtPath(bindingPath);
				MethodCode methodCode = new MethodCode();
				methodCode.Name = "RefreshDataSource" + datasourceVariableNameOfPath;
				methodCode.MethodSignature = "(" + viewModelCodeWriteableTypeAtPath + " newDataSource)";
				methodCode.AccessModifier = MethodCodeAccessModifier.Private;
				this.FillRefreshDataSourceMethod(bindingPathTargetDetails, methodCode);
				classCode.AddMethod(methodCode);
			}
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x000062F4 File Offset: 0x000044F4
		private string[] CreateAssignmentLines(string sourceVariable, Type sourceType, string targetVariable, Type targetType)
		{
			List<string> list = new List<string>();
			if (sourceType != targetType)
			{
				if (sourceType.IsEnum)
				{
					if (targetType == typeof(int))
					{
						string item = string.Concat(new string[]
						{
							targetVariable,
							" = (",
							WidgetTemplateGenerateContext.GetCodeFileType(targetType),
							")",
							sourceVariable,
							";"
						});
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

		// Token: 0x060000D6 RID: 214 RVA: 0x0000654C File Offset: 0x0000474C
		private void CreateEventMethods(ClassCode classCode)
		{
			Dictionary<WidgetCodeGenerationInfoDatabindingExtension, MethodCode> dictionary = new Dictionary<WidgetCodeGenerationInfoDatabindingExtension, MethodCode>();
			foreach (KeyValuePair<BindingPath, List<WidgetCodeGenerationInfoDatabindingExtension>> keyValuePair in this._extensionsWithPath)
			{
				BindingPath key = keyValuePair.Key;
				foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension in keyValuePair.Value)
				{
					foreach (GeneratedBindCommandInfo generatedBindCommandInfo in widgetCodeGenerationInfoDatabindingExtension.BindCommandInfos.Values)
					{
						BindingPath bindingPath = new BindingPath(generatedBindCommandInfo.Path);
						string lastNode = bindingPath.LastNode;
						BindingPath bindingPath2 = key;
						if (bindingPath.Nodes.Length > 1)
						{
							bindingPath2 = key.Append(bindingPath.ParentPath).Simplify();
						}
						string datasourceVariableNameOfPath = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPath2);
						if (!dictionary.ContainsKey(widgetCodeGenerationInfoDatabindingExtension))
						{
							MethodCode methodCode = new MethodCode();
							dictionary.Add(widgetCodeGenerationInfoDatabindingExtension, methodCode);
							methodCode.Name = "EventListenerOf" + widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName;
							methodCode.MethodSignature = "(TaleWorlds.GauntletUI.BaseTypes.Widget widget, System.String commandName, System.Object[] args)";
							methodCode.AccessModifier = MethodCodeAccessModifier.Private;
							classCode.AddMethod(methodCode);
						}
						MethodCode methodCode2 = dictionary[widgetCodeGenerationInfoDatabindingExtension];
						methodCode2.AddLine("if (commandName == \"" + generatedBindCommandInfo.Command + "\")");
						methodCode2.AddLine("{");
						if (generatedBindCommandInfo.Method == null)
						{
							methodCode2.AddLine("//Couldn't find method " + lastNode + " for action");
							methodCode2.AddLine(string.Concat(new string[] { "//", datasourceVariableNameOfPath, ".", lastNode, "();" }));
						}
						else
						{
							for (int i = 0; i < generatedBindCommandInfo.ParameterCount; i++)
							{
								if (i + 1 != generatedBindCommandInfo.ParameterCount || !generatedBindCommandInfo.GotParameter)
								{
									GeneratedBindCommandParameterInfo generatedBindCommandParameterInfo = generatedBindCommandInfo.MethodParameters[i];
									string fullName = generatedBindCommandParameterInfo.Type.FullName;
									if (generatedBindCommandParameterInfo.IsViewModel)
									{
										methodCode2.AddLine(string.Concat(new object[] { fullName, " arg", i, " = null;" }));
										methodCode2.AddLine("{");
										methodCode2.AddLine(string.Concat(new object[] { "var arg", i, "_widget = (TaleWorlds.GauntletUI.BaseTypes.Widget)args[", i, "];" }));
										methodCode2.AddLine(string.Concat(new object[] { "var arg", i, "_widget_data = arg", i, "_widget.GetComponent<TaleWorlds.GauntletUI.Data.GeneratedWidgetData>();" }));
										methodCode2.AddLine("if (arg" + i + "_widget_data != null)");
										methodCode2.AddLine("{");
										methodCode2.AddLine(string.Concat(new object[] { "arg", i, " = (", fullName, ")arg", i, "_widget_data.Data;" }));
										methodCode2.AddLine("}");
										methodCode2.AddLine("}");
									}
									else
									{
										methodCode2.AddLine(string.Concat(new object[] { "var arg", i, " = (", fullName, ")args[", i, "];" }));
									}
								}
							}
							if (generatedBindCommandInfo.GotParameter)
							{
								GeneratedBindCommandParameterInfo generatedBindCommandParameterInfo2 = generatedBindCommandInfo.MethodParameters[generatedBindCommandInfo.ParameterCount - 1];
								methodCode2.AddLine("//GotParameter " + generatedBindCommandInfo.Parameter);
								if (generatedBindCommandParameterInfo2.Type == typeof(int))
								{
									methodCode2.AddLine(string.Concat(new object[]
									{
										"var arg",
										generatedBindCommandInfo.ParameterCount - 1,
										" = ",
										generatedBindCommandInfo.Parameter,
										";"
									}));
								}
								else if (generatedBindCommandParameterInfo2.Type == typeof(float))
								{
									methodCode2.AddLine(string.Concat(new object[]
									{
										"var arg",
										generatedBindCommandInfo.ParameterCount - 1,
										" = ",
										generatedBindCommandInfo.Parameter,
										"f;"
									}));
								}
								else
								{
									methodCode2.AddLine(string.Concat(new object[]
									{
										"var arg",
										generatedBindCommandInfo.ParameterCount - 1,
										" = \"",
										generatedBindCommandInfo.Parameter,
										"\";"
									}));
								}
							}
							if (generatedBindCommandInfo.ParameterCount > 0)
							{
								string text = datasourceVariableNameOfPath + "." + lastNode + "(";
								for (int j = 0; j < generatedBindCommandInfo.ParameterCount; j++)
								{
									text = text + "arg" + j;
									if (j + 1 < generatedBindCommandInfo.ParameterCount)
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

		// Token: 0x060000D7 RID: 215 RVA: 0x00006B58 File Offset: 0x00004D58
		private void CreateWidgetPropertyChangedMethods(ClassCode classCode)
		{
			Dictionary<WidgetCodeGenerationInfoDatabindingExtension, MethodCode> dictionary = new Dictionary<WidgetCodeGenerationInfoDatabindingExtension, MethodCode>();
			foreach (BindingPathTargetDetails bindingPathTargetDetails in this._bindingPathTargetDetailsList)
			{
				BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
				string datasourceVariableNameOfPath = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPath);
				Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(this._dataSourceType, bindingPath);
				foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension in bindingPathTargetDetails.WidgetDatabindingInformations)
				{
					foreach (GeneratedBindDataInfo generatedBindDataInfo in widgetCodeGenerationInfoDatabindingExtension.BindDataInfos.Values)
					{
						if (!dictionary.ContainsKey(widgetCodeGenerationInfoDatabindingExtension))
						{
							UICodeGenerationDatabindingVariantExtension.AddWidgetPropertyChangedWithValueVariant(classCode, widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName, string.Empty);
							UICodeGenerationDatabindingVariantExtension.AddWidgetPropertyChangedWithValueVariant(classCode, widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName, "bool");
							UICodeGenerationDatabindingVariantExtension.AddWidgetPropertyChangedWithValueVariant(classCode, widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName, "float");
							UICodeGenerationDatabindingVariantExtension.AddWidgetPropertyChangedWithValueVariant(classCode, widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName, "Vec2");
							UICodeGenerationDatabindingVariantExtension.AddWidgetPropertyChangedWithValueVariant(classCode, widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName, "Vector2");
							UICodeGenerationDatabindingVariantExtension.AddWidgetPropertyChangedWithValueVariant(classCode, widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName, "double");
							UICodeGenerationDatabindingVariantExtension.AddWidgetPropertyChangedWithValueVariant(classCode, widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName, "int");
							UICodeGenerationDatabindingVariantExtension.AddWidgetPropertyChangedWithValueVariant(classCode, widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName, "uint");
							UICodeGenerationDatabindingVariantExtension.AddWidgetPropertyChangedWithValueVariant(classCode, widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName, "Color");
							string variableName = widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName;
							MethodCode methodCode = new MethodCode
							{
								Name = "HandleWidgetPropertyChangeOf" + variableName,
								MethodSignature = "(System.String propertyName)",
								AccessModifier = MethodCodeAccessModifier.Private
							};
							dictionary.Add(widgetCodeGenerationInfoDatabindingExtension, methodCode);
							classCode.AddMethod(methodCode);
						}
						MethodCode methodCode2 = dictionary[widgetCodeGenerationInfoDatabindingExtension];
						methodCode2.AddLine("if (propertyName == \"" + generatedBindDataInfo.Property + "\")");
						methodCode2.AddLine("{");
						if (generatedBindDataInfo.ViewModelPropertType == null)
						{
							methodCode2.AddLine("//Couldn't find property in ViewModel");
							methodCode2.AddLine(string.Concat(new string[]
							{
								"//",
								datasourceVariableNameOfPath,
								".",
								generatedBindDataInfo.Path,
								" = ",
								widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName,
								".",
								generatedBindDataInfo.Property,
								";"
							}));
						}
						else if (VariableCollection.GetPropertyInfo(viewModelTypeAtPath, generatedBindDataInfo.Path).GetSetMethod() == null)
						{
							methodCode2.AddLine("//Property in ViewModel does not have a set method");
							methodCode2.AddLine(string.Concat(new string[]
							{
								"//",
								datasourceVariableNameOfPath,
								".",
								generatedBindDataInfo.Path,
								" = ",
								widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName,
								".",
								generatedBindDataInfo.Property,
								";"
							}));
						}
						else
						{
							string[] lines = this.CreateAssignmentLines(widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName + "." + generatedBindDataInfo.Property, generatedBindDataInfo.WidgetPropertyType, datasourceVariableNameOfPath + "." + generatedBindDataInfo.Path, generatedBindDataInfo.ViewModelPropertType);
							methodCode2.AddLines(lines);
						}
						methodCode2.AddLine("return;");
						methodCode2.AddLine("}");
					}
				}
			}
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x00006F48 File Offset: 0x00005148
		private void CreateViewModelPropertyChangedMethods(ClassCode classCode)
		{
			foreach (BindingPathTargetDetails bindingPathTargetDetails in this._bindingPathTargetDetailsList)
			{
				BindingPath bindingPath = bindingPathTargetDetails.BindingPath;
				string datasourceVariableNameOfPath = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPath);
				Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(this._dataSourceType, bindingPath);
				if (!typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath))
				{
					MethodCode methodCode = new MethodCode();
					methodCode.Name = "ViewModelPropertyChangedListenerOf" + datasourceVariableNameOfPath;
					methodCode.MethodSignature = "(System.Object sender, System.ComponentModel.PropertyChangedEventArgs e)";
					methodCode.AccessModifier = MethodCodeAccessModifier.Private;
					methodCode.AddLine("HandleViewModelPropertyChangeOf" + datasourceVariableNameOfPath + "(e.PropertyName);");
					classCode.AddMethod(methodCode);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, string.Empty);
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Bool");
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Int");
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Float");
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "UInt");
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Color");
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Double");
					UICodeGenerationDatabindingVariantExtension.AddPropertyChangedWithValueVariant(classCode, datasourceVariableNameOfPath, "Vec2");
					MethodCode methodCode2 = new MethodCode();
					methodCode2.Name = "HandleViewModelPropertyChangeOf" + datasourceVariableNameOfPath;
					methodCode2.MethodSignature = "(System.String propertyName)";
					methodCode2.AccessModifier = MethodCodeAccessModifier.Private;
					classCode.AddMethod(methodCode2);
					methodCode2.AddLine("//DataSource property section");
					foreach (BindingPathTargetDetails bindingPathTargetDetails2 in bindingPathTargetDetails.Children)
					{
						string datasourceVariableNameOfPath2 = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(bindingPathTargetDetails2.BindingPath);
						string lastNode = bindingPathTargetDetails2.BindingPath.LastNode;
						methodCode2.AddLine("if (propertyName == \"" + lastNode + "\")");
						methodCode2.AddLine("{");
						methodCode2.AddLine(string.Concat(new string[] { "RefreshDataSource", datasourceVariableNameOfPath2, "(", datasourceVariableNameOfPath, ".", lastNode, ");" }));
						methodCode2.AddLine("return;");
						methodCode2.AddLine("}");
					}
					Dictionary<string, List<WidgetCodeGenerationInfoDatabindingExtension>> dictionary = new Dictionary<string, List<WidgetCodeGenerationInfoDatabindingExtension>>();
					foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension in bindingPathTargetDetails.WidgetDatabindingInformations)
					{
						foreach (GeneratedBindDataInfo generatedBindDataInfo in widgetCodeGenerationInfoDatabindingExtension.BindDataInfos.Values)
						{
							if (!dictionary.ContainsKey(generatedBindDataInfo.Path))
							{
								dictionary.Add(generatedBindDataInfo.Path, new List<WidgetCodeGenerationInfoDatabindingExtension>());
							}
							if (!dictionary[generatedBindDataInfo.Path].Contains(widgetCodeGenerationInfoDatabindingExtension))
							{
								dictionary[generatedBindDataInfo.Path].Add(widgetCodeGenerationInfoDatabindingExtension);
							}
						}
					}
					methodCode2.AddLine("//Primitive property section");
					foreach (KeyValuePair<string, List<WidgetCodeGenerationInfoDatabindingExtension>> keyValuePair in dictionary)
					{
						string key = keyValuePair.Key;
						List<WidgetCodeGenerationInfoDatabindingExtension> value = keyValuePair.Value;
						methodCode2.AddLine("if (propertyName == \"" + key + "\")");
						methodCode2.AddLine("{");
						foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension2 in value)
						{
							foreach (GeneratedBindDataInfo generatedBindDataInfo2 in widgetCodeGenerationInfoDatabindingExtension2.BindDataInfos.Values)
							{
								if (generatedBindDataInfo2.Path == key)
								{
									if (generatedBindDataInfo2.ViewModelPropertType == null)
									{
										methodCode2.AddLine("//Couldn't find property in ViewModel");
										methodCode2.AddLine(string.Concat(new string[]
										{
											"//",
											widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName,
											".",
											generatedBindDataInfo2.Property,
											" = ",
											datasourceVariableNameOfPath,
											".",
											generatedBindDataInfo2.Path,
											";"
										}));
									}
									else
									{
										if (generatedBindDataInfo2.RequiresConversion)
										{
											methodCode2.AddLine("//Requires conversion");
										}
										string[] lines = this.CreateAssignmentLines(datasourceVariableNameOfPath + "." + generatedBindDataInfo2.Path, generatedBindDataInfo2.ViewModelPropertType, widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName + "." + generatedBindDataInfo2.Property, generatedBindDataInfo2.WidgetPropertyType);
										methodCode2.AddLines(lines);
									}
								}
							}
						}
						methodCode2.AddLine("return;");
						methodCode2.AddLine("}");
					}
				}
			}
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x000074D4 File Offset: 0x000056D4
		private static void AddPropertyChangedWithValueVariant(ClassCode classCode, string dataSourceVariableName, string typeVariant)
		{
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "ViewModelPropertyChangedWith" + typeVariant + "ValueListenerOf" + dataSourceVariableName;
			methodCode.MethodSignature = "(System.Object sender, TaleWorlds.Library.PropertyChangedWith" + typeVariant + "ValueEventArgs e)";
			methodCode.AccessModifier = MethodCodeAccessModifier.Private;
			methodCode.AddLine("HandleViewModelPropertyChangeOf" + dataSourceVariableName + "(e.PropertyName);");
			classCode.AddMethod(methodCode);
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00007538 File Offset: 0x00005738
		private void CreateListChangedMethods(ClassCode classCode)
		{
			foreach (KeyValuePair<BindingPath, List<WidgetCodeGenerationInfoDatabindingExtension>> keyValuePair in this._extensionsWithPath)
			{
				List<WidgetCodeGenerationInfoDatabindingExtension> value = keyValuePair.Value;
				BindingPath key = keyValuePair.Key;
				string datasourceVariableNameOfPath = UICodeGenerationDatabindingVariantExtension.GetDatasourceVariableNameOfPath(key);
				Type viewModelTypeAtPath = WidgetCodeGenerationInfoDatabindingExtension.GetViewModelTypeAtPath(this._dataSourceType, key);
				if (typeof(IMBBindingList).IsAssignableFrom(viewModelTypeAtPath))
				{
					MethodCode methodCode = new MethodCode();
					methodCode.Name = "OnList" + datasourceVariableNameOfPath + "Changed";
					methodCode.MethodSignature = "(System.Object sender, TaleWorlds.Library.ListChangedEventArgs e)";
					methodCode.AddLine("switch (e.ListChangedType)");
					methodCode.AddLine("{");
					methodCode.AddLine("case TaleWorlds.Library.ListChangedType.Reset:");
					methodCode.AddLine("{");
					foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension in value)
					{
						if (widgetCodeGenerationInfoDatabindingExtension.ItemTemplateCodeGenerationInfo != null)
						{
							methodCode.AddLine("for (var i = " + widgetCodeGenerationInfoDatabindingExtension.WidgetCodeGenerationInfo.VariableName + ".ChildCount - 1; i >= 0; i--)");
							methodCode.AddLine("{");
							this.AddBindingListItemBeforeDeletionSection(methodCode, widgetCodeGenerationInfoDatabindingExtension, "i", false);
							this.AddBindingListItemDeletionSection(methodCode, widgetCodeGenerationInfoDatabindingExtension, "i", false);
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
					foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension2 in value)
					{
						if (widgetCodeGenerationInfoDatabindingExtension2.ItemTemplateCodeGenerationInfo != null)
						{
							methodCode.AddLine("{");
							methodCode.AddLine("var target = " + widgetCodeGenerationInfoDatabindingExtension2.WidgetCodeGenerationInfo.VariableName + ".FindChild(widget => widget.GetComponent<TaleWorlds.GauntletUI.Data.GeneratedWidgetData>().Data == bindingObject);");
							methodCode.AddLine("target.SetSiblingIndex(i);");
							methodCode.AddLine("}");
						}
					}
					methodCode.AddLine("}");
					methodCode.AddLine("}");
					methodCode.AddLine("break;");
					methodCode.AddLine("case TaleWorlds.Library.ListChangedType.ItemAdded:");
					methodCode.AddLine("{");
					foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension3 in value)
					{
						if (widgetCodeGenerationInfoDatabindingExtension3.ItemTemplateCodeGenerationInfo != null)
						{
							this.AddBindingListItemCreationSection(methodCode, widgetCodeGenerationInfoDatabindingExtension3, datasourceVariableNameOfPath, "e.NewIndex");
						}
					}
					methodCode.AddLine("}");
					methodCode.AddLine("break;");
					methodCode.AddLine("case TaleWorlds.Library.ListChangedType.ItemBeforeDeleted:");
					methodCode.AddLine("{");
					foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension4 in value)
					{
						if (widgetCodeGenerationInfoDatabindingExtension4.ItemTemplateCodeGenerationInfo != null)
						{
							methodCode.AddLine("{");
							this.AddBindingListItemBeforeDeletionSection(methodCode, widgetCodeGenerationInfoDatabindingExtension4, "e.NewIndex", false);
							methodCode.AddLine("}");
						}
					}
					methodCode.AddLine("}");
					methodCode.AddLine("break;");
					methodCode.AddLine("case TaleWorlds.Library.ListChangedType.ItemDeleted:");
					methodCode.AddLine("{");
					foreach (WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension5 in value)
					{
						if (widgetCodeGenerationInfoDatabindingExtension5.ItemTemplateCodeGenerationInfo != null)
						{
							methodCode.AddLine("{");
							this.AddBindingListItemDeletionSection(methodCode, widgetCodeGenerationInfoDatabindingExtension5, "e.NewIndex", false);
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
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00007A20 File Offset: 0x00005C20
		private BindingPathTargetDetails GetRootBindingPathTargetDetails()
		{
			foreach (BindingPathTargetDetails bindingPathTargetDetails in this._bindingPathTargetDetailsList)
			{
				if (bindingPathTargetDetails.IsRoot)
				{
					return bindingPathTargetDetails;
				}
			}
			return null;
		}

		// Token: 0x060000DC RID: 220 RVA: 0x00007A7C File Offset: 0x00005C7C
		private BindingPathTargetDetails GetBindingPathTargetDetails(BindingPath bindingPath)
		{
			foreach (BindingPathTargetDetails bindingPathTargetDetails in this._bindingPathTargetDetailsList)
			{
				if (bindingPathTargetDetails.BindingPath == bindingPath)
				{
					return bindingPathTargetDetails;
				}
			}
			return null;
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00007AE0 File Offset: 0x00005CE0
		private void FindBindingPathTargetDetails()
		{
			this._bindingPathTargetDetailsList = new List<BindingPathTargetDetails>();
			foreach (WidgetCodeGenerationInfo widgetCodeGenerationInfo in this._widgetTemplateGenerateContext.WidgetCodeGenerationInformations)
			{
				BindingPath fullBindingPath = ((WidgetCodeGenerationInfoDatabindingExtension)widgetCodeGenerationInfo.Extension).FullBindingPath;
				if (this.GetBindingPathTargetDetails(fullBindingPath) == null)
				{
					BindingPathTargetDetails item = new BindingPathTargetDetails(fullBindingPath);
					this._bindingPathTargetDetailsList.Add(item);
				}
			}
			for (int i = 0; i < this._bindingPathTargetDetailsList.Count; i++)
			{
				BindingPathTargetDetails bindingPathTargetDetails = this._bindingPathTargetDetailsList[i];
				if (!bindingPathTargetDetails.IsRoot)
				{
					BindingPath parentPath = bindingPathTargetDetails.BindingPath.ParentPath;
					BindingPathTargetDetails bindingPathTargetDetails2 = this.GetBindingPathTargetDetails(parentPath);
					if (bindingPathTargetDetails2 == null)
					{
						bindingPathTargetDetails2 = new BindingPathTargetDetails(parentPath);
						this._bindingPathTargetDetailsList.Add(bindingPathTargetDetails2);
					}
					bindingPathTargetDetails.SetParent(bindingPathTargetDetails2);
				}
			}
		}

		// Token: 0x060000DE RID: 222 RVA: 0x00007BD0 File Offset: 0x00005DD0
		public override void DoExtraCodeGeneration(ClassCode classCode)
		{
			this.FindBindingPathTargetDetails();
			this._extensions = new List<WidgetCodeGenerationInfoDatabindingExtension>();
			this._extensionsWithPath = new Dictionary<BindingPath, List<WidgetCodeGenerationInfoDatabindingExtension>>();
			foreach (WidgetCodeGenerationInfo widgetCodeGenerationInfo in this._widgetTemplateGenerateContext.WidgetCodeGenerationInformations)
			{
				WidgetCodeGenerationInfoDatabindingExtension widgetCodeGenerationInfoDatabindingExtension = (WidgetCodeGenerationInfoDatabindingExtension)widgetCodeGenerationInfo.Extension;
				this._extensions.Add(widgetCodeGenerationInfoDatabindingExtension);
				BindingPath fullBindingPath = widgetCodeGenerationInfoDatabindingExtension.FullBindingPath;
				this.GetBindingPathTargetDetails(fullBindingPath).WidgetDatabindingInformations.Add(widgetCodeGenerationInfoDatabindingExtension);
				if (!this._extensionsWithPath.ContainsKey(fullBindingPath))
				{
					this._extensionsWithPath.Add(fullBindingPath, new List<WidgetCodeGenerationInfoDatabindingExtension>());
				}
				this._extensionsWithPath[fullBindingPath].Add(widgetCodeGenerationInfoDatabindingExtension);
			}
			if (this._widgetTemplateGenerateContext.ContextType == WidgetTemplateGenerateContextType.RootPrefab)
			{
				classCode.InheritedInterfaces.Add("TaleWorlds.GauntletUI.Data.IGeneratedGauntletMovieRoot");
				this.CreateRefreshBindingWithChildrenMethod(classCode);
			}
			this.CreateDestroyDataSourceyMethod(classCode);
			this.CreateSetDataSourceVariables(classCode);
			this.CreateSetDataSourceMethod(classCode);
			this.CreateEventMethods(classCode);
			this.CreateWidgetPropertyChangedMethods(classCode);
			this.CreateViewModelPropertyChangedMethods(classCode);
			this.CreateListChangedMethods(classCode);
			this.CreateRefreshDataSourceMethod(classCode);
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00007CFC File Offset: 0x00005EFC
		public override void AddExtrasToCreatorMethod(MethodCode methodCode)
		{
			methodCode.AddLine("var movie = new TaleWorlds.GauntletUI.Data.GeneratedGauntletMovie(\"" + this._widgetTemplateGenerateContext.PrefabName + "\", widget);");
			methodCode.AddLine("var dataSource = data[\"DataSource\"];");
			methodCode.AddLine("widget.SetDataSource((" + this._dataSourceType.FullName + ")dataSource);");
			methodCode.AddLine("result.AddData(\"Movie\", movie);");
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00007D5F File Offset: 0x00005F5F
		public override WidgetCodeGenerationInfoExtension CreateWidgetCodeGenerationInfoExtension(WidgetCodeGenerationInfo widgetCodeGenerationInfo)
		{
			return new WidgetCodeGenerationInfoDatabindingExtension(widgetCodeGenerationInfo);
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00007D68 File Offset: 0x00005F68
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

		// Token: 0x060000E2 RID: 226 RVA: 0x00007DDC File Offset: 0x00005FDC
		private static void AddPropertyChangedMethod(MethodCode methodCode, WidgetCodeGenerationInfoDatabindingExtension extension, string typeName, bool add)
		{
			methodCode.AddLine(string.Concat(new string[]
			{
				extension.WidgetCodeGenerationInfo.VariableName,
				".",
				typeName,
				"PropertyChanged ",
				add ? "+" : "-",
				"= ",
				typeName,
				"PropertyChangedListenerOf",
				extension.WidgetCodeGenerationInfo.VariableName,
				";"
			}));
		}

		// Token: 0x0400004F RID: 79
		private WidgetTemplateGenerateContext _widgetTemplateGenerateContext;

		// Token: 0x04000050 RID: 80
		private Type _dataSourceType;

		// Token: 0x04000051 RID: 81
		private List<WidgetCodeGenerationInfoDatabindingExtension> _extensions;

		// Token: 0x04000052 RID: 82
		private Dictionary<BindingPath, List<WidgetCodeGenerationInfoDatabindingExtension>> _extensionsWithPath;

		// Token: 0x04000053 RID: 83
		private List<BindingPathTargetDetails> _bindingPathTargetDetailsList;
	}
}
