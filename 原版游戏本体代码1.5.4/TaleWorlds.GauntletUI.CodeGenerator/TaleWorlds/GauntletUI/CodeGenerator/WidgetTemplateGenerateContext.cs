using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library.CodeGeneration;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class WidgetTemplateGenerateContext
{
	private WidgetPrefab _prefab;

	private WidgetTemplate _rootWidgetTemplate;

	private UICodeGenerationVariantExtension _variantExtension;

	private WidgetCodeGenerationInfo _rootWidgetCodeGenerationInfo;

	public WidgetFactory WidgetFactory { get; private set; }

	public UICodeGenerationContext UICodeGenerationContext { get; private set; }

	public VariableCollection VariableCollection { get; private set; }

	public string PrefabName { get; private set; }

	public string VariantName { get; private set; }

	public string ClassName { get; private set; }

	public Dictionary<string, object> Data { get; private set; }

	public List<WidgetCodeGenerationInfo> WidgetCodeGenerationInformations { get; private set; }

	public PrefabDependencyContext PrefabDependencyContext { get; private set; }

	public WidgetTemplateGenerateContextType ContextType { get; private set; }

	public bool GotLogicalChildrenLocation { get; private set; }

	private WidgetTemplateGenerateContext(UICodeGenerationContext uiCodeGenerationContext, PrefabDependencyContext prefabDependencyContext, WidgetTemplateGenerateContextType contextType, string className)
	{
		UICodeGenerationContext = uiCodeGenerationContext;
		WidgetFactory = UICodeGenerationContext.WidgetFactory;
		BrushFactory brushFactory = UICodeGenerationContext.BrushFactory;
		SpriteData spriteData = UICodeGenerationContext.SpriteData;
		VariableCollection = new VariableCollection(WidgetFactory, brushFactory, spriteData);
		WidgetCodeGenerationInformations = new List<WidgetCodeGenerationInfo>();
		ClassName = className;
		PrefabDependencyContext = prefabDependencyContext;
		ContextType = contextType;
	}

	public static WidgetTemplateGenerateContext CreateAsRoot(UICodeGenerationContext uiCodeGenerationContext, string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data)
	{
		string useablePrefabClassName = GetUseablePrefabClassName(prefabName, variantName);
		PrefabDependencyContext prefabDependencyContext = new PrefabDependencyContext(useablePrefabClassName);
		WidgetTemplateGenerateContext widgetTemplateGenerateContext = new WidgetTemplateGenerateContext(uiCodeGenerationContext, prefabDependencyContext, WidgetTemplateGenerateContextType.RootPrefab, useablePrefabClassName);
		widgetTemplateGenerateContext.PrepareAsPrefab(prefabName, variantName, variantExtension, data, new Dictionary<string, WidgetAttributeTemplate>());
		return widgetTemplateGenerateContext;
	}

	public static WidgetTemplateGenerateContext CreateAsDependendPrefab(UICodeGenerationContext uiCodeGenerationContext, PrefabDependencyContext prefabDependencyContext, string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data, Dictionary<string, WidgetAttributeTemplate> givenParameters)
	{
		string className = prefabDependencyContext.GenerateDependencyName() + "_" + GetUseablePrefabClassName(prefabName, variantName);
		WidgetTemplateGenerateContext widgetTemplateGenerateContext = new WidgetTemplateGenerateContext(uiCodeGenerationContext, prefabDependencyContext, WidgetTemplateGenerateContextType.DependendPrefab, className);
		widgetTemplateGenerateContext.PrepareAsPrefab(prefabName, variantName, variantExtension, data, givenParameters);
		prefabDependencyContext.AddDependentWidgetTemplateGenerateContext(widgetTemplateGenerateContext);
		return widgetTemplateGenerateContext;
	}

	public static WidgetTemplateGenerateContext CreateAsInheritedDependendPrefab(UICodeGenerationContext uiCodeGenerationContext, PrefabDependencyContext prefabDependencyContext, string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data, Dictionary<string, WidgetAttributeTemplate> givenParameters)
	{
		string className = prefabDependencyContext.GenerateDependencyName() + "_" + GetUseablePrefabClassName(prefabName, variantName);
		WidgetTemplateGenerateContext widgetTemplateGenerateContext = new WidgetTemplateGenerateContext(uiCodeGenerationContext, prefabDependencyContext, WidgetTemplateGenerateContextType.InheritedDependendPrefab, className);
		widgetTemplateGenerateContext.PrepareAsPrefab(prefabName, variantName, variantExtension, data, givenParameters);
		prefabDependencyContext.AddDependentWidgetTemplateGenerateContext(widgetTemplateGenerateContext);
		return widgetTemplateGenerateContext;
	}

	public static WidgetTemplateGenerateContext CreateAsCustomWidgetTemplate(UICodeGenerationContext uiCodeGenerationContext, PrefabDependencyContext prefabDependencyContext, WidgetTemplate widgetTemplate, string identifierName, VariableCollection variableCollection, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data)
	{
		string className = prefabDependencyContext.GenerateDependencyName() + "_" + identifierName;
		WidgetTemplateGenerateContext widgetTemplateGenerateContext = new WidgetTemplateGenerateContext(uiCodeGenerationContext, prefabDependencyContext, WidgetTemplateGenerateContextType.CustomWidgetTemplate, className);
		widgetTemplateGenerateContext.PrepareAsWidgetTemplate(widgetTemplate, variableCollection, variantExtension, data);
		prefabDependencyContext.AddDependentWidgetTemplateGenerateContext(widgetTemplateGenerateContext);
		return widgetTemplateGenerateContext;
	}

	private void PrepareAsPrefab(string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data, Dictionary<string, WidgetAttributeTemplate> givenParameters)
	{
		PrefabName = prefabName;
		VariantName = variantName;
		_variantExtension = variantExtension;
		Data = data;
		VariableCollection.SetGivenParameters(givenParameters);
		_prefab = WidgetFactory.GetCustomType(PrefabName);
		_rootWidgetTemplate = _prefab.RootTemplate;
		GotLogicalChildrenLocation = FindLogicalChildrenLocation(_rootWidgetTemplate) != null;
		if (_variantExtension != null)
		{
			_variantExtension.Initialize(this);
		}
		VariableCollection.FillFromPrefab(_prefab);
	}

	private void PrepareAsWidgetTemplate(WidgetTemplate rootWidgetTemplate, VariableCollection variableCollection, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data)
	{
		_rootWidgetTemplate = rootWidgetTemplate;
		Data = data;
		_variantExtension = variantExtension;
		VariableCollection = variableCollection;
		if (_variantExtension != null)
		{
			_variantExtension.Initialize(this);
		}
	}

	private WidgetTemplate FindLogicalChildrenLocation(WidgetTemplate template)
	{
		if (template.LogicalChildrenLocation)
		{
			return template;
		}
		for (int i = 0; i < template.ChildCount; i++)
		{
			WidgetTemplate childAt = template.GetChildAt(i);
			WidgetTemplate widgetTemplate = FindLogicalChildrenLocation(childAt);
			if (widgetTemplate != null)
			{
				return widgetTemplate;
			}
		}
		return null;
	}

	private CommentSection CreateCommentSection()
	{
		CommentSection commentSection = new CommentSection();
		foreach (KeyValuePair<string, object> datum in Data)
		{
			commentSection.AddCommentLine("Data: " + datum.Key + " - " + datum.Value);
		}
		foreach (KeyValuePair<string, WidgetAttributeTemplate> givenParameter in VariableCollection.GivenParameters)
		{
			WidgetAttributeTemplate value = givenParameter.Value;
			commentSection.AddCommentLine(string.Concat("Given Parameter: ", givenParameter.Key, " - ", value.Value, " ", value.KeyType, " ", value.ValueType));
		}
		return commentSection;
	}

	public Type GetWidgetTypeWithinPrefabRoots(string typeName)
	{
		if (WidgetFactory.IsCustomType(typeName))
		{
			WidgetPrefab customType = WidgetFactory.GetCustomType(typeName);
			return GetWidgetTypeWithinPrefabRoots(customType.RootTemplate.Type);
		}
		return WidgetFactory.GetBuiltinType(typeName);
	}

	public bool CheckIfInheritsAnotherPrefab()
	{
		string type = _rootWidgetTemplate.Type;
		return WidgetFactory.IsCustomType(type);
	}

	private void AddInheritedType(ClassCode classCode)
	{
		string type = _rootWidgetTemplate.Type;
		string text = "";
		text = ((!WidgetFactory.IsCustomType(type)) ? WidgetFactory.GetBuiltinType(type).FullName : _rootWidgetCodeGenerationInfo.ChildWidgetTemplateGenerateContext.ClassName);
		classCode.InheritedInterfaces.Add(text);
	}

	public void GenerateInto(NamespaceCode namespaceCode)
	{
		ClassCode classCode = new ClassCode();
		classCode.Name = ClassName;
		classCode.AccessModifier = ClassCodeAccessModifier.Public;
		if (_variantExtension != null)
		{
			_variantExtension.AddExtensionVariables(classCode);
		}
		CreateWidgetInformations(_rootWidgetTemplate, "_widget", null);
		if (GotLogicalChildrenLocation)
		{
			GenerateAddChildToLogicalLocationMethod(classCode);
		}
		CheckDependencies();
		VariableCollection.FillVisualDefinitionCreators(classCode);
		FillWidgetVariables(classCode);
		GenerateCreateWidgetsMethod(classCode);
		GenerateSetIdsMethod(classCode);
		GenerateSetAttributesMethod(classCode);
		AddInheritedType(classCode);
		if (_variantExtension != null)
		{
			_variantExtension.DoExtraCodeGeneration(classCode);
		}
		ConstructorCode constructorCode = new ConstructorCode();
		constructorCode.MethodSignature = "(TaleWorlds.GauntletUI.UIContext context)";
		constructorCode.BaseCall = "(context)";
		classCode.AddConsturctor(constructorCode);
		classCode.CommentSection = CreateCommentSection();
		namespaceCode.AddClass(classCode);
		if (ContextType == WidgetTemplateGenerateContextType.RootPrefab)
		{
			PrefabDependencyContext.GenerateInto(namespaceCode);
		}
	}

	private void GenerateAddChildToLogicalLocationMethod(ClassCode classCode)
	{
		WidgetTemplate widgetTemplate = FindLogicalChildrenLocation(_rootWidgetTemplate);
		WidgetCodeGenerationInfo widgetCodeGenerationInfo = FindWidgetCodeGenerationInformation(widgetTemplate);
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "AddChildToLogicalLocation";
		methodCode.AccessModifier = MethodCodeAccessModifier.Public;
		methodCode.MethodSignature = "(TaleWorlds.GauntletUI.BaseTypes.Widget widget)";
		methodCode.AddLine(widgetCodeGenerationInfo.VariableName + ".AddChild(widget);");
		classCode.AddMethod(methodCode);
	}

	public MethodCode GenerateCreatorMethod()
	{
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "Create" + ClassName;
		methodCode.ReturnParameter = "TaleWorlds.GauntletUI.PrefabSystem.GeneratedPrefabInstantiationResult";
		methodCode.MethodSignature = "(TaleWorlds.GauntletUI.UIContext context, System.Collections.Generic.Dictionary<string, object> data)";
		methodCode.AddLine("var widget = new " + ClassName + "(context);");
		methodCode.AddLine("widget.CreateWidgets();");
		methodCode.AddLine("widget.SetIds();");
		methodCode.AddLine("widget.SetAttributes();");
		methodCode.AddLine("var result = new TaleWorlds.GauntletUI.PrefabSystem.GeneratedPrefabInstantiationResult(widget);");
		if (_variantExtension != null)
		{
			_variantExtension.AddExtrasToCreatorMethod(methodCode);
		}
		methodCode.AddLine("return result;");
		return methodCode;
	}

	public static string GetUseableName(string name)
	{
		return name.Replace(".", "_");
	}

	private static string GetUseablePrefabClassName(string name, string variantName)
	{
		string text = name.Replace(".", "_");
		string text2 = variantName.Replace(".", "_");
		return text + "__" + text2;
	}

	private WidgetCodeGenerationInfo FindWidgetCodeGenerationInformation(WidgetTemplate widgetTemplate)
	{
		foreach (WidgetCodeGenerationInfo widgetCodeGenerationInformation in WidgetCodeGenerationInformations)
		{
			if (widgetCodeGenerationInformation.WidgetTemplate == widgetTemplate)
			{
				return widgetCodeGenerationInformation;
			}
		}
		return null;
	}

	private void CreateWidgetInformations(WidgetTemplate widgetTemplate, string variableName, WidgetCodeGenerationInfo parent)
	{
		WidgetCodeGenerationInfo widgetCodeGenerationInfo = new WidgetCodeGenerationInfo(this, widgetTemplate, variableName, parent);
		WidgetCodeGenerationInformations.Add(widgetCodeGenerationInfo);
		if (parent != null)
		{
			parent.AddChild(widgetCodeGenerationInfo);
		}
		else
		{
			_rootWidgetCodeGenerationInfo = widgetCodeGenerationInfo;
		}
		if (_variantExtension != null)
		{
			WidgetCodeGenerationInfoExtension widgetCodeGenerationInfoExtension = _variantExtension.CreateWidgetCodeGenerationInfoExtension(widgetCodeGenerationInfo);
			widgetCodeGenerationInfo.AddExtension(widgetCodeGenerationInfoExtension);
			widgetCodeGenerationInfoExtension.Initialize();
		}
		for (int i = 0; i < widgetTemplate.ChildCount; i++)
		{
			WidgetTemplate childAt = widgetTemplate.GetChildAt(i);
			string variableName2 = variableName + "_" + i;
			CreateWidgetInformations(childAt, variableName2, widgetCodeGenerationInfo);
		}
	}

	private void CheckDependencies()
	{
		foreach (WidgetCodeGenerationInfo widgetCodeGenerationInformation in WidgetCodeGenerationInformations)
		{
			widgetCodeGenerationInformation.CheckDependendType();
		}
	}

	private void FillWidgetVariables(ClassCode classCode)
	{
		foreach (WidgetCodeGenerationInfo widgetCodeGenerationInformation in WidgetCodeGenerationInformations)
		{
			VariableCode variableCode = widgetCodeGenerationInformation.CreateVariableCode();
			classCode.AddVariable(variableCode);
		}
	}

	public bool IsBuiltinType(WidgetTemplate widgetTemplate)
	{
		return WidgetFactory.IsBuiltinType(widgetTemplate.Type);
	}

	private void GenerateCreateWidgetsMethod(ClassCode classCode)
	{
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "CreateWidgets";
		classCode.AddMethod(methodCode);
		if (CheckIfInheritsAnotherPrefab())
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
			methodCode.AddLine("base.CreateWidgets();");
		}
		else if (ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
		}
		foreach (WidgetCodeGenerationInfo widgetCodeGenerationInformation in WidgetCodeGenerationInformations)
		{
			widgetCodeGenerationInformation.FillCreateWidgetsMethod(methodCode);
		}
	}

	private void GenerateSetIdsMethod(ClassCode classCode)
	{
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "SetIds";
		classCode.AddMethod(methodCode);
		if (CheckIfInheritsAnotherPrefab())
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
			methodCode.AddLine("base.SetIds();");
		}
		else if (ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
		}
		foreach (WidgetCodeGenerationInfo widgetCodeGenerationInformation in WidgetCodeGenerationInformations)
		{
			widgetCodeGenerationInformation.FillSetIdsMethod(methodCode);
		}
	}

	private void GenerateSetAttributesMethod(ClassCode classCode)
	{
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "SetAttributes";
		classCode.AddMethod(methodCode);
		if (CheckIfInheritsAnotherPrefab())
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
			methodCode.AddLine("base.SetAttributes();");
		}
		else if (ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
		{
			methodCode.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
		}
		foreach (WidgetCodeGenerationInfo widgetCodeGenerationInformation in WidgetCodeGenerationInformations)
		{
			widgetCodeGenerationInformation.FillSetAttributesMethod(methodCode);
		}
	}

	public static string GetCodeFileType(Type type)
	{
		return type.FullName.Replace('+', '.');
	}
}
