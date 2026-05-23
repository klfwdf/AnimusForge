using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class HeroKnownInformationCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTickHero);
		CampaignEvents.ConversationEnded.AddNonSerializedListener(this, ConversationEnded);
		CampaignEvents.OnAgentJoinedConversationEvent.AddNonSerializedListener(this, OnAgentJoinedConversation);
		CampaignEvents.OnPlayerMetHeroEvent.AddNonSerializedListener(this, OnPlayerMetHero);
		CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnHeroesMarried);
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinishedEvent);
		CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCharacterCreationIsOver);
		CampaignEvents.OnPlayerLearnsAboutHeroEvent.AddNonSerializedListener(this, OnPlayerLearnsAboutHero);
		CampaignEvents.NearbyPartyAddedToPlayerMapEvent.AddNonSerializedListener(this, OnNearbyPartyAddedToPlayerMapEvent);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuChanged);
		CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, OnAfterMissionStarted);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, OnPartyAttachedAnotherParty);
		CampaignEvents.OnPlayerJoinedTournamentEvent.AddNonSerializedListener(this, OnPlayerJoinedTournament);
		CampaignEvents.OnMarriageOfferedToPlayerEvent.AddNonSerializedListener(this, OnMarriageOfferedToPlayer);
	}

	private void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
	{
		if (suitor.Clan == Clan.PlayerClan)
		{
			maiden.IsKnownToPlayer = true;
		}
		else
		{
			suitor.IsKnownToPlayer = true;
		}
	}

	private void OnPlayerJoinedTournament(Town town, bool isParticipant)
	{
		foreach (CharacterObject participantCharacter in Campaign.Current.TournamentManager.GetTournamentGame(town).GetParticipantCharacters(town.Settlement, includePlayer: false))
		{
			if (participantCharacter.IsHero && !participantCharacter.HeroObject.IsKnownToPlayer)
			{
				participantCharacter.HeroObject.IsKnownToPlayer = true;
			}
		}
	}

	private void OnNearbyPartyAddedToPlayerMapEvent(MobileParty mobileParty)
	{
		if (mobileParty.LeaderHero != null)
		{
			mobileParty.LeaderHero.IsKnownToPlayer = true;
		}
	}

	private void OnPartyAttachedAnotherParty(MobileParty party)
	{
		if (party == MobileParty.MainParty)
		{
			if (party.AttachedTo.LeaderHero != null)
			{
				party.AttachedTo.LeaderHero.IsKnownToPlayer = true;
			}
			{
				foreach (MobileParty attachedParty in party.AttachedTo.AttachedParties)
				{
					if (attachedParty.LeaderHero != null)
					{
						attachedParty.LeaderHero.IsKnownToPlayer = true;
					}
				}
				return;
			}
		}
		if ((party.AttachedTo == MobileParty.MainParty || party.AttachedTo == MobileParty.MainParty.AttachedTo) && party.LeaderHero != null)
		{
			party.LeaderHero.IsKnownToPlayer = true;
		}
	}

	private void OnPartyAttachedToAnotherParty(MobileParty mobileParty)
	{
		if (mobileParty == MobileParty.MainParty)
		{
			if (mobileParty.AttachedTo.LeaderHero != null)
			{
				mobileParty.AttachedTo.LeaderHero.IsKnownToPlayer = true;
			}
			{
				foreach (MobileParty attachedParty in mobileParty.AttachedTo.AttachedParties)
				{
					if (attachedParty.LeaderHero != null)
					{
						attachedParty.LeaderHero.IsKnownToPlayer = true;
					}
				}
				return;
			}
		}
		if ((mobileParty.AttachedTo == MobileParty.MainParty || mobileParty.AttachedTo == MobileParty.MainParty.AttachedTo) && mobileParty.LeaderHero != null)
		{
			mobileParty.LeaderHero.IsKnownToPlayer = true;
		}
	}

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		if (MapEvent.PlayerMapEvent != mapEvent)
		{
			return;
		}
		foreach (PartyBase involvedParty in mapEvent.InvolvedParties)
		{
			if (involvedParty.LeaderHero != null)
			{
				involvedParty.LeaderHero.IsKnownToPlayer = true;
			}
		}
	}

	private void OnPlayerLearnsAboutHero(Hero hero)
	{
		UpdateHeroLocation(hero);
		if (hero.Clan != Clan.PlayerClan)
		{
			TextObject textObject = new TextObject("{=oSghSUxp}You've learned about {?IS_RULER}{RULER_NAME_AND_TITLE}{?}{HERO.NAME}{\\?}.");
			textObject.SetTextVariable("IS_RULER", hero.IsKingdomLeader ? 1 : 0);
			if (hero.IsKingdomLeader)
			{
				TextObject textObject2 = GameTexts.FindText("str_faction_ruler_name_with_title", hero.MapFaction.Culture.StringId);
				textObject2.SetCharacterProperties("RULER", hero.CharacterObject);
				textObject.SetTextVariable("RULER_NAME_AND_TITLE", textObject2);
			}
			else
			{
				textObject.SetCharacterProperties("HERO", hero.CharacterObject);
			}
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
		}
	}

	private void OnAfterMissionStarted(IMission mission)
	{
		if (CampaignMission.Current.Location != null)
		{
			LearnAboutLocationCharacters(CampaignMission.Current.Location);
		}
	}

	private void OnGameMenuChanged(MenuCallbackArgs args)
	{
		foreach (Location menuLocation in Campaign.Current.GameMenuManager.MenuLocations)
		{
			LearnAboutLocationCharacters(menuLocation);
		}
	}

	private void LearnAboutLocationCharacters(Location location)
	{
		foreach (LocationCharacter character in location.GetCharacterList())
		{
			if (character.Character.IsHero && character.Character.HeroObject.CurrentSettlement == Settlement.CurrentSettlement)
			{
				character.Character.HeroObject.IsKnownToPlayer = true;
			}
		}
	}

	private void OnPlayerMetHero(Hero hero)
	{
		hero.IsKnownToPlayer = true;
	}

	private void OnDailyTickHero(Hero hero)
	{
		UpdateHeroLocation(hero);
	}

	private void OnAgentJoinedConversation(IAgent agent)
	{
		CharacterObject characterObject = (CharacterObject)agent.Character;
		if (characterObject.IsHero)
		{
			UpdateHeroLocation(characterObject.HeroObject);
			characterObject.HeroObject.IsKnownToPlayer = true;
		}
		Hero hero = MobileParty.ConversationParty?.CaravanPartyComponent?.Owner;
		if (hero != null)
		{
			hero.IsKnownToPlayer = true;
		}
	}

	private void UpdateHeroLocation(Hero hero)
	{
		if (hero.IsKnownToPlayer)
		{
			if (hero.IsActive || hero.IsPrisoner)
			{
				Settlement closestSettlement = HeroHelper.GetClosestSettlement(hero);
				if (closestSettlement != null)
				{
					hero.UpdateLastKnownClosestSettlement(closestSettlement);
				}
			}
		}
		else
		{
			hero.UpdateLastKnownClosestSettlement(null);
		}
	}

	private void OnCharacterCreationIsOver()
	{
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			UpdateHeroLocation(allAliveHero);
		}
	}

	private void OnGameLoadFinishedEvent()
	{
		if (!MBSaveLoad.IsUpdatingGameVersion || !(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.8.1.0")))
		{
			return;
		}
		foreach (Hero hero in Clan.PlayerClan.Heroes)
		{
			hero.SetHasMet();
		}
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero.LastKnownClosestSettlement == null)
			{
				UpdateHeroLocation(allAliveHero);
			}
			if (allAliveHero.HasMet)
			{
				allAliveHero.IsKnownToPlayer = true;
			}
		}
	}

	private void OnHeroesMarried(Hero hero1, Hero hero2, bool showNotification)
	{
		if (hero1 == Hero.MainHero)
		{
			hero2.SetHasMet();
		}
		if (hero2 == Hero.MainHero)
		{
			hero1.SetHasMet();
		}
	}

	private void OnHeroCreated(Hero hero, bool isBornNaturally)
	{
		if (hero.Clan == Clan.PlayerClan)
		{
			hero.SetHasMet();
		}
	}

	private void ConversationEnded(IEnumerable<CharacterObject> conversationCharacters)
	{
		foreach (CharacterObject conversationCharacter in conversationCharacters)
		{
			if (conversationCharacter.IsHero)
			{
				bool result = true;
				CampaignEventDispatcher.Instance.CanPlayerMeetWithHeroAfterConversation(conversationCharacter.HeroObject, ref result);
				if (result)
				{
					conversationCharacter.HeroObject.SetHasMet();
				}
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
