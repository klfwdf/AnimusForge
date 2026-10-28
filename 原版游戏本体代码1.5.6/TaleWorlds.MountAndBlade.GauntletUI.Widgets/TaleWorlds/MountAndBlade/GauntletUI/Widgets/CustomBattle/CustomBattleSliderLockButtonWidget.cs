using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CustomBattle;

public class CustomBattleSliderLockButtonWidget : ButtonWidget
{
	public Brush LockOpenedBrush { get; set; }

	public Brush LockClosedBrush { get; set; }

	public CustomBattleSliderLockButtonWidget(UIContext context)
		: base(context)
	{
		base.boolPropertyChanged += CustomBattleSliderLockButtonWidget_PropertyChanged;
	}

	private void CustomBattleSliderLockButtonWidget_PropertyChanged(PropertyOwnerObject widget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsSelected")
		{
			bool flag = propertyValue;
			base.Brush = (flag ? LockClosedBrush : LockOpenedBrush);
		}
	}
}
