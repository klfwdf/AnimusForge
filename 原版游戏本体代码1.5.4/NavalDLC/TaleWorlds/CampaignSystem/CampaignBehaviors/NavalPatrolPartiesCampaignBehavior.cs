using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC;
using NavalDLC.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class NavalPatrolPartiesCampaignBehavior : CampaignBehaviorBase, INavalPatrolPartiesCampaignBehavior
{
	private Dictionary<Settlement, CampaignTime> _partyGenerationQueue = new Dictionary<Settlement, CampaignTime>();

	private Dictionary<Settlement, MobileParty> _patrolParties = new Dictionary<Settlement, MobileParty>();

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChangedEvent);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, SettlementEntered);
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
	{
		if (party.IsPatrolParty && party.PatrolPartyComponent.IsNaval)
		{
			_patrolParties.Remove(party.HomeSettlement);
		}
	}

	private void SettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party == null || !party.IsPatrolParty || !party.PatrolPartyComponent.IsNaval || settlement != party.HomeSettlement)
		{
			return;
		}
		for (int i = 0; i < party.Ships.Count; i++)
		{
			RepairShipAction.ApplyForFree(party.Ships[i]);
		}
		foreach (ShipTemplateStack shipHull2 in Campaign.Current.Models.SettlementPatrolModel.GetPartyTemplateForPatrolParty(settlement, naval: true).ShipHulls)
		{
			ShipHull shipHull = shipHull2.ShipHull;
			int num = party.Ships.Count((Ship x) => x.ShipHull == shipHull);
			if (num < shipHull2.MaxValue)
			{
				for (int num2 = 0; num2 < shipHull2.MaxValue - num; num2++)
				{
					Ship ship = new Ship(shipHull);
					ChangeShipOwnerAction.ApplyByTransferring(party.Party, ship);
				}
			}
		}
	}

	private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
	{
		if (mobileParty.IsPatrolParty && !mobileParty.IsDisbanding && mobileParty.PatrolPartyComponent.IsNaval && (mobileParty.CurrentSettlement?.SiegeEvent == null || !mobileParty.CurrentSettlement.SiegeEvent.IsBlockadeActive))
		{
			CalculateVisitHomeSettlementScoreDueToShipHealth(mobileParty, p);
		}
	}

	private void CalculateVisitHomeSettlementScoreDueToShipHealth(MobileParty mobileParty, PartyThinkParams p)
	{
		if (!CanVisitSettlement(mobileParty, mobileParty.HomeSettlement))
		{
			return;
		}
		float overallShipHealthRatio = GetOverallShipHealthRatio(mobileParty);
		if (overallShipHealthRatio < 0.95f)
		{
			float num = 1f / TaleWorlds.Library.MathF.Max(overallShipHealthRatio, 0.01f);
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, mobileParty.HomeSettlement, isTargetingPort: true, out var bestNavigationType, out var _, out var isFromPort);
			AIBehaviorData aiBehaviorData = new AIBehaviorData(mobileParty.HomeSettlement, AiBehavior.GoToSettlement, bestNavigationType, willGatherArmy: false, isFromPort, isTargetingPort: true);
			if (p.TryGetBehaviorScore(in aiBehaviorData, out var score))
			{
				p.SetBehaviorScore(in aiBehaviorData, num + score);
			}
			else
			{
				p.AddBehaviorScore((aiBehaviorData, num));
			}
		}
	}

	private bool CanVisitSettlement(MobileParty mobileParty, Settlement settlement)
	{
		if (settlement.SiegeEvent != null)
		{
			return !settlement.SiegeEvent.IsBlockadeActive;
		}
		return true;
	}

	private float GetOverallShipHealthRatio(MobileParty mobileParty)
	{
		float num = Campaign.Current.Models.SettlementPatrolModel.GetPartyTemplateForPatrolParty(mobileParty.HomeSettlement, naval: true).ShipHulls.Sum((ShipTemplateStack x) => x.ShipHull.MaxHitPoints * x.MaxValue);
		return mobileParty.Ships.Sum((Ship x) => TaleWorlds.Library.MathF.Min(x.HitPoints, (float)x.ShipHull.MaxHitPoints)) / num;
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party.IsPatrolParty && party.PatrolPartyComponent.IsNaval)
		{
			_ = party.HomeSettlement;
		}
	}

	private void DailyTickSettlement(Settlement settlement)
	{
		if (CanSettlementSpawnNewPartyCurrently(settlement, includeReason: false, out var _))
		{
			if (!_partyGenerationQueue.TryGetValue(settlement, out var value))
			{
				UpdateSettlementQueue(settlement, CampaignTime.Now + Campaign.Current.Models.SettlementPatrolModel.GetPatrolPartySpawnDuration(settlement, naval: true));
			}
			else if (value.IsPast)
			{
				SpawnPatrolParty(settlement);
			}
		}
		else
		{
			UpdateSettlementParties(settlement);
		}
	}

	private void OnNewGameCreated(CampaignGameStarter starter, int index)
	{
		if (index != 88)
		{
			return;
		}
		foreach (Town allFief in Town.AllFiefs)
		{
			if (CanSettlementSpawnNewPartyCurrently(allFief.Settlement, includeReason: false, out var _))
			{
				SpawnPatrolParty(allFief.Settlement);
			}
		}
	}

	private void OnSettlementOwnerChangedEvent(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (GetNavalPatrolParty(settlement) != null)
		{
			RemoveSettlementParties(settlement);
		}
	}

	private bool CanSettlementSpawnNewPartyCurrently(Settlement settlement, bool includeReason, out TextObject reason)
	{
		reason = null;
		if (!Campaign.Current.Models.SettlementPatrolModel.CanSettlementHavePatrolParties(settlement, naval: true))
		{
			PolicyObject coastalGuardEdict = NavalPolicies.CoastalGuardEdict;
			if (includeReason)
			{
				reason = new TextObject("{=ipat9DbO}No {POLICY_NAME}");
				reason.SetTextVariable("POLICY_NAME", coastalGuardEdict.Name);
			}
			return false;
		}
		if (settlement.InRebelliousState)
		{
			if (includeReason)
			{
				reason = new TextObject("{=UHDv0qer}Rebellious");
			}
			return false;
		}
		if (settlement.Town.IsUnderSiege || settlement.Party.MapEvent != null)
		{
			if (includeReason)
			{
				reason = new TextObject("{=BhiOmgst}Under Siege");
			}
			return false;
		}
		if (includeReason)
		{
			reason = TextObject.GetEmpty();
		}
		return GetNavalPatrolParty(settlement) == null;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_partyGenerationQueue", ref _partyGenerationQueue);
		dataStore.SyncData("_patrolParties", ref _patrolParties);
	}

	private void UpdateSettlementParties(Settlement settlement)
	{
		if (!Campaign.Current.Models.SettlementPatrolModel.CanSettlementHavePatrolParties(settlement, naval: true) && GetNavalPatrolParty(settlement) != null)
		{
			RemoveSettlementParties(settlement);
		}
	}

	private void RemoveSettlementParties(Settlement settlement)
	{
		_partyGenerationQueue.Remove(settlement);
		MobileParty navalPatrolParty = GetNavalPatrolParty(settlement);
		navalPatrolParty.MapEventSide = null;
		if (navalPatrolParty.IsActive)
		{
			DestroyPartyAction.Apply(null, navalPatrolParty);
		}
	}

	private void UpdateSettlementQueue(Settlement settlement, CampaignTime time)
	{
		_partyGenerationQueue[settlement] = time;
	}

	private void SpawnPatrolParty(Settlement settlement)
	{
		_partyGenerationQueue.Remove(settlement);
		PartyTemplateObject partyTemplateForPatrolParty = Campaign.Current.Models.SettlementPatrolModel.GetPartyTemplateForPatrolParty(settlement, naval: true);
		MobileParty value = PatrolPartyComponent.CreatePatrolParty("naval_patrol_party_1", settlement.PortPosition, 8f * Campaign.Current.EstimatedAverageBanditPartySpeed, settlement, partyTemplateForPatrolParty);
		_patrolParties[settlement] = value;
	}

	public TextObject GetSettlementPatrolStatus(Settlement settlement)
	{
		TextObject empty = TextObject.GetEmpty();
		MobileParty navalPatrolParty = GetNavalPatrolParty(settlement);
		TextObject reason;
		CampaignTime value;
		if (navalPatrolParty != null)
		{
			empty = new TextObject("{=sUb6FHIE}{REMAINING_TROOP_COUNT}/{TOTAL_TROOP_COUNT}");
			empty.SetTextVariable("REMAINING_TROOP_COUNT", navalPatrolParty.MemberRoster.TotalManCount);
			empty.SetTextVariable("TOTAL_TROOP_COUNT", navalPatrolParty.Party.PartySizeLimit);
		}
		else if (!CanSettlementSpawnNewPartyCurrently(settlement, includeReason: true, out reason))
		{
			empty = reason;
		}
		else if (_partyGenerationQueue.TryGetValue(settlement, out value))
		{
			int variable = ((value == CampaignTime.Zero) ? 1 : Math.Max((int)Math.Ceiling(value.RemainingDaysFromNow), 1));
			empty = new TextObject("{=LvwUsZ9p}Ready in {DAYS} {?DAYS > 1}days{?}day{\\?}");
			empty.SetTextVariable("DAYS", variable);
		}
		else
		{
			empty = new TextObject("{=trainingPatrolParties}Training");
		}
		return empty;
	}

	public MobileParty GetNavalPatrolParty(Settlement settlement)
	{
		if (_patrolParties.TryGetValue(settlement, out var value))
		{
			return value;
		}
		return null;
	}
}
