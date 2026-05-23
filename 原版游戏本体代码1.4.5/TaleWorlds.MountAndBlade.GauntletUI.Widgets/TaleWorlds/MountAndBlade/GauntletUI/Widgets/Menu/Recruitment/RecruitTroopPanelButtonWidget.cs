using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Recruitment;

public class RecruitTroopPanelButtonWidget : ButtonWidget
{
	private bool _canBeRecruited;

	private bool _isInCart;

	private bool _playerHasEnoughRelation;

	private bool _isTroopEmpty;

	private ButtonWidget _removeFromCartButton;

	private ImageIdentifierWidget _characterImageWidget;

	[Editor(false)]
	public bool CanBeRecruited
	{
		get
		{
			return _canBeRecruited;
		}
		set
		{
			if (_canBeRecruited != value)
			{
				_canBeRecruited = value;
				OnPropertyChanged(value, "CanBeRecruited");
			}
		}
	}

	[Editor(false)]
	public bool IsInCart
	{
		get
		{
			return _isInCart;
		}
		set
		{
			if (_isInCart != value)
			{
				_isInCart = value;
				OnPropertyChanged(value, "IsInCart");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget RemoveFromCartButton
	{
		get
		{
			return _removeFromCartButton;
		}
		set
		{
			if (_removeFromCartButton != value)
			{
				_removeFromCartButton = value;
				OnPropertyChanged(value, "RemoveFromCartButton");
			}
		}
	}

	[Editor(false)]
	public ImageIdentifierWidget CharacterImageWidget
	{
		get
		{
			return _characterImageWidget;
		}
		set
		{
			if (_characterImageWidget != value)
			{
				_characterImageWidget = value;
				OnPropertyChanged(value, "CharacterImageWidget");
			}
		}
	}

	[Editor(false)]
	public bool IsTroopEmpty
	{
		get
		{
			return _isTroopEmpty;
		}
		set
		{
			if (_isTroopEmpty != value)
			{
				_isTroopEmpty = value;
				OnPropertyChanged(value, "IsTroopEmpty");
			}
		}
	}

	[Editor(false)]
	public bool PlayerHasEnoughRelation
	{
		get
		{
			return _playerHasEnoughRelation;
		}
		set
		{
			if (_playerHasEnoughRelation != value)
			{
				_playerHasEnoughRelation = value;
				OnPropertyChanged(value, "PlayerHasEnoughRelation");
			}
		}
	}

	public RecruitTroopPanelButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (IsTroopEmpty)
		{
			if (PlayerHasEnoughRelation)
			{
				SetState("EmptyEnoughRelation");
			}
			else
			{
				SetState("EmptyNoRelation");
			}
		}
		else if (CanBeRecruited)
		{
			SetState("Available");
		}
		else
		{
			SetState("Unavailable");
		}
		if (!PlayerHasEnoughRelation && !IsTroopEmpty && CharacterImageWidget != null)
		{
			CharacterImageWidget.Brush.ValueFactor = -50f;
			CharacterImageWidget.Brush.SaturationFactor = -100f;
		}
		if (CharacterImageWidget != null)
		{
			CharacterImageWidget.IsHidden = IsTroopEmpty;
		}
		RemoveFromCartButton.SetState(((base.IsHovered || base.IsPressed || base.IsSelected) && ((!IsTroopEmpty && PlayerHasEnoughRelation) || IsInCart)) ? "Hovered" : "Default");
	}

	private bool IsMouseOverWidget()
	{
		Vector2 globalPosition = base.GlobalPosition;
		if (IsBetween(base.EventManager.MousePosition.X, globalPosition.X, globalPosition.X + base.Size.X))
		{
			return IsBetween(base.EventManager.MousePosition.Y, globalPosition.Y, globalPosition.Y + base.Size.Y);
		}
		return false;
	}

	private bool IsBetween(float number, float min, float max)
	{
		if (number >= min)
		{
			return number <= max;
		}
		return false;
	}
}
