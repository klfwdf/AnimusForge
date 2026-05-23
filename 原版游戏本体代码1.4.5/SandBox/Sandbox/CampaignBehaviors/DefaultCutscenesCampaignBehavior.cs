using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SandBox.CampaignBehaviors;

public class DefaultCutscenesCampaignBehavior : CampaignBehaviorBase
{
	private bool _heroWonLastMapEVent;

	private CultureObject _lastEnemyCulture;

	public override void RegisterEvents()
	{
		CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnHeroesMarried);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnd);
		CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
		CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, OnKingdomCreated);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
		CampaignEvents.OnBeforeMainCharacterDiedEvent.AddNonSerializedListener(this, OnBeforeMainCharacterDied);
		CampaignEvents.OnMercenaryServiceEndedEvent.AddNonSerializedListener(this, OnMercenaryServiceEnded);
	}

	private void OnBeforeMainCharacterDied(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		SceneNotificationData sceneNotificationData = null;
		if (victim == Hero.MainHero)
		{
			MobileParty partyBelongedTo = victim.PartyBelongedTo;
			if (partyBelongedTo != null && partyBelongedTo.IsCurrentlyAtSea)
			{
				sceneNotificationData = new NavalDeathSceneNotificationItem(victim, CampaignTime.Now, detail);
			}
			else
			{
				switch (detail)
				{
				case KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge:
					sceneNotificationData = new DeathOldAgeSceneNotificationItem(victim);
					break;
				case KillCharacterAction.KillCharacterActionDetail.DiedInBattle:
					if (_heroWonLastMapEVent)
					{
						bool noCompanions = !victim.CompanionsInParty.Any();
						List<CharacterObject> allyCharacters = new List<CharacterObject>();
						FillAllyCharacters(noCompanions, ref allyCharacters);
						sceneNotificationData = new MainHeroBattleVictoryDeathNotificationItem(victim, allyCharacters);
					}
					else
					{
						sceneNotificationData = new MainHeroBattleDeathNotificationItem(victim, _lastEnemyCulture);
					}
					break;
				case KillCharacterAction.KillCharacterActionDetail.Executed:
				case KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent:
				{
					TextObject to = new TextObject("{=uYjEknNX}{VICTIM.NAME}'s execution by {EXECUTER.NAME}");
					to.SetCharacterProperties("VICTIM", victim.CharacterObject);
					to.SetCharacterProperties("EXECUTER", killer.CharacterObject);
					sceneNotificationData = HeroExecutionSceneNotificationData.CreateForInformingPlayer(killer, victim, SceneNotificationData.RelevantContextType.Map);
					break;
				}
				}
			}
		}
		if (sceneNotificationData != null)
		{
			MBInformationManager.ShowSceneNotification(sceneNotificationData);
		}
	}

	private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
	{
		KingSelectionKingdomDecision.KingSelectionDecisionOutcome kingSelectionDecisionOutcome;
		if ((kingSelectionDecisionOutcome = chosenOutcome as KingSelectionKingdomDecision.KingSelectionDecisionOutcome) != null && isPlayerInvolved && kingSelectionDecisionOutcome.King == Hero.MainHero)
		{
			MBInformationManager.ShowSceneNotification(new BecomeKingSceneNotificationItem(kingSelectionDecisionOutcome.King));
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (showNotification)
		{
			SceneNotificationData sceneNotificationData = null;
			if (clan == Clan.PlayerClan && detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom)
			{
				sceneNotificationData = new JoinKingdomSceneNotificationItem(clan, newKingdom);
			}
			else if (Clan.PlayerClan.Kingdom == newKingdom && detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection)
			{
				sceneNotificationData = new JoinKingdomSceneNotificationItem(clan, newKingdom);
			}
			if (sceneNotificationData != null)
			{
				MBInformationManager.ShowSceneNotification(sceneNotificationData);
			}
		}
	}

	private void OnMercenaryServiceEnded(Clan clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails detail)
	{
		if (clan.Kingdom != null && clan.Kingdom == Clan.PlayerClan.Kingdom && detail == EndMercenaryServiceAction.EndMercenaryServiceActionDetails.ApplyByBecomingVassal)
		{
			MBInformationManager.ShowSceneNotification(new JoinKingdomSceneNotificationItem(clan, clan.Kingdom));
		}
	}

	private void OnKingdomDestroyed(Kingdom kingdom)
	{
		if (!kingdom.IsRebelClan)
		{
			if (kingdom.Leader == Hero.MainHero)
			{
				MBInformationManager.ShowSceneNotification(Campaign.Current.Models.CutsceneSelectionModel.GetKingdomDestroyedSceneNotification(kingdom));
			}
			else
			{
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new KingdomDestroyedMapNotification(kingdom, CampaignTime.Now));
			}
		}
	}

	private void OnKingdomCreated(Kingdom kingdom)
	{
		if (Hero.MainHero.Clan.Kingdom == kingdom)
		{
			MBInformationManager.ShowSceneNotification(new KingdomCreatedSceneNotificationItem(kingdom));
		}
	}

	private void OnHeroComesOfAge(Hero hero)
	{
		if (hero.Mother?.Clan == Clan.PlayerClan || hero.Father?.Clan == Clan.PlayerClan)
		{
			Hero mentorHeroForComeOfAge = GetMentorHeroForComeOfAge(hero);
			TextObject textObject = new TextObject("{=t4KwQOB7}{HERO.NAME} is now of age.");
			textObject.SetCharacterProperties("HERO", hero.CharacterObject);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new HeirComeOfAgeMapNotification(hero, mentorHeroForComeOfAge, textObject, CampaignTime.Now));
		}
	}

	private void OnMapEventEnd(MapEvent mapEvent)
	{
		if (mapEvent.IsPlayerMapEvent && mapEvent.HasWinner)
		{
			_heroWonLastMapEVent = mapEvent.WinningSide != BattleSideEnum.None && mapEvent.WinningSide == mapEvent.PlayerSide;
			_lastEnemyCulture = ((mapEvent.PlayerSide == BattleSideEnum.Attacker) ? mapEvent.DefenderSide.MapFaction.Culture : mapEvent.AttackerSide.MapFaction.Culture);
		}
	}

	private static void OnHeroesMarried(Hero firstHero, Hero secondHero, bool showNotification)
	{
		if (firstHero == Hero.MainHero || secondHero == Hero.MainHero)
		{
			Hero obj = (firstHero.IsFemale ? secondHero : firstHero);
			MBInformationManager.ShowSceneNotification(new MarriageSceneNotificationItem(obj, obj.Spouse, CampaignTime.Now));
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private static void FillAllyCharacters(bool noCompanions, ref List<CharacterObject> allyCharacters)
	{
		if (noCompanions)
		{
			allyCharacters.Add(Hero.MainHero.MapFaction.Culture.RangedEliteMilitiaTroop);
			return;
		}
		List<CharacterObject> source = (from t in MobileParty.MainParty.MemberRoster.GetTroopRoster()
			where t.Character != CharacterObject.PlayerCharacter && t.Character.IsHero
			select t.Character).ToList();
		allyCharacters.AddRange(source.Take(3));
		int count = allyCharacters.Count;
		for (int num = 0; num < 3 - count; num++)
		{
			allyCharacters.Add(Hero.AllAliveHeroes.GetRandomElement().CharacterObject);
		}
	}

	private Hero GetMentorHeroForComeOfAge(Hero hero)
	{
		Hero result = Hero.MainHero;
		if (hero.IsFemale)
		{
			if (hero.Mother != null && hero.Mother.IsAlive)
			{
				result = hero.Mother;
			}
			else if (hero.Father != null && hero.Father.IsAlive)
			{
				result = hero.Father;
			}
		}
		else if (hero.Father != null && hero.Father.IsAlive)
		{
			result = hero.Father;
		}
		else if (hero.Mother != null && hero.Mother.IsAlive)
		{
			result = hero.Mother;
		}
		if (hero.Mother == Hero.MainHero || hero.Father == Hero.MainHero)
		{
			result = Hero.MainHero;
		}
		return result;
	}
}
