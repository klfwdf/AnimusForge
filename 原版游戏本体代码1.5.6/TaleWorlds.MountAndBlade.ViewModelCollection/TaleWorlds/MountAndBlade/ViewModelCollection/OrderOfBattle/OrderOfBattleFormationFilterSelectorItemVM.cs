using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

public class OrderOfBattleFormationFilterSelectorItemVM : ViewModel
{
	public readonly FormationFilterType FilterType;

	private Action<OrderOfBattleFormationFilterSelectorItemVM> _onToggled;

	private int _filterType;

	private bool _isActive;

	private bool _isEnabled;

	private HintViewModel _hint;

	[DataSourceProperty]
	public int FilterTypeValue
	{
		get
		{
			return _filterType;
		}
		set
		{
			if (value != _filterType)
			{
				_filterType = value;
				OnPropertyChangedWithValue(value, "FilterTypeValue");
			}
		}
	}

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
				_onToggled?.Invoke(this);
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	public OrderOfBattleFormationFilterSelectorItemVM(FormationFilterType filterType, Action<OrderOfBattleFormationFilterSelectorItemVM> onToggled)
	{
		FilterType = filterType;
		FilterTypeValue = (int)filterType;
		_onToggled = onToggled;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		Hint = new HintViewModel(FilterType.GetFilterDescription());
	}
}
