using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CampaignWarManagerBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
	}

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		if (raidEvent.AttackerSide.LeaderParty.MapFaction == null || raidEvent.AttackerSide.LeaderParty.MapFaction.IsBanditFaction || raidEvent.DefenderSide.LeaderParty.MapFaction == null || raidEvent.DefenderSide.LeaderParty.MapFaction.IsBanditFaction)
		{
			return;
		}
		IFaction mapFaction = raidEvent.AttackerSide.MapFaction;
		IFaction mapFaction2 = raidEvent.DefenderSide.MapFaction;
		if (mapFaction.MapFaction == mapFaction2.MapFaction)
		{
			return;
		}
		StanceLink stanceWith = mapFaction.GetStanceWith(mapFaction2);
		if (raidEvent.MapEventSettlement != null && raidEvent.BattleState == BattleState.AttackerVictory && raidEvent.MapEventSettlement.IsVillage && raidEvent.MapEventSettlement.Village.VillageState == Village.VillageStates.Looted)
		{
			if (mapFaction == stanceWith.Faction1)
			{
				stanceWith.SuccessfulRaids1++;
			}
			else
			{
				stanceWith.SuccessfulRaids2++;
			}
		}
	}

	private void MapEventEnded(MapEvent mapEvent)
	{
		if (mapEvent.AttackerSide.LeaderParty.MapFaction == null || mapEvent.AttackerSide.LeaderParty.MapFaction.IsBanditFaction || mapEvent.DefenderSide.LeaderParty.MapFaction == null || mapEvent.DefenderSide.LeaderParty.MapFaction.IsBanditFaction)
		{
			return;
		}
		IFaction mapFaction = mapEvent.AttackerSide.MapFaction;
		IFaction mapFaction2 = mapEvent.DefenderSide.MapFaction;
		if (mapFaction.MapFaction == mapFaction2.MapFaction)
		{
			return;
		}
		StanceLink stanceWith = mapFaction.GetStanceWith(mapFaction2);
		stanceWith.TroopCasualties1 += ((stanceWith.Faction1 == mapFaction) ? mapEvent.AttackerSide.TroopCasualties : mapEvent.DefenderSide.TroopCasualties);
		stanceWith.TroopCasualties2 += ((stanceWith.Faction2 == mapFaction) ? mapEvent.AttackerSide.TroopCasualties : mapEvent.DefenderSide.TroopCasualties);
		stanceWith.ShipCasualties1 += ((stanceWith.Faction1 == mapFaction) ? mapEvent.AttackerSide.ShipCasualties : mapEvent.DefenderSide.ShipCasualties);
		stanceWith.ShipCasualties2 += ((stanceWith.Faction2 == mapFaction) ? mapEvent.AttackerSide.ShipCasualties : mapEvent.DefenderSide.ShipCasualties);
		if (mapEvent.MapEventSettlement == null || mapEvent.BattleState != BattleState.AttackerVictory || !mapEvent.MapEventSettlement.IsFortification || mapEvent.EventType != MapEvent.BattleTypes.Siege)
		{
			return;
		}
		if (mapFaction == stanceWith.Faction1)
		{
			stanceWith.SuccessfulSieges1++;
			if (mapEvent.MapEventSettlement.IsTown)
			{
				stanceWith.SuccessfulTownSieges1++;
			}
		}
		else
		{
			stanceWith.SuccessfulSieges2++;
			if (mapEvent.MapEventSettlement.IsTown)
			{
				stanceWith.SuccessfulTownSieges2++;
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
