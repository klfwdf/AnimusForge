using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryItemValueTextWidget : TextWidget
{
	private bool _firstHandled;

	private int _profitType;

	[Editor(false)]
	public int ProfitType
	{
		get
		{
			return _profitType;
		}
		set
		{
			if (_profitType != value)
			{
				_profitType = value;
				OnPropertyChanged(value, "ProfitType");
				HandleVisuals();
			}
		}
	}

	public InventoryItemValueTextWidget(UIContext context)
		: base(context)
	{
	}

	private void HandleVisuals()
	{
		if (!_firstHandled)
		{
			this.RegisterBrushStatesOfWidget();
			_firstHandled = true;
		}
		switch (ProfitType)
		{
		case -2:
			SetState("VeryBad");
			break;
		case -1:
			SetState("Bad");
			break;
		case 0:
			SetState("Default");
			break;
		case 1:
			SetState("Good");
			break;
		case 2:
			SetState("VeryGood");
			break;
		}
	}
}
