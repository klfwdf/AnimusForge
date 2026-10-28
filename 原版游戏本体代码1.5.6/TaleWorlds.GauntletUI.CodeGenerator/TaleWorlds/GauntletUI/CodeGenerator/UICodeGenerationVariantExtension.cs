using System;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library.CodeGeneration;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public abstract class UICodeGenerationVariantExtension
{
	public abstract PrefabExtension GetPrefabExtension();

	public abstract Type GetAttributeType(WidgetAttributeTemplate widgetAttributeTemplate);

	public abstract void AddExtensionVariables(ClassCode classCode);

	public abstract void Initialize(WidgetTemplateGenerateContext widgetTemplateGenerateContext);

	public abstract void AddExtrasToCreatorMethod(MethodCode methodCode);

	public abstract WidgetCodeGenerationInfoExtension CreateWidgetCodeGenerationInfoExtension(WidgetCodeGenerationInfo widgetCodeGenerationInfo);

	public abstract void DoExtraCodeGeneration(ClassCode classCode);
}
