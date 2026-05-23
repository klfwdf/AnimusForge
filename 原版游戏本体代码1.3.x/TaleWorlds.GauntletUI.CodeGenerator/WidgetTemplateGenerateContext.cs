using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library.CodeGeneration;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x0200000D RID: 13
	public class WidgetTemplateGenerateContext
	{
		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000080 RID: 128 RVA: 0x000039B3 File Offset: 0x00001BB3
		// (set) Token: 0x06000081 RID: 129 RVA: 0x000039BB File Offset: 0x00001BBB
		public WidgetFactory WidgetFactory { get; private set; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000082 RID: 130 RVA: 0x000039C4 File Offset: 0x00001BC4
		// (set) Token: 0x06000083 RID: 131 RVA: 0x000039CC File Offset: 0x00001BCC
		public UICodeGenerationContext UICodeGenerationContext { get; private set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000084 RID: 132 RVA: 0x000039D5 File Offset: 0x00001BD5
		// (set) Token: 0x06000085 RID: 133 RVA: 0x000039DD File Offset: 0x00001BDD
		public VariableCollection VariableCollection { get; private set; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000086 RID: 134 RVA: 0x000039E6 File Offset: 0x00001BE6
		// (set) Token: 0x06000087 RID: 135 RVA: 0x000039EE File Offset: 0x00001BEE
		public string PrefabName { get; private set; }

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000088 RID: 136 RVA: 0x000039F7 File Offset: 0x00001BF7
		// (set) Token: 0x06000089 RID: 137 RVA: 0x000039FF File Offset: 0x00001BFF
		public string VariantName { get; private set; }

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x0600008A RID: 138 RVA: 0x00003A08 File Offset: 0x00001C08
		// (set) Token: 0x0600008B RID: 139 RVA: 0x00003A10 File Offset: 0x00001C10
		public string ClassName { get; private set; }

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x0600008C RID: 140 RVA: 0x00003A19 File Offset: 0x00001C19
		// (set) Token: 0x0600008D RID: 141 RVA: 0x00003A21 File Offset: 0x00001C21
		public Dictionary<string, object> Data { get; private set; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x0600008E RID: 142 RVA: 0x00003A2A File Offset: 0x00001C2A
		// (set) Token: 0x0600008F RID: 143 RVA: 0x00003A32 File Offset: 0x00001C32
		public List<WidgetCodeGenerationInfo> WidgetCodeGenerationInformations { get; private set; }

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x06000090 RID: 144 RVA: 0x00003A3B File Offset: 0x00001C3B
		// (set) Token: 0x06000091 RID: 145 RVA: 0x00003A43 File Offset: 0x00001C43
		public PrefabDependencyContext PrefabDependencyContext { get; private set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x06000092 RID: 146 RVA: 0x00003A4C File Offset: 0x00001C4C
		// (set) Token: 0x06000093 RID: 147 RVA: 0x00003A54 File Offset: 0x00001C54
		public WidgetTemplateGenerateContextType ContextType { get; private set; }

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x06000094 RID: 148 RVA: 0x00003A5D File Offset: 0x00001C5D
		// (set) Token: 0x06000095 RID: 149 RVA: 0x00003A65 File Offset: 0x00001C65
		public bool GotLogicalChildrenLocation { get; private set; }

		// Token: 0x06000096 RID: 150 RVA: 0x00003A70 File Offset: 0x00001C70
		private WidgetTemplateGenerateContext(UICodeGenerationContext uiCodeGenerationContext, PrefabDependencyContext prefabDependencyContext, WidgetTemplateGenerateContextType contextType, string className)
		{
			this.UICodeGenerationContext = uiCodeGenerationContext;
			this.WidgetFactory = this.UICodeGenerationContext.WidgetFactory;
			BrushFactory brushFactory = this.UICodeGenerationContext.BrushFactory;
			SpriteData spriteData = this.UICodeGenerationContext.SpriteData;
			this.VariableCollection = new VariableCollection(this.WidgetFactory, brushFactory, spriteData);
			this.WidgetCodeGenerationInformations = new List<WidgetCodeGenerationInfo>();
			this.ClassName = className;
			this.PrefabDependencyContext = prefabDependencyContext;
			this.ContextType = contextType;
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00003AE8 File Offset: 0x00001CE8
		public static WidgetTemplateGenerateContext CreateAsRoot(UICodeGenerationContext uiCodeGenerationContext, string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data)
		{
			string useablePrefabClassName = WidgetTemplateGenerateContext.GetUseablePrefabClassName(prefabName, variantName);
			PrefabDependencyContext prefabDependencyContext = new PrefabDependencyContext(useablePrefabClassName);
			WidgetTemplateGenerateContext widgetTemplateGenerateContext = new WidgetTemplateGenerateContext(uiCodeGenerationContext, prefabDependencyContext, WidgetTemplateGenerateContextType.RootPrefab, useablePrefabClassName);
			widgetTemplateGenerateContext.PrepareAsPrefab(prefabName, variantName, variantExtension, data, new Dictionary<string, WidgetAttributeTemplate>());
			return widgetTemplateGenerateContext;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00003B20 File Offset: 0x00001D20
		public static WidgetTemplateGenerateContext CreateAsDependendPrefab(UICodeGenerationContext uiCodeGenerationContext, PrefabDependencyContext prefabDependencyContext, string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data, Dictionary<string, WidgetAttributeTemplate> givenParameters)
		{
			string className = prefabDependencyContext.GenerateDependencyName() + "_" + WidgetTemplateGenerateContext.GetUseablePrefabClassName(prefabName, variantName);
			WidgetTemplateGenerateContext widgetTemplateGenerateContext = new WidgetTemplateGenerateContext(uiCodeGenerationContext, prefabDependencyContext, WidgetTemplateGenerateContextType.DependendPrefab, className);
			widgetTemplateGenerateContext.PrepareAsPrefab(prefabName, variantName, variantExtension, data, givenParameters);
			prefabDependencyContext.AddDependentWidgetTemplateGenerateContext(widgetTemplateGenerateContext);
			return widgetTemplateGenerateContext;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00003B68 File Offset: 0x00001D68
		public static WidgetTemplateGenerateContext CreateAsInheritedDependendPrefab(UICodeGenerationContext uiCodeGenerationContext, PrefabDependencyContext prefabDependencyContext, string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data, Dictionary<string, WidgetAttributeTemplate> givenParameters)
		{
			string className = prefabDependencyContext.GenerateDependencyName() + "_" + WidgetTemplateGenerateContext.GetUseablePrefabClassName(prefabName, variantName);
			WidgetTemplateGenerateContext widgetTemplateGenerateContext = new WidgetTemplateGenerateContext(uiCodeGenerationContext, prefabDependencyContext, WidgetTemplateGenerateContextType.InheritedDependendPrefab, className);
			widgetTemplateGenerateContext.PrepareAsPrefab(prefabName, variantName, variantExtension, data, givenParameters);
			prefabDependencyContext.AddDependentWidgetTemplateGenerateContext(widgetTemplateGenerateContext);
			return widgetTemplateGenerateContext;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00003BB0 File Offset: 0x00001DB0
		public static WidgetTemplateGenerateContext CreateAsCustomWidgetTemplate(UICodeGenerationContext uiCodeGenerationContext, PrefabDependencyContext prefabDependencyContext, WidgetTemplate widgetTemplate, string identifierName, VariableCollection variableCollection, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data)
		{
			string className = prefabDependencyContext.GenerateDependencyName() + "_" + identifierName;
			WidgetTemplateGenerateContext widgetTemplateGenerateContext = new WidgetTemplateGenerateContext(uiCodeGenerationContext, prefabDependencyContext, WidgetTemplateGenerateContextType.CustomWidgetTemplate, className);
			widgetTemplateGenerateContext.PrepareAsWidgetTemplate(widgetTemplate, variableCollection, variantExtension, data);
			prefabDependencyContext.AddDependentWidgetTemplateGenerateContext(widgetTemplateGenerateContext);
			return widgetTemplateGenerateContext;
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00003BF0 File Offset: 0x00001DF0
		private void PrepareAsPrefab(string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data, Dictionary<string, WidgetAttributeTemplate> givenParameters)
		{
			this.PrefabName = prefabName;
			this.VariantName = variantName;
			this._variantExtension = variantExtension;
			this.Data = data;
			this.VariableCollection.SetGivenParameters(givenParameters);
			this._prefab = this.WidgetFactory.GetCustomType(this.PrefabName);
			this._rootWidgetTemplate = this._prefab.RootTemplate;
			this.GotLogicalChildrenLocation = this.FindLogicalChildrenLocation(this._rootWidgetTemplate) != null;
			if (this._variantExtension != null)
			{
				this._variantExtension.Initialize(this);
			}
			this.VariableCollection.FillFromPrefab(this._prefab);
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00003C89 File Offset: 0x00001E89
		private void PrepareAsWidgetTemplate(WidgetTemplate rootWidgetTemplate, VariableCollection variableCollection, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data)
		{
			this._rootWidgetTemplate = rootWidgetTemplate;
			this.Data = data;
			this._variantExtension = variantExtension;
			this.VariableCollection = variableCollection;
			if (this._variantExtension != null)
			{
				this._variantExtension.Initialize(this);
			}
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00003CBC File Offset: 0x00001EBC
		private WidgetTemplate FindLogicalChildrenLocation(WidgetTemplate template)
		{
			if (template.LogicalChildrenLocation)
			{
				return template;
			}
			for (int i = 0; i < template.ChildCount; i++)
			{
				WidgetTemplate childAt = template.GetChildAt(i);
				WidgetTemplate widgetTemplate = this.FindLogicalChildrenLocation(childAt);
				if (widgetTemplate != null)
				{
					return widgetTemplate;
				}
			}
			return null;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00003CFC File Offset: 0x00001EFC
		private CommentSection CreateCommentSection()
		{
			CommentSection commentSection = new CommentSection();
			foreach (KeyValuePair<string, object> keyValuePair in this.Data)
			{
				commentSection.AddCommentLine(string.Concat(new object[] { "Data: ", keyValuePair.Key, " - ", keyValuePair.Value }));
			}
			foreach (KeyValuePair<string, WidgetAttributeTemplate> keyValuePair2 in this.VariableCollection.GivenParameters)
			{
				WidgetAttributeTemplate value = keyValuePair2.Value;
				commentSection.AddCommentLine(string.Concat(new object[] { "Given Parameter: ", keyValuePair2.Key, " - ", value.Value, " ", value.KeyType, " ", value.ValueType }));
			}
			return commentSection;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00003E28 File Offset: 0x00002028
		public Type GetWidgetTypeWithinPrefabRoots(string typeName)
		{
			if (this.WidgetFactory.IsCustomType(typeName))
			{
				WidgetPrefab customType = this.WidgetFactory.GetCustomType(typeName);
				return this.GetWidgetTypeWithinPrefabRoots(customType.RootTemplate.Type);
			}
			return this.WidgetFactory.GetBuiltinType(typeName);
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00003E70 File Offset: 0x00002070
		public bool CheckIfInheritsAnotherPrefab()
		{
			string type = this._rootWidgetTemplate.Type;
			return this.WidgetFactory.IsCustomType(type);
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00003E98 File Offset: 0x00002098
		private void AddInheritedType(ClassCode classCode)
		{
			string type = this._rootWidgetTemplate.Type;
			string item;
			if (this.WidgetFactory.IsCustomType(type))
			{
				item = this._rootWidgetCodeGenerationInfo.ChildWidgetTemplateGenerateContext.ClassName;
			}
			else
			{
				item = this.WidgetFactory.GetBuiltinType(type).FullName;
			}
			classCode.InheritedInterfaces.Add(item);
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00003EF8 File Offset: 0x000020F8
		public void GenerateInto(NamespaceCode namespaceCode)
		{
			ClassCode classCode = new ClassCode();
			classCode.Name = this.ClassName;
			classCode.AccessModifier = ClassCodeAccessModifier.Public;
			if (this._variantExtension != null)
			{
				this._variantExtension.AddExtensionVariables(classCode);
			}
			this.CreateWidgetInformations(this._rootWidgetTemplate, "_widget", null);
			if (this.GotLogicalChildrenLocation)
			{
				this.GenerateAddChildToLogicalLocationMethod(classCode);
			}
			this.CheckDependencies();
			this.VariableCollection.FillVisualDefinitionCreators(classCode);
			this.FillWidgetVariables(classCode);
			this.GenerateCreateWidgetsMethod(classCode);
			this.GenerateSetIdsMethod(classCode);
			this.GenerateSetAttributesMethod(classCode);
			this.AddInheritedType(classCode);
			if (this._variantExtension != null)
			{
				this._variantExtension.DoExtraCodeGeneration(classCode);
			}
			classCode.AddConsturctor(new ConstructorCode
			{
				MethodSignature = "(TaleWorlds.GauntletUI.UIContext context)",
				BaseCall = "(context)"
			});
			classCode.CommentSection = this.CreateCommentSection();
			namespaceCode.AddClass(classCode);
			if (this.ContextType == WidgetTemplateGenerateContextType.RootPrefab)
			{
				this.PrefabDependencyContext.GenerateInto(namespaceCode);
			}
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00003FE8 File Offset: 0x000021E8
		private void GenerateAddChildToLogicalLocationMethod(ClassCode classCode)
		{
			WidgetTemplate widgetTemplate = this.FindLogicalChildrenLocation(this._rootWidgetTemplate);
			WidgetCodeGenerationInfo widgetCodeGenerationInfo = this.FindWidgetCodeGenerationInformation(widgetTemplate);
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "AddChildToLogicalLocation";
			methodCode.AccessModifier = MethodCodeAccessModifier.Public;
			methodCode.MethodSignature = "(TaleWorlds.GauntletUI.BaseTypes.Widget widget)";
			methodCode.AddLine(widgetCodeGenerationInfo.VariableName + ".AddChild(widget);");
			classCode.AddMethod(methodCode);
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x0000404C File Offset: 0x0000224C
		public MethodCode GenerateCreatorMethod()
		{
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "Create" + this.ClassName;
			methodCode.ReturnParameter = "TaleWorlds.GauntletUI.PrefabSystem.GeneratedPrefabInstantiationResult";
			methodCode.MethodSignature = "(TaleWorlds.GauntletUI.UIContext context, System.Collections.Generic.Dictionary<string, object> data)";
			methodCode.AddLine("var widget = new " + this.ClassName + "(context);");
			methodCode.AddLine("widget.CreateWidgets();");
			methodCode.AddLine("widget.SetIds();");
			methodCode.AddLine("widget.SetAttributes();");
			methodCode.AddLine("var result = new TaleWorlds.GauntletUI.PrefabSystem.GeneratedPrefabInstantiationResult(widget);");
			if (this._variantExtension != null)
			{
				this._variantExtension.AddExtrasToCreatorMethod(methodCode);
			}
			methodCode.AddLine("return result;");
			return methodCode;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x000022DC File Offset: 0x000004DC
		public static string GetUseableName(string name)
		{
			return name.Replace(".", "_");
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x000040F4 File Offset: 0x000022F4
		private static string GetUseablePrefabClassName(string name, string variantName)
		{
			string str = name.Replace(".", "_");
			string str2 = variantName.Replace(".", "_");
			return str + "__" + str2;
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00004130 File Offset: 0x00002330
		private WidgetCodeGenerationInfo FindWidgetCodeGenerationInformation(WidgetTemplate widgetTemplate)
		{
			foreach (WidgetCodeGenerationInfo widgetCodeGenerationInfo in this.WidgetCodeGenerationInformations)
			{
				if (widgetCodeGenerationInfo.WidgetTemplate == widgetTemplate)
				{
					return widgetCodeGenerationInfo;
				}
			}
			return null;
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x0000418C File Offset: 0x0000238C
		private void CreateWidgetInformations(WidgetTemplate widgetTemplate, string variableName, WidgetCodeGenerationInfo parent)
		{
			WidgetCodeGenerationInfo widgetCodeGenerationInfo = new WidgetCodeGenerationInfo(this, widgetTemplate, variableName, parent);
			this.WidgetCodeGenerationInformations.Add(widgetCodeGenerationInfo);
			if (parent != null)
			{
				parent.AddChild(widgetCodeGenerationInfo);
			}
			else
			{
				this._rootWidgetCodeGenerationInfo = widgetCodeGenerationInfo;
			}
			if (this._variantExtension != null)
			{
				WidgetCodeGenerationInfoExtension widgetCodeGenerationInfoExtension = this._variantExtension.CreateWidgetCodeGenerationInfoExtension(widgetCodeGenerationInfo);
				widgetCodeGenerationInfo.AddExtension(widgetCodeGenerationInfoExtension);
				widgetCodeGenerationInfoExtension.Initialize();
			}
			for (int i = 0; i < widgetTemplate.ChildCount; i++)
			{
				WidgetTemplate childAt = widgetTemplate.GetChildAt(i);
				string variableName2 = variableName + "_" + i;
				this.CreateWidgetInformations(childAt, variableName2, widgetCodeGenerationInfo);
			}
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x0000421C File Offset: 0x0000241C
		private void CheckDependencies()
		{
			foreach (WidgetCodeGenerationInfo widgetCodeGenerationInfo in this.WidgetCodeGenerationInformations)
			{
				widgetCodeGenerationInfo.CheckDependendType();
			}
		}

		// Token: 0x060000AA RID: 170 RVA: 0x0000426C File Offset: 0x0000246C
		private void FillWidgetVariables(ClassCode classCode)
		{
			foreach (WidgetCodeGenerationInfo widgetCodeGenerationInfo in this.WidgetCodeGenerationInformations)
			{
				VariableCode variableCode = widgetCodeGenerationInfo.CreateVariableCode();
				classCode.AddVariable(variableCode);
			}
		}

		// Token: 0x060000AB RID: 171 RVA: 0x000042C4 File Offset: 0x000024C4
		public bool IsBuiltinType(WidgetTemplate widgetTemplate)
		{
			return this.WidgetFactory.IsBuiltinType(widgetTemplate.Type);
		}

		// Token: 0x060000AC RID: 172 RVA: 0x000042D8 File Offset: 0x000024D8
		private void GenerateCreateWidgetsMethod(ClassCode classCode)
		{
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "CreateWidgets";
			classCode.AddMethod(methodCode);
			if (this.CheckIfInheritsAnotherPrefab())
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
				methodCode.AddLine("base.CreateWidgets();");
			}
			else if (this.ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
			}
			foreach (WidgetCodeGenerationInfo widgetCodeGenerationInfo in this.WidgetCodeGenerationInformations)
			{
				widgetCodeGenerationInfo.FillCreateWidgetsMethod(methodCode);
			}
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00004370 File Offset: 0x00002570
		private void GenerateSetIdsMethod(ClassCode classCode)
		{
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "SetIds";
			classCode.AddMethod(methodCode);
			if (this.CheckIfInheritsAnotherPrefab())
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
				methodCode.AddLine("base.SetIds();");
			}
			else if (this.ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
			}
			foreach (WidgetCodeGenerationInfo widgetCodeGenerationInfo in this.WidgetCodeGenerationInformations)
			{
				widgetCodeGenerationInfo.FillSetIdsMethod(methodCode);
			}
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00004408 File Offset: 0x00002608
		private void GenerateSetAttributesMethod(ClassCode classCode)
		{
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "SetAttributes";
			classCode.AddMethod(methodCode);
			if (this.CheckIfInheritsAnotherPrefab())
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
				methodCode.AddLine("base.SetAttributes();");
			}
			else if (this.ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
			{
				methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
			}
			foreach (WidgetCodeGenerationInfo widgetCodeGenerationInfo in this.WidgetCodeGenerationInformations)
			{
				widgetCodeGenerationInfo.FillSetAttributesMethod(methodCode);
			}
		}

		// Token: 0x060000AF RID: 175 RVA: 0x000044A0 File Offset: 0x000026A0
		public static string GetCodeFileType(Type type)
		{
			return type.FullName.Replace('+', '.');
		}

		// Token: 0x0400003A RID: 58
		private WidgetPrefab _prefab;

		// Token: 0x0400003B RID: 59
		private WidgetTemplate _rootWidgetTemplate;

		// Token: 0x04000040 RID: 64
		private UICodeGenerationVariantExtension _variantExtension;

		// Token: 0x04000041 RID: 65
		private WidgetCodeGenerationInfo _rootWidgetCodeGenerationInfo;
	}
}
