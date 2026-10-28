using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryArmorAnimationTextWidget : TextWidget
{
	private bool _isSettingInitialValue;

	private float _floatAmount;

	[Editor(false)]
	public float FloatAmount
	{
		get
		{
			return _floatAmount;
		}
		set
		{
			if (_floatAmount != value)
			{
				HandleAnimation(_floatAmount, value);
				_floatAmount = value;
				base.FloatText = _floatAmount;
				OnPropertyChanged(value, "FloatAmount");
			}
			_isSettingInitialValue = false;
		}
	}

	public InventoryArmorAnimationTextWidget(UIContext context)
		: base(context)
	{
		base.FloatText = 0f;
		_isSettingInitialValue = true;
	}

	private void HandleAnimation(float oldValue, float newValue)
	{
		if (!_isSettingInitialValue)
		{
			if (oldValue > newValue)
			{
				SetState("Decrease");
			}
			else if (oldValue < newValue)
			{
				SetState("Increase");
			}
			else
			{
				SetState("Default");
			}
		}
	}
}
