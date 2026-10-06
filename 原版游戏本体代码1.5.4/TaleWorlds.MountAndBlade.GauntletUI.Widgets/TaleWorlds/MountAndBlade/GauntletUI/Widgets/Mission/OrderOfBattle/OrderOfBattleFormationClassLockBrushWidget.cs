using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleFormationClassLockBrushWidget : BrushWidget
{
	private bool _isInitialStateSet;

	private bool _isLocked;

	private Brush _lockedBrush;

	private Brush _unlockedBrush;

	[Editor(false)]
	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			if (value != _isLocked || !_isInitialStateSet)
			{
				_isLocked = value;
				OnPropertyChanged(value, "IsLocked");
				OnLockStateSet();
			}
		}
	}

	[Editor(false)]
	public Brush LockedBrush
	{
		get
		{
			return _lockedBrush;
		}
		set
		{
			if (value != _lockedBrush)
			{
				_lockedBrush = value;
				OnPropertyChanged(value, "LockedBrush");
			}
		}
	}

	[Editor(false)]
	public Brush UnlockedBrush
	{
		get
		{
			return _unlockedBrush;
		}
		set
		{
			if (value != _unlockedBrush)
			{
				_unlockedBrush = value;
				OnPropertyChanged(value, "UnlockedBrush");
			}
		}
	}

	public OrderOfBattleFormationClassLockBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void OnLockStateSet()
	{
		if (IsLocked)
		{
			base.Brush = LockedBrush;
		}
		else
		{
			base.Brush = UnlockedBrush;
		}
		_isInitialStateSet = true;
	}
}
