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

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class DefaultLogsCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.AlleyOwnerChanged.AddNonSerializedListener(this, OnAlleyOwnerChanged);
		CampaignEvents.ArmyGathered.AddNonSerializedListener(this, OnArmyGathered);
		CampaignEvents.BattleStarted.AddNonSerializedListener(this, OnBattleStarted);
		CampaignEvents.CharacterBecameFugitiveEvent.AddNonSerializedListener(this, OnCharacterBecameFugitive);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, ClanChangedKingdom);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnPrisonerTaken);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnHeroesMarried);
		CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
		CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
		CampaignEvents.OnTradeAgreementSignedEvent.AddNonSerializedListener(this, OnTradeAgreementSigned);
		CampaignEvents.RebellionFinished.AddNonSerializedListener(this, OnRebellionFinished);
		CampaignEvents.KingdomDecisionAdded.AddNonSerializedListener(this, OnKingdomDecisionAdded);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
		CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
		CampaignEvents.PlayerTraitChangedEvent.AddNonSerializedListener(this, OnPlayerTraitChanged);
		CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChanged);
		CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener(this, OnSiegeAftermathApplied);
		CampaignEvents.OnAllianceStartedEvent.AddNonSerializedListener(this, OnAllianceStartedEvent);
		CampaignEvents.OnAllianceEndedEvent.AddNonSerializedListener(this, OnAllianceEndedEvent);
		CampaignEvents.OnCallToWarAgreementStartedEvent.AddNonSerializedListener(this, OnCallToWarAgreementStarted);
		CampaignEvents.OnCallToWarAgreementEndedEvent.AddNonSerializedListener(this, OnCallToWarAgreementEnded);
	}

	private void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
	{
		LogEntry.AddLogEntry(new SiegeAftermathLogEntry(attackerParty, partyContributions.Keys, settlement, aftermathType));
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMobileParty, bool isMainPartyChanged)
	{
		LogEntry.AddLogEntry(new PlayerCharacterChangedLogEntry(oldPlayer, newPlayer));
	}

	private void OnPrisonerTaken(PartyBase party, Hero hero)
	{
		LogEntry.AddLogEntry(new TakePrisonerLogEntry(party, hero));
	}

	private void OnHeroPrisonerReleased(Hero hero, PartyBase party, IFaction captuererFaction, EndCaptivityDetail detail, bool showNotification)
	{
		if (showNotification)
		{
			LogEntry.AddLogEntry(new EndCaptivityLogEntry(hero, captuererFaction, detail));
		}
	}

	private void OnCommonAreaFightOccured(MobileParty attackerParty, MobileParty defenderParty, Hero attackerHero, Settlement settlement)
	{
		LogEntry.AddLogEntry(new CommonAreaFightLogEntry(attackerParty, defenderParty, attackerHero, settlement));
	}

	private void ClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotifications)
	{
		if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary)
		{
			LogEntry.AddLogEntry(new MercenaryClanChangedKingdomLogEntry(clan, oldKingdom, newKingdom));
		}
	}

	private void OnCharacterBecameFugitive(Hero hero, bool showNotification)
	{
		LogEntry.AddLogEntry(new CharacterBecameFugitiveLogEntry(hero));
	}

	private void OnBattleStarted(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification)
	{
		if (showNotification)
		{
			LogEntry.AddLogEntry(new BattleStartedLogEntry(attackerParty, defenderParty, subject));
		}
	}

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

	private void OnArmyGathered(Army army, IMapPoint gatheringPoint)
	{
		LogEntry.AddLogEntry(new GatherArmyLogEntry(army, gatheringPoint));
	}

	private void OnArmyCreated(Army army)
	{
		ArmyCreationLogEntry armyCreationLogEntry = new ArmyCreationLogEntry(army);
		LogEntry.AddLogEntry(armyCreationLogEntry);
		if (army.LeaderParty.MapFaction == MobileParty.MainParty.MapFaction && army.LeaderParty != MobileParty.MainParty)
		{
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new ArmyCreationMapNotification(army, armyCreationLogEntry.GetEncyclopediaText()));
		}
	}

	private void OnTradeAgreementSigned(Kingdom kingdom1, Kingdom kingdom2)
	{
		LogEntry.AddLogEntry(new TradeAgreementLogEntry(kingdom1, kingdom2));
	}

	private void OnRebellionFinished(Settlement settlement, Clan oldOwnerClan)
	{
		RebellionStartedLogEntry rebellionStartedLogEntry = new RebellionStartedLogEntry(settlement, oldOwnerClan);
		LogEntry.AddLogEntry(rebellionStartedLogEntry);
		if (oldOwnerClan == Clan.PlayerClan)
		{
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new SettlementRebellionMapNotification(settlement, rebellionStartedLogEntry.GetNotificationText()));
		}
	}

	private void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
	{
		LogEntry.AddLogEntry(new KingdomDecisionAddedLogEntry(decision, isPlayerInvolved));
		if (decision.NotifyPlayer && isPlayerInvolved && !decision.IsEnforced)
		{
			TextObject descriptionText = (decision.DetermineChooser().Leader.IsHumanPlayerCharacter ? decision.GetChooseTitle() : decision.GetSupportTitle());
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new KingdomDecisionMapNotification(decision.Kingdom, decision, descriptionText));
		}
	}

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

	private void OnAlleyOwnerChanged(Alley alley, Hero newOwner, Hero oldOwner)
	{
		if (Campaign.Current.GameStarted)
		{
			LogEntry.AddLogEntry(new ChangeAlleyOwnerLogEntry(alley, newOwner, oldOwner));
		}
	}

	private void OnHeroesMarried(Hero marriedHero, Hero marriedTo, bool showNotification)
	{
		CharacterMarriedLogEntry characterMarriedLogEntry = new CharacterMarriedLogEntry(marriedHero, marriedTo);
		LogEntry.AddLogEntry(characterMarriedLogEntry);
		if (marriedHero.Clan == Clan.PlayerClan || marriedTo.Clan == Clan.PlayerClan)
		{
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new MarriageMapNotification(marriedHero, marriedTo, characterMarriedLogEntry.GetEncyclopediaText(), CampaignTime.Now));
		}
	}

	private void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		BesiegeSettlementLogEntry besiegeSettlementLogEntry = new BesiegeSettlementLogEntry(siegeEvent.BesiegerCamp.LeaderParty, siegeEvent.BesiegedSettlement);
		LogEntry.AddLogEntry(besiegeSettlementLogEntry);
		if (siegeEvent.BesiegedSettlement.OwnerClan == Clan.PlayerClan)
		{
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new SettlementUnderSiegeMapNotification(siegeEvent, besiegeSettlementLogEntry.GetEncyclopediaText()));
		}
	}

	private void OnTournamentFinished(CharacterObject character, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		if (character.IsHero)
		{
			LogEntry.AddLogEntry(new TournamentWonLogEntry(character.HeroObject, town, participants));
		}
	}

	private void OnPlayerTraitChanged(TraitObject trait, int previousLevel)
	{
		int traitLevel = Hero.MainHero.GetTraitLevel(trait);
		TextObject traitChangedText = GetTraitChangedText(trait, traitLevel, previousLevel);
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new TraitChangedMapNotification(trait, traitLevel != 0, previousLevel, traitChangedText));
	}

	private void OnCallToWarAgreementEnded(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		LogEntry.AddLogEntry(new EndCallToWarAgreementLogEntry(callingKingdom, calledKingdom, kingdomToCallToWarAgainst));
	}

	private void OnCallToWarAgreementStarted(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		LogEntry.AddLogEntry(new StartCallToWarAgreementLogEntry(callingKingdom, calledKingdom, kingdomToCallToWarAgainst));
	}

	private void OnAllianceEndedEvent(Kingdom kingdom1, Kingdom kingdom2)
	{
		LogEntry.AddLogEntry(new EndAllianceLogEntry(kingdom1, kingdom2));
	}

	private void OnAllianceStartedEvent(Kingdom kingdom1, Kingdom kingdom2)
	{
		LogEntry.AddLogEntry(new StartAllianceLogEntry(kingdom1, kingdom2));
	}

	private static TextObject GetTraitChangedText(TraitObject traitObject, int level, int previousLevel)
	{
		TextObject variable;
		TextObject textObject;
		if (level != 0)
		{
			variable = GameTexts.FindText("str_trait_name_" + traitObject.StringId.ToLower(), (level + MathF.Abs(traitObject.MinValue)).ToString());
			textObject = GameTexts.FindText("str_trait_gained_text");
		}
		else
		{
			variable = GameTexts.FindText("str_trait_name_" + traitObject.StringId.ToLower(), (previousLevel + MathF.Abs(traitObject.MinValue)).ToString());
			textObject = GameTexts.FindText("str_trait_lost_text");
		}
		textObject.SetCharacterProperties("HERO", Hero.MainHero.CharacterObject);
		textObject.SetTextVariable("TRAIT_NAME", variable);
		return textObject;
	}
}
