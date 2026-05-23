using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultTargetScoreCalculatingModel : TargetScoreCalculatingModel
{
	private const float SiegeBaseValueFactor = 0.8f;

	private const float RaidBaseValueFactor = 0.875f;

	private const float DefenseBaseValueFactor = 1.75f;

	private const float DefenseVillageBaseValueFactor = 1.28f;

	private const float DefenseFollowEnemyBaseValueFactor = 0.8f;

	private const float GiveUpDistanceLimitAsDays = 0.5f;

	public override float TravelingToAssignmentFactor => 1.33f;

	public override float BesiegingFactor => 1.67f;

	public override float AssaultingTownFactor => 2f;

	public override float RaidingFactor => 1.67f;

	public override float DefendingFactor => 2f;

	public override float GetDefensivePatrollingFactor(bool isNavalPatrolling)
	{
		return 0.66f;
	}

	public override float GetOffensivePatrollingFactor(bool isNavalPatrolling)
	{
		return 0f;
	}

	public override float CalculateOffensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty)
	{
		return 0f;
	}

	public override float CurrentObjectiveValue(MobileParty mobileParty)
	{
		float result = 0f;
		if (mobileParty.TargetSettlement == null)
		{
			return 0f;
		}
		if (mobileParty.DefaultBehavior != AiBehavior.BesiegeSettlement && mobileParty.DefaultBehavior != AiBehavior.RaidSettlement && mobileParty.DefaultBehavior != AiBehavior.DefendSettlement)
		{
			return result;
		}
		float totalLandStrengthWithFollowers = mobileParty.GetTotalLandStrengthWithFollowers(includeNonAttachedArmyMembers: false);
		result = GetTargetScoreForFaction(mobileParty.TargetSettlement, (mobileParty.DefaultBehavior != AiBehavior.BesiegeSettlement) ? ((mobileParty.DefaultBehavior == AiBehavior.RaidSettlement) ? Army.ArmyTypes.Raider : Army.ArmyTypes.Defender) : Army.ArmyTypes.Besieger, mobileParty, totalLandStrengthWithFollowers);
		switch (mobileParty.DefaultBehavior)
		{
		case AiBehavior.BesiegeSettlement:
			result *= ((mobileParty.Party.MapEvent == null && mobileParty.TargetSettlement.SiegeEvent != null && mobileParty.TargetSettlement.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(mobileParty.Party)) ? BesiegingFactor : ((mobileParty.Party.MapEvent != null && mobileParty.Party.MapEvent.MapEventSettlement == mobileParty.TargetSettlement) ? AssaultingTownFactor : TravelingToAssignmentFactor));
			break;
		case AiBehavior.RaidSettlement:
			result *= ((mobileParty.Party.MapEvent != null && mobileParty.MapEvent.MapEventSettlement == mobileParty.TargetSettlement) ? RaidingFactor : TravelingToAssignmentFactor);
			break;
		case AiBehavior.DefendSettlement:
			result *= ((mobileParty.Party.MapEvent != null && mobileParty.MapEvent.MapEventSettlement == mobileParty.TargetSettlement) ? DefendingFactor : TravelingToAssignmentFactor);
			break;
		}
		return result;
	}

	public override float CalculateDefensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty)
	{
		bool flag = mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && !mobileParty.Army.IsWaitingForArmyMembers();
		if (mobileParty.Army != null && !flag && mobileParty.Army.Cohesion > (float)mobileParty.Army.CohesionThresholdForDispersion && mobileParty.AttachedTo != null)
		{
			return 0f;
		}
		float num = ((mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan != null && mobileParty.LeaderHero.Clan.Fiefs.Count > 0) ? (mobileParty.LeaderHero.RandomFloat(0.2f, 0.4f) + (float)TaleWorlds.Library.MathF.Min(4, mobileParty.LeaderHero.Clan.Fiefs.Count - 1) * 0.05f + mobileParty.LeaderHero.RandomFloatWithSeed((uint)CampaignTime.Now.ToHours, 0.2f)) : 0.5f);
		float num2 = 1f - num + ((mobileParty.LeaderHero?.Clan != null && (settlement.OwnerClan == mobileParty.LeaderHero.Clan || mobileParty.LeaderHero.Clan.Settlements.Count == 0)) ? num : 0f);
		float num3 = 1f;
		if (settlement.MapFaction == mobileParty.MapFaction)
		{
			float nearbyLandThreatIntensity = settlement.NearbyLandThreatIntensity;
			float nearbyLandAllyIntensity = settlement.NearbyLandAllyIntensity;
			float num4 = TaleWorlds.Library.MathF.Max(0f, nearbyLandThreatIntensity - nearbyLandAllyIntensity * 0.25f);
			if (num4 > 1f)
			{
				int num5 = 0;
				foreach (WarPartyComponent warPartyComponent in mobileParty.MapFaction.WarPartyComponents)
				{
					MobileParty mobileParty2 = warPartyComponent.MobileParty;
					if (mobileParty2 != mobileParty && (mobileParty2.Army == null || mobileParty2.Army != mobileParty.Army) && (mobileParty2.Army == null || mobileParty2.Army.LeaderParty == mobileParty) && mobileParty2.DefaultBehavior == AiBehavior.PatrolAroundPoint && mobileParty2.TargetSettlement == settlement)
					{
						num5++;
					}
				}
				num3 += TaleWorlds.Library.MathF.Pow(TaleWorlds.Library.MathF.Min(10f, num4), 0.25f) - (float)num5;
			}
			else
			{
				num3 += num4;
			}
		}
		float num6 = ((mobileParty.Army != null && mobileParty.Army.LeaderParty != mobileParty && mobileParty.Army.Cohesion < (float)mobileParty.Army.CohesionThresholdForDispersion) ? (((float)mobileParty.Army.CohesionThresholdForDispersion - mobileParty.Army.Cohesion) / (float)mobileParty.Army.CohesionThresholdForDispersion) : 1f);
		float num7 = 1f;
		if (mobileParty.MapFaction.IsMinorFaction)
		{
			num7 = settlement.RandomFloatWithSeed((uint)CampaignTime.Now.ToWeeks, 0.2f, 1.8f);
		}
		float num8 = ((mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && !mobileParty.TargetPosition.IsOnLand && mobileParty.TargetSettlement != null && !mobileParty.TargetSettlement.MapFaction.IsAtWarWith(mobileParty.MapFaction)) ? 0.5f : 1f);
		float num9 = ((mobileParty.LeaderHero != null && settlement.OwnerClan == mobileParty.LeaderHero.Clan) ? 1f : 0.5f);
		return num7 * num3 * num2 * num6 * num8 * num9 * Campaign.Current.Models.TargetScoreCalculatingModel.GetDefensivePatrollingFactor(isNavalPatrolling: false);
	}

	public override float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength)
	{
		IFaction mapFaction = mobileParty.MapFaction;
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort: false, out var _, out var bestNavigationDistance, out var _);
		float num = 0f;
		if (missionType == Army.ArmyTypes.Defender)
		{
			float num2 = 0f;
			float num3 = 0f;
			foreach (WarPartyComponent warPartyComponent in mapFaction.WarPartyComponents)
			{
				MobileParty mobileParty2 = warPartyComponent.MobileParty;
				if (mobileParty2 == mobileParty || (mobileParty2.Army != null && mobileParty2.Army == mobileParty.Army) || mobileParty2.AttachedTo != null)
				{
					continue;
				}
				if (mobileParty2.Army != null)
				{
					Army army = mobileParty2.Army;
					if ((army.IsWaitingForArmyMembers() && army.AiBehaviorObject == targetSettlement) || (!army.IsWaitingForArmyMembers() && army.LeaderParty.DefaultBehavior == AiBehavior.DefendSettlement && army.AiBehaviorObject == targetSettlement) || (army.LeaderParty.TargetParty != null && targetSettlement.LastAttackerParty != null && (army.LeaderParty.TargetParty == targetSettlement.LastAttackerParty || (army.LeaderParty.TargetParty.MapEvent != null && army.LeaderParty.TargetParty.MapEvent == targetSettlement.LastAttackerParty.MapEvent) || (army.LeaderParty.TargetParty.BesiegedSettlement != null && army.LeaderParty.TargetParty.BesiegedSettlement == targetSettlement.LastAttackerParty.BesiegedSettlement))))
					{
						num3 += army.EstimatedStrength;
					}
				}
				else if ((mobileParty2.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty2.TargetSettlement == targetSettlement) || (mobileParty2.TargetParty != null && (mobileParty2.TargetParty == targetSettlement.LastAttackerParty || (mobileParty2.TargetParty.MapEvent != null && mobileParty2.TargetParty.MapEvent == targetSettlement.LastAttackerParty.MapEvent) || (mobileParty2.TargetParty.BesiegedSettlement != null && mobileParty2.TargetParty.BesiegedSettlement == targetSettlement.LastAttackerParty.BesiegedSettlement))))
				{
					num3 += mobileParty2.Party.EstimatedStrength;
				}
			}
			float num4 = 0f;
			MobileParty lastAttackerParty = targetSettlement.LastAttackerParty;
			if (lastAttackerParty != null)
			{
				if ((lastAttackerParty.MapEvent != null && lastAttackerParty.MapEvent.MapEventSettlement == targetSettlement) || lastAttackerParty.BesiegedSettlement == targetSettlement)
				{
					LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(targetSettlement.GatePosition.ToVec2(), Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 3f);
					for (MobileParty mobileParty3 = MobileParty.FindNextLocatable(ref data); mobileParty3 != null; mobileParty3 = MobileParty.FindNextLocatable(ref data))
					{
						if (mobileParty3.Aggressiveness > 0f && mobileParty3.MapFaction == lastAttackerParty.MapFaction)
						{
							num2 += ((mobileParty3.Aggressiveness > 0.5f) ? 1f : (mobileParty3.Aggressiveness * 2f)) * mobileParty3.Party.EstimatedStrength;
						}
					}
				}
				else
				{
					num2 = lastAttackerParty.Army?.EstimatedStrength ?? lastAttackerParty.Party.EstimatedStrength;
				}
			}
			float num5 = ourStrength + num3;
			num4 = TaleWorlds.Library.MathF.Max(100f, num2) * 1.1f;
			num = num5 / num2;
			if (num5 < num4)
			{
				num *= 0.9f;
			}
			if (num3 > num4 * 1.75f)
			{
				num *= 0.25f;
			}
			if (ourStrength < num2)
			{
				num *= TaleWorlds.Library.MathF.Pow(ourStrength / num2, 0.25f);
			}
		}
		else
		{
			float num6 = 0f;
			float num7 = 0f;
			bool flag = Hero.MainHero.CurrentSettlement == targetSettlement;
			foreach (MobileParty party in targetSettlement.Parties)
			{
				if (party.Aggressiveness > 0.01f || party.IsGarrison || party.IsMilitia)
				{
					float num8 = ((party == MobileParty.MainParty) ? 0.5f : ((party.Army != null && party.Army.LeaderParty == MobileParty.MainParty) ? 0.8f : 1f));
					float num9 = (flag ? 0.8f : 1f);
					num6 += num8 * num9 * party.Party.EstimatedStrength;
					if (!party.IsGarrison && !party.IsMilitia && party.LeaderHero != null)
					{
						num7 += num8 * num9 * party.Party.EstimatedStrength;
					}
				}
			}
			float num10 = 0f;
			float num11 = 0f;
			num11 = ((missionType != Army.ArmyTypes.Besieger || mobileParty.BesiegedSettlement == targetSettlement) ? 1f : (targetSettlement.IsTown ? 4f : 3f));
			float num12 = 0.7f;
			num11 *= 1f - 0.6f * (1f - num12) * (1f - num12);
			if (num6 < 100f && missionType == Army.ArmyTypes.Besieger)
			{
				num11 *= 0.5f + 0.5f * (num6 / 100f);
			}
			if (missionType == Army.ArmyTypes.Raider)
			{
				num11 *= 0.66f;
			}
			if ((mobileParty.MapEvent == null || mobileParty.MapEvent.MapEventSettlement != targetSettlement) && targetSettlement.MapFaction.IsKingdomFaction)
			{
				int count = targetSettlement.MapFaction.Settlements.Count;
				float b = (targetSettlement.MapFaction.CurrentTotalStrength * 0.25f - num6 - num7) / ((float)count + 10f);
				num10 = TaleWorlds.Library.MathF.Max(0f, b) * num11;
			}
			float num13 = ((missionType == Army.ArmyTypes.Besieger) ? (1f + 0.33f * (float)targetSettlement.Town.GetWallLevel()) : 1f);
			if (missionType == Army.ArmyTypes.Besieger && targetSettlement.Town.FoodStocks < 100f)
			{
				num13 -= 0.5f * (num13 - 1f) * ((100f - targetSettlement.Town.FoodStocks) / 100f);
			}
			float num14 = ((missionType == Army.ArmyTypes.Besieger && mobileParty.LeaderHero != null) ? (mobileParty.LeaderHero.RandomFloat(0.1f) + (TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Min(1.2f, mobileParty.Aggressiveness), 0.8f) - 0.8f) * 0.5f) : 0f);
			float num15 = num6 * (num13 - num14) + num10 + 0.1f;
			if (targetSettlement.SiegeEvent == null)
			{
				float num16 = ((missionType == Army.ArmyTypes.Besieger) ? 2f : 0.75f);
				if (mobileParty.SiegeEvent != null && mobileParty.BesiegedSettlement == targetSettlement)
				{
					num16 = 1.5f;
				}
				if (ourStrength < num15 * num16)
				{
					return 0f;
				}
			}
			float num17 = 0f;
			if (missionType == Army.ArmyTypes.Besieger || (missionType == Army.ArmyTypes.Raider && targetSettlement.Party.MapEvent != null))
			{
				float num18 = ((missionType == Army.ArmyTypes.Besieger) ? (Campaign.Current.EstimatedAverageLordPartySpeed * 2f) : Campaign.Current.EstimatedAverageLordPartySpeed);
				float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All);
				if (bestNavigationDistance < Math.Max(averageDistanceBetweenClosestTwoTownsWithNavigationType, num18))
				{
					LocatableSearchData<MobileParty> data2 = MobileParty.StartFindingLocatablesAroundPosition(targetSettlement.GatePosition.ToVec2(), num18);
					for (MobileParty mobileParty4 = MobileParty.FindNextLocatable(ref data2); mobileParty4 != null; mobileParty4 = MobileParty.FindNextLocatable(ref data2))
					{
						if (mobileParty4.CurrentSettlement != targetSettlement && mobileParty4.Aggressiveness > 0.01f && mobileParty4.MapFaction == targetSettlement.Party.MapFaction && (!mobileParty4.IsMainParty || !mobileParty4.ShouldBeIgnored))
						{
							float num19 = ((mobileParty4 == MobileParty.MainParty || (mobileParty4.Army != null && mobileParty4.Army.LeaderParty == MobileParty.MainParty)) ? 0.5f : 1f);
							float num20 = float.MaxValue;
							num20 = ((mobileParty4.CurrentSettlement == null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty4, targetSettlement, isTargetingPort: false, MobileParty.NavigationType.All, out var _) : Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty4.CurrentSettlement, targetSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All));
							float value = num18 / (num20 + 0.001f);
							value = MBMath.ClampFloat(value, 0.3f, (missionType == Army.ArmyTypes.Raider) ? 0.8f : 1.2f);
							num17 += value * mobileParty4.Party.EstimatedStrength * num19;
						}
					}
				}
				if (num17 < ourStrength)
				{
					num17 = TaleWorlds.Library.MathF.Max(0f, num17 - ourStrength * 0.33f);
				}
				num15 += num17;
				num15 -= num10;
				if (targetSettlement.MapFaction.IsKingdomFaction)
				{
					int count2 = targetSettlement.MapFaction.Settlements.Count;
					float b2 = (targetSettlement.MapFaction.CurrentTotalStrength * 0.5f - (num7 + num17)) / ((float)count2 + 10f);
					num10 = TaleWorlds.Library.MathF.Max(0f, b2) * num11;
				}
				num15 += num10;
			}
			num = ourStrength / num15;
		}
		num = ((num > 2f) ? 2f : num);
		float averageDistanceBetweenClosestTwoTownsWithNavigationType2 = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All);
		float num21 = MBMath.Map((5f * averageDistanceBetweenClosestTwoTownsWithNavigationType2 - bestNavigationDistance) / averageDistanceBetweenClosestTwoTownsWithNavigationType2, 0f, 5f, 0.9f, (missionType == Army.ArmyTypes.Raider) ? 3f : 10f);
		float num22 = ((missionType == Army.ArmyTypes.Raider) ? targetSettlement.GetSettlementValueForEnemyHero(mobileParty.LeaderHero) : targetSettlement.GetSettlementValueForFaction(mapFaction));
		float num23 = TaleWorlds.Library.MathF.Pow(y: targetSettlement.IsVillage ? 0.5f : 0.33f, x: num22 / 50000f);
		float num24 = 1f;
		if (missionType == Army.ArmyTypes.Raider)
		{
			if (targetSettlement.Village.Bound.Town.FoodStocks < 100f)
			{
				num23 *= 1f + 0.3f * ((100f - targetSettlement.Village.Bound.Town.FoodStocks) / 100f);
			}
			num23 *= 1.5f;
			num24 += ((mobileParty.Army != null) ? 0.5f : 1f) * ((mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan != null && mobileParty.LeaderHero.Clan.Gold < 10000) ? ((10000f - (float)mobileParty.LeaderHero.Clan.Gold) / 20000f) : 0f);
		}
		float num25 = missionType switch
		{
			Army.ArmyTypes.Besieger => 0.8f, 
			Army.ArmyTypes.Defender => targetSettlement.IsVillage ? 1.28f : 1.75f, 
			_ => 0.875f * (1f + (1f - targetSettlement.SettlementHitPoints)), 
		};
		if (missionType == Army.ArmyTypes.Defender && targetSettlement.LastAttackerParty != null && ((targetSettlement.IsFortification && targetSettlement.LastAttackerParty.BesiegedSettlement != targetSettlement) || (!targetSettlement.IsFortification && targetSettlement.LastAttackerParty.MapEvent == null)))
		{
			MobileParty lastAttackerParty2 = targetSettlement.LastAttackerParty;
			MobileParty.NavigationType navCapabilities = ((!lastAttackerParty2.HasNavalNavigationCapability) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.All);
			float b3 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(lastAttackerParty2, targetSettlement, navCapabilities) / (lastAttackerParty2._lastCalculatedSpeed * (float)CampaignTime.HoursInDay);
			float num26 = TaleWorlds.Library.MathF.Min(0.5f, b3) / 0.5f;
			num25 = num26 * 0.8f + (1f - num26) * num25;
		}
		float num27 = 1f;
		if ((missionType == Army.ArmyTypes.Raider || missionType == Army.ArmyTypes.Besieger) && targetSettlement.OwnerClan != null && mobileParty.LeaderHero != null)
		{
			int relationWithClan = mobileParty.LeaderHero.Clan.GetRelationWithClan(targetSettlement.OwnerClan);
			if (relationWithClan > 0)
			{
				num27 = 1f - ((missionType == Army.ArmyTypes.Besieger) ? 0.4f : 0.8f) * (TaleWorlds.Library.MathF.Sqrt(relationWithClan) / 10f);
			}
			else if (relationWithClan < 0)
			{
				num27 = 1f + ((missionType == Army.ArmyTypes.Besieger) ? 0.1f : 0.05f) * (TaleWorlds.Library.MathF.Sqrt(-relationWithClan) / 10f);
			}
		}
		float num28 = 1f;
		if (mobileParty.MapFaction != null && mobileParty.MapFaction.IsKingdomFaction && mobileParty.MapFaction.Leader == Hero.MainHero && (missionType != Army.ArmyTypes.Defender || (targetSettlement.LastAttackerParty != null && targetSettlement.LastAttackerParty.MapFaction != Hero.MainHero.MapFaction)))
		{
			StanceLink stanceLink = ((missionType != Army.ArmyTypes.Defender) ? Hero.MainHero.MapFaction.GetStanceWith(targetSettlement.MapFaction) : Hero.MainHero.MapFaction.GetStanceWith(targetSettlement.LastAttackerParty.MapFaction));
			if (stanceLink != null)
			{
				if (stanceLink.BehaviorPriority == 1)
				{
					switch (missionType)
					{
					case Army.ArmyTypes.Besieger:
					case Army.ArmyTypes.Raider:
						num28 = 0.65f;
						break;
					case Army.ArmyTypes.Defender:
						num28 = 1.1f;
						break;
					}
				}
				else if (stanceLink.BehaviorPriority == 2 && (missionType == Army.ArmyTypes.Besieger || missionType == Army.ArmyTypes.Raider))
				{
					num28 = 1.3f;
				}
			}
		}
		float num29 = 1f;
		if (mobileParty.TargetSettlement == targetSettlement)
		{
			num29 = 1.3f;
			if (mobileParty.SiegeEvent != null && mobileParty.SiegeEvent.BesiegedSettlement == targetSettlement)
			{
				num29 = 4f;
			}
			if (mobileParty.MapEvent != null && mobileParty.MapEvent.IsRaid && mobileParty.MapEvent.MapEventSettlement == targetSettlement)
			{
				num29 = 1.5f;
			}
		}
		float num30 = 1f;
		if (mobileParty.SiegeEvent == null && targetSettlement.SiegeEvent != null && targetSettlement.SiegeEvent.BesiegerCamp.MapFaction == mobileParty.MapFaction)
		{
			float num31 = targetSettlement.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType().Sum((PartyBase x) => x.EstimatedStrength);
			float num32 = targetSettlement.GetInvolvedPartiesForEventType().Sum((PartyBase x) => x.EstimatedStrength);
			if (num32 > 0f)
			{
				float num33 = num31 / num32 - 5f;
				if (num33 > 0f)
				{
					num33 = MBMath.ClampFloat(num33, 0f, 1f);
					num30 = MBMath.Map(num33, 0f, 1f, 1f, 0f);
				}
			}
			else
			{
				num30 = 0f;
			}
		}
		float num34 = num27 * num * num23 * num24 * num25 * num28 * num29 * num30 * num21;
		if (mobileParty.Objective == MobileParty.PartyObjective.Defensive && missionType == Army.ArmyTypes.Defender)
		{
			num34 *= 1.2f;
		}
		else if (mobileParty.Objective == MobileParty.PartyObjective.Aggressive && (missionType == Army.ArmyTypes.Besieger || missionType == Army.ArmyTypes.Raider))
		{
			num34 *= 1.2f;
		}
		return (num34 < 0f) ? 0f : num34;
	}
}
