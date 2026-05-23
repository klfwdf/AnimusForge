using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Clan;

public class ClanPartyRoleSelectionToggleWidget : ButtonWidget
{
	private float _lastPopupSizeY;

	private ClanPartyRoleSelectionPopupWidget _popup;

	[Editor(false)]
	public ClanPartyRoleSelectionPopupWidget Popup
	{
		get
		{
			return _popup;
		}
		set
		{
			if (_popup != value)
			{
				_popup = value;
				OnPropertyChanged(value, "Popup");
				_popup.AddToggleWidget(this);
			}
		}
	}

	public ClanPartyRoleSelectionToggleWidget(UIContext context)
		: base(context)
	{
		ClickEventHandlers.Add(OnClick);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (Popup?.ActiveToggleWidget == this && MathF.Abs(Popup.Size.Y - _lastPopupSizeY) > 1E-05f)
		{
			UpdatePopupPosition();
			_lastPopupSizeY = Popup.Size.Y;
		}
	}

	protected virtual void OnClick(Widget widget)
	{
		if (Popup != null)
		{
			if (Popup.ActiveToggleWidget == this)
			{
				ClosePopup();
			}
			else
			{
				OpenPopup();
			}
		}
	}

	private void OpenPopup()
	{
		Popup.ActiveToggleWidget = this;
		Popup.IsVisible = true;
		UpdatePopupPosition();
		_lastPopupSizeY = Popup.Size.Y;
	}

	private void ClosePopup()
	{
		Popup.ActiveToggleWidget = null;
		Popup.IsVisible = false;
	}

	private void UpdatePopupPosition()
	{
		Popup.ScaledPositionYOffset += base.GlobalPosition.Y - Popup.GlobalPosition.Y - Popup.Size.Y + 47f * base._scaleToUse;
		Popup.ScaledPositionXOffset += base.GlobalPosition.X - Popup.GlobalPosition.X + 80f * base._scaleToUse;
	}
}
