using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.GatherArmy;

public class BoostCohesionPopupWidget : Widget
{
	private ButtonWidget _closePopupButton;

	[Editor(false)]
	public ButtonWidget ClosePopupButton
	{
		get
		{
			return _closePopupButton;
		}
		set
		{
			if (_closePopupButton != value)
			{
				_closePopupButton = value;
				OnPropertyChanged(value, "ClosePopupButton");
			}
		}
	}

	public BoostCohesionPopupWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (ClosePopupButton != null && !ClosePopupButton.ClickEventHandlers.Contains(ClosePopup))
		{
			ClosePopupButton.ClickEventHandlers.Add(ClosePopup);
		}
	}

	public void ClosePopup(Widget widget)
	{
		base.ParentWidget.IsVisible = false;
	}
}
