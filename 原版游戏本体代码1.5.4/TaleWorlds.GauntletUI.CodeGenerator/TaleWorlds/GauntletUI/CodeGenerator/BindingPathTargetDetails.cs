using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class BindingPathTargetDetails
{
	public BindingPath BindingPath { get; private set; }

	public bool IsRoot => BindingPath.Path == "Root";

	public BindingPathTargetDetails Parent { get; private set; }

	public List<BindingPathTargetDetails> Children { get; private set; }

	public List<WidgetCodeGenerationInfoDatabindingExtension> WidgetDatabindingInformations { get; private set; }

	public BindingPathTargetDetails(BindingPath bindingPath)
	{
		BindingPath = bindingPath;
		Children = new List<BindingPathTargetDetails>();
		WidgetDatabindingInformations = new List<WidgetCodeGenerationInfoDatabindingExtension>();
	}

	public void SetParent(BindingPathTargetDetails parent)
	{
		Parent = parent;
		Parent.Children.Add(this);
	}
}
