using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterDeveloper;

public class SkillProgressFillBarWidget : FillBarWidget
{
	private Widget _percentageIndicatorWidget;

	public Widget PercentageIndicatorWidget
	{
		get
		{
			return _percentageIndicatorWidget;
		}
		set
		{
			if (_percentageIndicatorWidget != value)
			{
				_percentageIndicatorWidget = value;
				OnPropertyChanged(value, "PercentageIndicatorWidget");
			}
		}
	}

	public SkillProgressFillBarWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (PercentageIndicatorWidget != null)
		{
			base.ScaledPositionXOffset = Mathf.Clamp((PercentageIndicatorWidget.ScaledPositionXOffset - base.Size.X / 2f) * base._scaleToUse, 0f, 600f * base._scaleToUse);
		}
		base.OnRender(twoDimensionContext, drawContext);
	}
}
