using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class ReloadBarHeightAdjustmentWidget : Widget
{
	private const float _baseHeight = 50f;

	private float _relativeDurationToMaxDuration;

	private Widget _fillWidget;

	public float RelativeDurationToMaxDuration
	{
		get
		{
			return _relativeDurationToMaxDuration;
		}
		set
		{
			if (value != _relativeDurationToMaxDuration)
			{
				_relativeDurationToMaxDuration = value;
				Refresh();
			}
		}
	}

	public Widget FillWidget
	{
		get
		{
			return _fillWidget;
		}
		set
		{
			if (value != _fillWidget)
			{
				_fillWidget = value;
				Refresh();
			}
		}
	}

	public ReloadBarHeightAdjustmentWidget(UIContext context)
		: base(context)
	{
	}

	private void Refresh()
	{
		if (FillWidget != null)
		{
			base.ScaledSuggestedHeight = 50f * RelativeDurationToMaxDuration * base._scaleToUse;
			FillWidget.ScaledSuggestedHeight = base.ScaledSuggestedHeight - (FillWidget.MarginBottom + FillWidget.MarginTop) * base._scaleToUse;
		}
	}
}
