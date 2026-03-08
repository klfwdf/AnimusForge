using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000E0 RID: 224
	public class TradersCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000B09 RID: 2825 RVA: 0x0005246A File Offset: 0x0005066A
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x06000B0A RID: 2826 RVA: 0x00052483 File Offset: 0x00050683
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06000B0B RID: 2827 RVA: 0x00052485 File Offset: 0x00050685
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x06000B0C RID: 2828 RVA: 0x00052490 File Offset: 0x00050690
		protected void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("weaponsmith_talk_start_normal", "start", "weaponsmith_talk_player", "{=7IxFrati}Greetings my {?PLAYER.GENDER}lady{?}lord{\\?}, how may I help you?", new ConversationSentence.OnConditionDelegate(this.conversation_weaponsmith_talk_start_normal_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("weaponsmith_talk_start_to_player_in_disguise", "start", "close_window", "{=1auLEn9y}Look, my good {?PLAYER.GENDER}woman{?}man{\\?}, these are hard times for sure, but I need you to move along. You'll scare away my customers.", new ConversationSentence.OnConditionDelegate(this.conversation_weaponsmith_talk_start_to_player_in_disguise_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("weaponsmith_talk_initial", "weaponsmith_begin", "weaponsmith_talk_player", "{=jxw54Ijt}Okay, is there anything more I can help with?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("weaponsmith_talk_player_1", "weaponsmith_talk_player", "merchant_response_1", "{=ExltvaKo}Let me see what you have for sale...", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("weaponsmith_talk_player_request_craft", "weaponsmith_talk_player", "merchant_response_crafting", "{=w1vzpCNi}I need you to craft a weapon for me", new ConversationSentence.OnConditionDelegate(this.conversation_open_crafting_on_condition), null, 100, null, null);
			campaignGameStarter.AddPlayerLine("weaponsmith_talk_player_3", "weaponsmith_talk_player", "merchant_response_3", "{=8hNYr2VX}I was just passing by.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_response_1", "merchant_response_1", "player_merchant_talk_close", "{=K5mG9nDv}With pleasure.", null, null, 100, null);
			campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_response_2", "merchant_response_2", "player_merchant_talk_2", "{=5bRQ0gt7}How many men do you need for it? For each men I want 100{GOLD_ICON}.", null, null, 100, null);
			campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_response_craft", "merchant_response_crafting", "player_merchant_craft_talk_close", "{=lF5HkBDy}As you wish.", null, null, 100, null);
			campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_craft_opened", "player_merchant_craft_talk_close", "close_window", "{=TD8Jxn7U}Have a nice day my {?PLAYER.GENDER}lady{?}lord{\\?}.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_weaponsmith_craft_on_consequence), 100, null);
			campaignGameStarter.AddDialogLine("weaponsmith_talk_merchant_response_3", "merchant_response_3", "close_window", "{=FpNWdIaT}Yes, of course. Just ask me if there is anything you need.", null, null, 100, null);
			campaignGameStarter.AddDialogLine("weaponsmith_talk_end", "player_merchant_talk_close", "close_window", "{=Yh0danUf}Thank you and good day my {?PLAYER.GENDER}lady{?}lord{\\?}.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_weaponsmith_talk_player_on_consequence), 100, null);
		}

		// Token: 0x06000B0D RID: 2829 RVA: 0x00052657 File Offset: 0x00050857
		private bool conversation_open_crafting_on_condition()
		{
			return CharacterObject.OneToOneConversationCharacter != null && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Blacksmith;
		}

		// Token: 0x06000B0E RID: 2830 RVA: 0x00052670 File Offset: 0x00050870
		private bool conversation_weaponsmith_talk_start_normal_on_condition()
		{
			return !Campaign.Current.IsMainHeroDisguised && this.IsTrader();
		}

		// Token: 0x06000B0F RID: 2831 RVA: 0x00052686 File Offset: 0x00050886
		private bool conversation_weaponsmith_talk_start_to_player_in_disguise_on_condition()
		{
			return Campaign.Current.IsMainHeroDisguised && this.IsTrader();
		}

		// Token: 0x06000B10 RID: 2832 RVA: 0x0005269C File Offset: 0x0005089C
		private bool IsTrader()
		{
			return CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Weaponsmith || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Armorer || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.HorseTrader || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.GoodsTrader;
		}

		// Token: 0x06000B11 RID: 2833 RVA: 0x000526D8 File Offset: 0x000508D8
		private void conversation_weaponsmith_talk_player_on_consequence()
		{
			InventoryScreenHelper.InventoryCategoryType merchantItemType = InventoryScreenHelper.InventoryCategoryType.None;
			Occupation occupation = CharacterObject.OneToOneConversationCharacter.Occupation;
			if (occupation != Occupation.GoodsTrader)
			{
				switch (occupation)
				{
				case Occupation.Weaponsmith:
					merchantItemType = InventoryScreenHelper.InventoryCategoryType.Weapon;
					break;
				case Occupation.Armorer:
					merchantItemType = InventoryScreenHelper.InventoryCategoryType.Armors;
					break;
				case Occupation.HorseTrader:
					merchantItemType = InventoryScreenHelper.InventoryCategoryType.HorseCategory;
					break;
				default:
					if (occupation == Occupation.Blacksmith)
					{
						merchantItemType = InventoryScreenHelper.InventoryCategoryType.Weapon;
					}
					break;
				}
			}
			else
			{
				merchantItemType = InventoryScreenHelper.InventoryCategoryType.Goods;
			}
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (Mission.Current != null)
			{
				InventoryScreenHelper.OpenScreenAsTrade(currentSettlement.ItemRoster, currentSettlement.Town, merchantItemType, new Action(this.OnInventoryScreenDone));
				return;
			}
			InventoryScreenHelper.OpenScreenAsTrade(currentSettlement.ItemRoster, currentSettlement.Town, merchantItemType, null);
		}

		// Token: 0x06000B12 RID: 2834 RVA: 0x00052763 File Offset: 0x00050963
		private void conversation_weaponsmith_craft_on_consequence()
		{
			CraftingHelper.OpenCrafting(CraftingTemplate.All[0], null);
		}

		// Token: 0x06000B13 RID: 2835 RVA: 0x00052778 File Offset: 0x00050978
		private void OnInventoryScreenDone()
		{
			foreach (Agent agent in Mission.Current.Agents)
			{
				CharacterObject characterObject = (CharacterObject)agent.Character;
				if (agent.IsHuman && characterObject != null && characterObject.IsHero && characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty)
				{
					agent.UpdateSpawnEquipmentAndRefreshVisuals(Mission.Current.DoesMissionRequireCivilianEquipment ? characterObject.FirstCivilianEquipment : characterObject.FirstBattleEquipment);
				}
			}
		}
	}
}
