using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class PortShipStashItemVM : ViewModel
{
	private readonly ShipItemVM _shipItemVM;

	private readonly Ship _ship;

	private Action<PortShipStashItemVM> _onToggleSelect;

	private string _name;

	private string _prefabId;

	private bool _isEnabled;

	private bool _isSelected;

	private bool _isHealthRelevant;

	private bool _hasChanges;

	private float _maxHp;

	private float _initialHp;

	private float _currentHp;

	private MBBindingList<PortShipPropertyVM> _properties;

	private ShipUpgradeContainerVM _upgrades;

	public Ship CurrentShip
	{
		get
		{
			if (_shipItemVM == null)
			{
				return _ship;
			}
			return _shipItemVM.Ship;
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
	public MBBindingList<PortShipPropertyVM> Properties
	{
		get
		{
			return _properties;
		}
		set
		{
			if (value != _properties)
			{
				_properties = value;
				OnPropertyChangedWithValue(value, "Properties");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHealthRelevant
	{
		get
		{
			return _isHealthRelevant;
		}
		set
		{
			if (value != _isHealthRelevant)
			{
				_isHealthRelevant = value;
				OnPropertyChangedWithValue(value, "IsHealthRelevant");
			}
		}
	}

	[DataSourceProperty]
	public bool HasChanges
	{
		get
		{
			return _hasChanges;
		}
		set
		{
			if (value != _hasChanges)
			{
				_hasChanges = value;
				OnPropertyChangedWithValue(value, "HasChanges");
			}
		}
	}

	[DataSourceProperty]
	public float MaxHp
	{
		get
		{
			return _maxHp;
		}
		set
		{
			if (value != _maxHp)
			{
				_maxHp = value;
				OnPropertyChangedWithValue(value, "MaxHp");
			}
		}
	}

	[DataSourceProperty]
	public float InitialHp
	{
		get
		{
			return _initialHp;
		}
		set
		{
			if (value != _initialHp)
			{
				_initialHp = value;
				OnPropertyChangedWithValue(value, "InitialHp");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentHp
	{
		get
		{
			return _currentHp;
		}
		set
		{
			if (value != _currentHp)
			{
				_currentHp = value;
				OnPropertyChangedWithValue(value, "CurrentHp");
			}
		}
	}

	[DataSourceProperty]
	public ShipUpgradeContainerVM Upgrades
	{
		get
		{
			return _upgrades;
		}
		set
		{
			if (value != _upgrades)
			{
				_upgrades = value;
				OnPropertyChangedWithValue(value, "Upgrades");
			}
		}
	}

	public PortShipStashItemVM(ShipItemVM shipItemVM, bool isEnabled, Action<PortShipStashItemVM> onToggleSelect)
	{
		_shipItemVM = shipItemVM;
		CreatePortShipStashItemVM(isEnabled, onToggleSelect);
	}

	public PortShipStashItemVM(Ship ship, bool isEnabled, Action<PortShipStashItemVM> onToggleSelect)
	{
		_ship = ship;
		CreatePortShipStashItemVM(isEnabled, onToggleSelect);
	}

	private void CreatePortShipStashItemVM(bool isEnabled, Action<PortShipStashItemVM> onToggleSelect)
	{
		Upgrades = ((_shipItemVM != null) ? _shipItemVM.Upgrades : new ShipUpgradeContainerVM(_ship));
		PrefabId = NavalUIHelper.GetPrefabIdOfShipHull(CurrentShip.ShipHull);
		_onToggleSelect = onToggleSelect;
		IsEnabled = isEnabled;
		Properties = new MBBindingList<PortShipPropertyVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Properties.Clear();
		Upgrades.RefreshValues();
		HasChanges = _shipItemVM != null && _shipItemVM.HasChanges;
		Name = ((_shipItemVM != null) ? _shipItemVM.Name : CurrentShip.Name.ToString());
		TextObject value = new TextObject("{=!}{VALUE}").SetTextVariable("VALUE", CurrentShip.ShipHull.Name.ToString());
		Properties.Add(new PortShipPropertyVM(new TextObject("{=wEmx6fZi}Hull"), value));
		InitialHp = CurrentShip.HitPoints;
		MaxHp = CurrentShip.MaxHitPoints;
		CurrentHp = ((_shipItemVM != null && _shipItemVM.IsRepaired) ? MaxHp : InitialHp);
		IsHealthRelevant = InitialHp < MaxHp;
		TextObject value2 = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", (int)CurrentHp).SetTextVariable("RIGHT", (int)MaxHp);
		Properties.Add(new PortShipPropertyVM(new TextObject("{=oBbiVeKE}Hit Points"), value2));
		TextObject value3 = new TextObject("{=!}{VALUE}").SetTextVariable("VALUE", CurrentShip.GetCampaignSpeed().ToString("0.##"));
		Properties.Add(new PortShipPropertyVM(new TextObject("{=DbERaPfF}Travel Speed"), value3));
		TextObject value4 = new TextObject("{=!}{VALUE}").SetTextVariable("VALUE", CurrentShip.GetCombatFactor().ToString("0.##"));
		Properties.Add(new PortShipPropertyVM(new TextObject("{=81X52bc3}Combat Factor"), value4));
		TextObject value5 = new TextObject("{=!}{VALUE}").SetTextVariable("VALUE", CurrentShip.SeaWorthiness.ToString());
		Properties.Add(new PortShipPropertyVM(new TextObject("{=yCzuXN3O}Seaworthiness"), value5));
	}

	public void ExecuteToggleSelect()
	{
		if (IsEnabled)
		{
			IsSelected = !IsSelected;
			_onToggleSelect?.Invoke(this);
		}
	}
}
