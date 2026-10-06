using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.GatherArmy;

public class GatherArmyTupleButtonWidget : ButtonWidget
{
	private bool _isInCart;

	private bool _isEligible;

	private bool _isTransferDisabled;

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
	public bool IsEligible
	{
		get
		{
			return _isEligible;
		}
		set
		{
			if (_isEligible != value)
			{
				_isEligible = value;
				OnPropertyChanged(value, "IsEligible");
			}
		}
	}

	[Editor(false)]
	public bool IsTransferDisabled
	{
		get
		{
			return _isTransferDisabled;
		}
		set
		{
			if (_isTransferDisabled != value)
			{
				_isTransferDisabled = value;
				OnPropertyChanged(value, "IsTransferDisabled");
			}
		}
	}

	public GatherArmyTupleButtonWidget(UIContext context)
		: base(context)
	{
		base.OverrideDefaultStateSwitchingEnabled = true;
	}

	protected override void HandleClick()
	{
		if (!IsTransferDisabled && (IsInCart || IsEligible))
		{
			base.HandleClick();
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (IsTransferDisabled || (!IsInCart && !IsEligible))
		{
			SetState("Disabled");
		}
		else if (IsInCart)
		{
			SetState("Selected");
		}
		else if (base.IsPressed)
		{
			SetState("Pressed");
		}
		else if (base.IsHovered)
		{
			SetState("Hovered");
		}
		else
		{
			SetState("Default");
		}
	}
}
