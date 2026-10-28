using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.ViewModelCollection.GameMenus;

public class NavalGameMenuShipItemVM : ViewModel
{
	public readonly Ship Ship;

	private readonly Action _onToggled;

	private bool _isSelected;

	private bool _isDisabled;

	private bool _hasCustomName;

	private float _maxHitPoints;

	private float _currentHitPoints;

	private string _prefabId;

	private string _name;

	private string _hullName;

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
	public bool HasCustomName
	{
		get
		{
			return _hasCustomName;
		}
		set
		{
			if (value != _hasCustomName)
			{
				_hasCustomName = value;
				OnPropertyChangedWithValue(value, "HasCustomName");
			}
		}
	}

	[DataSourceProperty]
	public float MaxHitPoints
	{
		get
		{
			return _maxHitPoints;
		}
		set
		{
			if (value != _maxHitPoints)
			{
				_maxHitPoints = value;
				OnPropertyChangedWithValue(value, "MaxHitPoints");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentHitPoints
	{
		get
		{
			return _currentHitPoints;
		}
		set
		{
			if (value != _currentHitPoints)
			{
				_currentHitPoints = value;
				OnPropertyChangedWithValue(value, "CurrentHitPoints");
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
	public string HullName
	{
		get
		{
			return _hullName;
		}
		set
		{
			if (value != _hullName)
			{
				_hullName = value;
				OnPropertyChangedWithValue(value, "HullName");
			}
		}
	}

	public NavalGameMenuShipItemVM(Ship ship, Action onToggled)
	{
		Ship = ship;
		_onToggled = onToggled;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PrefabId = NavalUIHelper.GetPrefabIdOfShipHull(Ship.ShipHull);
		Name = Ship.Name.ToString();
		HullName = Ship.ShipHull.Name.ToString();
		HasCustomName = Name != HullName;
		MaxHitPoints = Ship.MaxHitPoints;
		CurrentHitPoints = Ship.HitPoints;
	}

	public void ExecuteToggleSelect()
	{
		if (!IsDisabled)
		{
			IsSelected = !IsSelected;
			_onToggled?.Invoke();
		}
	}

	public void ExecuteLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(Campaign.Current.EncyclopediaManager.GetIdentifier(typeof(ShipHull)) + "-" + Ship.ShipHull.StringId);
	}

	public void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(Ship), Ship);
	}

	public void ExecuteEndHint()
	{
		InformationManager.HideTooltip();
	}
}
