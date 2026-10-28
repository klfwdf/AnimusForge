using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterDeveloper;

public class CharacterDeveloperPerkSelectionWidget : Widget
{
	private float _distBetweenPerkItemsMultiplier = 16f;

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

	public CharacterDeveloperPerkSelectionWidget(UIContext context)
		: base(context)
	{
		base.IsVisible = false;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.IsVisible && _latestMouseUpWidgetWhenActivated != base.EventManager.LatestMouseUpWidget && !CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget))
		{
			Deactivate();
		}
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		if (!base.IsVisible || _latestMouseUpWidgetWhenActivated == null)
		{
			return;
		}
		float value = _latestMouseUpWidgetWhenActivated.GlobalPosition.X + _latestMouseUpWidgetWhenActivated.Size.X + _distBetweenPerkItemsMultiplier * 2f * base._scaleToUse;
		float value2 = 0f;
		if (GetChild(0).ChildCount > 1)
		{
			if (_latestMouseUpWidgetWhenActivated is PerkItemButtonWidget perkItemButtonWidget)
			{
				if (perkItemButtonWidget.AlternativeType == 1)
				{
					value2 = _latestMouseUpWidgetWhenActivated.GlobalPosition.Y + (_latestMouseUpWidgetWhenActivated.Size.Y - 4f * base._scaleToUse) - base.Size.Y / 2f;
				}
				else if (perkItemButtonWidget.AlternativeType == 2)
				{
					value2 = _latestMouseUpWidgetWhenActivated.GlobalPosition.Y - base.Size.Y / 2f;
				}
			}
		}
		else
		{
			value2 = _latestMouseUpWidgetWhenActivated.GlobalPosition.Y + _latestMouseUpWidgetWhenActivated.Size.Y / 2f - base.Size.Y / 2f;
		}
		base.ScaledPositionXOffset = MathF.Clamp(value, 0f, base.EventManager.PageSize.X - base.Size.X);
		base.ScaledPositionYOffset = MathF.Clamp(value2, 0f, base.EventManager.PageSize.Y - base.Size.Y);
	}

	private void Activate()
	{
		if (_latestMouseUpWidgetWhenActivated == null)
		{
			_latestMouseUpWidgetWhenActivated = base.EventManager.LatestMouseDownWidget;
		}
		base.IsVisible = true;
	}

	private void Deactivate()
	{
		EventFired("Deactivate");
		base.IsVisible = false;
		_latestMouseUpWidgetWhenActivated = null;
	}
}
