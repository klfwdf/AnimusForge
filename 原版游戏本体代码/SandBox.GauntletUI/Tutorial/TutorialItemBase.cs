using System;
using System.Collections.Generic;
using SandBox.View.Map;
using SandBox.ViewModelCollection.MapSiege;
using SandBox.ViewModelCollection.Missions.NameMarker;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace SandBox.GauntletUI.Tutorial
{
	// Token: 0x02000017 RID: 23
	public abstract class TutorialItemBase
	{
		// Token: 0x06000139 RID: 313
		public abstract bool IsConditionsMetForCompletion();

		// Token: 0x0600013A RID: 314
		public abstract bool IsConditionsMetForActivation();

		// Token: 0x0600013B RID: 315
		public abstract TutorialContexts GetTutorialsRelevantContext();

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600013D RID: 317 RVA: 0x0000A101 File Offset: 0x00008301
		// (set) Token: 0x0600013C RID: 316 RVA: 0x0000A0F8 File Offset: 0x000082F8
		public TutorialItemVM.ItemPlacements Placement { get; protected set; }

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x0600013F RID: 319 RVA: 0x0000A112 File Offset: 0x00008312
		// (set) Token: 0x0600013E RID: 318 RVA: 0x0000A109 File Offset: 0x00008309
		public bool MouseRequired { get; protected set; }

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000141 RID: 321 RVA: 0x0000A123 File Offset: 0x00008323
		// (set) Token: 0x06000140 RID: 320 RVA: 0x0000A11A File Offset: 0x0000831A
		public string HighlightedVisualElementID { get; protected set; }

		// Token: 0x06000142 RID: 322 RVA: 0x0000A12B File Offset: 0x0000832B
		protected virtual string GetCustomTutorialElementHighlightID()
		{
			return "";
		}

		// Token: 0x06000143 RID: 323 RVA: 0x0000A132 File Offset: 0x00008332
		public virtual void OnDeactivate()
		{
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000A134 File Offset: 0x00008334
		public virtual bool IsConditionsMetForVisibility()
		{
			return this.GetTutorialsRelevantContext() != TutorialContexts.Mission || !BannerlordConfig.HideBattleUI;
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000A149 File Offset: 0x00008349
		public virtual void OnInventoryTransferItem(InventoryTransferItemEvent obj)
		{
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000A14B File Offset: 0x0000834B
		public virtual void OnTutorialContextChanged(TutorialContextChangedEvent obj)
		{
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0000A14D File Offset: 0x0000834D
		public virtual void OnInventoryFilterChanged(InventoryFilterChangedEvent obj)
		{
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000A14F File Offset: 0x0000834F
		public virtual void OnPerkSelectedByPlayer(PerkSelectedByPlayerEvent obj)
		{
		}

		// Token: 0x06000149 RID: 329 RVA: 0x0000A151 File Offset: 0x00008351
		public virtual void OnFocusAddedByPlayer(FocusAddedByPlayerEvent obj)
		{
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000A153 File Offset: 0x00008353
		public virtual void OnGameMenuOpened(MenuCallbackArgs obj)
		{
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000A155 File Offset: 0x00008355
		public virtual void OnMainMapCameraMove(MapScreen.MainMapCameraMoveEvent obj)
		{
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000A157 File Offset: 0x00008357
		public virtual void OnCharacterPortraitPopUpOpened(CharacterObject obj)
		{
		}

		// Token: 0x0600014D RID: 333 RVA: 0x0000A159 File Offset: 0x00008359
		public virtual void OnPlayerStartTalkFromMenuOverlay(Hero obj)
		{
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0000A15B File Offset: 0x0000835B
		public virtual void OnGameMenuOptionSelected(GameMenuOption obj)
		{
		}

		// Token: 0x0600014F RID: 335 RVA: 0x0000A15D File Offset: 0x0000835D
		public virtual void OnPlayerStartRecruitment(CharacterObject obj)
		{
		}

		// Token: 0x06000150 RID: 336 RVA: 0x0000A15F File Offset: 0x0000835F
		public virtual void OnNewCompanionAdded(Hero obj)
		{
		}

		// Token: 0x06000151 RID: 337 RVA: 0x0000A161 File Offset: 0x00008361
		public virtual void OnPlayerRecruitedUnit(CharacterObject obj, int count)
		{
		}

		// Token: 0x06000152 RID: 338 RVA: 0x0000A163 File Offset: 0x00008363
		public virtual void OnPlayerInventoryExchange(List<ValueTuple<ItemRosterElement, int>> purchasedItems, List<ValueTuple<ItemRosterElement, int>> soldItems, bool isTrading)
		{
		}

		// Token: 0x06000153 RID: 339 RVA: 0x0000A165 File Offset: 0x00008365
		public virtual void OnMissionNameMarkerToggled(MissionNameMarkerToggleEvent obj)
		{
		}

		// Token: 0x06000154 RID: 340 RVA: 0x0000A167 File Offset: 0x00008367
		public virtual void OnPlayerToggleTrackSettlementFromEncyclopedia(PlayerToggleTrackSettlementFromEncyclopediaEvent obj)
		{
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0000A169 File Offset: 0x00008369
		public virtual void OnInventoryEquipmentTypeChange(InventoryEquipmentTypeChangedEvent obj)
		{
		}

		// Token: 0x06000156 RID: 342 RVA: 0x0000A16B File Offset: 0x0000836B
		public virtual void OnArmyCohesionByPlayerBoosted(ArmyCohesionBoostedByPlayerEvent obj)
		{
		}

		// Token: 0x06000157 RID: 343 RVA: 0x0000A16D File Offset: 0x0000836D
		public virtual void OnPartyAddedToArmyByPlayer(PartyAddedToArmyByPlayerEvent obj)
		{
		}

		// Token: 0x06000158 RID: 344 RVA: 0x0000A16F File Offset: 0x0000836F
		public virtual void OnPlayerStartEngineConstruction(PlayerStartEngineConstructionEvent obj)
		{
		}

		// Token: 0x06000159 RID: 345 RVA: 0x0000A171 File Offset: 0x00008371
		public virtual void OnPlayerUpgradeTroop(CharacterObject arg1, CharacterObject arg2, int arg3)
		{
		}

		// Token: 0x0600015A RID: 346 RVA: 0x0000A173 File Offset: 0x00008373
		public virtual void OnPlayerMoveTroop(PlayerMoveTroopEvent obj)
		{
		}

		// Token: 0x0600015B RID: 347 RVA: 0x0000A175 File Offset: 0x00008375
		public virtual void OnPerkSelectionToggle(PerkSelectionToggleEvent obj)
		{
		}

		// Token: 0x0600015C RID: 348 RVA: 0x0000A177 File Offset: 0x00008377
		public virtual void OnPlayerInspectedPartySpeed(PlayerInspectedPartySpeedEvent obj)
		{
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000A179 File Offset: 0x00008379
		public virtual void OnPlayerMovementFlagChanged(MissionPlayerMovementFlagsChangeEvent obj)
		{
		}

		// Token: 0x0600015E RID: 350 RVA: 0x0000A17B File Offset: 0x0000837B
		public virtual void OnPlayerToggledUpgradePopup(PlayerToggledUpgradePopupEvent obj)
		{
		}

		// Token: 0x0600015F RID: 351 RVA: 0x0000A17D File Offset: 0x0000837D
		public virtual void OnOrderOfBattleHeroAssignedToFormation(OrderOfBattleHeroAssignedToFormationEvent obj)
		{
		}

		// Token: 0x06000160 RID: 352 RVA: 0x0000A17F File Offset: 0x0000837F
		public virtual void OnOrderOfBattleFormationClassChanged(OrderOfBattleFormationClassChangedEvent obj)
		{
		}

		// Token: 0x06000161 RID: 353 RVA: 0x0000A181 File Offset: 0x00008381
		public virtual void OnOrderOfBattleFormationWeightChanged(OrderOfBattleFormationWeightChangedEvent obj)
		{
		}

		// Token: 0x06000162 RID: 354 RVA: 0x0000A183 File Offset: 0x00008383
		public virtual void OnCraftingWeaponClassSelectionOpened(CraftingWeaponClassSelectionOpenedEvent obj)
		{
		}

		// Token: 0x06000163 RID: 355 RVA: 0x0000A185 File Offset: 0x00008385
		public virtual void OnCraftingOnWeaponResultPopupOpened(CraftingWeaponResultPopupToggledEvent obj)
		{
		}

		// Token: 0x06000164 RID: 356 RVA: 0x0000A187 File Offset: 0x00008387
		public virtual void OnCraftingOrderTabOpened(CraftingOrderTabOpenedEvent obj)
		{
		}

		// Token: 0x06000165 RID: 357 RVA: 0x0000A189 File Offset: 0x00008389
		public virtual void OnCraftingOrderSelectionOpened(CraftingOrderSelectionOpenedEvent obj)
		{
		}

		// Token: 0x06000166 RID: 358 RVA: 0x0000A18B File Offset: 0x0000838B
		public virtual void OnInventoryItemInspected(InventoryItemInspectedEvent obj)
		{
		}

		// Token: 0x06000167 RID: 359 RVA: 0x0000A18D File Offset: 0x0000838D
		public virtual void OnCrimeValueInspectedInSettlementOverlay(CrimeValueInspectedInSettlementOverlayEvent obj)
		{
		}

		// Token: 0x06000168 RID: 360 RVA: 0x0000A18F File Offset: 0x0000838F
		public virtual void OnClanRoleAssignedThroughClanScreen(ClanRoleAssignedThroughClanScreenEvent obj)
		{
		}

		// Token: 0x06000169 RID: 361 RVA: 0x0000A191 File Offset: 0x00008391
		public virtual void OnPlayerSelectedAKingdomDecisionOption(PlayerSelectedAKingdomDecisionOptionEvent obj)
		{
		}
	}
}
