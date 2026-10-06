using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace NavalDLC.ViewModelCollection.Port;

public class PortShipStashSettlementGroupVM : ViewModel
{
	private readonly Action<PortShipStashSettlementGroupVM> _onSelect;

	private string _settlementIconName;

	private string _settlementName;

	private MBBindingList<PortShipStashItemVM> _ships;

	private bool _isSelected;

	[DataSourceProperty]
	public string SettlementIconName
	{
		get
		{
			return _settlementIconName;
		}
		set
		{
			if (value != _settlementIconName)
			{
				_settlementIconName = value;
				OnPropertyChangedWithValue(value, "SettlementIconName");
			}
		}
	}

	[DataSourceProperty]
	public string SettlementName
	{
		get
		{
			return _settlementName;
		}
		set
		{
			if (value != _settlementName)
			{
				_settlementName = value;
				OnPropertyChangedWithValue(value, "SettlementName");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PortShipStashItemVM> Ships
	{
		get
		{
			return _ships;
		}
		set
		{
			if (value != _ships)
			{
				_ships = value;
				OnPropertyChangedWithValue(value, "Ships");
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

	public PortShipStashSettlementGroupVM(Settlement settlement, Action<PortShipStashSettlementGroupVM> onSelect)
	{
		_onSelect = onSelect;
		SettlementName = settlement.Name.ToString();
		SettlementComponent settlementComponent = settlement.SettlementComponent;
		SettlementIconName = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
		Ships = new MBBindingList<PortShipStashItemVM>();
	}

	public void ExecuteSelect()
	{
		_onSelect?.Invoke(this);
	}
}
