using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.TownManagement;

public class SliderPopupWidget : Widget
{
	private ButtonWidget _closePopupWidget;

	private TextWidget _sliderValueTextWidget;

	private SliderWidget _reserveAmountSlider;

	private Widget _popupParentWidget;

	[Editor(false)]
	public Widget PopupParentWidget
	{
		get
		{
			return _popupParentWidget;
		}
		set
		{
			if (_popupParentWidget != value)
			{
				_popupParentWidget = value;
				OnPropertyChanged(value, "PopupParentWidget");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget ClosePopupWidget
	{
		get
		{
			return _closePopupWidget;
		}
		set
		{
			if (_closePopupWidget != value)
			{
				_closePopupWidget = value;
				OnPropertyChanged(value, "ClosePopupWidget");
			}
		}
	}

	[Editor(false)]
	public TextWidget SliderValueTextWidget
	{
		get
		{
			return _sliderValueTextWidget;
		}
		set
		{
			if (_sliderValueTextWidget != value)
			{
				_sliderValueTextWidget = value;
				OnPropertyChanged(value, "SliderValueTextWidget");
			}
		}
	}

	[Editor(false)]
	public SliderWidget ReserveAmountSlider
	{
		get
		{
			return _reserveAmountSlider;
		}
		set
		{
			if (_reserveAmountSlider != value)
			{
				_reserveAmountSlider = value;
				OnPropertyChanged(value, "ReserveAmountSlider");
			}
		}
	}

	public SliderPopupWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (SliderValueTextWidget != null && ReserveAmountSlider != null)
		{
			SliderValueTextWidget.Text = ReserveAmountSlider.ValueInt.ToString();
		}
		if (base.ParentWidget.IsVisible && base.EventManager.LatestMouseDownWidget != this && base.EventManager.LatestMouseDownWidget != base.ParentWidget && base.EventManager.LatestMouseDownWidget != PopupParentWidget && !CheckIsMyChildRecursive(base.EventManager.LatestMouseDownWidget))
		{
			EventFired("ClosePopup");
		}
	}
}
