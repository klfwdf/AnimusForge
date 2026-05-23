using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class GovernorCampaignBehavior : CampaignBehaviorBase
{
	private const int CultureDialogueOptionCount = 3;

	private List<CultureObject> _availablePlayerKingdomCultures = new List<CultureObject>();

	private int _kingdomCreationCurrentCulturePageIndex;

	private CultureObject _kingdomCreationChosenCulture;

	private TextObject _kingdomCreationChosenName;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
		CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	private void OnGameLoadFinished()
	{
		if (!MBSaveLoad.IsUpdatingGameVersion || !(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0")))
		{
			return;
		}
		foreach (Town allFief in Town.AllFiefs)
		{
			if (allFief.Governor != null && allFief != allFief.Governor.GovernorOf)
			{
				allFief.Governor = null;
			}
		}
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	private void DailyTickSettlement(Settlement settlement)
	{
		if ((!settlement.IsTown && !settlement.IsCastle) || settlement.Town.Governor == null)
		{
			return;
		}
		Hero governor = settlement.Town.Governor;
		if (MBRandom.RandomFloat <= DefaultPerks.Charm.InBloom.SecondaryBonus && governor.GetPerkValue(DefaultPerks.Charm.InBloom))
		{
			Hero randomElementWithPredicate = settlement.Notables.GetRandomElementWithPredicate((Hero x) => x.IsFemale != governor.IsFemale);
			if (randomElementWithPredicate != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(governor.Clan.Leader, randomElementWithPredicate, 1);
			}
		}
		if (MBRandom.RandomFloat <= DefaultPerks.Charm.YoungAndRespectful.SecondaryBonus && governor.GetPerkValue(DefaultPerks.Charm.YoungAndRespectful))
		{
			Hero randomElementWithPredicate2 = settlement.Notables.GetRandomElementWithPredicate((Hero x) => x.IsFemale == governor.IsFemale);
			if (randomElementWithPredicate2 != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(governor.Clan.Leader, randomElementWithPredicate2, 1);
			}
		}
		if (MBRandom.RandomFloat <= DefaultPerks.Charm.MeaningfulFavors.SecondaryBonus && governor.GetPerkValue(DefaultPerks.Charm.MeaningfulFavors))
		{
			foreach (Hero notable in settlement.Notables)
			{
				if (notable.Power >= 200f)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(settlement.OwnerClan.Leader, notable, 1);
				}
			}
		}
		SkillLevelingManager.OnSettlementGoverned(governor, settlement);
	}

	private void OnHeroChangedClan(Hero hero, Clan oldClan)
	{
		if (hero.GovernorOf != null && hero.GovernorOf.OwnerClan != hero.Clan)
		{
			ChangeGovernorAction.RemoveGovernorOf(hero);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void AddDialogs(CampaignGameStarter starter)
	{
		starter.AddPlayerLine("governor_talk_start", "hero_main_options", "governor_talk_start_reply", "{=zBo78JQb}How are things doing here in {GOVERNOR_SETTLEMENT}?", governor_talk_start_on_condition, null);
		starter.AddDialogLine("governor_talk_start_reply", "governor_talk_start_reply", "lord_pretalk", "{=!}{SETTLEMENT_DESCRIPTION}", governor_talk_start_reply_on_condition, null, 200);
		starter.AddPlayerLine("governor_talk_kingdom_creation_start", "hero_main_options", "governor_kingdom_creation_reply", "{=EKuB6Ohf}It is time to take a momentous step... It is time to proclaim a new kingdom.", governor_talk_kingdom_creation_start_on_condition, governor_talk_kingdom_creation_start_on_consequence, 200, governor_talk_kingdom_creation_start_clickable_condition);
		starter.AddDialogLine("governor_talk_kingdom_creation_reply", "governor_kingdom_creation_reply", "governor_kingdom_creation_culture_selection", "{=ZyNjXUHc}I am at your command.", null, null);
		starter.AddDialogLine("governor_talk_kingdom_creation_culture_selection", "governor_kingdom_creation_culture_selection", "governor_kingdom_creation_culture_selection_options", "{=jxEVSu98}The language of our documents, and our customary laws... Whose should we use?", null, null);
		starter.AddPlayerLine("governor_talk_kingdom_creation_culture_selection_option", "governor_kingdom_creation_culture_selection_options", "governor_kingdom_creation_culture_selected", "{CULTURE_OPTION_0}", governor_talk_kingdom_creation_culture_option_0_on_condition, governor_talk_kingdom_creation_culture_option_0_on_consequence);
		starter.AddPlayerLine("governor_talk_kingdom_creation_culture_selection_option_2", "governor_kingdom_creation_culture_selection_options", "governor_kingdom_creation_culture_selected", "{CULTURE_OPTION_1}", governor_talk_kingdom_creation_culture_option_1_on_condition, governor_talk_kingdom_creation_culture_option_1_on_consequence);
		starter.AddPlayerLine("governor_talk_kingdom_creation_culture_selection_option_3", "governor_kingdom_creation_culture_selection_options", "governor_kingdom_creation_culture_selected", "{CULTURE_OPTION_2}", governor_talk_kingdom_creation_culture_option_2_on_condition, governor_talk_kingdom_creation_culture_option_2_on_consequence);
		starter.AddPlayerLine("governor_talk_kingdom_creation_culture_selection_other", "governor_kingdom_creation_culture_selection_options", "governor_kingdom_creation_culture_selection", "{=kcuNzSvf}I have another people in mind.", governor_talk_kingdom_creation_culture_other_on_condition, governor_talk_kingdom_creation_culture_other_on_consequence);
		starter.AddPlayerLine("governor_talk_kingdom_creation_culture_selection_cancel", "governor_kingdom_creation_culture_selection_options", "governor_kingdom_creation_exit", "{=hbzs5tLd}On second thought, perhaps now is not the right time.", null, null);
		starter.AddDialogLine("governor_talk_kingdom_creation_exit_reply", "governor_kingdom_creation_exit", "close_window", "{=ppi6eVos}As you wish.", null, null);
		starter.AddDialogLine("governor_talk_kingdom_creation_culture_selected", "governor_kingdom_creation_culture_selected", "governor_kingdom_creation_culture_selected_confirmation", "{=VOtKthQU}Yes. A kingdom using {CULTURE_ADJECTIVE} law would institute the following: {INITIAL_POLICY_NAMES}.", governor_kingdom_creation_culture_selected_on_condition, null);
		starter.AddPlayerLine("governor_talk_kingdom_creation_culture_selected_player_reply", "governor_kingdom_creation_culture_selected_confirmation", "governor_kingdom_creation_name_selection", "{=dzXaXKaC}Very well.", null, null);
		starter.AddPlayerLine("governor_talk_kingdom_creation_culture_selected_player_reply_2", "governor_kingdom_creation_culture_selected_confirmation", "governor_kingdom_creation_culture_selection", "{=kTjsx8gN}Perhaps we should choose another set of laws and customs.", null, null);
		starter.AddDialogLine("governor_talk_kingdom_creation_name_selection", "governor_kingdom_creation_name_selection", "governor_kingdom_creation_name_selection_response", "{=wT1ducZX}Now. What will the kingdom be called?", null, null);
		starter.AddPlayerLine("governor_talk_kingdom_creation_name_selection_player", "governor_kingdom_creation_name_selection_response", "governor_kingdom_creation_name_selection_prompted", "{=XRoG766S}I'll name it...", null, governor_talk_kingdom_creation_name_selection_on_consequence);
		starter.AddDialogLine("governor_talk_kingdom_creation_name_selection_response", "governor_kingdom_creation_name_selection_prompted", "governor_kingdom_creation_name_selected", "{=shf5aY3l}I'm listening...", null, null);
		starter.AddPlayerLine("governor_talk_kingdom_creation_name_selection_cancel", "governor_kingdom_creation_name_selection_response", "governor_kingdom_creation_exit", "{=7HpfrmIU}On a second thought... Now is not the right time to do this.", null, null);
		starter.AddDialogLine("governor_talk_kingdom_creation_name_selection_final_response", "governor_kingdom_creation_name_selected", "governor_kingdom_creation_finalization", "{=CzJZ5zhT}So it shall be proclaimed throughout your domain. May {KINGDOM_NAME} forever be victorious!", governor_talk_kingdom_creation_finalization_on_condition, null);
		starter.AddPlayerLine("governor_talk_kingdom_creation_finalization", "governor_kingdom_creation_finalization", "close_window", "{=VRbbIWNf}So it shall be.", governor_talk_kingdom_creation_finalization_on_condition, governor_talk_kingdom_creation_finalization_on_consequence);
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if (victim.GovernorOf != null)
		{
			ChangeGovernorAction.RemoveGovernorOf(victim);
		}
	}

	private bool governor_talk_start_on_condition()
	{
		if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.GovernorOf != null && Hero.OneToOneConversationHero.CurrentSettlement != null && Hero.OneToOneConversationHero.CurrentSettlement.IsTown && Hero.OneToOneConversationHero.CurrentSettlement.Town == Hero.OneToOneConversationHero.GovernorOf && Hero.OneToOneConversationHero.GovernorOf.Owner.Owner == Hero.MainHero)
		{
			MBTextManager.SetTextVariable("GOVERNOR_SETTLEMENT", Hero.OneToOneConversationHero.CurrentSettlement.Name);
			return true;
		}
		return false;
	}

	private bool governor_talk_start_reply_on_condition()
	{
		Settlement currentSettlement = Hero.OneToOneConversationHero.CurrentSettlement;
		TextObject textObject = TextObject.GetEmpty();
		switch (currentSettlement.Town.GetProsperityLevel())
		{
		case SettlementComponent.ProsperityLevel.High:
			textObject = new TextObject("{=8G94SlPD}We are doing well, my {?HERO.GENDER}lady{?}lord{\\?}. The merchants say business is brisk, and everything the people need appears to be in good supply.");
			break;
		case SettlementComponent.ProsperityLevel.Mid:
			textObject = new TextObject("{=HgdbSrq9}Things are all right, my {?HERO.GENDER}lady{?}lord{\\?}. The merchants say that they are breaking even, for the most part. Some prices are high, but most of what the people need is available.");
			break;
		case SettlementComponent.ProsperityLevel.Low:
			textObject = new TextObject("{=rbJEuVKg}Things could certainly be better, my {?HERO.GENDER}lady{?}lord{\\?}. The merchants say business is slow, and the people complain that goods are expensive and in short supply.");
			break;
		}
		StringHelpers.SetCharacterProperties("HERO", CharacterObject.PlayerCharacter, textObject);
		MBTextManager.SetTextVariable("SETTLEMENT_DESCRIPTION", textObject.ToString());
		return true;
	}

	private bool governor_talk_kingdom_creation_start_on_condition()
	{
		if (Clan.PlayerClan.Kingdom == null && Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.GovernorOf != null)
		{
			return Hero.OneToOneConversationHero.GovernorOf.Settlement.MapFaction == Hero.MainHero.MapFaction;
		}
		return false;
	}

	private void governor_talk_kingdom_creation_start_on_consequence()
	{
		_availablePlayerKingdomCultures.Clear();
		_availablePlayerKingdomCultures = Campaign.Current.Models.KingdomCreationModel.GetAvailablePlayerKingdomCultures().ToList();
		_kingdomCreationCurrentCulturePageIndex = 0;
	}

	private bool governor_talk_kingdom_creation_start_clickable_condition(out TextObject explanation)
	{
		List<TextObject> explanations;
		bool result = Campaign.Current.Models.KingdomCreationModel.IsPlayerKingdomCreationPossible(out explanations);
		string text = "";
		foreach (TextObject item in explanations)
		{
			text += item;
			if (item != explanations[explanations.Count - 1])
			{
				text += "\n";
			}
		}
		explanation = new TextObject(text);
		return result;
	}

	private bool governor_talk_kingdom_creation_culture_option_0_on_condition()
	{
		return HandleAvailableCultureConditionAndText(0);
	}

	private bool governor_talk_kingdom_creation_culture_option_1_on_condition()
	{
		return HandleAvailableCultureConditionAndText(1);
	}

	private bool governor_talk_kingdom_creation_culture_option_2_on_condition()
	{
		return HandleAvailableCultureConditionAndText(2);
	}

	private bool HandleAvailableCultureConditionAndText(int index)
	{
		int cultureIndex = GetCultureIndex(index);
		if (_availablePlayerKingdomCultures.Count > cultureIndex)
		{
			TextObject textObject = new TextObject("{=mY6DbVfc}The language and laws of {CULTURE_NAME}.");
			textObject.SetTextVariable("CULTURE_NAME", FactionHelper.GetInformalNameForFactionCulture(_availablePlayerKingdomCultures[cultureIndex]));
			MBTextManager.SetTextVariable("CULTURE_OPTION_" + index, textObject);
			return true;
		}
		return false;
	}

	private int GetCultureIndex(int optionIndex)
	{
		return _kingdomCreationCurrentCulturePageIndex * 3 + optionIndex;
	}

	private void governor_talk_kingdom_creation_culture_option_0_on_consequence()
	{
		_kingdomCreationChosenCulture = _availablePlayerKingdomCultures[GetCultureIndex(0)];
	}

	private void governor_talk_kingdom_creation_culture_option_1_on_consequence()
	{
		_kingdomCreationChosenCulture = _availablePlayerKingdomCultures[GetCultureIndex(1)];
	}

	private void governor_talk_kingdom_creation_culture_option_2_on_consequence()
	{
		_kingdomCreationChosenCulture = _availablePlayerKingdomCultures[GetCultureIndex(2)];
	}

	private bool governor_talk_kingdom_creation_culture_other_on_condition()
	{
		return _availablePlayerKingdomCultures.Count > 3;
	}

	private void governor_talk_kingdom_creation_culture_other_on_consequence()
	{
		_kingdomCreationCurrentCulturePageIndex++;
		if (_kingdomCreationCurrentCulturePageIndex > MathF.Ceiling((float)_availablePlayerKingdomCultures.Count / 3f) - 1)
		{
			_kingdomCreationCurrentCulturePageIndex = 0;
		}
	}

	private bool governor_kingdom_creation_culture_selected_on_condition()
	{
		TextObject text = GameTexts.GameTextHelper.MergeTextObjectsWithComma(_kingdomCreationChosenCulture.DefaultPolicyList.Select((PolicyObject t) => t.Name).ToList(), includeAnd: true);
		MBTextManager.SetTextVariable("INITIAL_POLICY_NAMES", text);
		MBTextManager.SetTextVariable("CULTURE_ADJECTIVE", FactionHelper.GetAdjectiveForFactionCulture(_kingdomCreationChosenCulture));
		return true;
	}

	private void governor_talk_kingdom_creation_name_selection_on_consequence()
	{
		_kingdomCreationChosenName = TextObject.GetEmpty();
		InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=RuaA8t97}Kingdom Name").ToString(), string.Empty, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), OnKingdomNameSelectionDone, OnKingdomNameSelectionCancel, shouldInputBeObfuscated: false, FactionHelper.IsKingdomNameApplicable));
	}

	private void OnKingdomNameSelectionDone(string chosenName)
	{
		_kingdomCreationChosenName = new TextObject(chosenName);
		Campaign.Current.ConversationManager.ContinueConversation();
	}

	private void OnKingdomNameSelectionCancel()
	{
		Campaign.Current.ConversationManager.EndConversation();
	}

	private bool governor_talk_kingdom_creation_finalization_on_condition()
	{
		MBTextManager.SetTextVariable("KINGDOM_NAME", _kingdomCreationChosenName);
		return true;
	}

	private void governor_talk_kingdom_creation_finalization_on_consequence()
	{
		Campaign.Current.KingdomManager.CreateKingdom(_kingdomCreationChosenName, _kingdomCreationChosenName, _kingdomCreationChosenCulture, Clan.PlayerClan, _kingdomCreationChosenCulture.DefaultPolicyList);
	}
}
