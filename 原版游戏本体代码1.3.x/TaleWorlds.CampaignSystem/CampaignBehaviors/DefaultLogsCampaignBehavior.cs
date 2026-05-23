using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003E3 RID: 995
	public class DefaultLogsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003D20 RID: 15648 RVA: 0x0010978C File Offset: 0x0010798C
		public override void RegisterEvents()
		{
			CampaignEvents.AlleyOwnerChanged.AddNonSerializedListener(this, new Action<Alley, Hero, Hero>(this.OnAlleyOwnerChanged));
			CampaignEvents.ArmyGathered.AddNonSerializedListener(this, new Action<Army, IMapPoint>(this.OnArmyGathered));
			CampaignEvents.BattleStarted.AddNonSerializedListener(this, new Action<PartyBase, PartyBase, object, bool>(this.OnBattleStarted));
			CampaignEvents.CharacterBecameFugitiveEvent.AddNonSerializedListener(this, new Action<Hero, bool>(this.OnCharacterBecameFugitive));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.ClanChangedKingdom));
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.OnPrisonerTaken));
			CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, new Action<Hero, PartyBase, IFaction, EndCaptivityDetail, bool>(this.OnHeroPrisonerReleased));
			CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, new Action<Hero, Hero, bool>(this.OnHeroesMarried));
			CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, new Action<Army, Army.ArmyDispersionReason, bool>(this.OnArmyDispersed));
			CampaignEvents.ArmyCreated.AddNonSerializedListener(this, new Action<Army>(this.OnArmyCreated));
			CampaignEvents.OnTradeAgreementSignedEvent.AddNonSerializedListener(this, new Action<Kingdom, Kingdom>(this.OnTradeAgreementSigned));
			CampaignEvents.RebellionFinished.AddNonSerializedListener(this, new Action<Settlement, Clan>(this.OnRebellionFinished));
			CampaignEvents.KingdomDecisionAdded.AddNonSerializedListener(this, new Action<KingdomDecision, bool>(this.OnKingdomDecisionAdded));
			CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, new Action<KingdomDecision, DecisionOutcome, bool>(this.OnKingdomDecisionConcluded));
			CampaignEvents.TournamentFinished.AddNonSerializedListener(this, new Action<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject>(this.OnTournamentFinished));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventStarted));
			CampaignEvents.PlayerTraitChangedEvent.AddNonSerializedListener(this, new Action<TraitObject, int>(this.OnPlayerTraitChanged));
			CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, new Action<Hero, Hero, MobileParty, bool>(this.OnPlayerCharacterChanged));
			CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement, SiegeAftermathAction.SiegeAftermath, Clan, Dictionary<MobileParty, float>>(this.OnSiegeAftermathApplied));
			CampaignEvents.OnAllianceStartedEvent.AddNonSerializedListener(this, new Action<Kingdom, Kingdom>(this.OnAllianceStartedEvent));
			CampaignEvents.OnAllianceEndedEvent.AddNonSerializedListener(this, new Action<Kingdom, Kingdom>(this.OnAllianceEndedEvent));
			CampaignEvents.OnCallToWarAgreementStartedEvent.AddNonSerializedListener(this, new Action<Kingdom, Kingdom, Kingdom>(this.OnCallToWarAgreementStarted));
			CampaignEvents.OnCallToWarAgreementEndedEvent.AddNonSerializedListener(this, new Action<Kingdom, Kingdom, Kingdom>(this.OnCallToWarAgreementEnded));
		}

		// Token: 0x06003D21 RID: 15649 RVA: 0x001099AA File Offset: 0x00107BAA
		private void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
		{
			LogEntry.AddLogEntry(new SiegeAftermathLogEntry(attackerParty, partyContributions.Keys, settlement, aftermathType));
		}

		// Token: 0x06003D22 RID: 15650 RVA: 0x001099C0 File Offset: 0x00107BC0
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003D23 RID: 15651 RVA: 0x001099C2 File Offset: 0x00107BC2
		private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMobileParty, bool isMainPartyChanged)
		{
			LogEntry.AddLogEntry(new PlayerCharacterChangedLogEntry(oldPlayer, newPlayer));
		}

		// Token: 0x06003D24 RID: 15652 RVA: 0x001099D0 File Offset: 0x00107BD0
		private void OnPrisonerTaken(PartyBase party, Hero hero)
		{
			LogEntry.AddLogEntry(new TakePrisonerLogEntry(party, hero));
		}

		// Token: 0x06003D25 RID: 15653 RVA: 0x001099DE File Offset: 0x00107BDE
		private void OnHeroPrisonerReleased(Hero hero, PartyBase party, IFaction captuererFaction, EndCaptivityDetail detail, bool showNotification)
		{
			if (showNotification)
			{
				LogEntry.AddLogEntry(new EndCaptivityLogEntry(hero, captuererFaction, detail));
			}
		}

		// Token: 0x06003D26 RID: 15654 RVA: 0x001099F2 File Offset: 0x00107BF2
		private void OnCommonAreaFightOccured(MobileParty attackerParty, MobileParty defenderParty, Hero attackerHero, Settlement settlement)
		{
			LogEntry.AddLogEntry(new CommonAreaFightLogEntry(attackerParty, defenderParty, attackerHero, settlement));
		}

		// Token: 0x06003D27 RID: 15655 RVA: 0x00109A03 File Offset: 0x00107C03
		private void ClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotifications)
		{
			if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary)
			{
				LogEntry.AddLogEntry(new MercenaryClanChangedKingdomLogEntry(clan, oldKingdom, newKingdom));
			}
		}

		// Token: 0x06003D28 RID: 15656 RVA: 0x00109A1B File Offset: 0x00107C1B
		private void OnCharacterBecameFugitive(Hero hero, bool showNotification)
		{
			LogEntry.AddLogEntry(new CharacterBecameFugitiveLogEntry(hero));
		}

		// Token: 0x06003D29 RID: 15657 RVA: 0x00109A28 File Offset: 0x00107C28
		private void OnBattleStarted(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification)
		{
			if (showNotification)
			{
				LogEntry.AddLogEntry(new BattleStartedLogEntry(attackerParty, defenderParty, subject));
			}
		}

		// Token: 0x06003D2A RID: 15658 RVA: 0x00109A3C File Offset: 0x00107C3C
		public void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
		{
			if (isPlayersArmy)
			{
				ArmyDispersionLogEntry armyDispersionLogEntry = new ArmyDispersionLogEntry(army, reason);
				LogEntry.AddLogEntry(armyDispersionLogEntry);
				if (army.LeaderParty.MapFaction == Hero.MainHero.MapFaction && army.Parties.IndexOf(MobileParty.MainParty) < 0)
				{
					Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new ArmyDispersionMapNotification(army, reason, armyDispersionLogEntry.GetEncyclopediaText()));
				}
			}
		}

		// Token: 0x06003D2B RID: 15659 RVA: 0x00109AA0 File Offset: 0x00107CA0
		private void OnArmyGathered(Army army, IMapPoint gatheringPoint)
		{
			LogEntry.AddLogEntry(new GatherArmyLogEntry(army, gatheringPoint));
		}

		// Token: 0x06003D2C RID: 15660 RVA: 0x00109AB0 File Offset: 0x00107CB0
		private void OnArmyCreated(Army army)
		{
			ArmyCreationLogEntry armyCreationLogEntry = new ArmyCreationLogEntry(army);
			LogEntry.AddLogEntry(armyCreationLogEntry);
			if (army.LeaderParty.MapFaction == MobileParty.MainParty.MapFaction && army.LeaderParty != MobileParty.MainParty)
			{
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new ArmyCreationMapNotification(army, armyCreationLogEntry.GetEncyclopediaText()));
			}
		}

		// Token: 0x06003D2D RID: 15661 RVA: 0x00109B09 File Offset: 0x00107D09
		private void OnTradeAgreementSigned(Kingdom kingdom1, Kingdom kingdom2)
		{
			LogEntry.AddLogEntry(new TradeAgreementLogEntry(kingdom1, kingdom2));
		}

		// Token: 0x06003D2E RID: 15662 RVA: 0x00109B18 File Offset: 0x00107D18
		private void OnRebellionFinished(Settlement settlement, Clan oldOwnerClan)
		{
			RebellionStartedLogEntry rebellionStartedLogEntry = new RebellionStartedLogEntry(settlement, oldOwnerClan);
			LogEntry.AddLogEntry(rebellionStartedLogEntry);
			if (oldOwnerClan == Clan.PlayerClan)
			{
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new SettlementRebellionMapNotification(settlement, rebellionStartedLogEntry.GetNotificationText()));
			}
		}

		// Token: 0x06003D2F RID: 15663 RVA: 0x00109B58 File Offset: 0x00107D58
		private void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
		{
			LogEntry.AddLogEntry(new KingdomDecisionAddedLogEntry(decision, isPlayerInvolved));
			if (decision.NotifyPlayer && isPlayerInvolved && !decision.IsEnforced)
			{
				TextObject descriptionText = (decision.DetermineChooser().Leader.IsHumanPlayerCharacter ? decision.GetChooseTitle() : decision.GetSupportTitle());
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new KingdomDecisionMapNotification(decision.Kingdom, decision, descriptionText));
			}
		}

		// Token: 0x06003D30 RID: 15664 RVA: 0x00109BC0 File Offset: 0x00107DC0
		private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
		{
			KingdomDecisionConcludedLogEntry kingdomDecisionConcludedLogEntry = new KingdomDecisionConcludedLogEntry(decision, chosenOutcome, isPlayerInvolved);
			LogEntry.AddLogEntry(kingdomDecisionConcludedLogEntry);
			if (decision.Kingdom == Hero.MainHero.MapFaction && decision.NotifyPlayer && !decision.IsEnforced && !isPlayerInvolved)
			{
				MBInformationManager.AddQuickInformation(kingdomDecisionConcludedLogEntry.GetNotificationText(), 0, null, null, "event:/ui/notification/kingdom_decision");
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new KingdomDecisionMapNotification(decision.Kingdom, decision, kingdomDecisionConcludedLogEntry.GetNotificationText()));
			}
		}

		// Token: 0x06003D31 RID: 15665 RVA: 0x00109C35 File Offset: 0x00107E35
		private void OnAlleyOwnerChanged(Alley alley, Hero newOwner, Hero oldOwner)
		{
			LogEntry.AddLogEntry(new ChangeAlleyOwnerLogEntry(alley, newOwner, oldOwner));
		}

		// Token: 0x06003D32 RID: 15666 RVA: 0x00109C44 File Offset: 0x00107E44
		private void OnHeroesMarried(Hero marriedHero, Hero marriedTo, bool showNotification)
		{
			CharacterMarriedLogEntry characterMarriedLogEntry = new CharacterMarriedLogEntry(marriedHero, marriedTo);
			LogEntry.AddLogEntry(characterMarriedLogEntry);
			if (marriedHero.Clan == Clan.PlayerClan || marriedTo.Clan == Clan.PlayerClan)
			{
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new MarriageMapNotification(marriedHero, marriedTo, characterMarriedLogEntry.GetEncyclopediaText(), CampaignTime.Now));
			}
		}

		// Token: 0x06003D33 RID: 15667 RVA: 0x00109C9C File Offset: 0x00107E9C
		private void OnSiegeEventStarted(SiegeEvent siegeEvent)
		{
			BesiegeSettlementLogEntry besiegeSettlementLogEntry = new BesiegeSettlementLogEntry(siegeEvent.BesiegerCamp.LeaderParty, siegeEvent.BesiegedSettlement);
			LogEntry.AddLogEntry(besiegeSettlementLogEntry);
			if (siegeEvent.BesiegedSettlement.OwnerClan == Clan.PlayerClan)
			{
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new SettlementUnderSiegeMapNotification(siegeEvent, besiegeSettlementLogEntry.GetEncyclopediaText()));
			}
		}

		// Token: 0x06003D34 RID: 15668 RVA: 0x00109CF3 File Offset: 0x00107EF3
		private void OnTournamentFinished(CharacterObject character, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
		{
			if (character.IsHero)
			{
				LogEntry.AddLogEntry(new TournamentWonLogEntry(character.HeroObject, town, participants));
			}
		}

		// Token: 0x06003D35 RID: 15669 RVA: 0x00109D10 File Offset: 0x00107F10
		private void OnPlayerTraitChanged(TraitObject trait, int previousLevel)
		{
			int traitLevel = Hero.MainHero.GetTraitLevel(trait);
			TextObject traitChangedText = DefaultLogsCampaignBehavior.GetTraitChangedText(trait, traitLevel, previousLevel);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new TraitChangedMapNotification(trait, traitLevel != 0, previousLevel, traitChangedText));
		}

		// Token: 0x06003D36 RID: 15670 RVA: 0x00109D4D File Offset: 0x00107F4D
		private void OnCallToWarAgreementEnded(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			LogEntry.AddLogEntry(new EndCallToWarAgreementLogEntry(callingKingdom, calledKingdom, kingdomToCallToWarAgainst));
		}

		// Token: 0x06003D37 RID: 15671 RVA: 0x00109D5C File Offset: 0x00107F5C
		private void OnCallToWarAgreementStarted(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
			LogEntry.AddLogEntry(new StartCallToWarAgreementLogEntry(callingKingdom, calledKingdom, kingdomToCallToWarAgainst));
		}

		// Token: 0x06003D38 RID: 15672 RVA: 0x00109D6B File Offset: 0x00107F6B
		private void OnAllianceEndedEvent(Kingdom kingdom1, Kingdom kingdom2)
		{
			LogEntry.AddLogEntry(new EndAllianceLogEntry(kingdom1, kingdom2));
		}

		// Token: 0x06003D39 RID: 15673 RVA: 0x00109D79 File Offset: 0x00107F79
		private void OnAllianceStartedEvent(Kingdom kingdom1, Kingdom kingdom2)
		{
			LogEntry.AddLogEntry(new StartAllianceLogEntry(kingdom1, kingdom2));
		}

		// Token: 0x06003D3A RID: 15674 RVA: 0x00109D88 File Offset: 0x00107F88
		private static TextObject GetTraitChangedText(TraitObject traitObject, int level, int previousLevel)
		{
			TextObject variable;
			TextObject textObject;
			if (level != 0)
			{
				variable = GameTexts.FindText("str_trait_name_" + traitObject.StringId.ToLower(), (level + MathF.Abs(traitObject.MinValue)).ToString());
				textObject = GameTexts.FindText("str_trait_gained_text", null);
			}
			else
			{
				variable = GameTexts.FindText("str_trait_name_" + traitObject.StringId.ToLower(), (previousLevel + MathF.Abs(traitObject.MinValue)).ToString());
				textObject = GameTexts.FindText("str_trait_lost_text", null);
			}
			textObject.SetCharacterProperties("HERO", Hero.MainHero.CharacterObject, false);
			textObject.SetTextVariable("TRAIT_NAME", variable);
			return textObject;
		}
	}
}
