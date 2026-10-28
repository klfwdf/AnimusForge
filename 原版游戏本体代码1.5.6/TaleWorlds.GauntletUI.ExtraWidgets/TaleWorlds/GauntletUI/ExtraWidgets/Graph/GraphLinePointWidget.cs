using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets.Graph;

public class GraphLinePointWidget : BrushWidget
{
	public float HorizontalValue { get; set; }

	public float VerticalValue { get; set; }

	public GraphLinePointWidget(UIContext context)
		: base(context)
	{
	}
}
