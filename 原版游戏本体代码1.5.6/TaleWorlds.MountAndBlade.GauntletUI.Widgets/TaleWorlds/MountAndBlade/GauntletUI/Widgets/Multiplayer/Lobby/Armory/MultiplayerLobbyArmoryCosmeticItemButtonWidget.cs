using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class MultiplayerLobbyArmoryCosmeticItemButtonWidget : ButtonWidget
{
	private float _selectableTimer;

	private float _animationTargetAlpha;

	private float _animationStartAlpha;

	private int _itemType;

	private bool _isUnlocked;

	private float _selectableStateAnimationDuration;

	private float _selectableStateAlpha;

	private float _nonSelectableStateAlpha;

	private bool _isSelectable;

	public int ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			if (value != _itemType)
			{
				_itemType = value;
				OnPropertyChanged(value, "ItemType");
			}
		}
	}

	public bool IsUnlocked
	{
		get
		{
			return _isUnlocked;
		}
		set
		{
			if (value != _isUnlocked)
			{
				_isUnlocked = value;
				OnPropertyChanged(value, "IsUnlocked");
			}
		}
	}

	public float SelectableStateAnimationDuration
	{
		get
		{
			return _selectableStateAnimationDuration;
		}
		set
		{
			if (value != _selectableStateAnimationDuration)
			{
				_selectableStateAnimationDuration = value;
				OnPropertyChanged(value, "SelectableStateAnimationDuration");
			}
		}
	}

	public float SelectableStateAlpha
	{
		get
		{
			return _selectableStateAlpha;
		}
		set
		{
			if (value != _selectableStateAlpha)
			{
				_selectableStateAlpha = value;
				OnPropertyChanged(value, "SelectableStateAlpha");
			}
		}
	}

	public float NonSelectableStateAlpha
	{
		get
		{
			return _nonSelectableStateAlpha;
		}
		set
		{
			if (value != _nonSelectableStateAlpha)
			{
				_nonSelectableStateAlpha = value;
				OnPropertyChanged(value, "NonSelectableStateAlpha");
			}
		}
	}

	public bool IsSelectable
	{
		get
		{
			return _isSelectable;
		}
		set
		{
			if (value != _isSelectable)
			{
				_isSelectable = value;
				OnPropertyChanged(value, "IsSelectable");
				UpdateSelectableState();
			}
		}
	}

	public MultiplayerLobbyArmoryCosmeticItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.EventManager.HoveredWidget == this && Input.IsKeyPressed(InputKey.ControllerRUp))
		{
			OnMouseAlternatePressed();
		}
		else if (base.EventManager.HoveredWidget == this && Input.IsKeyReleased(InputKey.ControllerRUp))
		{
			OnMouseAlternateReleased(isFromInput: true);
		}
	}

	private void UpdateSelectableState()
	{
		_selectableTimer = 0f;
		base.IsDisabled = !IsSelectable;
		_animationStartAlpha = (IsSelectable ? NonSelectableStateAlpha : SelectableStateAlpha);
		_animationTargetAlpha = (IsSelectable ? SelectableStateAlpha : NonSelectableStateAlpha);
		base.EventManager.AddLateUpdateAction(this, AnimateSelectableState, 1);
	}

	private void AnimateSelectableState(float dt)
	{
		_selectableTimer += dt;
		float amount;
		if (_selectableTimer < SelectableStateAnimationDuration)
		{
			amount = _selectableTimer / SelectableStateAnimationDuration;
			base.EventManager.AddLateUpdateAction(this, AnimateSelectableState, 1);
		}
		else
		{
			amount = 1f;
		}
		float num = MathF.Lerp(_animationStartAlpha, _animationTargetAlpha, amount);
		base.IsVisible = num != 0f;
		this.SetGlobalAlphaRecursively(num);
	}

	protected override void HandleClick()
	{
		base.HandleClick();
		if (IsUnlocked)
		{
			HandleSoundEvent();
		}
		else
		{
			EventFired("Obtain");
		}
	}

	protected override void HandleAlternateClick()
	{
		base.HandleAlternateClick();
		HandleSoundEvent();
	}

	private void HandleSoundEvent()
	{
		switch (ItemType)
		{
		case 12:
			EventFired("WearHelmet");
			break;
		case 13:
			EventFired("WearArmorBig");
			break;
		case 14:
		case 15:
		case 22:
			EventFired("WearArmorSmall");
			break;
		default:
			EventFired("WearGeneric");
			break;
		}
	}
}
