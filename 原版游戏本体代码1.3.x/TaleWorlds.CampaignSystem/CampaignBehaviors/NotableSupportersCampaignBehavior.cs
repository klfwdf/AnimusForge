using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200041D RID: 1053
	public class NotableSupportersCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060042B8 RID: 17080 RVA: 0x00141EE4 File Offset: 0x001400E4
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
		}

		// Token: 0x060042B9 RID: 17081 RVA: 0x00141F14 File Offset: 0x00140114
		private void OnGameLoadFinished()
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0)))
			{
				for (int i = Clan.PlayerClan.SupporterNotables.Count - 1; i >= 0; i--)
				{
					Clan.PlayerClan.SupporterNotables[i].SupporterOf = null;
				}
			}
		}

		// Token: 0x060042BA RID: 17082 RVA: 0x00141F74 File Offset: 0x00140174
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x060042BB RID: 17083 RVA: 0x00141F80 File Offset: 0x00140180
		private void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddPlayerLine("notable_support_request", "hero_main_options", "notable_support_request_response", "{=lxL9hHEf}Would you use your influence to support my clan?", new ConversationSentence.OnConditionDelegate(this.notable_support_request_on_condition), null, 100, new ConversationSentence.OnClickableConditionDelegate(this.notable_support_request_on_clickable_condition), null);
			campaignGameStarter.AddDialogLine("notable_support_offer", "notable_support_request_response", "notable_support_decision", "{=!}{SUPPORT_RESPONSE}", new ConversationSentence.OnConditionDelegate(this.notable_support_offer_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("notable_support_player_decision_accept", "notable_support_decision", "notable_support_accepted", "{=LjeuI2kN}It's a deal.", null, new ConversationSentence.OnConsequenceDelegate(this.notable_support_player_decision_accept_on_consequences), 100, new ConversationSentence.OnClickableConditionDelegate(this.notable_support_player_decision_accept_on_clickable_condition), null);
			campaignGameStarter.AddPlayerLine("notable_support_player_decision_reject", "notable_support_decision", "notable_support_rejected", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("notable_support_agreement", "notable_support_accepted", "lord_pretalk", "{=QIrR9NhL}A wise decision.", null, null, 100, null);
			campaignGameStarter.AddDialogLine("notable_support_rejection", "notable_support_rejected", "lord_pretalk", "{=ppi6eVos}As you wish.", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("notable_support_end", "hero_main_options", "notable_support_end_response", "{=qPgpPGUA}I wish to end our arrangement.", new ConversationSentence.OnConditionDelegate(this.notable_support_end_on_condition), null, 100, null, null);
			campaignGameStarter.AddDialogLine("notable_support_end_check", "notable_support_end_response", "notable_support_end_confirmation", "{=dK2fLdEX}Are you sure about this?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("notable_support_end_agreement", "notable_support_end_confirmation", "close_window", "{=kB65SzbF}Yes.", null, new ConversationSentence.OnConsequenceDelegate(this.notable_support_end_agreement_on_consequences), 100, null, null);
			campaignGameStarter.AddPlayerLine("notable_support_end_rejection", "notable_support_end_confirmation", "lord_pretalk", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
		}

		// Token: 0x060042BC RID: 17084 RVA: 0x00142120 File Offset: 0x00140320
		private bool notable_support_request_on_condition()
		{
			return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.IsNotable && Hero.OneToOneConversationHero.SupporterOf != Clan.PlayerClan;
		}

		// Token: 0x060042BD RID: 17085 RVA: 0x0014214C File Offset: 0x0014034C
		private bool notable_support_request_on_clickable_condition(out TextObject explanation)
		{
			explanation = TextObject.GetEmpty();
			float relationWithPlayer = Hero.OneToOneConversationHero.GetRelationWithPlayer();
			if (Hero.OneToOneConversationHero.SupporterOf != null)
			{
				float num = (float)Hero.OneToOneConversationHero.GetRelation(Hero.OneToOneConversationHero.SupporterOf.Leader);
				if (num == (float)Campaign.Current.Models.DiplomacyModel.MaxRelationLimit)
				{
					explanation = new TextObject("{=ySXERZlE}This notable has a very good relationship with {CLAN_NAME} and is not interested in supporting another clan.", null);
					explanation.SetTextVariable("CLAN_NAME", Hero.OneToOneConversationHero.SupporterOf.EncyclopediaLinkWithName);
					return false;
				}
				if (relationWithPlayer < num)
				{
					explanation = new TextObject("{=ztand1Kr}This notable currently supports {CLAN_NAME}. You need at least {RELATION_LEVEL} relationship to ask them to support you.", null);
					explanation.SetTextVariable("RELATION_LEVEL", Hero.OneToOneConversationHero.GetRelation(Hero.OneToOneConversationHero.SupporterOf.Leader) + 1);
					explanation.SetTextVariable("CLAN_NAME", Hero.OneToOneConversationHero.SupporterOf.EncyclopediaLinkWithName);
					return false;
				}
			}
			if (relationWithPlayer < 50f)
			{
				explanation = new TextObject("{=qmF8DIxA}You need at least {RELATION_LEVEL} relationship to do this.", null);
				explanation.SetTextVariable("RELATION_LEVEL", 50);
				return false;
			}
			return true;
		}

		// Token: 0x060042BE RID: 17086 RVA: 0x00142258 File Offset: 0x00140458
		private bool notable_support_offer_on_condition()
		{
			int initialNotableSupporterCost = Campaign.Current.Models.NotablePowerModel.GetInitialNotableSupporterCost(Hero.OneToOneConversationHero);
			TextObject textObject;
			if (Hero.OneToOneConversationHero.SupporterOf == null)
			{
				textObject = new TextObject("{=FZRRiJNW}Of course. I will speak of your qualities whenever I can. But to maintain my standing costs money - solving problems for the low, buying gifts for the high, you know how it goes... Perhaps {AMOUNT}{GOLD_ICON}?", null);
			}
			else
			{
				textObject = new TextObject("{=iqYXn1Us}I have been blessed with many good friends and I am glad to count you among them. However, to support you, I would need to forsake {SUPPORTED_CLAN}. This will cost me greatly. But, if you can provide compensation, I can lend you my support. {AMOUNT}{GOLD_ICON} should cover my loss.", null);
				textObject.SetTextVariable("SUPPORTED_CLAN", Hero.OneToOneConversationHero.SupporterOf.Name);
			}
			textObject.SetTextVariable("AMOUNT", initialNotableSupporterCost);
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			MBTextManager.SetTextVariable("SUPPORT_RESPONSE", textObject, false);
			return true;
		}

		// Token: 0x060042BF RID: 17087 RVA: 0x001422F0 File Offset: 0x001404F0
		private bool notable_support_player_decision_accept_on_clickable_condition(out TextObject explanation)
		{
			int initialNotableSupporterCost = Campaign.Current.Models.NotablePowerModel.GetInitialNotableSupporterCost(Hero.OneToOneConversationHero);
			if (Hero.MainHero.Gold < initialNotableSupporterCost)
			{
				explanation = new TextObject("{=1jhoMqHv}You don't have enough gold to do this.", null);
				return false;
			}
			explanation = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x060042C0 RID: 17088 RVA: 0x0014233C File Offset: 0x0014053C
		private void notable_support_player_decision_accept_on_consequences()
		{
			int initialNotableSupporterCost = Campaign.Current.Models.NotablePowerModel.GetInitialNotableSupporterCost(Hero.OneToOneConversationHero);
			Hero.OneToOneConversationHero.SupporterOf = Clan.PlayerClan;
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, initialNotableSupporterCost, false);
			ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, 5, true, true);
		}

		// Token: 0x060042C1 RID: 17089 RVA: 0x00142390 File Offset: 0x00140590
		private bool notable_support_end_on_condition()
		{
			return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.IsNotable && Hero.OneToOneConversationHero.SupporterOf == Clan.PlayerClan;
		}

		// Token: 0x060042C2 RID: 17090 RVA: 0x001423B8 File Offset: 0x001405B8
		private void notable_support_end_agreement_on_consequences()
		{
			Hero.OneToOneConversationHero.SupporterOf = null;
			TextObject textObject = new TextObject("{=afzeDAPd}{NOTABLE.NAME} no longer supports your clan.", null);
			textObject.SetCharacterProperties("NOTABLE", Hero.OneToOneConversationHero.CharacterObject, false);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0f, 1f, 0f, 1f)));
		}

		// Token: 0x060042C3 RID: 17091 RVA: 0x00142419 File Offset: 0x00140619
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0400130B RID: 4875
		private const int SupporterRelationThreshold = 50;

		// Token: 0x0400130C RID: 4876
		private const int RelationBonusOnSupport = 5;
	}
}
