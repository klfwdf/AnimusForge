using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class NotableSupportersCampaignBehavior : CampaignBehaviorBase
{
	private const int SupporterRelationThreshold = 50;

	private const int RelationBonusOnSupport = 5;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	private void OnGameLoadFinished()
	{
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			for (int num = Clan.PlayerClan.SupporterNotables.Count - 1; num >= 0; num--)
			{
				Clan.PlayerClan.SupporterNotables[num].SupporterOf = null;
			}
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	private void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddPlayerLine("notable_support_request", "hero_main_options", "notable_support_request_response", "{=lxL9hHEf}Would you use your influence to support my clan?", notable_support_request_on_condition, null, 100, notable_support_request_on_clickable_condition);
		campaignGameStarter.AddDialogLine("notable_support_offer", "notable_support_request_response", "notable_support_decision", "{=!}{SUPPORT_RESPONSE}", notable_support_offer_on_condition, null);
		campaignGameStarter.AddPlayerLine("notable_support_player_decision_accept", "notable_support_decision", "notable_support_accepted", "{=LjeuI2kN}It's a deal.", null, notable_support_player_decision_accept_on_consequences, 100, notable_support_player_decision_accept_on_clickable_condition);
		campaignGameStarter.AddPlayerLine("notable_support_player_decision_reject", "notable_support_decision", "notable_support_rejected", "{=D33fIGQe}Never mind.", null, null);
		campaignGameStarter.AddDialogLine("notable_support_agreement", "notable_support_accepted", "lord_pretalk", "{=QIrR9NhL}A wise decision.", null, null);
		campaignGameStarter.AddDialogLine("notable_support_rejection", "notable_support_rejected", "lord_pretalk", "{=ppi6eVos}As you wish.", null, null);
		campaignGameStarter.AddPlayerLine("notable_support_end", "hero_main_options", "notable_support_end_response", "{=qPgpPGUA}I wish to end our arrangement.", notable_support_end_on_condition, null);
		campaignGameStarter.AddDialogLine("notable_support_end_check", "notable_support_end_response", "notable_support_end_confirmation", "{=dK2fLdEX}Are you sure about this?", null, null);
		campaignGameStarter.AddPlayerLine("notable_support_end_agreement", "notable_support_end_confirmation", "close_window", "{=kB65SzbF}Yes.", null, notable_support_end_agreement_on_consequences);
		campaignGameStarter.AddPlayerLine("notable_support_end_rejection", "notable_support_end_confirmation", "lord_pretalk", "{=D33fIGQe}Never mind.", null, null);
	}

	private bool notable_support_request_on_condition()
	{
		if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.IsNotable)
		{
			return Hero.OneToOneConversationHero.SupporterOf != Clan.PlayerClan;
		}
		return false;
	}

	private bool notable_support_request_on_clickable_condition(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		float relationWithPlayer = Hero.OneToOneConversationHero.GetRelationWithPlayer();
		if (Hero.OneToOneConversationHero.SupporterOf != null)
		{
			float num = Hero.OneToOneConversationHero.GetRelation(Hero.OneToOneConversationHero.SupporterOf.Leader);
			if (num == (float)Campaign.Current.Models.DiplomacyModel.MaxRelationLimit)
			{
				explanation = new TextObject("{=ySXERZlE}This notable has a very good relationship with {CLAN_NAME} and is not interested in supporting another clan.");
				explanation.SetTextVariable("CLAN_NAME", Hero.OneToOneConversationHero.SupporterOf.EncyclopediaLinkWithName);
				return false;
			}
			if (relationWithPlayer < num)
			{
				explanation = new TextObject("{=ztand1Kr}This notable currently supports {CLAN_NAME}. You need at least {RELATION_LEVEL} relationship to ask them to support you.");
				explanation.SetTextVariable("RELATION_LEVEL", Hero.OneToOneConversationHero.GetRelation(Hero.OneToOneConversationHero.SupporterOf.Leader) + 1);
				explanation.SetTextVariable("CLAN_NAME", Hero.OneToOneConversationHero.SupporterOf.EncyclopediaLinkWithName);
				return false;
			}
		}
		if (relationWithPlayer < 50f)
		{
			explanation = new TextObject("{=qmF8DIxA}You need at least {RELATION_LEVEL} relationship to do this.");
			explanation.SetTextVariable("RELATION_LEVEL", 50);
			return false;
		}
		return true;
	}

	private bool notable_support_offer_on_condition()
	{
		TextObject textObject = null;
		int initialNotableSupporterCost = Campaign.Current.Models.NotablePowerModel.GetInitialNotableSupporterCost(Hero.OneToOneConversationHero);
		if (Hero.OneToOneConversationHero.SupporterOf == null)
		{
			textObject = new TextObject("{=FZRRiJNW}Of course. I will speak of your qualities whenever I can. But to maintain my standing costs money - solving problems for the low, buying gifts for the high, you know how it goes... Perhaps {AMOUNT}{GOLD_ICON}?");
		}
		else
		{
			textObject = new TextObject("{=iqYXn1Us}I have been blessed with many good friends and I am glad to count you among them. However, to support you, I would need to forsake {SUPPORTED_CLAN}. This will cost me greatly. But, if you can provide compensation, I can lend you my support. {AMOUNT}{GOLD_ICON} should cover my loss.");
			textObject.SetTextVariable("SUPPORTED_CLAN", Hero.OneToOneConversationHero.SupporterOf.Name);
		}
		textObject.SetTextVariable("AMOUNT", initialNotableSupporterCost);
		textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		MBTextManager.SetTextVariable("SUPPORT_RESPONSE", textObject);
		return true;
	}

	private bool notable_support_player_decision_accept_on_clickable_condition(out TextObject explanation)
	{
		int initialNotableSupporterCost = Campaign.Current.Models.NotablePowerModel.GetInitialNotableSupporterCost(Hero.OneToOneConversationHero);
		if (Hero.MainHero.Gold < initialNotableSupporterCost)
		{
			explanation = new TextObject("{=1jhoMqHv}You don't have enough gold to do this.");
			return false;
		}
		explanation = TextObject.GetEmpty();
		return true;
	}

	private void notable_support_player_decision_accept_on_consequences()
	{
		int initialNotableSupporterCost = Campaign.Current.Models.NotablePowerModel.GetInitialNotableSupporterCost(Hero.OneToOneConversationHero);
		Hero.OneToOneConversationHero.SupporterOf = Clan.PlayerClan;
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, initialNotableSupporterCost);
		ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, 5);
	}

	private bool notable_support_end_on_condition()
	{
		if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.IsNotable)
		{
			return Hero.OneToOneConversationHero.SupporterOf == Clan.PlayerClan;
		}
		return false;
	}

	private void notable_support_end_agreement_on_consequences()
	{
		Hero.OneToOneConversationHero.SupporterOf = null;
		TextObject textObject = new TextObject("{=afzeDAPd}{NOTABLE.NAME} no longer supports your clan.");
		textObject.SetCharacterProperties("NOTABLE", Hero.OneToOneConversationHero.CharacterObject);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0f, 1f, 0f)));
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
