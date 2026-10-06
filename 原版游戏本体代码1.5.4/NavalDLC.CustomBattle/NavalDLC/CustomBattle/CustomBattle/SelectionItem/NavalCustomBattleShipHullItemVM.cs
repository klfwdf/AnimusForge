using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.CustomBattle.CustomBattle.SelectionItem;

public class NavalCustomBattleShipHullItemVM : ViewModel
{
	public readonly ShipHull ShipHull;

	private readonly TextObject _nameText;

	private readonly Action<NavalCustomBattleShipHullItemVM> _onSelected;

	private BasicTooltipViewModel _tooltip;

	private HintViewModel _disabledHint;

	private string _name;

	private bool _isSelected;

	private bool _isDisabled;

	private bool _isEmpty;

	private string _prefabId;

	[DataSourceProperty]
	public BasicTooltipViewModel Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				OnPropertyChangedWithValue(value, "Tooltip");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisabledHint
	{
		get
		{
			return _disabledHint;
		}
		set
		{
			if (value != _disabledHint)
			{
				_disabledHint = value;
				OnPropertyChangedWithValue(value, "DisabledHint");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEmpty
	{
		get
		{
			return _isEmpty;
		}
		set
		{
			if (value != _isEmpty)
			{
				_isEmpty = value;
				OnPropertyChangedWithValue(value, "IsEmpty");
			}
		}
	}

	[DataSourceProperty]
	public string PrefabId
	{
		get
		{
			return _prefabId;
		}
		set
		{
			if (value != _prefabId)
			{
				_prefabId = value;
				OnPropertyChangedWithValue(value, "PrefabId");
			}
		}
	}

	public NavalCustomBattleShipHullItemVM(ShipHull shipHull, TextObject disabledHintText, Action<NavalCustomBattleShipHullItemVM> onSelected)
	{
		ShipHull = shipHull;
		PrefabId = NavalUIHelper.GetPrefabIdOfShipHull(ShipHull);
		_nameText = ShipHull.Name;
		Tooltip = new BasicTooltipViewModel(() => GetTooltip());
		DisabledHint = new HintViewModel(disabledHintText);
		_onSelected = onSelected;
		IsEmpty = false;
		RefreshValues();
	}

	public NavalCustomBattleShipHullItemVM(TextObject nameText, TextObject disabledHintText, Action<NavalCustomBattleShipHullItemVM> onSelected)
	{
		_nameText = nameText;
		_onSelected = onSelected;
		DisabledHint = new HintViewModel(disabledHintText);
		IsEmpty = true;
		RefreshValues();
	}

	protected virtual List<TooltipProperty> GetTooltip()
	{
		object[] invokedArgs = new object[1] { ShipHull };
		return new PropertyBasedTooltipVM(typeof(ShipHull), invokedArgs).TooltipPropertyList.ToList();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _nameText.ToString();
	}

	public void ExecuteSelect()
	{
		_onSelected?.Invoke(this);
	}
}
