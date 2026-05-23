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

namespace SandBox.GauntletUI.Tutorial;

public abstract class TutorialItemBase
{
	public TutorialItemVM.ItemPlacements Placement { get; protected set; }

	public bool MouseRequired { get; protected set; }

	public string HighlightedVisualElementID { get; protected set; }

	public abstract bool IsConditionsMetForCompletion();

	public abstract bool IsConditionsMetForActivation();

	public abstract TutorialContexts GetTutorialsRelevantContext();

	protected virtual string GetCustomTutorialElementHighlightID()
	{
		return "";
	}

	public virtual void OnDeactivate()
	{
	}

	public virtual bool IsConditionsMetForVisibility()
	{
		if (GetTutorialsRelevantContext() == TutorialContexts.Mission)
		{
			return !BannerlordConfig.HideBattleUI;
		}
		return true;
	}

	public virtual void OnInventoryTransferItem(InventoryTransferItemEvent obj)
	{
	}

	public virtual void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
	}

	public virtual void OnInventoryFilterChanged(InventoryFilterChangedEvent obj)
	{
	}

	public virtual void OnPerkSelectedByPlayer(PerkSelectedByPlayerEvent obj)
	{
	}

	public virtual void OnFocusAddedByPlayer(FocusAddedByPlayerEvent obj)
	{
	}

	public virtual void OnGameMenuOpened(MenuCallbackArgs obj)
	{
	}

	public virtual void OnMainMapCameraMove(MapScreen.MainMapCameraMoveEvent obj)
	{
	}

	public virtual void OnCharacterPortraitPopUpOpened(CharacterObject obj)
	{
	}

	public virtual void OnPlayerStartTalkFromMenuOverlay(Hero obj)
	{
	}

	public virtual void OnGameMenuOptionSelected(GameMenuOption obj)
	{
	}

	public virtual void OnPlayerStartRecruitment(CharacterObject obj)
	{
	}

	public virtual void OnNewCompanionAdded(Hero obj)
	{
	}

	public virtual void OnPlayerRecruitedUnit(CharacterObject obj, int count)
	{
	}

	public virtual void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
	{
	}

	public virtual void OnMissionNameMarkerToggled(MissionNameMarkerToggleEvent obj)
	{
	}

	public virtual void OnPlayerToggleTrackSettlementFromEncyclopedia(PlayerToggleTrackSettlementFromEncyclopediaEvent obj)
	{
	}

	public virtual void OnInventoryEquipmentTypeChange(InventoryEquipmentTypeChangedEvent obj)
	{
	}

	public virtual void OnArmyCohesionByPlayerBoosted(ArmyCohesionBoostedByPlayerEvent obj)
	{
	}

	public virtual void OnPartyAddedToArmyByPlayer(PartyAddedToArmyByPlayerEvent obj)
	{
	}

	public virtual void OnPlayerStartEngineConstruction(PlayerStartEngineConstructionEvent obj)
	{
	}

	public virtual void OnPlayerUpgradeTroop(CharacterObject arg1, CharacterObject arg2, int arg3)
	{
	}

	public virtual void OnPlayerMoveTroop(PlayerMoveTroopEvent obj)
	{
	}

	public virtual void OnPerkSelectionToggle(PerkSelectionToggleEvent obj)
	{
	}

	public virtual void OnPlayerInspectedPartySpeed(PlayerInspectedPartySpeedEvent obj)
	{
	}

	public virtual void OnPlayerMovementFlagChanged(MissionPlayerMovementFlagsChangeEvent obj)
	{
	}

	public virtual void OnPlayerToggledUpgradePopup(PlayerToggledUpgradePopupEvent obj)
	{
	}

	public virtual void OnOrderOfBattleHeroAssignedToFormation(OrderOfBattleHeroAssignedToFormationEvent obj)
	{
	}

	public virtual void OnOrderOfBattleFormationClassChanged(OrderOfBattleFormationClassChangedEvent obj)
	{
	}

	public virtual void OnOrderOfBattleFormationWeightChanged(OrderOfBattleFormationWeightChangedEvent obj)
	{
	}

	public virtual void OnCraftingWeaponClassSelectionOpened(CraftingWeaponClassSelectionOpenedEvent obj)
	{
	}

	public virtual void OnCraftingOnWeaponResultPopupOpened(CraftingWeaponResultPopupToggledEvent obj)
	{
	}

	public virtual void OnCraftingOrderTabOpened(CraftingOrderTabOpenedEvent obj)
	{
	}

	public virtual void OnCraftingOrderSelectionOpened(CraftingOrderSelectionOpenedEvent obj)
	{
	}

	public virtual void OnInventoryItemInspected(InventoryItemInspectedEvent obj)
	{
	}

	public virtual void OnCrimeValueInspectedInSettlementOverlay(CrimeValueInspectedInSettlementOverlayEvent obj)
	{
	}

	public virtual void OnClanRoleAssignedThroughClanScreen(ClanRoleAssignedThroughClanScreenEvent obj)
	{
	}

	public virtual void OnPlayerSelectedAKingdomDecisionOption(PlayerSelectedAKingdomDecisionOptionEvent obj)
	{
	}
}
