using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;

public class AiVisitSettlementBehavior : CampaignBehaviorBase
{
	private readonly struct SettlementNavigationData : IComparable<SettlementNavigationData>
	{
		public readonly float Distance;

		public readonly int SettlementIdentifier;

		public readonly Settlement Settlement;

		public readonly MobileParty.NavigationType BestNavigationType;

		public readonly bool IsFromPort;

		public readonly bool IsTargetingPortBetter;

		public SettlementNavigationData(float distance, int settlementIdentifier, Settlement settlement, MobileParty.NavigationType bestNavigationType, bool isFromPort, bool isTargetingPortBetter)
		{
			Distance = distance;
			SettlementIdentifier = settlementIdentifier;
			Settlement = settlement;
			BestNavigationType = bestNavigationType;
			IsFromPort = isFromPort;
			IsTargetingPortBetter = isTargetingPortBetter;
		}

		public int CompareTo(SettlementNavigationData otherSettlementNavigationData)
		{
			float distance = Distance;
			int num = distance.CompareTo(otherSettlementNavigationData.Distance);
			if (num == 0)
			{
				int settlementIdentifier = SettlementIdentifier;
				num = settlementIdentifier.CompareTo(otherSettlementNavigationData.SettlementIdentifier);
			}
			return num;
		}
	}

	public const float GoodEnoughScore = 8f;

	public const float MeaningfulScoreThreshold = 0.025f;

	public const float BaseVisitScore = 1.6f;

	private const float DefaultMoneyLimitForRecruiting = 2000f;

	private readonly List<SettlementNavigationData> _settlementsNavigationData = new List<SettlementNavigationData>();

	private readonly Dictionary<Settlement, int> _numberOfAlliedMobilePartiesTargetingSettlement = new Dictionary<Settlement, int>();

	private IDisbandPartyCampaignBehavior _disbandPartyCampaignBehavior;

	private static float SearchForNeutralSettlementRadiusAsDays => 0.5f;

	private float NumberOfHoursAtDay => Campaign.Current.Models.CampaignTimeModel.HoursInDay;

	private float IdealTimePeriodForVisitingOwnedSettlement => (float)Campaign.Current.Models.CampaignTimeModel.HoursInDay * 15f;

	private static float GetMaximumDistanceAsDays(MobileParty.NavigationType navigationType)
	{
		return Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(navigationType) * 4f / (Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay);
	}

	private float MaximumMeaningfulDistanceAsDays(MobileParty.NavigationType navigationType)
	{
		return GetMaximumDistanceAsDays(navigationType) * 0.7f;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		_disbandPartyCampaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
	}

	private void OnNewGameCreated(CampaignGameStarter obj)
	{
		RefreshTheTargetingSettlementDictionary();
	}

	private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		RefreshTheTargetingSettlementDictionary();
	}

	private void OnHourlyTick()
	{
		RefreshTheTargetingSettlementDictionary();
	}

	private void RefreshTheTargetingSettlementDictionary()
	{
		foreach (Settlement item in Settlement.All)
		{
			if (item.IsFortification || item.IsVillage)
			{
				_numberOfAlliedMobilePartiesTargetingSettlement[item] = 0;
			}
		}
		foreach (MobileParty allLordParty in MobileParty.AllLordParties)
		{
			if ((allLordParty.Army == null || allLordParty.AttachedTo == null || allLordParty.Army.LeaderParty == allLordParty) && allLordParty.TargetSettlement != null && allLordParty.CurrentSettlement != allLordParty.TargetSettlement && allLordParty.TargetSettlement.MapFaction == allLordParty.MapFaction)
			{
				int num = allLordParty.Army?.LeaderPartyAndAttachedPartiesCount ?? 1;
				_numberOfAlliedMobilePartiesTargetingSettlement[allLordParty.TargetSettlement] += num;
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
	{
		if (mobileParty.CurrentSettlement?.SiegeEvent != null)
		{
			return;
		}
		Settlement currentSettlementOfMobilePartyForAICalculation = MobilePartyHelper.GetCurrentSettlementOfMobilePartyForAICalculation(mobileParty);
		if (mobileParty.IsBandit)
		{
			CalculateVisitHideoutScoresForBanditParty(mobileParty, currentSettlementOfMobilePartyForAICalculation, p);
			return;
		}
		IFaction mapFaction = mobileParty.MapFaction;
		if (mobileParty.IsMilitia || mobileParty.IsCaravan || mobileParty.IsPatrolParty || mobileParty.IsVillager || (!mapFaction.IsMinorFaction && !mapFaction.IsKingdomFaction && (mobileParty.LeaderHero == null || !mobileParty.LeaderHero.IsLord)) || (mobileParty.Army != null && mobileParty.AttachedTo != null && mobileParty.Army.LeaderParty != mobileParty))
		{
			return;
		}
		Hero leaderHero = mobileParty.LeaderHero;
		(float, float, int, int) tuple = CalculatePartyParameters(mobileParty);
		float item = tuple.Item1;
		float item2 = tuple.Item2;
		int item3 = tuple.Item3;
		int item4 = tuple.Item4;
		float num = item2 / Math.Min(1f, Math.Max(0.1f, item));
		float num2 = ((num >= 1f) ? 0.33f : ((TaleWorlds.Library.MathF.Max(1f, TaleWorlds.Library.MathF.Min(2f, num)) - 0.5f) / 1.5f));
		float num3 = mobileParty.Food;
		float num4 = 0f - mobileParty.FoodChange;
		int num5 = mobileParty.PartyTradeGold;
		if (mobileParty.Army != null && mobileParty == mobileParty.Army.LeaderParty)
		{
			foreach (MobileParty attachedParty in mobileParty.Army.LeaderParty.AttachedParties)
			{
				num3 += attachedParty.Food;
				num4 += 0f - attachedParty.FoodChange;
				num5 += attachedParty.PartyTradeGold;
			}
		}
		float num6 = 1f;
		if (leaderHero != null && mobileParty.IsLordParty)
		{
			num6 = CalculateSellItemScore(mobileParty);
		}
		int num7 = mobileParty.Party.PrisonerSizeLimit;
		if (mobileParty.Army != null)
		{
			foreach (MobileParty attachedParty2 in mobileParty.Army.LeaderParty.AttachedParties)
			{
				num7 += attachedParty2.Party.PrisonerSizeLimit;
			}
		}
		_settlementsNavigationData.Clear();
		FillSettlementsToVisitWithDistancesAsDays(mobileParty, _settlementsNavigationData);
		float num8 = PartyBaseHelper.FindPartySizeNormalLimit(mobileParty);
		float num9 = 2000f;
		float num10 = 2000f;
		if (leaderHero != null)
		{
			num9 = HeroHelper.StartRecruitingMoneyLimitForClanLeader(leaderHero);
			num10 = HeroHelper.StartRecruitingMoneyLimit(leaderHero);
		}
		float num11 = 0.2f;
		float num12 = 1f;
		_settlementsNavigationData.Sort();
		foreach (SettlementNavigationData settlementsNavigationDatum in _settlementsNavigationData)
		{
			Settlement settlement = settlementsNavigationDatum.Settlement;
			MobileParty.NavigationType bestNavigationType = settlementsNavigationDatum.BestNavigationType;
			float distance = settlementsNavigationDatum.Distance;
			bool isFromPort = settlementsNavigationDatum.IsFromPort;
			bool isTargetingPortBetter = settlementsNavigationDatum.IsTargetingPortBetter;
			float num13 = 1.6f;
			if (!mobileParty.IsDisbanding)
			{
				IDisbandPartyCampaignBehavior disbandPartyCampaignBehavior = _disbandPartyCampaignBehavior;
				if (disbandPartyCampaignBehavior == null || !disbandPartyCampaignBehavior.IsPartyWaitingForDisband(mobileParty))
				{
					if (leaderHero == null)
					{
						bool canMerge;
						float visitingNearbySettlementScore = CalculateMergeScoreForLeaderlessParty(mobileParty, settlement, distance, out canMerge);
						if (canMerge)
						{
							AddBehaviorTupleWithScore(p, settlement, visitingNearbySettlementScore, bestNavigationType, isFromPort, isTargetingPortBetter);
						}
					}
					else
					{
						if (distance >= MaximumMeaningfulDistanceAsDays(bestNavigationType))
						{
							AddBehaviorTupleWithScore(p, settlement, 0.025f, bestNavigationType, isFromPort, isTargetingPortBetter);
							continue;
						}
						float num14 = TaleWorlds.Library.MathF.Max(num11, distance);
						float num15 = 1f;
						if (distance > num11)
						{
							num15 = num12 / (num12 - num11 + distance);
						}
						float num16 = num15;
						if (item < 0.6f)
						{
							num16 = TaleWorlds.Library.MathF.Pow(num15, TaleWorlds.Library.MathF.Pow(0.6f / TaleWorlds.Library.MathF.Max(0.15f, item), 0.3f));
						}
						float num17 = 1f;
						float num18 = (float)item3 / (float)item4;
						bool flag = mobileParty.Army != null && mobileParty.AttachedTo == null && mobileParty.Army.LeaderParty != mobileParty;
						if (settlement.IsFortification && num18 > 0.2f)
						{
							num17 = MBMath.Map(num18 - 0.2f, 0f, 0.8f, 1f, 5f);
							if (flag || mobileParty.MapEvent != null || mobileParty.SiegeEvent != null)
							{
								num17 *= 0.6f;
							}
						}
						float num19 = 1f;
						if (mobileParty.DefaultBehavior == AiBehavior.GoToSettlement && ((settlement == currentSettlementOfMobilePartyForAICalculation && currentSettlementOfMobilePartyForAICalculation.IsFortification) || (currentSettlementOfMobilePartyForAICalculation == null && settlement == mobileParty.TargetSettlement)))
						{
							num19 = 1.2f;
						}
						else if (currentSettlementOfMobilePartyForAICalculation == null && settlement == mobileParty.LastVisitedSettlement)
						{
							num19 = 0.8f;
						}
						float num20 = ((num18 > 0.2f) ? 1f : 0.16f);
						float num21 = Math.Max(0f, num3) / num4;
						if (num4 > 0f && (mobileParty.BesiegedSettlement == null || num21 <= 1f) && num5 > 100 && (settlement.IsTown || (settlement.IsVillage && mobileParty.Army == null)))
						{
							float neededFoodsInDaysThresholdForSiege = Campaign.Current.Models.MobilePartyAIModel.NeededFoodsInDaysThresholdForSiege;
							if (num21 < neededFoodsInDaysThresholdForSiege)
							{
								int num22 = (int)(num4 * ((num21 < 1f && settlement.IsVillage) ? Campaign.Current.Models.PartyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromVillage : Campaign.Current.Models.PartyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromTown)) + 1;
								float num23 = neededFoodsInDaysThresholdForSiege * 0.5f;
								float num24 = num23 - Math.Min(num23, Math.Max(0f, num21 - 1f));
								float num25 = (float)num22 + 20f * (float)((!settlement.IsTown) ? 1 : 2) * ((num14 > num12) ? 1f : (num14 / num12));
								int val = (int)((float)(num5 - 100) / Campaign.Current.Models.PartyFoodBuyingModel.LowCostFoodPriceAverage);
								num20 += num24 * num24 * 0.093f * ((num21 < num23) ? (15f + 0.5f * (num23 - num21)) : 1f) * Math.Min(num25, Math.Min(val, settlement.ItemRoster.TotalFood)) / num25;
							}
						}
						float num26 = 0f;
						int num27 = 0;
						int num28 = 0;
						float num29 = 1f;
						if (!settlement.IsCastle && item < 1f && mobileParty.GetAvailableWageBudget() > 0)
						{
							num28 = _numberOfAlliedMobilePartiesTargetingSettlement[settlement];
							if (currentSettlementOfMobilePartyForAICalculation == settlement)
							{
								num27 -= mobileParty.Army?.LeaderPartyAndAttachedPartiesCount ?? 1;
								if (num27 < 0)
								{
									num27 = 0;
								}
							}
							if (mobileParty.TargetSettlement == settlement || (mobileParty.Army != null && mobileParty.Army.LeaderParty.TargetSettlement == settlement))
							{
								num28 -= mobileParty.Army?.LeaderPartyAndAttachedPartiesCount ?? 1;
								if (num28 < 0)
								{
									num28 = 0;
								}
							}
							if (mobileParty.Army != null)
							{
								num28 += mobileParty.Army.LeaderPartyAndAttachedPartiesCount;
							}
							if (!mobileParty.Party.IsStarving && (float)mobileParty.PartyTradeGold > num10 && (leaderHero.Clan.Leader == leaderHero || (float)leaderHero.Clan.Gold > num9) && num8 > mobileParty.PartySizeRatio)
							{
								(int, float) approximateVolunteersCanBeRecruitedDataFromSettlement = GetApproximateVolunteersCanBeRecruitedDataFromSettlement(leaderHero, settlement);
								num26 = approximateVolunteersCanBeRecruitedDataFromSettlement.Item1;
								if (num26 > 0f)
								{
									float item5 = approximateVolunteersCanBeRecruitedDataFromSettlement.Item2;
									num26 = Math.Min(num26, TaleWorlds.Library.MathF.Floor((float)mobileParty.GetAvailableWageBudget() / item5));
								}
							}
							float num30 = num26 * num15 / TaleWorlds.Library.MathF.Sqrt(1 + num27 + num28);
							float num31 = ((num30 < 1f) ? num30 : ((float)Math.Pow(num30, num2)));
							num29 = Math.Max(Math.Min(1f, num20), Math.Max((mapFaction == settlement.MapFaction) ? 0.25f : 0.16f, num * Math.Max(1f, Math.Min(2f, num)) * num31 * (1f - 0.9f * num18) * (1f - 0.9f * num18)));
						}
						num13 *= num29 * num17 * num20 * num16;
						if (num13 >= 8f)
						{
							AddBehaviorTupleWithScore(p, settlement, num13, bestNavigationType, isFromPort, isTargetingPortBetter);
							break;
						}
						float num32 = 1f;
						if (num26 > 0f && !flag)
						{
							num32 = 1f + ((mobileParty.DefaultBehavior == AiBehavior.GoToSettlement && settlement != currentSettlementOfMobilePartyForAICalculation && num14 < num11) ? (0.1f * TaleWorlds.Library.MathF.Min(5f, num26) - 0.1f * TaleWorlds.Library.MathF.Min(5f, num26) * (num14 / num11) * (num14 / num11)) : 0f);
						}
						float num33 = ((settlement.IsCastle && !flag && num20 < 1f) ? 1.4f : 1f);
						num13 *= (settlement.IsTown ? num6 : 1f) * num32 * num33;
						if (num13 >= 8f)
						{
							AddBehaviorTupleWithScore(p, settlement, num13, bestNavigationType, isFromPort, isTargetingPortBetter);
							break;
						}
						int num34 = mobileParty.PrisonRoster.TotalRegulars;
						if (mobileParty.PrisonRoster.TotalHeroes > 0)
						{
							foreach (TroopRosterElement item6 in mobileParty.PrisonRoster.GetTroopRoster())
							{
								if (item6.Character.IsHero && item6.Character.HeroObject.Clan.IsAtWarWith(settlement.MapFaction))
								{
									num34 += 6;
								}
							}
						}
						float num35 = 1f;
						float num36 = 1f;
						if (mobileParty.Army != null && mobileParty.Army.LeaderParty.AttachedParties.Contains(mobileParty))
						{
							if (mobileParty.Army.LeaderParty != mobileParty)
							{
								num35 = ((float)mobileParty.Army.CohesionThresholdForDispersion - mobileParty.Army.Cohesion) / (float)mobileParty.Army.CohesionThresholdForDispersion;
							}
							num36 = ((MobileParty.MainParty != null && mobileParty.Army == MobileParty.MainParty.Army) ? 0.6f : 0.8f);
							foreach (MobileParty attachedParty3 in mobileParty.Army.LeaderParty.AttachedParties)
							{
								num34 += attachedParty3.PrisonRoster.TotalRegulars;
								if (attachedParty3.PrisonRoster.TotalHeroes <= 0)
								{
									continue;
								}
								foreach (TroopRosterElement item7 in attachedParty3.PrisonRoster.GetTroopRoster())
								{
									if (item7.Character.IsHero && item7.Character.HeroObject.Clan.IsAtWarWith(settlement.MapFaction))
									{
										num34 += 6;
									}
								}
							}
						}
						float num37 = (settlement.IsFortification ? (1f + 2f * (float)(num34 / num7)) : 1f);
						float num38 = ((mobileParty.DesiredAiNavigationType == bestNavigationType) ? 1.5f : 1f);
						float num39 = 1f;
						float num40 = 1f;
						float num41 = 1f;
						float num42 = 1f;
						float num43 = 1f;
						if (num20 <= 0.5f)
						{
							(num39, num40, num41, num42) = CalculateBeingSettlementOwnerScores(mobileParty, settlement, currentSettlementOfMobilePartyForAICalculation, -1f, num15, item);
						}
						float num44 = 1f;
						if (settlement.HasPort && mobileParty.Ships.Any())
						{
							float num45 = mobileParty.Ships.AverageQ((Ship x) => x.HitPoints / x.MaxHitPoints);
							if (num45 < 0.8f)
							{
								num44 = ((num45 > 0.6f) ? 1.5f : ((!(num45 > 0.4f)) ? 3f : 1.75f));
							}
						}
						num13 *= num43 * num19 * num35 * num37 * num36 * num39 * num41 * num40 * num42 * num38 * num44;
					}
					goto IL_0bfb;
				}
			}
			float visitingNearbySettlementScore2 = CalculateMergeScoreForDisbandingParty(mobileParty, settlement, distance);
			AddBehaviorTupleWithScore(p, settlement, visitingNearbySettlementScore2, bestNavigationType, isFromPort, isTargetingPortBetter);
			goto IL_0bfb;
			IL_0bfb:
			if (num13 > 0.025f)
			{
				AddBehaviorTupleWithScore(p, settlement, num13, bestNavigationType, isFromPort, isTargetingPortBetter);
			}
		}
	}

	private (int, float) GetApproximateVolunteersCanBeRecruitedDataFromSettlement(Hero hero, Settlement settlement)
	{
		int num = 4;
		if (hero.MapFaction != settlement.MapFaction)
		{
			num = 2;
		}
		int num2 = 0;
		int num3 = 0;
		foreach (Hero notable in settlement.Notables)
		{
			if (!notable.IsAlive)
			{
				continue;
			}
			for (int i = 0; i < num; i++)
			{
				if (notable.VolunteerTypes[i] != null)
				{
					num2++;
					num3 += Campaign.Current.Models.PartyWageModel.GetCharacterWage(notable.VolunteerTypes[i]);
				}
			}
		}
		if (num2 > 0)
		{
			num3 /= num2;
		}
		return (num2, num3);
	}

	private float CalculateSellItemScore(MobileParty mobileParty)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < mobileParty.ItemRoster.Count; i++)
		{
			ItemRosterElement itemRosterElement = mobileParty.ItemRoster[i];
			if (itemRosterElement.EquipmentElement.Item.IsMountable)
			{
				num2 += (float)(itemRosterElement.Amount * itemRosterElement.EquipmentElement.Item.Value);
			}
			else if (!itemRosterElement.EquipmentElement.Item.IsFood)
			{
				num += (float)(itemRosterElement.Amount * itemRosterElement.EquipmentElement.Item.Value);
			}
		}
		float num3 = ((num2 > (float)mobileParty.PartyTradeGold * 0.1f) ? TaleWorlds.Library.MathF.Min(3f, TaleWorlds.Library.MathF.Pow((num2 + 1000f) / ((float)mobileParty.PartyTradeGold * 0.1f + 1000f), 0.33f)) : 1f);
		float num4 = 1f + TaleWorlds.Library.MathF.Min(3f, TaleWorlds.Library.MathF.Pow(num / (((float)mobileParty.MemberRoster.TotalManCount + 5f) * 100f), 0.33f));
		float num5 = num3 * num4;
		if (mobileParty.Army != null)
		{
			num5 = TaleWorlds.Library.MathF.Sqrt(num5);
		}
		return num5;
	}

	private (float, float, int, int) CalculatePartyParameters(MobileParty mobileParty)
	{
		float num = 0f;
		int num2 = 0;
		int num3 = 0;
		float item;
		if (mobileParty.Army != null && (mobileParty.AttachedTo != null || mobileParty.Army.LeaderParty == mobileParty))
		{
			float num4 = 0f;
			foreach (MobileParty attachedParty in mobileParty.AttachedParties)
			{
				float partySizeRatio = attachedParty.PartySizeRatio;
				num4 += partySizeRatio;
				num2 += attachedParty.MemberRoster.TotalWounded;
				num3 += attachedParty.MemberRoster.TotalManCount;
				float num5 = PartyBaseHelper.FindPartySizeNormalLimit(attachedParty);
				num += num5;
			}
			item = num4 / (float)mobileParty.Army.Parties.Count;
			num /= (float)mobileParty.Army.Parties.Count;
		}
		else
		{
			item = mobileParty.PartySizeRatio;
			num2 += mobileParty.MemberRoster.TotalWounded;
			num3 += mobileParty.MemberRoster.TotalManCount;
			num += PartyBaseHelper.FindPartySizeNormalLimit(mobileParty);
		}
		return (item, num, num2, num3);
	}

	private void CalculateVisitHideoutScoresForBanditParty(MobileParty mobileParty, Settlement currentSettlement, PartyThinkParams p)
	{
		if (!mobileParty.MapFaction.Culture.CanHaveSettlement || (currentSettlement != null && currentSettlement.IsHideout))
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < mobileParty.ItemRoster.Count; i++)
		{
			ItemRosterElement itemRosterElement = mobileParty.ItemRoster[i];
			num += itemRosterElement.Amount * itemRosterElement.EquipmentElement.Item.Value;
		}
		float num2 = 1f + 4f * Math.Min(num, 1000f) / 1000f;
		int num3 = 0;
		MBReadOnlyList<Hideout> allHideouts = Campaign.Current.AllHideouts;
		foreach (Hideout item in allHideouts)
		{
			if (item.Settlement.Culture == mobileParty.Party.Culture && item.IsInfested)
			{
				num3++;
			}
		}
		float num4 = 1f + 4f * (float)Math.Sqrt(mobileParty.PrisonRoster.TotalManCount / mobileParty.Party.PrisonerSizeLimit);
		int numberOfMinimumBanditPartiesInAHideoutToInfestIt = Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt;
		int numberOfMaximumBanditPartiesInEachHideout = Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesInEachHideout;
		int numberOfMaximumHideoutsAtEachBanditFaction = Campaign.Current.Models.BanditDensityModel.NumberOfMaximumHideoutsAtEachBanditFaction;
		foreach (Hideout item2 in allHideouts)
		{
			Settlement settlement = item2.Settlement;
			if (settlement.Party.MapEvent != null || settlement.Culture != mobileParty.Party.Culture)
			{
				continue;
			}
			bool isTargetingPort = false;
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, settlement, isTargetingPort, out var bestNavigationType, out var bestNavigationDistance, out var _);
			if (bestNavigationType == MobileParty.NavigationType.None)
			{
				continue;
			}
			float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(bestNavigationType);
			float num5 = averageDistanceBetweenClosestTwoTownsWithNavigationType * 6f / (Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay);
			bestNavigationDistance = Math.Max(averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.15f, bestNavigationDistance);
			float num6 = bestNavigationDistance / (Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay);
			float num7 = num5 / (num5 + num6);
			int num8 = 0;
			foreach (MobileParty party in settlement.Parties)
			{
				if (party.IsBandit && !party.IsBanditBossParty)
				{
					num8++;
				}
			}
			float num10;
			if (num8 < numberOfMinimumBanditPartiesInAHideoutToInfestIt)
			{
				float num9 = (float)(numberOfMaximumHideoutsAtEachBanditFaction - num3) / (float)numberOfMaximumHideoutsAtEachBanditFaction;
				num10 = ((num3 < numberOfMaximumHideoutsAtEachBanditFaction) ? (0.25f + 0.75f * num9) : 0f);
			}
			else
			{
				num10 = Math.Max(0f, 1f * (1f - (float)(Math.Min(numberOfMaximumBanditPartiesInEachHideout, num8) - numberOfMinimumBanditPartiesInAHideoutToInfestIt) / (float)(numberOfMaximumBanditPartiesInEachHideout - numberOfMinimumBanditPartiesInAHideoutToInfestIt)));
			}
			float num11 = ((mobileParty.DefaultBehavior == AiBehavior.GoToSettlement && mobileParty.TargetSettlement == settlement) ? 1f : (MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat));
			float num12 = num7 * num10 * num2 * num11 * num4;
			if (num12 > 0f)
			{
				AddBehaviorTupleWithScore(p, item2.Settlement, num12, bestNavigationType, isFromPort: false, isTargetingPortBetter: false);
			}
		}
	}

	private (float, float, float, float) CalculateBeingSettlementOwnerScores(MobileParty mobileParty, Settlement settlement, Settlement currentSettlement, float idealGarrisonStrengthPerWalledCenter, float distanceScorePure, float averagePartySizeRatioToMaximumSize)
	{
		float num = 1f;
		float num2 = 1f;
		float num3 = 1f;
		float item = 1f;
		Hero leaderHero = mobileParty.LeaderHero;
		IFaction mapFaction = mobileParty.MapFaction;
		if (currentSettlement != settlement && (mobileParty.Army == null || mobileParty.Army.LeaderParty != mobileParty))
		{
			if (settlement.OwnerClan.Leader == leaderHero)
			{
				float currentTime = Campaign.CurrentTime;
				float lastVisitTimeOfOwner = settlement.LastVisitTimeOfOwner;
				float num4 = ((currentTime - lastVisitTimeOfOwner > NumberOfHoursAtDay) ? (currentTime - lastVisitTimeOfOwner) : ((NumberOfHoursAtDay - (currentTime - lastVisitTimeOfOwner)) * (IdealTimePeriodForVisitingOwnedSettlement / NumberOfHoursAtDay))) / IdealTimePeriodForVisitingOwnedSettlement;
				num += num4;
			}
			if (MBRandom.RandomFloatWithSeed((uint)mobileParty.RandomValue, (uint)CampaignTime.Now.ToDays) < 0.5f && settlement.IsFortification && leaderHero.Clan != Clan.PlayerClan && (settlement.OwnerClan.Leader == leaderHero || settlement.OwnerClan == leaderHero.Clan))
			{
				if (idealGarrisonStrengthPerWalledCenter == -1f)
				{
					idealGarrisonStrengthPerWalledCenter = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(mapFaction as Kingdom);
				}
				int num5 = Campaign.Current.Models.SettlementGarrisonModel.FindNumberOfTroopsToTakeFromGarrison(mobileParty, settlement, idealGarrisonStrengthPerWalledCenter);
				if (num5 > 0)
				{
					num2 = 1f + TaleWorlds.Library.MathF.Pow(num5, 0.67f);
					if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty)
					{
						num2 = 1f + (num2 - 1f) / TaleWorlds.Library.MathF.Sqrt(mobileParty.Army.Parties.Count);
					}
				}
			}
		}
		if (settlement == leaderHero.HomeSettlement && mobileParty.Army == null && !settlement.IsVillage)
		{
			float num6 = (leaderHero.HomeSettlement.IsCastle ? 1.5f : 1f);
			num3 = ((currentSettlement != settlement) ? (num3 + 1000f * num6 / (250f + leaderHero.PassedTimeAtHomeSettlement * leaderHero.PassedTimeAtHomeSettlement)) : (num3 + 3000f * num6 / (250f + leaderHero.PassedTimeAtHomeSettlement * leaderHero.PassedTimeAtHomeSettlement)));
		}
		if (settlement != currentSettlement)
		{
			float num7 = 1f;
			if (mobileParty.LastVisitedSettlement == settlement)
			{
				num7 = 0.25f;
			}
			if (settlement.IsFortification && settlement.MapFaction == mapFaction && settlement.OwnerClan != Clan.PlayerClan)
			{
				float num8 = ((settlement.Town.GarrisonParty != null) ? settlement.Town.GarrisonParty.Party.EstimatedStrength : 0f);
				float num9 = FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(settlement.OwnerClan);
				float num10 = FactionHelper.SettlementProsperityEffectOnGarrisonSizeConstant(settlement.Town);
				float num11 = FactionHelper.SettlementFoodPotentialEffectOnGarrisonSizeConstant(settlement);
				if (idealGarrisonStrengthPerWalledCenter == -1f)
				{
					idealGarrisonStrengthPerWalledCenter = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(mapFaction as Kingdom);
				}
				float num12 = idealGarrisonStrengthPerWalledCenter;
				if (settlement.Town.GarrisonParty != null && settlement.Town.GarrisonParty.HasLimitedWage())
				{
					num12 = (float)settlement.Town.GarrisonParty.PaymentLimit / Campaign.Current.AverageWage;
				}
				else
				{
					if (mobileParty.Army != null)
					{
						num12 *= 0.75f;
					}
					num12 *= num9 * num10 * num11;
				}
				float num13 = num12;
				if (num8 < num13)
				{
					float num14 = ((settlement.OwnerClan == leaderHero.Clan) ? 149f : 99f);
					if (settlement.OwnerClan == Clan.PlayerClan)
					{
						num14 *= 0.5f;
					}
					float num15 = 1f - num8 / num13;
					item = 1f + num14 * distanceScorePure * distanceScorePure * (averagePartySizeRatioToMaximumSize - 0.5f) * num15 * num15 * num15 * num7;
				}
			}
		}
		return (num, num2, num3, item);
	}

	private float CalculateMergeScoreForDisbandingParty(MobileParty disbandParty, Settlement settlement, float distanceAsDays)
	{
		float num = Campaign.MapDiagonal / (disbandParty._lastCalculatedSpeed * (float)CampaignTime.HoursInDay);
		float num2 = TaleWorlds.Library.MathF.Pow(3.5f - 0.95f * (Math.Min(num, distanceAsDays) / num), 3f);
		float num3 = ((disbandParty.Party.Owner?.Clan == settlement.OwnerClan) ? 1f : ((disbandParty.Party.Owner?.MapFaction == settlement.MapFaction) ? 0.35f : 0.025f));
		float num4 = ((disbandParty.DefaultBehavior == AiBehavior.GoToSettlement && disbandParty.TargetSettlement == settlement) ? 1f : 0.3f);
		float num5 = (settlement.IsFortification ? 3f : 1f);
		float num6 = num2 * num3 * num4 * num5;
		if (num6 < 0.025f)
		{
			num6 = 0.035f;
		}
		return num6;
	}

	private float CalculateMergeScoreForLeaderlessParty(MobileParty leaderlessParty, Settlement settlement, float distanceAsDays, out bool canMerge)
	{
		if (settlement.IsVillage)
		{
			canMerge = false;
			return -1f;
		}
		float num = Campaign.MapDiagonal / (leaderlessParty._lastCalculatedSpeed * (float)CampaignTime.HoursInDay);
		float num2 = TaleWorlds.Library.MathF.Pow(3.5f - 0.95f * (Math.Min(num, distanceAsDays) / num), 3f);
		float num3 = ((leaderlessParty.ActualClan == settlement.OwnerClan) ? 2f : ((leaderlessParty.ActualClan?.MapFaction == settlement.MapFaction) ? 0.35f : 0f));
		float num4 = ((leaderlessParty.DefaultBehavior == AiBehavior.GoToSettlement && leaderlessParty.TargetSettlement == settlement) ? 1f : 0.3f);
		float num5 = (settlement.IsFortification ? 3f : 0.5f);
		canMerge = true;
		return num2 * num3 * num4 * num5;
	}

	private static void FillSettlementsToVisitWithDistancesAsDays(MobileParty mobileParty, List<SettlementNavigationData> listToFill)
	{
		float num = SearchForNeutralSettlementRadiusAsDays * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
		if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.MapFaction.IsKingdomFaction)
		{
			MBReadOnlyList<Settlement> settlements = mobileParty.MapFaction.Settlements;
			float num2 = 0f;
			foreach (Settlement item in settlements)
			{
				if (IsSettlementSuitableForVisitingCondition(mobileParty, item))
				{
					GetBestNavigationDataForVisitingSettlement(mobileParty, item, out var bestNavigationType, out var distanceAsDays, out var isFromPort, out var isTargetingPortBetter);
					if (bestNavigationType != MobileParty.NavigationType.None && distanceAsDays < GetMaximumDistanceAsDays(bestNavigationType))
					{
						num2 += distanceAsDays;
						listToFill.Add(new SettlementNavigationData(distanceAsDays, item.GetHashCode(), item, bestNavigationType, isFromPort, isTargetingPortBetter));
					}
				}
			}
			num2 /= (float)listToFill.Count;
			if (num2 > GetMaximumDistanceAsDays(mobileParty.NavigationCapability) * 0.7f && (mobileParty.Army == null || mobileParty.Army.LeaderParty == mobileParty))
			{
				LocatableSearchData<Settlement> data = Settlement.StartFindingLocatablesAroundPosition(mobileParty.Position.ToVec2(), num);
				for (Settlement settlement = Settlement.FindNextLocatable(ref data); settlement != null; settlement = Settlement.FindNextLocatable(ref data))
				{
					if (!settlement.IsCastle && settlement.MapFaction != mobileParty.MapFaction && IsSettlementSuitableForVisitingCondition(mobileParty, settlement))
					{
						GetBestNavigationDataForVisitingSettlement(mobileParty, settlement, out var bestNavigationType2, out var distanceAsDays2, out var isFromPort2, out var isTargetingPortBetter2);
						if (bestNavigationType2 != MobileParty.NavigationType.None && distanceAsDays2 < GetMaximumDistanceAsDays(bestNavigationType2))
						{
							listToFill.Add(new SettlementNavigationData(distanceAsDays2, settlement.GetHashCode(), settlement, bestNavigationType2, isFromPort2, isTargetingPortBetter2));
						}
					}
				}
			}
		}
		else
		{
			LocatableSearchData<Settlement> data2 = Settlement.StartFindingLocatablesAroundPosition(mobileParty.Position.ToVec2(), num * 1.6f);
			for (Settlement settlement2 = Settlement.FindNextLocatable(ref data2); settlement2 != null; settlement2 = Settlement.FindNextLocatable(ref data2))
			{
				if (IsSettlementSuitableForVisitingCondition(mobileParty, settlement2))
				{
					GetBestNavigationDataForVisitingSettlement(mobileParty, settlement2, out var bestNavigationType3, out var distanceAsDays3, out var isFromPort3, out var isTargetingPortBetter3);
					if (bestNavigationType3 != MobileParty.NavigationType.None && distanceAsDays3 < GetMaximumDistanceAsDays(bestNavigationType3))
					{
						listToFill.Add(new SettlementNavigationData(distanceAsDays3, settlement2.GetHashCode(), settlement2, bestNavigationType3, isFromPort3, isTargetingPortBetter3));
					}
				}
			}
		}
		if (listToFill.AnyQ())
		{
			return;
		}
		Settlement factionMidSettlement = mobileParty.MapFaction.FactionMidSettlement;
		if (factionMidSettlement == null)
		{
			return;
		}
		if (factionMidSettlement.IsFortification)
		{
			foreach (Village boundVillage in factionMidSettlement.BoundVillages)
			{
				if (IsSettlementSuitableForVisitingCondition(mobileParty, boundVillage.Settlement))
				{
					GetBestNavigationDataForVisitingSettlement(mobileParty, boundVillage.Settlement, out var bestNavigationType4, out var distanceAsDays4, out var isFromPort4, out var isTargetingPortBetter4);
					if (bestNavigationType4 != MobileParty.NavigationType.None)
					{
						listToFill.Add(new SettlementNavigationData(distanceAsDays4, boundVillage.GetHashCode(), boundVillage.Settlement, bestNavigationType4, isFromPort4, isTargetingPortBetter4));
					}
				}
			}
			return;
		}
		if (IsSettlementSuitableForVisitingCondition(mobileParty, factionMidSettlement))
		{
			GetBestNavigationDataForVisitingSettlement(mobileParty, factionMidSettlement, out var bestNavigationType5, out var distanceAsDays5, out var isFromPort5, out var isTargetingPortBetter5);
			if (bestNavigationType5 != MobileParty.NavigationType.None)
			{
				listToFill.Add(new SettlementNavigationData(distanceAsDays5, factionMidSettlement.GetHashCode(), factionMidSettlement, bestNavigationType5, isFromPort5, isTargetingPortBetter5));
			}
		}
	}

	private static void GetBestNavigationDataForVisitingSettlement(MobileParty mobileParty, Settlement settlement, out MobileParty.NavigationType bestNavigationType, out float distanceAsDays, out bool isFromPort, out bool isTargetingPortBetter)
	{
		bestNavigationType = MobileParty.NavigationType.None;
		float bestNavigationDistance = float.MaxValue;
		bool isFromPort2 = false;
		isTargetingPortBetter = false;
		isFromPort = false;
		if (!settlement.HasPort || settlement.SiegeEvent == null || settlement.SiegeEvent.IsBlockadeActive || !mobileParty.HasNavalNavigationCapability)
		{
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, settlement, isTargetingPort: false, out bestNavigationType, out bestNavigationDistance, out isFromPort2);
		}
		if (mobileParty.HasNavalNavigationCapability && settlement.HasPort && settlement.IsFortification)
		{
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, settlement, isTargetingPort: true, out var bestNavigationType2, out var bestNavigationDistance2, out var isFromPort3);
			if (bestNavigationDistance2 < bestNavigationDistance)
			{
				bestNavigationType = bestNavigationType2;
				bestNavigationDistance = bestNavigationDistance2;
				isFromPort = isFromPort3;
				isTargetingPortBetter = true;
			}
			else
			{
				isFromPort = isFromPort2;
				isTargetingPortBetter = false;
			}
		}
		distanceAsDays = bestNavigationDistance / (Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay);
	}

	private void AddBehaviorTupleWithScore(PartyThinkParams p, Settlement settlement, float visitingNearbySettlementScore, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPortBetter)
	{
		AIBehaviorData aiBehaviorData = new AIBehaviorData(settlement, AiBehavior.GoToSettlement, navigationType, willGatherArmy: false, isFromPort, isTargetingPortBetter);
		if (p.TryGetBehaviorScore(in aiBehaviorData, out var score))
		{
			p.SetBehaviorScore(in aiBehaviorData, score + visitingNearbySettlementScore);
		}
		else
		{
			p.AddBehaviorScore((aiBehaviorData, visitingNearbySettlementScore));
		}
	}

	private static bool IsSettlementSuitableForVisitingCondition(MobileParty mobileParty, Settlement settlement)
	{
		if (settlement.Party.MapEvent == null && (settlement.Party.SiegeEvent == null || (!settlement.Party.SiegeEvent.IsBlockadeActive && mobileParty.HasNavalNavigationCapability)) && (!mobileParty.Party.Owner.MapFaction.IsAtWarWith(settlement.MapFaction) || ((mobileParty.Party.Owner.MapFaction.IsMinorFaction || mobileParty.MapFaction.Settlements.Count == 0) && settlement.IsVillage)) && (settlement.IsVillage || settlement.IsFortification))
		{
			if (settlement.IsVillage)
			{
				return settlement.Village.VillageState == Village.VillageStates.Normal;
			}
			return true;
		}
		return false;
	}
}
