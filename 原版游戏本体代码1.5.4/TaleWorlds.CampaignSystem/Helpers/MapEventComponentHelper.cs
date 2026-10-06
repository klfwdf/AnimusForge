using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Helpers;

public static class MapEventComponentHelper
{
	public static void AddInsideSettlementParties(MapEvent mapEvent)
	{
		List<PartyBase> list = new List<PartyBase>();
		foreach (PartyBase item in mapEvent.MapEventSettlement.GetInvolvedPartiesForEventType(mapEvent.EventType))
		{
			if (item != PartyBase.MainParty && item.MobileParty?.AttachedTo != MobileParty.MainParty)
			{
				list.Add(item);
			}
		}
		foreach (PartyBase item2 in list)
		{
			if (mapEvent.CanPartyJoinBattle(item2, BattleSideEnum.Defender))
			{
				item2.MapEventSide = mapEvent.DefenderSide;
			}
			else if (mapEvent.CanPartyJoinBattle(item2, BattleSideEnum.Attacker))
			{
				item2.MapEventSide = mapEvent.AttackerSide;
			}
			else if (item2.MobileParty != null && !item2.MobileParty.IsGarrison && !item2.MobileParty.IsMilitia)
			{
				LeaveSettlementAction.ApplyForParty(item2.MobileParty);
			}
		}
	}

	public static void AddNearbyPartiesToPlayerMapEvent(MapEvent mapEvent)
	{
		List<MobileParty> list = new List<MobileParty>();
		List<MobileParty> list2 = new List<MobileParty>();
		foreach (MapEventParty item in mapEvent.PartiesOnSide(mapEvent.PlayerSide))
		{
			if (item.Party.IsMobile)
			{
				list.Add(item.Party.MobileParty);
			}
		}
		foreach (MapEventParty item2 in mapEvent.PartiesOnSide(mapEvent.PlayerSide.GetOppositeSide()))
		{
			if (item2.Party.IsMobile)
			{
				list2.Add(item2.Party.MobileParty);
			}
		}
		PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list, list2);
		foreach (MobileParty item3 in list)
		{
			mapEvent.GetMapEventSide(mapEvent.PlayerSide).AddNearbyPartyToPlayerMapEvent(item3);
		}
		foreach (MobileParty item4 in list2)
		{
			mapEvent.GetMapEventSide(mapEvent.PlayerSide.GetOppositeSide()).AddNearbyPartyToPlayerMapEvent(item4);
		}
	}

	public static void PlayerEncounterDoWaitCommon(MapEvent mapEvent, CampaignBattleResult campaignBattleResult, out PlayerEncounterState nextEncounterState, out bool stateHandled)
	{
		nextEncounterState = PlayerEncounter.Current.EncounterState;
		stateHandled = false;
		if (campaignBattleResult != null && campaignBattleResult.BattleResolved)
		{
			if (campaignBattleResult.PlayerVictory)
			{
				mapEvent?.SetOverrideWinner(PartyBase.MainParty.Side);
			}
			else
			{
				mapEvent?.SetOverrideWinner(PartyBase.MainParty.OpponentSide);
			}
			nextEncounterState = PlayerEncounterState.PrepareResults;
		}
		else if (PlayerEncounter.Current.BattleSimulation != null && (PlayerEncounter.BattleState == BattleState.AttackerVictory || PlayerEncounter.BattleState == BattleState.DefenderVictory))
		{
			if (mapEvent.WinningSide == PlayerEncounter.Current.PlayerSide && PlayerEncounter.Battle.RetreatingSide == BattleSideEnum.None)
			{
				PlayerEncounter.EnemySurrender = true;
			}
			else
			{
				int totalManCount = MobileParty.MainParty.MemberRoster.TotalManCount;
				int totalWounded = MobileParty.MainParty.MemberRoster.TotalWounded;
				if (totalManCount - totalWounded == 0)
				{
					PlayerEncounter.PlayerSurrender = true;
				}
			}
			nextEncounterState = PlayerEncounterState.PrepareResults;
		}
		else if (mapEvent != null && PlayerEncounter.PlayerSurrender && mapEvent.HasWinner)
		{
			nextEncounterState = PlayerEncounterState.PrepareResults;
		}
		else
		{
			stateHandled = true;
			if (PlayerEncounter.Current.IsJoinedBattle && Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu.StringId == "join_encounter")
			{
				PlayerEncounter.LeaveBattle();
			}
		}
	}

	public static void OnPlayerEncounterContinueBattleCommon(MapEvent mapEvent, CampaignBattleResult campaignBattleResult, out PlayerEncounterState nextEncounterState, out bool stateHandled)
	{
		nextEncounterState = PlayerEncounter.Current.EncounterState;
		mapEvent.ApplyGainedVariablesOnPlayerBattleContinues();
		mapEvent.SetOverrideWinner(BattleSideEnum.None);
		stateHandled = true;
	}

	public static void OnPlayerEncounterContinueNavalBattleCommon(MapEvent mapEvent, CampaignBattleResult campaignBattleResult, out PlayerEncounterState nextEncounterState)
	{
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.PlayerSide);
		MapEventSide otherSide = mapEventSide.OtherSide;
		if (otherSide.Parties.Sum((MapEventParty x) => x.Ships.Count) == 0)
		{
			Debug.FailedAssert("This case should not be called anymore, make sure this is intended", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "OnPlayerEncounterContinueNavalBattleCommon", 4746);
			Debug.Print("Player side wins according to the strength ratio.");
			mapEvent?.SetOverrideWinner(mapEvent.PlayerSide);
			PlayerEncounter.EnemySurrender = true;
			nextEncounterState = PlayerEncounterState.PrepareResults;
		}
		else if (mapEventSide.Parties.Sum((MapEventParty x) => x.Ships.Count) == 0)
		{
			Debug.FailedAssert("This case should not be called anymore, make sure this is intended", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "OnPlayerEncounterContinueNavalBattleCommon", 4755);
			Debug.Print("Other side wins according to the strength ratio.");
			mapEvent?.SetOverrideWinner(otherSide.MissionSide);
			nextEncounterState = PlayerEncounterState.PrepareResults;
		}
		else
		{
			nextEncounterState = PlayerEncounter.Current.EncounterState;
		}
	}

	public static bool CheckIfBattleShouldContinueAfterBattleMissionCommonCondition(MapEvent mapEvent, CampaignBattleResult campaignBattleResult)
	{
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.PlayerSide);
		if (PlayerEncounter.PlayerSurrender || campaignBattleResult == null || campaignBattleResult.EnemyRetreated)
		{
			return false;
		}
		bool flag = !mapEvent.CheckIfOneSideHasLost();
		if (mapEvent.DefeatedSide != BattleSideEnum.None)
		{
			MapEventSide mapEventSide2 = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
			bool num = campaignBattleResult.PlayerDefeat || campaignBattleResult.PlayerVictory || campaignBattleResult.EnemyPulledBack;
			bool flag2 = mapEventSide2.GetTotalHealthyTroopCountOfSide() + mapEventSide2.GetTotalHealthyHeroCountOfSide() >= 1;
			flag = num && flag2;
		}
		if (flag)
		{
			return !mapEventSide.IsSurrendered;
		}
		return false;
	}
}
