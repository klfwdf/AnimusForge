using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay;

public class OverlayBaseWidget : Widget
{
	private OverlayPopupWidget _popupWidget;

	[Editor(false)]
	public OverlayPopupWidget PopupWidget
	{
		get
		{
			return _popupWidget;
		}
		set
		{
			if (_popupWidget != value)
			{
				_popupWidget = value;
				OnPropertyChanged(value, "PopupWidget");
			}
		}
	}

	public OverlayBaseWidget(UIContext context)
		: base(context)
	{
	}
}
