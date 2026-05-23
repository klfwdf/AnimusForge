using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class WidgetCodeGenerationInfoChildSearchResult
{
	public WidgetCodeGenerationInfo FoundWidget { get; set; }

	public BindingPath RemainingPath { get; internal set; }

	public WidgetCodeGenerationInfo ReachedWidget { get; internal set; }
}
