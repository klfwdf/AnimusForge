using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class DropdownItemButtonWidget : ButtonWidget
{
	private bool _canBeSelected = true;

	[Editor(false)]
	public bool CanBeSelected
	{
		get
		{
			return _canBeSelected;
		}
		set
		{
			if (_canBeSelected != value)
			{
				_canBeSelected = value;
				OnPropertyChanged(value, "CanBeSelected");
				RefreshState();
			}
		}
	}

	public DropdownItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void RefreshState()
	{
		if (!CanBeSelected && !base.OverrideDefaultStateSwitchingEnabled)
		{
			SetState("Disabled");
			if (base.UpdateChildrenStates)
			{
				for (int i = 0; i < base.ChildCount; i++)
				{
					GetChild(i).SetState("Disabled");
				}
			}
		}
		else
		{
			base.RefreshState();
		}
	}

	protected override void HandleClick()
	{
		if (CanBeSelected)
		{
			base.HandleClick();
		}
	}
}
