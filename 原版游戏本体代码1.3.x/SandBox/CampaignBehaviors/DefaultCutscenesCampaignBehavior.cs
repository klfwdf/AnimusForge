using System;
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

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000D2 RID: 210
	public class DefaultCutscenesCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600098D RID: 2445 RVA: 0x00047CE8 File Offset: 0x00045EE8
		public override void RegisterEvents()
		{
			CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, new Action<Hero, Hero, bool>(DefaultCutscenesCampaignBehavior.OnHeroesMarried));
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnd));
			CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeroComesOfAge));
			CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomCreated));
			CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomDestroyed));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, new Action<KingdomDecision, DecisionOutcome, bool>(this.OnKingdomDecisionConcluded));
			CampaignEvents.OnBeforeMainCharacterDiedEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnBeforeMainCharacterDied));
			CampaignEvents.OnMercenaryServiceEndedEvent.AddNonSerializedListener(this, new Action<Clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails>(this.OnMercenaryServiceEnded));
		}

		// Token: 0x0600098E RID: 2446 RVA: 0x00047DC4 File Offset: 0x00045FC4
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
				else if (detail == KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge)
				{
					sceneNotificationData = new DeathOldAgeSceneNotificationItem(victim);
				}
				else if (detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
				{
					if (this._heroWonLastMapEVent)
					{
						bool noCompanions = !victim.CompanionsInParty.Any<Hero>();
						List<CharacterObject> encounterAllyCharacters = new List<CharacterObject>();
						DefaultCutscenesCampaignBehavior.FillAllyCharacters(noCompanions, ref encounterAllyCharacters);
						sceneNotificationData = new MainHeroBattleVictoryDeathNotificationItem(victim, encounterAllyCharacters);
					}
					else
					{
						sceneNotificationData = new MainHeroBattleDeathNotificationItem(victim, this._lastEnemyCulture);
					}
				}
				else if (detail == KillCharacterAction.KillCharacterActionDetail.Executed || detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
				{
					TextObject to = new TextObject("{=uYjEknNX}{VICTIM.NAME}'s execution by {EXECUTER.NAME}", null);
					to.SetCharacterProperties("VICTIM", victim.CharacterObject, false);
					to.SetCharacterProperties("EXECUTER", killer.CharacterObject, false);
					sceneNotificationData = HeroExecutionSceneNotificationData.CreateForInformingPlayer(killer, victim, SceneNotificationData.RelevantContextType.Map);
				}
			}
			if (sceneNotificationData != null)
			{
				MBInformationManager.ShowSceneNotification(sceneNotificationData);
			}
		}

		// Token: 0x0600098F RID: 2447 RVA: 0x00047E9C File Offset: 0x0004609C
		private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
		{
			KingSelectionKingdomDecision.KingSelectionDecisionOutcome kingSelectionDecisionOutcome;
			if ((kingSelectionDecisionOutcome = chosenOutcome as KingSelectionKingdomDecision.KingSelectionDecisionOutcome) != null && isPlayerInvolved && kingSelectionDecisionOutcome.King == Hero.MainHero)
			{
				MBInformationManager.ShowSceneNotification(new BecomeKingSceneNotificationItem(kingSelectionDecisionOutcome.King));
			}
		}

		// Token: 0x06000990 RID: 2448 RVA: 0x00047ED8 File Offset: 0x000460D8
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

		// Token: 0x06000991 RID: 2449 RVA: 0x00047F25 File Offset: 0x00046125
		private void OnMercenaryServiceEnded(Clan clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails detail)
		{
			if (clan.Kingdom != null && clan.Kingdom == Clan.PlayerClan.Kingdom && detail == EndMercenaryServiceAction.EndMercenaryServiceActionDetails.ApplyByBecomingVassal)
			{
				MBInformationManager.ShowSceneNotification(new JoinKingdomSceneNotificationItem(clan, clan.Kingdom));
			}
		}

		// Token: 0x06000992 RID: 2450 RVA: 0x00047F58 File Offset: 0x00046158
		private void OnKingdomDestroyed(Kingdom kingdom)
		{
			if (!kingdom.IsRebelClan)
			{
				if (kingdom.Leader == Hero.MainHero)
				{
					MBInformationManager.ShowSceneNotification(Campaign.Current.Models.CutsceneSelectionModel.GetKingdomDestroyedSceneNotification(kingdom));
					return;
				}
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new KingdomDestroyedMapNotification(kingdom, CampaignTime.Now));
			}
		}

		// Token: 0x06000993 RID: 2451 RVA: 0x00047FAF File Offset: 0x000461AF
		private void OnKingdomCreated(Kingdom kingdom)
		{
			if (Hero.MainHero.Clan.Kingdom == kingdom)
			{
				MBInformationManager.ShowSceneNotification(new KingdomCreatedSceneNotificationItem(kingdom));
			}
		}

		// Token: 0x06000994 RID: 2452 RVA: 0x00047FD0 File Offset: 0x000461D0
		private void OnHeroComesOfAge(Hero hero)
		{
			Hero mother = hero.Mother;
			if (((mother != null) ? mother.Clan : null) != Clan.PlayerClan)
			{
				Hero father = hero.Father;
				if (((father != null) ? father.Clan : null) != Clan.PlayerClan)
				{
					return;
				}
			}
			Hero mentorHeroForComeOfAge = this.GetMentorHeroForComeOfAge(hero);
			TextObject textObject = new TextObject("{=t4KwQOB7}{HERO.NAME} is now of age.", null);
			textObject.SetCharacterProperties("HERO", hero.CharacterObject, false);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new HeirComeOfAgeMapNotification(hero, mentorHeroForComeOfAge, textObject, CampaignTime.Now));
		}

		// Token: 0x06000995 RID: 2453 RVA: 0x00048054 File Offset: 0x00046254
		private void OnMapEventEnd(MapEvent mapEvent)
		{
			if (mapEvent.IsPlayerMapEvent && mapEvent.HasWinner)
			{
				this._heroWonLastMapEVent = mapEvent.WinningSide != BattleSideEnum.None && mapEvent.WinningSide == mapEvent.PlayerSide;
				this._lastEnemyCulture = ((mapEvent.PlayerSide == BattleSideEnum.Attacker) ? mapEvent.DefenderSide.MapFaction.Culture : mapEvent.AttackerSide.MapFaction.Culture);
			}
		}

		// Token: 0x06000996 RID: 2454 RVA: 0x000480C2 File Offset: 0x000462C2
		private static void OnHeroesMarried(Hero firstHero, Hero secondHero, bool showNotification)
		{
			if (firstHero == Hero.MainHero || secondHero == Hero.MainHero)
			{
				Hero hero = (firstHero.IsFemale ? secondHero : firstHero);
				MBInformationManager.ShowSceneNotification(new MarriageSceneNotificationItem(hero, hero.Spouse, CampaignTime.Now, SceneNotificationData.RelevantContextType.Any));
			}
		}

		// Token: 0x06000997 RID: 2455 RVA: 0x000480F6 File Offset: 0x000462F6
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06000998 RID: 2456 RVA: 0x000480F8 File Offset: 0x000462F8
		private static void FillAllyCharacters(bool noCompanions, ref List<CharacterObject> allyCharacters)
		{
			if (noCompanions)
			{
				allyCharacters.Add(Hero.MainHero.MapFaction.Culture.RangedEliteMilitiaTroop);
				return;
			}
			List<CharacterObject> source = (from c in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where c.Character != CharacterObject.PlayerCharacter && c.Character.IsHero
				select c into t
				select t.Character).ToList<CharacterObject>();
			allyCharacters.AddRange(source.Take(3));
			int count = allyCharacters.Count;
			for (int i = 0; i < 3 - count; i++)
			{
				allyCharacters.Add(Hero.AllAliveHeroes.GetRandomElement<Hero>().CharacterObject);
			}
		}

		// Token: 0x06000999 RID: 2457 RVA: 0x000481BC File Offset: 0x000463BC
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

		// Token: 0x0400047B RID: 1147
		private bool _heroWonLastMapEVent;

		// Token: 0x0400047C RID: 1148
		private CultureObject _lastEnemyCulture;
	}
}
