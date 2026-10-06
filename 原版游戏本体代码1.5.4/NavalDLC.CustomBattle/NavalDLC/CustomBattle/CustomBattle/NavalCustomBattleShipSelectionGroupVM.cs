using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.CustomBattle.CustomBattle.SelectionItem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace NavalDLC.CustomBattle.CustomBattle;

public class NavalCustomBattleShipSelectionGroupVM : ViewModel
{
	private readonly Action _onShipSelectedOrUpgraded;

	private bool _isRaid;

	private MBBindingList<NavalCustomBattleShipSelectionItemVM> _shipSelectionItems;

	[DataSourceProperty]
	public bool IsRaid
	{
		get
		{
			return _isRaid;
		}
		set
		{
			if (value != _isRaid)
			{
				_isRaid = value;
				OnPropertyChangedWithValue(value, "IsRaid");
				for (int i = 0; i < ShipSelectionItems.Count; i++)
				{
					ShipSelectionItems[i].IsRaid = value;
					ShipSelectionItems[i].IsRelevant = !value || i < 3;
				}
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<NavalCustomBattleShipSelectionItemVM> ShipSelectionItems
	{
		get
		{
			return _shipSelectionItems;
		}
		set
		{
			if (value != _shipSelectionItems)
			{
				_shipSelectionItems = value;
				OnPropertyChangedWithValue(value, "ShipSelectionItems");
			}
		}
	}

	public NavalCustomBattleShipSelectionGroupVM(bool isPlayerSide, NavalCustomBattleShipSelectionPopUpVM shipSelectionPopUp, Action onShipSelectedOrUpgraded, Action<NavalCustomBattleShipItemVM> onShipFocused)
	{
		_onShipSelectedOrUpgraded = onShipSelectedOrUpgraded;
		ShipSelectionItems = new MBBindingList<NavalCustomBattleShipSelectionItemVM>();
		for (int i = 0; i < 8; i++)
		{
			ShipSelectionItems.Add(new NavalCustomBattleShipSelectionItemVM(isPlayerSide, shipSelectionPopUp, OnShipSelectedOrUpgraded, onShipFocused));
		}
		ShipSelectionItems[0].SelectedItem = new NavalCustomBattleShipItemVM(NavalCustomBattleData.ShipHulls.ElementAt(0), isPlayerSide, OnShipSelectedOrUpgraded);
		UpdateCanShipsBecomeEmpty();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ShipSelectionItems.ApplyActionOnAllItems(delegate(NavalCustomBattleShipSelectionItemVM x)
		{
			x.RefreshValues();
		});
	}

	public void ExecuteRandomize(int targetDeckSize)
	{
		List<ShipHull> source = new List<ShipHull>();
		int num = int.MaxValue;
		for (int i = 0; i < 20; i++)
		{
			int deckSize;
			List<ShipHull> list = CreateRandomFleet(targetDeckSize, out deckSize);
			int num2 = Math.Abs(targetDeckSize - deckSize);
			if (num2 < num)
			{
				num = num2;
				source = list;
				if (num2 == 0)
				{
					break;
				}
			}
		}
		for (int j = 0; j < ShipSelectionItems.Count; j++)
		{
			ShipSelectionItems[j].SetHull(source.ElementAtOrDefault(j));
			ShipSelectionItems[j].SelectedItem?.RandomizeUpgrades();
		}
	}

	private List<ShipHull> CreateRandomFleet(int targetDeckSize, out int deckSize)
	{
		List<ShipHull> list = new List<ShipHull>();
		deckSize = 0;
		for (int i = 0; i < ShipSelectionItems.Count((NavalCustomBattleShipSelectionItemVM x) => x.IsRelevant); i++)
		{
			ShipHull shipHull = (IsRaid ? NavalCustomBattleData.ShipHulls.ToArray().GetRandomElementWithPredicate((ShipHull x) => NavalCustomBattleHelper.CanShipHullBeUsedInRaid(x)) : NavalCustomBattleData.ShipHulls.GetRandomElementInefficiently());
			list.Add(shipHull);
			deckSize += shipHull.MainDeckCrewCapacity;
			if (deckSize >= targetDeckSize)
			{
				break;
			}
		}
		return list;
	}

	public List<IShipOrigin> GetSelectedShips()
	{
		List<IShipOrigin> list = new List<IShipOrigin>();
		foreach (NavalCustomBattleShipSelectionItemVM shipSelectionItem in ShipSelectionItems)
		{
			if (shipSelectionItem.IsRelevant && shipSelectionItem.HasSelectedItem)
			{
				list.Add(shipSelectionItem.SelectedItem.Ship);
			}
		}
		return list;
	}

	private void OnShipSelectedOrUpgraded()
	{
		_onShipSelectedOrUpgraded?.Invoke();
		UpdateCanShipsBecomeEmpty();
	}

	private void UpdateCanShipsBecomeEmpty()
	{
		int totalSelectedItemCount = ShipSelectionItems.Count((NavalCustomBattleShipSelectionItemVM x) => x.IsRelevant && x.HasSelectedItem);
		ShipSelectionItems.ApplyActionOnAllItems(delegate(NavalCustomBattleShipSelectionItemVM x)
		{
			x.CanBecomeEmpty = x.HasSelectedItem && totalSelectedItemCount > 1;
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ShipSelectionItems.ApplyActionOnAllItems(delegate(NavalCustomBattleShipSelectionItemVM x)
		{
			x.OnFinalize();
		});
	}

	public void SetCycleTierInputKey(HotKey hotkey)
	{
		foreach (NavalCustomBattleShipSelectionItemVM shipSelectionItem in ShipSelectionItems)
		{
			shipSelectionItem.SetCycleTierInputKey(hotkey);
		}
	}
}
