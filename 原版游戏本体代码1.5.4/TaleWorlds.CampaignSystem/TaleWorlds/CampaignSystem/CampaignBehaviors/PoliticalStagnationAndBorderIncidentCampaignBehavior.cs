using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PoliticalStagnationAndBorderIncidentCampaignBehavior : CampaignBehaviorBase
{
	private const float ThreatUpdateTimeThresholdInHours = 3f;

	private Dictionary<Settlement, CampaignTime> _lastUpdateTimePerSettlement;

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, HourlyTickSettlement);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, NewGameCreated);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, LoadFinished);
	}

	private void LoadFinished()
	{
		if (_lastUpdateTimePerSettlement == null)
		{
			InitializeDictionary();
		}
	}

	private void NewGameCreated(CampaignGameStarter obj)
	{
		InitializeDictionary();
	}

	private void InitializeDictionary()
	{
		_lastUpdateTimePerSettlement = new Dictionary<Settlement, CampaignTime>();
		foreach (Settlement item in Settlement.All)
		{
			if (item.IsFortification || item.IsVillage)
			{
				_lastUpdateTimePerSettlement.Add(item, CampaignTime.Now - CampaignTime.Hours(MBRandom.RandomFloat * 3f));
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_lastUpdateTimePerSettlement", ref _lastUpdateTimePerSettlement);
	}

	public void HourlyTickSettlement(Settlement settlement)
	{
		if (_lastUpdateTimePerSettlement.ContainsKey(settlement) && _lastUpdateTimePerSettlement[settlement].ElapsedHoursUntilNow > 3f)
		{
			UpdateNearbyValues(settlement, isCheckingNavalValues: false);
			settlement.NearbyLandThreatIntensity *= 0.85f;
			settlement.NearbyLandAllyIntensity *= 0.8f;
			if (settlement.HasPort)
			{
				UpdateNearbyValues(settlement, isCheckingNavalValues: true);
				settlement.NearbyNavalThreatIntensity *= 0.85f;
				settlement.NearbyNavalAllyIntensity *= 0.8f;
			}
			_lastUpdateTimePerSettlement[settlement] = CampaignTime.Now;
		}
	}

	private void UpdateNearbyValues(Settlement settlement, bool isCheckingNavalValues)
	{
		float settlementNearbyThreatAndAllyCheckRadius = Campaign.Current.Models.MobilePartyAIModel.GetSettlementNearbyThreatAndAllyCheckRadius(settlement, isCheckingNavalValues);
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition((isCheckingNavalValues ? settlement.PortPosition : settlement.GatePosition).ToVec2(), settlementNearbyThreatAndAllyCheckRadius);
		for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref data); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref data))
		{
			if (!mobileParty.IsGarrison && !mobileParty.IsMilitia && mobileParty.IsActive)
			{
				if (mobileParty.Ai.IsAlerted && mobileParty.MapFaction == settlement.MapFaction && (mobileParty.IsCaravan || mobileParty.IsVillager))
				{
					if (mobileParty.IsCurrentlyAtSea && isCheckingNavalValues)
					{
						settlement.NearbyNavalThreatIntensity += 0.6f;
					}
					else if (!isCheckingNavalValues)
					{
						settlement.NearbyLandThreatIntensity += 0.6f;
					}
				}
				if (mobileParty.Aggressiveness > 0f)
				{
					if (mobileParty.CurrentSettlement == null && mobileParty.Army == null && (mobileParty.IsBandit || FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, settlement.MapFaction)))
					{
						float threatValueOfEnemyToSettlement = GetThreatValueOfEnemyToSettlement(mobileParty, settlement);
						if (mobileParty.IsCurrentlyAtSea && isCheckingNavalValues)
						{
							settlement.NearbyNavalThreatIntensity += threatValueOfEnemyToSettlement * 3f;
						}
						else if (!isCheckingNavalValues)
						{
							settlement.NearbyLandThreatIntensity += threatValueOfEnemyToSettlement * 3f;
						}
					}
					else if (mobileParty.MapFaction == settlement.MapFaction)
					{
						bool flag = mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && !mobileParty.TargetPosition.IsOnLand;
						float num = GetThreatValueOfParty(mobileParty);
						if (flag)
						{
							num *= 0.5f;
						}
						if ((mobileParty.IsCurrentlyAtSea || flag) && isCheckingNavalValues)
						{
							settlement.NearbyNavalAllyIntensity += num * 3f;
						}
						else if (!isCheckingNavalValues)
						{
							settlement.NearbyLandAllyIntensity += num * 3f;
						}
					}
				}
			}
		}
	}

	private float GetThreatValueOfParty(MobileParty mobileParty)
	{
		return MathF.Min(1f, mobileParty.Party.EstimatedStrength / 400f * MathF.Min(1f, mobileParty.Aggressiveness));
	}

	private float GetThreatValueOfEnemyToSettlement(MobileParty mobileParty, Settlement settlement)
	{
		float num = GetThreatValueOfParty(mobileParty);
		if (mobileParty == MobileParty.MainParty)
		{
			num *= 2f;
		}
		if (!mobileParty.IsLordParty)
		{
			num *= 0.5f;
		}
		if (mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && mobileParty.TargetSettlement == settlement)
		{
			num *= 2f;
		}
		if (mobileParty.MapEvent != null && mobileParty.MapEvent.IsFieldBattle)
		{
			num = 3f * num;
		}
		return num;
	}

	public void DailyTick()
	{
		foreach (Kingdom item in Kingdom.All)
		{
			UpdatePoliticallyStagnation(item);
		}
	}

	private static void UpdatePoliticallyStagnation(Kingdom kingdom)
	{
		float num = 1f + (float)MathF.Min(60, kingdom.Fiefs.Count) * 0.2f;
		float num2 = 2f + (float)MathF.Min(60, kingdom.Fiefs.Count) * 0.6f;
		int num3 = 1;
		foreach (Kingdom item in Kingdom.All)
		{
			if (FactionManager.IsAtWarAgainstFaction(kingdom, item))
			{
				if ((float)item.Fiefs.Count >= num2)
				{
					num3 = -2;
					break;
				}
				if ((float)item.Fiefs.Count >= num)
				{
					num3 = -1;
				}
			}
		}
		kingdom.PoliticalStagnation += num3;
		if (kingdom.PoliticalStagnation < 0)
		{
			kingdom.PoliticalStagnation = 0;
		}
		else if (kingdom.PoliticalStagnation > 300)
		{
			kingdom.PoliticalStagnation = 300;
		}
	}
}
