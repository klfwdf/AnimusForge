using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterDeveloper;

public class CharacterDeveloperAttributeInspectionPopupWidget : Widget
{
	private Widget _latestMouseUpWidgetWhenActivated;

	private bool _isActive;

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (_isActive != value)
			{
				_isActive = value;
				OnPropertyChanged(value, "IsActive");
				if (_isActive)
				{
					Activate();
				}
				else
				{
					Deactivate();
				}
			}
		}
	}

	public CharacterDeveloperAttributeInspectionPopupWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.ParentWidget.IsVisible && _latestMouseUpWidgetWhenActivated != base.EventManager.LatestMouseUpWidget && !CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget))
		{
			Deactivate();
		}
	}

	private void Activate()
	{
		_latestMouseUpWidgetWhenActivated = base.EventManager.LatestMouseDownWidget;
		base.ParentWidget.IsVisible = true;
	}

	private void Deactivate()
	{
		EventFired("Deactivate");
		base.ParentWidget.IsVisible = false;
	}
}
