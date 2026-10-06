using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information.RundownTooltip;

public class RundownLineWidget : ListPanel
{
	public TextWidget NameTextWidget { get; set; }

	public TextWidget ValueTextWidget { get; set; }

	public float Value { get; set; }

	public RundownLineWidget(UIContext context)
		: base(context)
	{
	}

	public void RefreshValueOffset(float columnWidth)
	{
		if (columnWidth >= 0f && NameTextWidget.Size.X > 1E-05f && ValueTextWidget.Size.X > 1E-05f)
		{
			ValueTextWidget.ScaledPositionXOffset = columnWidth - (NameTextWidget.Size.X + ValueTextWidget.Size.X + base.ScaledMarginLeft + base.ScaledMarginRight);
		}
	}
}
