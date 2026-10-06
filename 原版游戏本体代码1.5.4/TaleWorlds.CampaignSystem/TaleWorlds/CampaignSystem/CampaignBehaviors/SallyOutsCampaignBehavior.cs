using System.Linq;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class SallyOutsCampaignBehavior : CampaignBehaviorBase
{
	private const int SallyOutCheckPeriodInHours = 4;

	private const float SallyOutPowerRatioForHelpingReliefForce = 1.5f;

	private const float SallyOutPowerRatio = 2f;

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, HourlyTickSettlement);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
	}

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		if (defenderParty.SiegeEvent != null)
		{
			CheckForSettlementSallyOut(defenderParty.SiegeEvent.BesiegedSettlement);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void HourlyTickSettlement(Settlement settlement)
	{
		CheckForSettlementSallyOut(settlement);
	}

	private void CheckForSettlementSallyOut(Settlement settlement, bool forceForCheck = false)
	{
		if (settlement.IsFortification && settlement.SiegeEvent != null && settlement.Party.MapEvent == null && settlement.Town.GarrisonParty != null && settlement.Town.GarrisonParty.MapEvent == null && ((settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent != null && (settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsSiegeOutside || settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsBlockade)) || MathF.Floor(CampaignTime.Now.ToHours) % 4 == 0) && (Hero.MainHero.CurrentSettlement != settlement || Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(settlement.SiegeEvent, BattleSideEnum.Defender) != Hero.MainHero))
		{
			CheckSallyOut(settlement, checkForNavalSallyOut: false, out var salliedOut);
			if (!salliedOut && settlement.HasPort && settlement.SiegeEvent.IsBlockadeActive)
			{
				CheckSallyOut(settlement, checkForNavalSallyOut: true, out var _);
			}
		}
	}

	private void CheckSallyOut(Settlement settlement, bool checkForNavalSallyOut, out bool salliedOut)
	{
		salliedOut = false;
		MobileParty leaderParty = settlement.SiegeEvent.BesiegerCamp.LeaderParty;
		bool flag = false;
		bool flag2 = false;
		if (settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent != null)
		{
			flag = settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsSiegeOutside;
			flag2 = settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsBlockade;
		}
		if ((flag2 && !checkForNavalSallyOut) || (flag && checkForNavalSallyOut))
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = (checkForNavalSallyOut ? settlement.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.BlockadeSallyOutBattle).Sum((PartyBase x) => x.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.SeaBattle)) : settlement.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.SallyOut).Sum((PartyBase x) => x.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.PlainBattle)));
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(settlement.SiegeEvent.BesiegerCamp.LeaderParty.Position.ToVec2(), Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius);
		for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref data); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref data))
		{
			if (mobileParty.CurrentSettlement == null && mobileParty.Aggressiveness > 0f)
			{
				float num4 = ((mobileParty.Aggressiveness > 0.5f) ? 1f : (mobileParty.Aggressiveness * 2f));
				if (mobileParty.MapFaction.IsAtWarWith(settlement.Party.MapFaction))
				{
					BattleSideEnum side = BattleSideEnum.Defender;
					num += num4 * (checkForNavalSallyOut ? mobileParty.Party.GetCustomStrength(side, MapEvent.PowerCalculationContext.SeaBattle) : mobileParty.Party.GetCustomStrength(side, MapEvent.PowerCalculationContext.PlainBattle));
				}
				else if (mobileParty.MapFaction == settlement.MapFaction && checkForNavalSallyOut == mobileParty.IsCurrentlyAtSea)
				{
					BattleSideEnum side2 = BattleSideEnum.Attacker;
					num2 += num4 * (checkForNavalSallyOut ? mobileParty.Party.GetCustomStrength(side2, MapEvent.PowerCalculationContext.SeaBattle) : mobileParty.Party.GetCustomStrength(side2, MapEvent.PowerCalculationContext.PlainBattle));
				}
			}
		}
		float num5 = num3 + num2;
		float num6 = ((flag || flag2) ? 1.5f : 2f);
		if (!(num5 > num * num6))
		{
			return;
		}
		if (flag || flag2)
		{
			foreach (PartyBase item in settlement.GetInvolvedPartiesForEventType(checkForNavalSallyOut ? MapEvent.BattleTypes.BlockadeSallyOutBattle : MapEvent.BattleTypes.SallyOut))
			{
				if (item.IsMobile && item.NumberOfHealthyMembers > 0 && !item.MobileParty.IsMainParty && item.MapEventSide == null)
				{
					if (settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
					{
						break;
					}
					item.MapEventSide = settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.AttackerSide;
				}
			}
		}
		else
		{
			if (checkForNavalSallyOut)
			{
				settlement.Town.GarrisonParty.SetTargetSettlement(settlement, isTargetingPort: true);
			}
			EncounterManager.StartPartyEncounter(settlement.Town.GarrisonParty.Party, leaderParty.Party);
		}
		salliedOut = true;
	}
}
