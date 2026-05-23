using System.Collections.Generic;
using TaleWorlds.Library.CodeGeneration;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public abstract class WidgetCodeGenerationInfoExtension
{
	public abstract void Initialize();

	public abstract void OnFillSetAttributesMethod(MethodCode methodCode);

	public abstract bool TryGetVariantPropertiesForNewDependency(out UICodeGenerationVariantExtension variantExtension, out Dictionary<string, object> data);

	public abstract void OnFillCreateWidgetMethod(MethodCode methodCode);
}
