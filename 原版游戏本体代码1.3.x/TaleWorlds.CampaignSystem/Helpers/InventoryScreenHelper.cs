using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers
{
	// Token: 0x02000023 RID: 35
	public static class InventoryScreenHelper
	{
		// Token: 0x0600013A RID: 314 RVA: 0x0000EEBC File Offset: 0x0000D0BC
		public static InventoryState GetActiveInventoryState()
		{
			GameStateManager gameStateManager = GameStateManager.Current;
			InventoryState result;
			if ((result = ((gameStateManager != null) ? gameStateManager.ActiveState : null) as InventoryState) != null)
			{
				return result;
			}
			Debug.FailedAssert("GetActiveInventoryState requested but the active state is not InventoryState!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetActiveInventoryState", 8498);
			return null;
		}

		// Token: 0x0600013B RID: 315 RVA: 0x0000EEFF File Offset: 0x0000D0FF
		public static void PlayerAcceptTradeOffer()
		{
			InventoryState activeInventoryState = InventoryScreenHelper.GetActiveInventoryState();
			if (activeInventoryState == null)
			{
				return;
			}
			InventoryLogic inventoryLogic = activeInventoryState.InventoryLogic;
			if (inventoryLogic == null)
			{
				return;
			}
			inventoryLogic.SetPlayerAcceptTraderOffer();
		}

		// Token: 0x0600013C RID: 316 RVA: 0x0000EF1A File Offset: 0x0000D11A
		public static void CloseScreen(bool fromCancel)
		{
			InventoryScreenHelper.CloseInventoryPresentation(fromCancel);
		}

		// Token: 0x0600013D RID: 317 RVA: 0x0000EF24 File Offset: 0x0000D124
		private static void CloseInventoryPresentation(bool fromCancel)
		{
			InventoryState activeInventoryState = InventoryScreenHelper.GetActiveInventoryState();
			InventoryLogic inventoryLogic = ((activeInventoryState != null) ? activeInventoryState.InventoryLogic : null);
			if (fromCancel && inventoryLogic != null)
			{
				inventoryLogic.Reset(fromCancel);
			}
			if (inventoryLogic != null && inventoryLogic.DoneLogic())
			{
				Action doneLogicExtrasDelegate = activeInventoryState.DoneLogicExtrasDelegate;
				if (doneLogicExtrasDelegate != null)
				{
					doneLogicExtrasDelegate();
				}
				activeInventoryState.DoneLogicExtrasDelegate = null;
				activeInventoryState.InventoryLogic = null;
				Game.Current.GameStateManager.PopState(0);
			}
		}

		// Token: 0x0600013E RID: 318 RVA: 0x0000EF8C File Offset: 0x0000D18C
		private static void OpenInventoryPresentation(TextObject leftRosterName, Action doneLogicExtrasDelegate = null)
		{
			ItemRoster itemRoster = new ItemRoster();
			if (Game.Current.CheatMode)
			{
				TestCommonBase baseInstance = TestCommonBase.BaseInstance;
				if (baseInstance == null || !baseInstance.IsTestEnabled)
				{
					MBReadOnlyList<ItemObject> objectTypeList = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>();
					for (int num = 0; num != objectTypeList.Count; num++)
					{
						ItemObject item = objectTypeList[num];
						itemRoster.AddToCounts(item, 10);
					}
				}
			}
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			InventoryLogic inventoryLogic = new InventoryLogic(null);
			inventoryLogic.Initialize(itemRoster, MobileParty.MainParty, false, true, CharacterObject.PlayerCharacter, InventoryScreenHelper.InventoryCategoryType.None, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, leftRosterName, null, null);
			inventoryState.InventoryLogic = inventoryLogic;
			inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x0600013F RID: 319 RVA: 0x0000F054 File Offset: 0x0000D254
		private static IMarketData GetCurrentMarketDataForPlayer()
		{
			IMarketData marketData = null;
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				Settlement settlement = MobileParty.MainParty.CurrentSettlement;
				if (settlement == null)
				{
					Town town = SettlementHelper.FindNearestTownToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, null);
					settlement = ((town != null) ? town.Settlement : null);
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

		// Token: 0x06000140 RID: 320 RVA: 0x0000F0D0 File Offset: 0x0000D2D0
		public static void OpenScreenAsInventoryOfSubParty(MobileParty rightParty, MobileParty leftParty, Action doneLogicExtrasDelegate)
		{
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			Hero leaderHero = rightParty.LeaderHero;
			InventoryLogic inventoryLogic = new InventoryLogic(rightParty, (leaderHero != null) ? leaderHero.CharacterObject : null, leftParty.Party);
			InventoryLogic inventoryLogic2 = inventoryLogic;
			ItemRoster itemRoster = leftParty.ItemRoster;
			ItemRoster itemRoster2 = rightParty.ItemRoster;
			TroopRoster memberRoster = rightParty.MemberRoster;
			bool isTrading = false;
			bool isSpecialActionsPermitted = false;
			Hero leaderHero2 = rightParty.LeaderHero;
			inventoryLogic2.Initialize(itemRoster, itemRoster2, memberRoster, isTrading, isSpecialActionsPermitted, (leaderHero2 != null) ? leaderHero2.CharacterObject : null, InventoryScreenHelper.InventoryCategoryType.None, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, null, null, null);
			inventoryState.InventoryLogic = inventoryLogic;
			inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x06000141 RID: 321 RVA: 0x0000F168 File Offset: 0x0000D368
		public static void OpenScreenAsInventoryForCraftedItemDecomposition(MobileParty party, CharacterObject character, Action doneLogicExtrasDelegate)
		{
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			InventoryLogic inventoryLogic = new InventoryLogic(null);
			inventoryLogic.Initialize(new ItemRoster(), party.ItemRoster, party.MemberRoster, false, false, character, InventoryScreenHelper.InventoryCategoryType.None, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, null, null, null);
			inventoryState.InventoryLogic = inventoryLogic;
			inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x06000142 RID: 322 RVA: 0x0000F1D8 File Offset: 0x0000D3D8
		public static void OpenScreenAsInventoryOf(MobileParty party, CharacterObject character)
		{
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			InventoryLogic inventoryLogic = new InventoryLogic(null);
			inventoryLogic.Initialize(new ItemRoster(), party.ItemRoster, party.MemberRoster, false, true, character, InventoryScreenHelper.InventoryCategoryType.None, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, null, null, null);
			inventoryState.InventoryLogic = inventoryLogic;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x06000143 RID: 323 RVA: 0x0000F23E File Offset: 0x0000D43E
		public static void OpenScreenAsInventoryOf(PartyBase rightParty, PartyBase leftParty)
		{
			Hero leaderHero = rightParty.LeaderHero;
			InventoryScreenHelper.OpenScreenAsInventoryOf(rightParty, leftParty, (leaderHero != null) ? leaderHero.CharacterObject : null, null, null, null);
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000F25C File Offset: 0x0000D45C
		public static void OpenScreenAsInventoryOf(PartyBase rightParty, PartyBase leftParty, CharacterObject character, TextObject leftRosterName = null, InventoryLogic.CapacityData capacityData = null, Action doneLogicExtrasDelegate = null)
		{
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			InventoryLogic inventoryLogic = new InventoryLogic(leftParty);
			inventoryLogic.Initialize(leftParty.ItemRoster, rightParty.ItemRoster, rightParty.MemberRoster, false, false, character, InventoryScreenHelper.InventoryCategoryType.None, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, InventoryScreenHelper.InventoryMode.Default, leftRosterName, leftParty.MemberRoster, capacityData);
			inventoryState.InventoryLogic = inventoryLogic;
			inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000F2CC File Offset: 0x0000D4CC
		public static void OpenScreenAsInventory(Action doneLogicExtrasDelegate = null)
		{
			InventoryScreenHelper.OpenInventoryPresentation(new TextObject("{=02c5bQSM}Discard", null), doneLogicExtrasDelegate);
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000F2DF File Offset: 0x0000D4DF
		public static void OpenCampaignBattleLootScreen()
		{
			InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster> { 
			{
				PartyBase.MainParty,
				MapEvent.PlayerMapEvent.ItemRosterForPlayerLootShare(PartyBase.MainParty)
			} });
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0000F308 File Offset: 0x0000D508
		public static void OpenScreenAsLoot(Dictionary<PartyBase, ItemRoster> itemRostersToLoot)
		{
			ItemRoster leftItemRoster = itemRostersToLoot[PartyBase.MainParty];
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			inventoryState.InventoryMode = InventoryScreenHelper.InventoryMode.Loot;
			InventoryLogic inventoryLogic = new InventoryLogic(null);
			inventoryLogic.Initialize(leftItemRoster, MobileParty.MainParty.ItemRoster, MobileParty.MainParty.MemberRoster, false, true, CharacterObject.PlayerCharacter, InventoryScreenHelper.InventoryCategoryType.None, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, null, null, null);
			inventoryState.InventoryLogic = inventoryLogic;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000F38C File Offset: 0x0000D58C
		public static void OpenScreenAsStash(ItemRoster stash)
		{
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			inventoryState.InventoryMode = InventoryScreenHelper.InventoryMode.Stash;
			InventoryLogic inventoryLogic = new InventoryLogic(null);
			inventoryLogic.Initialize(stash, MobileParty.MainParty, false, false, CharacterObject.PlayerCharacter, InventoryScreenHelper.InventoryCategoryType.None, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, new TextObject("{=nZbaYvVx}Stash", null), null, null);
			inventoryState.InventoryLogic = inventoryLogic;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x0000F3FC File Offset: 0x0000D5FC
		public static void OpenScreenAsWarehouse(ItemRoster stash, InventoryLogic.CapacityData otherSideCapacity)
		{
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			inventoryState.InventoryMode = InventoryScreenHelper.InventoryMode.Warehouse;
			InventoryLogic inventoryLogic = new InventoryLogic(null);
			inventoryLogic.Initialize(stash, MobileParty.MainParty, false, false, CharacterObject.PlayerCharacter, InventoryScreenHelper.InventoryCategoryType.None, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, new TextObject("{=anTRftmb}Warehouse", null), null, otherSideCapacity);
			inventoryState.InventoryLogic = inventoryLogic;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000F46C File Offset: 0x0000D66C
		public static void OpenScreenAsReceiveItems(ItemRoster items, TextObject leftRosterName, Action doneLogicDelegate = null)
		{
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			InventoryLogic inventoryLogic = new InventoryLogic(null);
			inventoryLogic.Initialize(items, MobileParty.MainParty.ItemRoster, MobileParty.MainParty.MemberRoster, false, true, CharacterObject.PlayerCharacter, InventoryScreenHelper.InventoryCategoryType.None, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, leftRosterName, null, null);
			inventoryState.InventoryLogic = inventoryLogic;
			inventoryState.DoneLogicExtrasDelegate = doneLogicDelegate;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000F4E4 File Offset: 0x0000D6E4
		public static void OpenTradeWithCaravanOrAlleyParty(MobileParty caravan, InventoryScreenHelper.InventoryCategoryType merchantItemType = InventoryScreenHelper.InventoryCategoryType.None)
		{
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			inventoryState.InventoryMode = InventoryScreenHelper.InventoryMode.Trade;
			InventoryLogic inventoryLogic = new InventoryLogic(caravan.Party);
			inventoryLogic.Initialize(caravan.Party.ItemRoster, PartyBase.MainParty.ItemRoster, PartyBase.MainParty.MemberRoster, true, true, CharacterObject.PlayerCharacter, merchantItemType, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, null, null, null);
			inventoryLogic.SetInventoryListener(new InventoryScreenHelper.CaravanInventoryListener(caravan));
			inventoryState.InventoryLogic = inventoryLogic;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000F574 File Offset: 0x0000D774
		public static void ActivateTradeWithCurrentSettlement()
		{
			InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.SettlementComponent, InventoryScreenHelper.InventoryCategoryType.None, null);
		}

		// Token: 0x0600014D RID: 333 RVA: 0x0000F594 File Offset: 0x0000D794
		public static void OpenScreenAsTrade(ItemRoster leftRoster, SettlementComponent settlementComponent, InventoryScreenHelper.InventoryCategoryType merchantItemType = InventoryScreenHelper.InventoryCategoryType.None, Action doneLogicExtrasDelegate = null)
		{
			InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
			inventoryState.InventoryMode = InventoryScreenHelper.InventoryMode.Trade;
			InventoryLogic inventoryLogic = new InventoryLogic(settlementComponent.Owner);
			inventoryLogic.Initialize(leftRoster, PartyBase.MainParty.ItemRoster, PartyBase.MainParty.MemberRoster, true, true, CharacterObject.PlayerCharacter, merchantItemType, InventoryScreenHelper.GetCurrentMarketDataForPlayer(), false, inventoryState.InventoryMode, null, null, null);
			inventoryLogic.SetInventoryListener(new InventoryScreenHelper.MerchantInventoryListener(settlementComponent));
			inventoryState.InventoryLogic = inventoryLogic;
			inventoryState.DoneLogicExtrasDelegate = doneLogicExtrasDelegate;
			Game.Current.GameStateManager.PushState(inventoryState, 0);
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0000F624 File Offset: 0x0000D824
		public static InventoryScreenHelper.InventoryItemType GetInventoryItemTypeOfItem(ItemObject item)
		{
			if (item != null)
			{
				switch (item.ItemType)
				{
				case ItemObject.ItemTypeEnum.Horse:
					return InventoryScreenHelper.InventoryItemType.Horse;
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
					return InventoryScreenHelper.InventoryItemType.Weapon;
				case ItemObject.ItemTypeEnum.Shield:
					return InventoryScreenHelper.InventoryItemType.Shield;
				case ItemObject.ItemTypeEnum.Goods:
					return InventoryScreenHelper.InventoryItemType.Goods;
				case ItemObject.ItemTypeEnum.HeadArmor:
					return InventoryScreenHelper.InventoryItemType.HeadArmor;
				case ItemObject.ItemTypeEnum.BodyArmor:
					return InventoryScreenHelper.InventoryItemType.BodyArmor;
				case ItemObject.ItemTypeEnum.LegArmor:
					return InventoryScreenHelper.InventoryItemType.LegArmor;
				case ItemObject.ItemTypeEnum.HandArmor:
					return InventoryScreenHelper.InventoryItemType.HandArmor;
				case ItemObject.ItemTypeEnum.Animal:
					return InventoryScreenHelper.InventoryItemType.Animal;
				case ItemObject.ItemTypeEnum.Book:
					return InventoryScreenHelper.InventoryItemType.Book;
				case ItemObject.ItemTypeEnum.Cape:
					return InventoryScreenHelper.InventoryItemType.Cape;
				case ItemObject.ItemTypeEnum.HorseHarness:
					return InventoryScreenHelper.InventoryItemType.HorseHarness;
				case ItemObject.ItemTypeEnum.Banner:
					return InventoryScreenHelper.InventoryItemType.Banner;
				}
			}
			return InventoryScreenHelper.InventoryItemType.None;
		}

		// Token: 0x020004E8 RID: 1256
		public enum InventoryMode
		{
			// Token: 0x040014D0 RID: 5328
			Default,
			// Token: 0x040014D1 RID: 5329
			Trade,
			// Token: 0x040014D2 RID: 5330
			Loot,
			// Token: 0x040014D3 RID: 5331
			Stash,
			// Token: 0x040014D4 RID: 5332
			Warehouse
		}

		// Token: 0x020004E9 RID: 1257
		// (Invoke) Token: 0x06004AB1 RID: 19121
		public delegate void InventoryFinishDelegate();

		// Token: 0x020004EA RID: 1258
		[Flags]
		public enum InventoryItemType
		{
			// Token: 0x040014D6 RID: 5334
			None = 0,
			// Token: 0x040014D7 RID: 5335
			Weapon = 1,
			// Token: 0x040014D8 RID: 5336
			Shield = 2,
			// Token: 0x040014D9 RID: 5337
			HeadArmor = 4,
			// Token: 0x040014DA RID: 5338
			BodyArmor = 8,
			// Token: 0x040014DB RID: 5339
			LegArmor = 16,
			// Token: 0x040014DC RID: 5340
			HandArmor = 32,
			// Token: 0x040014DD RID: 5341
			Horse = 64,
			// Token: 0x040014DE RID: 5342
			HorseHarness = 128,
			// Token: 0x040014DF RID: 5343
			Goods = 256,
			// Token: 0x040014E0 RID: 5344
			Book = 512,
			// Token: 0x040014E1 RID: 5345
			Animal = 1024,
			// Token: 0x040014E2 RID: 5346
			Cape = 2048,
			// Token: 0x040014E3 RID: 5347
			Banner = 4096,
			// Token: 0x040014E4 RID: 5348
			HorseCategory = 192,
			// Token: 0x040014E5 RID: 5349
			Armors = 2108,
			// Token: 0x040014E6 RID: 5350
			Equipable = 6399,
			// Token: 0x040014E7 RID: 5351
			All = 4095
		}

		// Token: 0x020004EB RID: 1259
		public enum InventoryCategoryType
		{
			// Token: 0x040014E9 RID: 5353
			None = -1,
			// Token: 0x040014EA RID: 5354
			All,
			// Token: 0x040014EB RID: 5355
			Armors,
			// Token: 0x040014EC RID: 5356
			Weapon,
			// Token: 0x040014ED RID: 5357
			Shield,
			// Token: 0x040014EE RID: 5358
			HorseCategory,
			// Token: 0x040014EF RID: 5359
			Goods,
			// Token: 0x040014F0 RID: 5360
			CategoryTypeAmount
		}

		// Token: 0x020004EC RID: 1260
		private class CaravanInventoryListener : InventoryListener
		{
			// Token: 0x06004AB4 RID: 19124 RVA: 0x00176D49 File Offset: 0x00174F49
			public CaravanInventoryListener(MobileParty caravan)
			{
				this._caravan = caravan;
			}

			// Token: 0x06004AB5 RID: 19125 RVA: 0x00176D58 File Offset: 0x00174F58
			public override int GetGold()
			{
				return this._caravan.PartyTradeGold;
			}

			// Token: 0x06004AB6 RID: 19126 RVA: 0x00176D65 File Offset: 0x00174F65
			public override TextObject GetTraderName()
			{
				if (this._caravan.LeaderHero == null)
				{
					return this._caravan.Name;
				}
				return this._caravan.LeaderHero.Name;
			}

			// Token: 0x06004AB7 RID: 19127 RVA: 0x00176D90 File Offset: 0x00174F90
			public override void SetGold(int gold)
			{
				this._caravan.PartyTradeGold = gold;
			}

			// Token: 0x06004AB8 RID: 19128 RVA: 0x00176D9E File Offset: 0x00174F9E
			public override PartyBase GetOppositeParty()
			{
				return this._caravan.Party;
			}

			// Token: 0x06004AB9 RID: 19129 RVA: 0x00176DAB File Offset: 0x00174FAB
			public override void OnTransaction()
			{
				throw new NotImplementedException();
			}

			// Token: 0x040014F1 RID: 5361
			private MobileParty _caravan;
		}

		// Token: 0x020004ED RID: 1261
		private class MerchantInventoryListener : InventoryListener
		{
			// Token: 0x06004ABA RID: 19130 RVA: 0x00176DB2 File Offset: 0x00174FB2
			public MerchantInventoryListener(SettlementComponent settlementComponent)
			{
				this._settlementComponent = settlementComponent;
			}

			// Token: 0x06004ABB RID: 19131 RVA: 0x00176DC1 File Offset: 0x00174FC1
			public override TextObject GetTraderName()
			{
				return this._settlementComponent.Owner.Name;
			}

			// Token: 0x06004ABC RID: 19132 RVA: 0x00176DD3 File Offset: 0x00174FD3
			public override PartyBase GetOppositeParty()
			{
				return this._settlementComponent.Owner;
			}

			// Token: 0x06004ABD RID: 19133 RVA: 0x00176DE0 File Offset: 0x00174FE0
			public override int GetGold()
			{
				return this._settlementComponent.Gold;
			}

			// Token: 0x06004ABE RID: 19134 RVA: 0x00176DED File Offset: 0x00174FED
			public override void SetGold(int gold)
			{
				this._settlementComponent.ChangeGold(gold - this._settlementComponent.Gold);
			}

			// Token: 0x06004ABF RID: 19135 RVA: 0x00176E07 File Offset: 0x00175007
			public override void OnTransaction()
			{
				throw new NotImplementedException();
			}

			// Token: 0x040014F2 RID: 5362
			private SettlementComponent _settlementComponent;
		}
	}
}
