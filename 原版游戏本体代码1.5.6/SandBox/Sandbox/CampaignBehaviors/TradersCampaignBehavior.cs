using Helpers;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors;

public class TradersCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	protected void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("weaponsmith_talk_start_normal", "start", "weaponsmith_talk_player", "{=!}{TRADER_GREETING}", conversation_weaponsmith_talk_start_normal_on_condition, null);
		campaignGameStarter.AddDialogLine("weaponsmith_talk_start_to_player_in_disguise", "start", "close_window", "{=1auLEn9y}Look, my good {?PLAYER.GENDER}woman{?}man{\\?}, these are hard times for sure, but I need you to move along. You'll scare away my customers.", conversation_weaponsmith_talk_start_to_player_in_disguise_on_condition, null);
		campaignGameStarter.AddDialogLine("weaponsmith_talk_initial", "weaponsmith_begin", "weaponsmith_talk_player", "{=jxw54Ijt}Okay, is there anything more I can help with?", null, null);
		campaignGameStarter.AddPlayerLine("weaponsmith_talk_player_1", "weaponsmith_talk_player", "merchant_response_1", "{=ExltvaKo}Let me see what you have for sale...", null, null);
		campaignGameStarter.AddPlayerLine("weaponsmith_talk_player_request_craft", "weaponsmith_talk_player", "merchant_response_crafting", "{=w1vzpCNi}I need you to craft a weapon for me", conversation_open_crafting_on_condition, null);
		campaignGameStarter.AddPlayerLine("weaponsmith_talk_player_3", "weaponsmith_talk_player", "merchant_response_3", "{=8hNYr2VX}I was just passing by.", null, null);
		campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_response_1", "merchant_response_1", "player_merchant_talk_close", "{=K5mG9nDv}With pleasure.", null, null);
		campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_response_2", "merchant_response_2", "player_merchant_talk_2", "{=5bRQ0gt7}How many men do you need for it? For each men I want 100{GOLD_ICON}.", null, null);
		campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_response_craft", "merchant_response_crafting", "player_merchant_craft_talk_close", "{=lF5HkBDy}As you wish.", null, null);
		campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_craft_opened", "player_merchant_craft_talk_close", "close_window", "{=TD8Jxn7U}Have a nice day my {?PLAYER.GENDER}lady{?}lord{\\?}.", null, conversation_weaponsmith_craft_on_consequence);
		campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_response_3", "merchant_response_3", "close_window", "{=FpNWdIaT}Yes, of course. Just ask me if there is anything you need.", null, null);
		campaignGameStarter.AddDialogLine("weaponsmith_talk_end", "player_merchant_talk_close", "close_window", "{=Yh0danUf}Thank you and good day my {?PLAYER.GENDER}lady{?}lord{\\?}.", null, conversation_weaponsmith_talk_player_on_consequence);
	}

	private bool conversation_open_crafting_on_condition()
	{
		if (CharacterObject.OneToOneConversationCharacter != null)
		{
			return CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Blacksmith;
		}
		return false;
	}

	private bool conversation_weaponsmith_talk_start_normal_on_condition()
	{
		if (IsTrader())
		{
			if (Campaign.Current.IsMainHeroDisguised)
			{
				if (Mission.Current.GetMissionBehavior<DisguiseMissionLogic>().ContactAlreadySetCommonCondition() && Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.SmugglerConnections))
				{
					MBTextManager.SetTextVariable("TRADER_GREETING", "{=bqg2gS7i}Ah, a friend of a friend. How may I help you?");
					return true;
				}
				return false;
			}
			MBTextManager.SetTextVariable("TRADER_GREETING", "{=7IxFrati}Greetings my {?PLAYER.GENDER}lady{?}lord{\\?}, how may I help you?");
			return true;
		}
		return false;
	}

	private bool conversation_weaponsmith_talk_start_to_player_in_disguise_on_condition()
	{
		if (IsTrader())
		{
			return !conversation_weaponsmith_talk_start_normal_on_condition();
		}
		return false;
	}

	private bool IsTrader()
	{
		if (CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.Weaponsmith && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.Armorer && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.HorseTrader)
		{
			return CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.GoodsTrader;
		}
		return true;
	}

	private void conversation_weaponsmith_talk_player_on_consequence()
	{
		InventoryScreenHelper.InventoryCategoryType merchantItemType = InventoryScreenHelper.InventoryCategoryType.None;
		switch (CharacterObject.OneToOneConversationCharacter.Occupation)
		{
		case Occupation.Weaponsmith:
			merchantItemType = InventoryScreenHelper.InventoryCategoryType.Weapon;
			break;
		case Occupation.Blacksmith:
			merchantItemType = InventoryScreenHelper.InventoryCategoryType.Weapon;
			break;
		case Occupation.Armorer:
			merchantItemType = InventoryScreenHelper.InventoryCategoryType.Armors;
			break;
		case Occupation.HorseTrader:
			merchantItemType = InventoryScreenHelper.InventoryCategoryType.HorseCategory;
			break;
		case Occupation.GoodsTrader:
			merchantItemType = InventoryScreenHelper.InventoryCategoryType.Goods;
			break;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (Mission.Current != null)
		{
			InventoryScreenHelper.OpenScreenAsTrade(currentSettlement.ItemRoster, currentSettlement.Town, merchantItemType, OnInventoryScreenDone);
		}
		else
		{
			InventoryScreenHelper.OpenScreenAsTrade(currentSettlement.ItemRoster, currentSettlement.Town, merchantItemType);
		}
	}

	private void conversation_weaponsmith_craft_on_consequence()
	{
		CraftingHelper.OpenCrafting(CraftingTemplate.All[0]);
	}

	private void OnInventoryScreenDone()
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			CharacterObject characterObject = (CharacterObject)agent.Character;
			if (agent.IsHuman && characterObject != null && characterObject.IsHero && characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty && (!agent.IsMainAgent || !Campaign.Current.IsMainHeroDisguised))
			{
				agent.UpdateSpawnEquipmentAndRefreshVisuals(Mission.Current.DoesMissionRequireCivilianEquipment ? characterObject.FirstCivilianEquipment : characterObject.FirstBattleEquipment);
			}
		}
	}
}
