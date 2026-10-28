using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.CustomBattle.CustomBattle.SelectionItem;

public class NavalCustomBattleShipItemVM : NavalCustomBattleShipHullItemVM
{
	private readonly Action _onUpgraded;

	private int _tier;

	private HintViewModel _cycleTierHint;

	public CustomBattleShip Ship { get; private set; }

	[DataSourceProperty]
	public int Tier
	{
		get
		{
			return _tier;
		}
		set
		{
			if (value != _tier)
			{
				_tier = value;
				OnPropertyChangedWithValue(value, "Tier");
				OnTierSelection();
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CycleTierHint
	{
		get
		{
			return _cycleTierHint;
		}
		set
		{
			if (value != _cycleTierHint)
			{
				_cycleTierHint = value;
				OnPropertyChangedWithValue(value, "CycleTierHint");
			}
		}
	}

	public NavalCustomBattleShipItemVM(ShipHull shipHull, bool isPlayerShip, Action onUpgraded)
		: base(shipHull, null, null)
	{
		Ship = new CustomBattleShip(ShipHull, isPlayerShip);
		_onUpgraded = onUpgraded;
		CycleTierHint = new HintViewModel(new TextObject("{=zbkzFaWE}Change upgrade tier"));
	}

	public void ExecuteCycleUpgradeTier()
	{
		Tier = (Tier + 1) % 4;
	}

	public void RandomizeUpgrades()
	{
		Tier = MBRandom.RandomInt(0, 4);
	}

	private void OnTierSelection()
	{
		if (Tier == 0)
		{
			foreach (KeyValuePair<string, ShipSlot> availableSlot in ShipHull.AvailableSlots)
			{
				Ship.SetPieceAtSlot(availableSlot.Key, null);
			}
		}
		else
		{
			IEnumerable<ShipUpgradePiece> source = from x in MBObjectManager.Instance.GetObjectTypeList<ShipUpgradePiece>()
				where !x.NotMerchandise
				select x;
			IEnumerable<ShipUpgradePiece> source2 = source.Where((ShipUpgradePiece x) => x.RequiredPortLevel == Tier);
			foreach (KeyValuePair<string, ShipSlot> slot in ShipHull.AvailableSlots)
			{
				ShipUpgradePiece upgradePiece = ((source2.Count() != 0) ? source2.Where((ShipUpgradePiece x) => x.DoesPieceMatchSlot(slot.Value)).GetRandomElementInefficiently() : source.Where((ShipUpgradePiece x) => x.RequiredPortLevel <= Tier && x.DoesPieceMatchSlot(slot.Value)).GetRandomElementInefficiently());
				Ship.SetPieceAtSlot(slot.Key, upgradePiece);
			}
		}
		_onUpgraded?.Invoke();
	}

	protected override List<TooltipProperty> GetTooltip()
	{
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty(base.Name.ToString(), string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title),
			new TooltipProperty(new TextObject("{=sqdzHOPe}Class").ToString(), GameTexts.FindText("str_ship_type", ShipHull.Type.ToString().ToLowerInvariant()).ToString(), 0),
			new TooltipProperty(new TextObject("{=UbZL2BJQ}Hitpoints").ToString(), ((int)Ship.MaxHitPoints).ToString(), 0)
		};
		int num = Ship.TotalCrewCapacity - Ship.MainDeckCrewCapacity;
		list.Add(new TooltipProperty(value: (num <= 0) ? Ship.TotalCrewCapacity.ToString() : new TextObject("{=r2fvxfwZ}{TOTAL} ({MAIN_DECK}+{RESERVE})").SetTextVariable("TOTAL", Ship.TotalCrewCapacity.ToString()).SetTextVariable("MAIN_DECK", Ship.MainDeckCrewCapacity.ToString()).SetTextVariable("RESERVE", num.ToString())
			.ToString(), definition: new TextObject("{=oqVVGxgb}Crew Capacity").ToString(), textHeight: 0));
		List<ShipSlotAndPieceName> shipSlotAndPieceNames = Ship.GetShipSlotAndPieceNames();
		if (shipSlotAndPieceNames.Count > 0)
		{
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
			list.Add(new TooltipProperty(string.Empty, new TextObject("{=zMvUzdKR}Ship Upgrades").ToString(), -1));
			foreach (ShipSlotAndPieceName item in shipSlotAndPieceNames)
			{
				list.Add(new TooltipProperty(item.SlotName, item.PieceName, 0));
			}
		}
		return list;
	}
}
