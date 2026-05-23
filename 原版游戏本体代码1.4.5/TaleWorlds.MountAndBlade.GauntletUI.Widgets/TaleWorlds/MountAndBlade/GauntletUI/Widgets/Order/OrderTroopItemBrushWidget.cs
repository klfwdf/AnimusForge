using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

public class OrderTroopItemBrushWidget : BrushWidget
{
	private int _currentMemberCount;

	private bool _isSelectable;

	private bool _isSelected;

	private bool _hasAmmo = true;

	private Brush _rangedCardBrush;

	private Brush _meleeCardBrush;

	[Editor(false)]
	public int CurrentMemberCount
	{
		get
		{
			return _currentMemberCount;
		}
		set
		{
			if (_currentMemberCount != value)
			{
				_currentMemberCount = value;
				OnPropertyChanged(value, "CurrentMemberCount");
				CurrentMemberCountChanged();
			}
		}
	}

	[Editor(false)]
	public bool IsSelectable
	{
		get
		{
			return _isSelectable;
		}
		set
		{
			if (_isSelectable != value)
			{
				_isSelectable = value;
				OnPropertyChanged(value, "IsSelectable");
				SelectableStateChanged();
			}
		}
	}

	[Editor(false)]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChanged(value, "IsSelected");
				SelectionStateChanged();
			}
		}
	}

	[Editor(false)]
	public bool HasAmmo
	{
		get
		{
			return _hasAmmo;
		}
		set
		{
			if (_hasAmmo != value)
			{
				_hasAmmo = value;
				OnPropertyChanged(value, "HasAmmo");
				UpdateBrush();
			}
		}
	}

	[Editor(false)]
	public Brush RangedCardBrush
	{
		get
		{
			return _rangedCardBrush;
		}
		set
		{
			if (value != _rangedCardBrush)
			{
				_rangedCardBrush = value;
				OnPropertyChanged(value, "RangedCardBrush");
				UpdateBrush();
			}
		}
	}

	[Editor(false)]
	public Brush MeleeCardBrush
	{
		get
		{
			return _meleeCardBrush;
		}
		set
		{
			if (value != _meleeCardBrush)
			{
				_meleeCardBrush = value;
				OnPropertyChanged(value, "MeleeCardBrush");
				UpdateBrush();
			}
		}
	}

	public OrderTroopItemBrushWidget(UIContext context)
		: base(context)
	{
		AddState("Selected");
		AddState("Disabled");
		UpdateBrush();
	}

	private void SelectionStateChanged()
	{
		UpdateBackgroundState();
	}

	private void SelectableStateChanged()
	{
		UpdateBackgroundState();
	}

	private void CurrentMemberCountChanged()
	{
		UpdateBackgroundState();
	}

	private void UpdateBackgroundState()
	{
		if (CurrentMemberCount <= 0 || !IsSelectable)
		{
			SetState("Disabled");
		}
		else
		{
			SetState(IsSelected ? "Selected" : "Default");
		}
	}

	private void UpdateBrush()
	{
		if (MeleeCardBrush != null && RangedCardBrush != null)
		{
			if (HasAmmo)
			{
				base.Brush = RangedCardBrush;
			}
			else
			{
				base.Brush = MeleeCardBrush;
			}
			UpdateBackgroundState();
		}
	}
}
