using System.Collections.Generic;
using NavalDLC.ViewModelCollection.Port.PortScreenHandlers;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class ShipUpgradeContainerVM : ViewModel
{
	public delegate void ShipSlotSelectedDelegate(ShipUpgradeSlotBaseVM slot);

	public static ShipSlotSelectedDelegate OnSlotSelected;

	private bool _canTradeUpgrades;

	private bool _hasSelectedSlot;

	private ShipUpgradeSlotBaseVM _selectedSlot;

	private MBBindingList<ShipUpgradeSlotBaseVM> _upgradeSlots;

	[DataSourceProperty]
	public bool CanTradeUpgrades
	{
		get
		{
			return _canTradeUpgrades;
		}
		set
		{
			if (value != _canTradeUpgrades)
			{
				_canTradeUpgrades = value;
				OnPropertyChangedWithValue(value, "CanTradeUpgrades");
			}
		}
	}

	[DataSourceProperty]
	public bool HasSelectedSlot
	{
		get
		{
			return _hasSelectedSlot;
		}
		set
		{
			if (value != _hasSelectedSlot)
			{
				_hasSelectedSlot = value;
				OnPropertyChangedWithValue(value, "HasSelectedSlot");
			}
		}
	}

	[DataSourceProperty]
	public ShipUpgradeSlotBaseVM SelectedSlot
	{
		get
		{
			return _selectedSlot;
		}
		set
		{
			if (value != _selectedSlot)
			{
				if (_selectedSlot != null)
				{
					_selectedSlot.IsSelected = false;
				}
				_selectedSlot = value;
				OnPropertyChangedWithValue(value, "SelectedSlot");
				if (_selectedSlot != null)
				{
					_selectedSlot.IsSelected = true;
				}
				HasSelectedSlot = _selectedSlot != null;
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ShipUpgradeSlotBaseVM> UpgradeSlots
	{
		get
		{
			return _upgradeSlots;
		}
		set
		{
			if (value != _upgradeSlots)
			{
				_upgradeSlots = value;
				OnPropertyChangedWithValue(value, "UpgradeSlots");
			}
		}
	}

	public ShipUpgradeContainerVM(Ship ship)
	{
		UpgradeSlots = new MBBindingList<ShipUpgradeSlotBaseVM>();
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			UpgradeSlots.Add(new ShipUpgradeSlotVM(ship, availableSlot.Value.GetSlotTypeName(), availableSlot.Key, availableSlot.Value.TypeId, OnSlotSelectedAux));
		}
		if (ship.CanEquipFigurehead)
		{
			UpgradeSlots.Add(new ShipFigureheadSlotVM(ship, new TextObject("{=YLbBHN0Z}Figurehead"), "figurehead", "figurehead", OnSlotSelectedAux));
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		UpgradeSlots.ApplyActionOnAllItems(delegate(ShipUpgradeSlotBaseVM us)
		{
			us.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		UpgradeSlots.ApplyActionOnAllItems(delegate(ShipUpgradeSlotBaseVM us)
		{
			us.OnFinalize();
		});
	}

	public void ResetUpgradePieces()
	{
		UpgradeSlots.ApplyActionOnAllItems(delegate(ShipUpgradeSlotBaseVM s)
		{
			s.ResetPieces();
		});
	}

	public void UpdateEnabledStatus(in PortActionInfo actionInfo)
	{
		CanTradeUpgrades = actionInfo.IsEnabled;
		for (int i = 0; i < UpgradeSlots.Count; i++)
		{
			UpgradeSlots[i].UpdateEnabledStatus(in actionInfo);
		}
	}

	private void OnSlotSelectedAux(ShipUpgradeSlotBaseVM slot)
	{
		if (SelectedSlot != null && SelectedSlot == slot)
		{
			SelectedSlot = null;
			OnSlotSelected?.Invoke(SelectedSlot);
		}
		else if (slot == null || slot.AvailablePieces.Count != 0 || slot.HasSelectedPiece)
		{
			SelectedSlot = slot;
			OnSlotSelected?.Invoke(SelectedSlot);
		}
	}

	public void ExecuteClearSelection()
	{
		SelectedSlot?.ExecuteDeselect();
	}

	public void Update()
	{
		for (int i = 0; i < UpgradeSlots.Count; i++)
		{
			UpgradeSlots[i].Update();
		}
	}
}
