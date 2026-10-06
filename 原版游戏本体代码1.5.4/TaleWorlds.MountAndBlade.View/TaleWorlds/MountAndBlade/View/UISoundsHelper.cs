using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View;

public static class UISoundsHelper
{
	public static class DefaultSounds
	{
		public const string DefaultSound = "event:/ui/default";

		public const string CheckboxSound = "event:/ui/checkbox";

		public const string TabSound = "event:/ui/tab";

		public const string SortSound = "event:/ui/sort";

		public const string TransferSound = "event:/ui/transfer";
	}

	public static class PanelSounds
	{
		public const string NextPanelSound = "event:/ui/panels/next";

		public const string InventoryPanelOpenSound = "event:/ui/panels/panel_inventory_open";

		public const string ClanPanelOpenSound = "event:/ui/panels/panel_clan_open";

		public const string CharacterPanelOpenSound = "event:/ui/panels/panel_character_open";

		public const string KingdomPanelOpenSound = "event:/ui/panels/panel_kingdom_open";

		public const string PartyPanelOpenSound = "event:/ui/panels/panel_party_open";

		public const string QuestPanelOpenSound = "event:/ui/panels/panel_quest_open";

		public const string CraftingPanelOpenSound = "event:/ui/panels/panel_settlement_enter_smithy";
	}

	public static class SiegeSounds
	{
		public const string SiegeEngineClickSound = "event:/ui/panels/siege/engine_click";

		public const string SiegeEngineBuildCompleteSound = "event:/ui/panels/siege/engine_build_complete";
	}

	public static class InventorySounds
	{
		public const string TakeAllSound = "event:/ui/inventory/take_all";
	}

	public static class PartySounds
	{
		public const string UpgradeSound = "event:/ui/party/upgrade";

		public const string RecruitSound = "event:/ui/party/recruit_prisoner";
	}

	public static class CraftingSounds
	{
		public const string RefineTabSound = "event:/ui/crafting/refine_tab";

		public const string CraftTabSound = "event:/ui/crafting/craft_tab";

		public const string SmeltTabSound = "event:/ui/crafting/smelt_tab";

		public const string RefineSuccessSound = "event:/ui/crafting/refine_success";

		public const string CraftSuccessSound = "event:/ui/crafting/craft_success";

		public const string SmeltSuccessSound = "event:/ui/crafting/smelt_success";
	}

	public static class EndgameSounds
	{
		public const string ClanDestroyedSound = "event:/ui/endgame/end_clan_destroyed";

		public const string RetirementSound = "event:/ui/endgame/end_retirement";

		public const string VictorySound = "event:/ui/endgame/end_victory";
	}

	public static class NotificationSounds
	{
		public const string HideoutFoundSound = "event:/ui/notification/hideout_found";
	}

	public static class CampaignSounds
	{
		public const string PartySound = "event:/ui/campaign/click_party";

		public const string PartyEnemySound = "event:/ui/campaign/click_party_enemy";

		public const string SettlementSound = "event:/ui/campaign/click_settlement";

		public const string SettlementEnemySound = "event:/ui/campaign/click_settlement_enemy";
	}

	public static class MissionSounds
	{
		public const string DeploySound = "event:/ui/mission/deploy";
	}

	public static class MultiplayerSounds
	{
		public const string MatchReadySound = "event:/ui/multiplayer/match_ready";
	}

	public static class OrderOfBattleSounds
	{
		public const string ClassSelectionDropdownSound = "event:/ui/oob/dropdown";

		public const string OfficerPickSound = "event:/ui/oob/officer_pick";
	}

	public static class PortSounds
	{
		public const string ConfirmSound = "event:/ui/port/confirm_ship";

		public const string ChooseShipSound = "event:/ui/port/choose_ship";
	}

	public static class KingdomSounds
	{
		public const string ConfirmVoteSound = "event:/ui/reign/decision";
	}

	public static void PlayUISound(string soundName)
	{
		SoundEvent.PlaySound2D(soundName);
	}
}
