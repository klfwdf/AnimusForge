namespace TaleWorlds.GauntletUI.CodeGenerator;

public class PrefabDependency
{
	public string Type { get; private set; }

	public string VariantName { get; private set; }

	public bool IsRoot { get; private set; }

	public WidgetTemplateGenerateContext WidgetTemplateGenerateContext { get; private set; }

	public PrefabDependency(string type, string variantName, bool isRoot, WidgetTemplateGenerateContext widgetTemplateGenerateContext)
	{
		Type = type;
		VariantName = variantName;
		IsRoot = isRoot;
		WidgetTemplateGenerateContext = widgetTemplateGenerateContext;
	}
}
