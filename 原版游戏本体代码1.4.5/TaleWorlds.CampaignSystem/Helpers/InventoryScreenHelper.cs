using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class InventoryScreenHelper
{
	public enum InventoryMode
	{
		Default,
		Trade,
		Loot,
		Stash,
		Warehouse
	}

	public delegate void InventoryFinishDelegate();

	[Flags]
	public enum InventoryItemType
	{
		None = 0,
		Weapon = 1,
		Shield = 2,
		HeadArmor = 4,
		BodyArmor = 8,
		LegArmor = 0x10,
		HandArmor = 0x20,
		Horse = 0x40,
		HorseHarness = 0x80,
		Goods = 0x100,
		Book = 0x200,
		Animal = 0x400,
		Cape = 0x800,
		Banner = 0x1000,
		HorseCategory = 0xC0,
		Armors = 0x83C,
		Equipable = 0x18FF,
		All = 0xFFF
	}

	public enum InventoryCategoryType
	{
		None = -1,
		All,
		Armors,
		Weapon,
		Shield,
		HorseCategory,
		Goods,
		CategoryTypeAmount
	}

	private class CaravanInventoryListener : InventoryListener
	{
		private MobileParty _caravan;

		public CaravanInventoryListener(MobileParty caravan)
		{
			_caravan = caravan;
		}

		public override int GetGold()
		{
			return _caravan.PartyTradeGold;
		}

		public override TextObject GetTraderName()
		{
			if (_caravan.LeaderHero == null)
			{
				return _caravan.Name;
			}
			return _caravan.LeaderHero.Name;
		}

		public override void SetGold(int gold)
		{
			_caravan.PartyTradeGold = gold;
		}

		public override PartyBase GetOppositeParty()
		{
			return _caravan.Party;
		}

		public override void OnTransaction()
		{
			throw new NotImplementedException();
		}
	}

	private class MerchantInventoryListener : InventoryListener
	{
		private SettlementComponent _settlementComponent;

		public MerchantInventoryListener(SettlementComponent settlementComponent)
		{
			_settlementComponent = settlementComponent;
		}

		public override TextObject GetTraderName()
		{
			return _settlementComponent.Owner.Name;
		}

		public override PartyBase GetOppositeParty()
		{
			return _settlementComponent.Owner;
		}

		public override int GetGold()
		{
			return _settlementComponent.Gold;
		}

		public override void SetGold(int gold)
		{
			_settlementComponent.ChangeGold(gold - _settlementComponent.Gold);
		}

		public override void OnTransaction()
		{
			throw new NotImplementedException();
		}
	}

	public static InventoryState GetActiveInventoryState()
	{
		if (GameStateManager.Current?.ActiveState is InventoryState result)
		{
			return result;
		}
		Debug.FailedAssert("GetActiveInventoryState requested but the active state is not InventoryState!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetActiveInventoryState", 8611);
		return null;
	}

	public static void PlayerAcceptTradeOffer()
	{
		GetActiveInventoryState()?.InventoryLogic?.SetPlayerAcceptTraderOffer();
	}

	public static void CloseScreen(bool fromCancel)
	{
		CloseInventoryPresentation(fromCancel);
	}

	private static void CloseInventoryPresentation(bool fromCancel)
	{
		InventoryState activeInventoryState = GetActiveInventoryState();
		InventoryLogic inventoryLogic = activeInventoryState?.InventoryLogic;
		if (fromCancel)
		{
			inventoryLogic?.Reset(fromCancel);
		}
		if (inventoryLogic != null && inventoryLogic.DoneLogic())
		{
			activeInventoryState.DoneLogicExtrasDelegate?.Invoke();
			activeInventoryState.DoneLogicExtrasDelegate = null;
			activeInventoryState.InventoryLogic = null;
			Game.Current.GameStateManager.PopState();
		}
	}

	private static void OpenInventoryPresentation(TextObject leftRosterName, Action doneLogicExtrasDelegate = null)
	{
		ItemRoster itemRoster = new ItemRoster();
		if (Game.Current.CheatMode)
		{
			TestCommonBase baseInstance = TestCommonBase.BaseInstance;
			if (baseInstance == null || !baseInstance.IsTestEnabled)
			{
				MBReadOnlyList<ItemObject> objectTypeList = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>();
				for (int i = 0; i != objectTypeList.Count; i++)
				{
					ItemObject item = objectTypeList[i];
					itemRoster.AddToCounts(item, 10);
				}
			}
		}
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		InventoryLogic inventoryLogic = new InventoryLogic(null);
		inventoryLogic.Initialize(itemRoster, MobileParty.MainParty, isTrading: false, isSpecialActionsPermitted: true, CharacterObject.PlayerCharacter, InventoryCategoryType.None, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode, leftRosterName);
		inventoryState.InventoryLogic = inventoryLogic;
		inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	private static IMarketData GetCurrentMarketDataForPlayer()
	{
		IMarketData marketData = null;
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
		{
			Settlement settlement = MobileParty.MainParty.CurrentSettlement;
			if (settlement == null)
			{
				settlement = SettlementHelper.FindNearestTownToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All)?.Settlement;
			}
			if (settlement != null)
			{
				if (settlement.IsVillage)
				{
					marketData = settlement.Village.MarketData;
				}
				else if (settlement.IsTown)
				{
					marketData = settlement.Town.MarketData;
				}
			}
		}
		if (marketData == null)
		{
			marketData = new FakeMarketData();
		}
		return marketData;
	}

	public static void OpenScreenAsInventoryOfSubParty(MobileParty rightParty, MobileParty leftParty, Action doneLogicExtrasDelegate)
	{
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		InventoryLogic inventoryLogic = new InventoryLogic(rightParty, rightParty.LeaderHero?.CharacterObject, leftParty.Party);
		inventoryLogic.Initialize(leftParty.ItemRoster, rightParty.ItemRoster, rightParty.MemberRoster, isTrading: false, isSpecialActionsPermitted: false, rightParty.LeaderHero?.CharacterObject, InventoryCategoryType.None, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode);
		inventoryState.InventoryLogic = inventoryLogic;
		inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static void OpenScreenAsInventoryForCraftedItemDecomposition(MobileParty party, CharacterObject character, Action doneLogicExtrasDelegate)
	{
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		InventoryLogic inventoryLogic = new InventoryLogic(null);
		inventoryLogic.Initialize(new ItemRoster(), party.ItemRoster, party.MemberRoster, isTrading: false, isSpecialActionsPermitted: false, character, InventoryCategoryType.None, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode);
		inventoryState.InventoryLogic = inventoryLogic;
		inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static void OpenScreenAsInventoryOf(MobileParty party, CharacterObject character)
	{
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		InventoryLogic inventoryLogic = new InventoryLogic(null);
		inventoryLogic.Initialize(new ItemRoster(), party.ItemRoster, party.MemberRoster, isTrading: false, isSpecialActionsPermitted: true, character, InventoryCategoryType.None, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode);
		inventoryState.InventoryLogic = inventoryLogic;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static void OpenScreenAsInventoryOf(PartyBase rightParty, PartyBase leftParty)
	{
		OpenScreenAsInventoryOf(rightParty, leftParty, rightParty.LeaderHero?.CharacterObject);
	}

	public static void OpenScreenAsInventoryOf(PartyBase rightParty, PartyBase leftParty, CharacterObject character, TextObject leftRosterName = null, InventoryLogic.CapacityData capacityData = null, Action doneLogicExtrasDelegate = null)
	{
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		InventoryLogic inventoryLogic = new InventoryLogic(leftParty);
		inventoryLogic.Initialize(leftParty.ItemRoster, rightParty.ItemRoster, rightParty.MemberRoster, isTrading: false, isSpecialActionsPermitted: false, character, InventoryCategoryType.None, GetCurrentMarketDataForPlayer(), useBasePrices: false, InventoryMode.Default, leftRosterName, leftParty.MemberRoster, capacityData);
		inventoryState.InventoryLogic = inventoryLogic;
		inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static void OpenScreenAsInventory(Action doneLogicExtrasDelegate = null)
	{
		OpenInventoryPresentation(new TextObject("{=02c5bQSM}Discard"), doneLogicExtrasDelegate);
	}

	public static void OpenScreenAsLoot(Dictionary<PartyBase, ItemRoster> itemRostersToLoot)
	{
		ItemRoster leftItemRoster = itemRostersToLoot[PartyBase.MainParty];
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		inventoryState.InventoryMode = InventoryMode.Loot;
		InventoryLogic inventoryLogic = new InventoryLogic(null);
		inventoryLogic.Initialize(leftItemRoster, MobileParty.MainParty.ItemRoster, MobileParty.MainParty.MemberRoster, isTrading: false, isSpecialActionsPermitted: true, CharacterObject.PlayerCharacter, InventoryCategoryType.None, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode, GameTexts.FindText("str_loot"));
		inventoryState.InventoryLogic = inventoryLogic;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static void OpenScreenAsStash(ItemRoster stash)
	{
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		inventoryState.InventoryMode = InventoryMode.Stash;
		InventoryLogic inventoryLogic = new InventoryLogic(null);
		inventoryLogic.Initialize(stash, MobileParty.MainParty, isTrading: false, isSpecialActionsPermitted: false, CharacterObject.PlayerCharacter, InventoryCategoryType.None, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode, new TextObject("{=nZbaYvVx}Stash"));
		inventoryState.InventoryLogic = inventoryLogic;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static void OpenScreenAsWarehouse(ItemRoster stash, InventoryLogic.CapacityData otherSideCapacity)
	{
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		inventoryState.InventoryMode = InventoryMode.Warehouse;
		InventoryLogic inventoryLogic = new InventoryLogic(null);
		inventoryLogic.Initialize(stash, MobileParty.MainParty, isTrading: false, isSpecialActionsPermitted: false, CharacterObject.PlayerCharacter, InventoryCategoryType.None, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode, new TextObject("{=anTRftmb}Warehouse"), null, otherSideCapacity);
		inventoryState.InventoryLogic = inventoryLogic;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static void OpenScreenAsReceiveItems(ItemRoster items, TextObject leftRosterName, Action doneLogicDelegate = null)
	{
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		InventoryLogic inventoryLogic = new InventoryLogic(null);
		inventoryLogic.Initialize(items, MobileParty.MainParty.ItemRoster, MobileParty.MainParty.MemberRoster, isTrading: false, isSpecialActionsPermitted: true, CharacterObject.PlayerCharacter, InventoryCategoryType.None, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode, leftRosterName);
		inventoryState.InventoryLogic = inventoryLogic;
		inventoryState.DoneLogicExtrasDelegate = doneLogicDelegate;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static void OpenTradeWithCaravanOrAlleyParty(MobileParty caravan, InventoryCategoryType merchantItemType = InventoryCategoryType.None)
	{
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		inventoryState.InventoryMode = InventoryMode.Trade;
		InventoryLogic inventoryLogic = new InventoryLogic(caravan.Party);
		inventoryLogic.Initialize(caravan.Party.ItemRoster, PartyBase.MainParty.ItemRoster, PartyBase.MainParty.MemberRoster, isTrading: true, isSpecialActionsPermitted: true, CharacterObject.PlayerCharacter, merchantItemType, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode);
		inventoryLogic.SetInventoryListener(new CaravanInventoryListener(caravan));
		inventoryState.InventoryLogic = inventoryLogic;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static void ActivateTradeWithCurrentSettlement()
	{
		OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.SettlementComponent);
	}

	public static void OpenScreenAsTrade(ItemRoster leftRoster, SettlementComponent settlementComponent, InventoryCategoryType merchantItemType = InventoryCategoryType.None, Action doneLogicExtrasDelegate = null)
	{
		InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
		inventoryState.InventoryMode = InventoryMode.Trade;
		InventoryLogic inventoryLogic = new InventoryLogic(settlementComponent.Owner);
		inventoryLogic.Initialize(leftRoster, PartyBase.MainParty.ItemRoster, PartyBase.MainParty.MemberRoster, isTrading: true, isSpecialActionsPermitted: true, CharacterObject.PlayerCharacter, merchantItemType, GetCurrentMarketDataForPlayer(), useBasePrices: false, inventoryState.InventoryMode);
		inventoryLogic.SetInventoryListener(new MerchantInventoryListener(settlementComponent));
		inventoryState.InventoryLogic = inventoryLogic;
		inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
		Game.Current.GameStateManager.PushState(inventoryState);
	}

	public static InventoryItemType GetInventoryItemTypeOfItem(ItemObject item)
	{
		if (item != null)
		{
			switch (item.ItemType)
			{
			case ItemObject.ItemTypeEnum.Horse:
				return InventoryItemType.Horse;
			case ItemObject.ItemTypeEnum.OneHandedWeapon:
			case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			case ItemObject.ItemTypeEnum.Polearm:
			case ItemObject.ItemTypeEnum.Arrows:
			case ItemObject.ItemTypeEnum.Bolts:
			case ItemObject.ItemTypeEnum.SlingStones:
			case ItemObject.ItemTypeEnum.Bow:
			case ItemObject.ItemTypeEnum.Crossbow:
			case ItemObject.ItemTypeEnum.Sling:
			case ItemObject.ItemTypeEnum.Thrown:
			case ItemObject.ItemTypeEnum.Pistol:
			case ItemObject.ItemTypeEnum.Musket:
			case ItemObject.ItemTypeEnum.Bullets:
				return InventoryItemType.Weapon;
			case ItemObject.ItemTypeEnum.Shield:
				return InventoryItemType.Shield;
			case ItemObject.ItemTypeEnum.Goods:
				return InventoryItemType.Goods;
			case ItemObject.ItemTypeEnum.HeadArmor:
				return InventoryItemType.HeadArmor;
			case ItemObject.ItemTypeEnum.BodyArmor:
				return InventoryItemType.BodyArmor;
			case ItemObject.ItemTypeEnum.LegArmor:
				return InventoryItemType.LegArmor;
			case ItemObject.ItemTypeEnum.HandArmor:
				return InventoryItemType.HandArmor;
			case ItemObject.ItemTypeEnum.Animal:
				return InventoryItemType.Animal;
			case ItemObject.ItemTypeEnum.Book:
				return InventoryItemType.Book;
			case ItemObject.ItemTypeEnum.HorseHarness:
				return InventoryItemType.HorseHarness;
			case ItemObject.ItemTypeEnum.Cape:
				return InventoryItemType.Cape;
			case ItemObject.ItemTypeEnum.Banner:
				return InventoryItemType.Banner;
			}
		}
		return InventoryItemType.None;
	}
}
