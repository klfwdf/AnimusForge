using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors
{
	// Token: 0x02000471 RID: 1137
	public class AiPatrollingBehavior : CampaignBehaviorBase
	{
		// Token: 0x060047E0 RID: 18400 RVA: 0x0016A350 File Offset: 0x00168550
		public override void RegisterEvents()
		{
			CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(this.AiHourlyTick));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.OnShipDestroyedEvent.AddNonSerializedListener(this, new Action<PartyBase, Ship, DestroyShipAction.ShipDestroyDetail>(this.OnShipDestroyed));
			CampaignEvents.OnBlockadeActivatedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnBlockadeActivated));
			CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, new Action<Ship, PartyBase, ChangeShipOwnerAction.ShipOwnerChangeDetail>(this.OnShipOwnerChanged));
		}

		// Token: 0x060047E1 RID: 18401 RVA: 0x0016A3D0 File Offset: 0x001685D0
		private void OnBlockadeActivated(SiegeEvent siegeEvent)
		{
			foreach (MobileParty mobileParty in MobileParty.All)
			{
				if (mobileParty.DefaultBehavior == AiBehavior.GoToSettlement && mobileParty.TargetSettlement == siegeEvent.BesiegedSettlement && mobileParty.CurrentSettlement != siegeEvent.BesiegedSettlement)
				{
					mobileParty.SetMoveModeHold();
				}
			}
		}

		// Token: 0x060047E2 RID: 18402 RVA: 0x0016A448 File Offset: 0x00168648
		private void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail changeDetail)
		{
			this.CheckPartyIfNeeded(oldOwner);
		}

		// Token: 0x060047E3 RID: 18403 RVA: 0x0016A451 File Offset: 0x00168651
		private void OnShipDestroyed(PartyBase owner, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
		{
			this.CheckPartyIfNeeded(owner);
		}

		// Token: 0x060047E4 RID: 18404 RVA: 0x0016A45C File Offset: 0x0016865C
		private void CheckPartyIfNeeded(PartyBase party)
		{
			if (party != null && party.IsMobile && party.MobileParty.IsLordParty && party.MobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && !party.MobileParty.TargetPosition.IsOnLand && !party.MobileParty.HasNavalNavigationCapability)
			{
				party.MobileParty.SetMoveModeHold();
			}
		}

		// Token: 0x060047E5 RID: 18405 RVA: 0x0016A4BA File Offset: 0x001686BA
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this._disbandPartyCampaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
		}

		// Token: 0x060047E6 RID: 18406 RVA: 0x0016A4CC File Offset: 0x001686CC
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060047E7 RID: 18407 RVA: 0x0016A4D0 File Offset: 0x001686D0
		private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
		{
			if (mobileParty.IsMilitia || mobileParty.IsCaravan || mobileParty.IsVillager || mobileParty.IsBandit || mobileParty.IsPatrolParty || mobileParty.IsDisbanding || (!mobileParty.MapFaction.IsMinorFaction && !mobileParty.MapFaction.IsKingdomFaction && !mobileParty.MapFaction.Leader.IsLord))
			{
				return;
			}
			if (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.IsUnderSiege)
			{
				return;
			}
			if (mobileParty.Army != null)
			{
				return;
			}
			if (mobileParty.GetNumDaysForFoodToLast() <= 6)
			{
				return;
			}
			Settlement currentSettlement = mobileParty.CurrentSettlement;
			if (((currentSettlement != null) ? currentSettlement.SiegeEvent : null) != null)
			{
				return;
			}
			float b;
			if (mobileParty.Army != null)
			{
				float num = 0f;
				foreach (MobileParty mobileParty2 in mobileParty.Army.Parties)
				{
					float num2 = PartyBaseHelper.FindPartySizeNormalLimit(mobileParty2);
					float num3 = mobileParty2.PartySizeRatio / num2;
					num += num3;
				}
				b = num / (float)mobileParty.Army.Parties.Count;
			}
			else
			{
				float num4 = PartyBaseHelper.FindPartySizeNormalLimit(mobileParty);
				b = mobileParty.PartySizeRatio / num4;
			}
			float num5 = MathF.Sqrt(MathF.Min(1f, b));
			if (!mobileParty.IsDisbanding)
			{
				IDisbandPartyCampaignBehavior disbandPartyCampaignBehavior = this._disbandPartyCampaignBehavior;
				if (disbandPartyCampaignBehavior == null || !disbandPartyCampaignBehavior.IsPartyWaitingForDisband(mobileParty))
				{
					goto IL_157;
				}
			}
			num5 *= 0.25f;
			IL_157:
			if (mobileParty.Party.MapFaction.Settlements.Count > 0)
			{
				float distanceToFurthestAllySettlementToFactionMidSettlement;
				SettlementHelper.FindFurthestFortificationToSettlement(mobileParty.MapFaction.Fiefs, MobileParty.NavigationType.Default, mobileParty.MapFaction.FactionMidSettlement, out distanceToFurthestAllySettlementToFactionMidSettlement);
				using (List<Settlement>.Enumerator enumerator2 = mobileParty.Party.MapFaction.Settlements.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						Settlement settlement = enumerator2.Current;
						if (settlement.IsTown || settlement.IsVillage)
						{
							float maxValue = float.MaxValue;
							if (settlement.HasPort && mobileParty.HasNavalNavigationCapability && (!mobileParty.MapFaction.IsKingdomFaction || mobileParty.MapFaction.Leader != mobileParty.LeaderHero))
							{
								this.GetDistanceScoreForNavalPatrolling(settlement, mobileParty, out maxValue);
								if (maxValue > 0.2f)
								{
									this.CalculatePatrollingScoreForSettlement(settlement, p, maxValue, true);
								}
							}
							this.GetDistanceScoreForLandPatrolling(settlement, mobileParty, distanceToFurthestAllySettlementToFactionMidSettlement, out maxValue);
							if (maxValue > 0.2f)
							{
								this.CalculatePatrollingScoreForSettlement(settlement, p, maxValue, false);
							}
						}
					}
					return;
				}
			}
			float maxDistance = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(mobileParty.NavigationCapability) * 4f / (Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay) * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
			int num6 = -1;
			do
			{
				num6 = SettlementHelper.FindNextSettlementAroundMobileParty(mobileParty, mobileParty.NavigationCapability, maxDistance, num6, (Settlement x) => x.IsTown);
				if (num6 >= 0)
				{
					Settlement settlement2 = Settlement.All[num6];
					float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default);
					float num7 = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.HomeSettlement, settlement2, false, false, MobileParty.NavigationType.Default);
					if (num7 < averageDistanceBetweenClosestTwoTownsWithNavigationType)
					{
						num7 = averageDistanceBetweenClosestTwoTownsWithNavigationType;
					}
					float num8 = averageDistanceBetweenClosestTwoTownsWithNavigationType * 5f / num7;
					this.CalculatePatrollingScoreForSettlement(settlement2, p, num5 * num8, false);
				}
			}
			while (num6 >= 0);
		}

		// Token: 0x060047E8 RID: 18408 RVA: 0x0016A838 File Offset: 0x00168A38
		private void GetDistanceScoreForNavalPatrolling(Settlement targetSettlement, MobileParty mobileParty, out float bestDistanceScore)
		{
			bestDistanceScore = 0f;
			MobileParty.NavigationType navigationType;
			float num;
			bool flag;
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, true, out navigationType, out num, out flag);
			if (navigationType != MobileParty.NavigationType.None)
			{
				float num2 = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Naval) * 2f;
				if (num > num2)
				{
					bestDistanceScore = -1f;
					return;
				}
				bestDistanceScore = MBMath.Map(1f - num / num2, 0f, 1f, 0.2f, 1f);
			}
		}

		// Token: 0x060047E9 RID: 18409 RVA: 0x0016A8A0 File Offset: 0x00168AA0
		private void GetDistanceScoreForLandPatrolling(Settlement targetSettlement, MobileParty mobileParty, float distanceToFurthestAllySettlementToFactionMidSettlement, out float bestDistanceScore)
		{
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.MapFaction.FactionMidSettlement, targetSettlement, false, false, mobileParty.NavigationCapability);
			float input;
			if (distanceToFurthestAllySettlementToFactionMidSettlement == 0f)
			{
				input = 0.5f;
			}
			else
			{
				input = distance / distanceToFurthestAllySettlementToFactionMidSettlement;
			}
			float num = MBMath.Map(input, 0f, 1f, 0.2f, 0.8f);
			if (mobileParty.PartySizeRatio >= num)
			{
				bestDistanceScore = MBMath.Map(0.8f - (mobileParty.PartySizeRatio - num), 0f, 0.8f, 0.2f, 1f);
				return;
			}
			bestDistanceScore = 0f;
		}

		// Token: 0x060047EA RID: 18410 RVA: 0x0016A948 File Offset: 0x00168B48
		private void CalculatePatrollingScoreForSettlement(Settlement settlement, PartyThinkParams p, float scoreAdjustment, bool isNavalPatrolling)
		{
			MobileParty mobilePartyOf = p.MobilePartyOf;
			MobileParty.NavigationType navigationType;
			float num;
			bool isFromPort;
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobilePartyOf, settlement, isNavalPatrolling, out navigationType, out num, out isFromPort);
			if (navigationType != MobileParty.NavigationType.None)
			{
				AIBehaviorData item = new AIBehaviorData(settlement, AiBehavior.PatrolAroundPoint, navigationType, false, isFromPort, isNavalPatrolling);
				float num2 = Campaign.Current.Models.TargetScoreCalculatingModel.CalculatePatrollingScoreForSettlement(settlement, isNavalPatrolling, mobilePartyOf);
				num2 *= scoreAdjustment;
				if (num2 > 0f)
				{
					if (!mobilePartyOf.IsCurrentlyAtSea)
					{
					}
					ValueTuple<AIBehaviorData, float> valueTuple = new ValueTuple<AIBehaviorData, float>(item, 1.44f + num2);
					p.AddBehaviorScore(valueTuple);
				}
			}
		}

		// Token: 0x040013D5 RID: 5077
		private const float BasePatrolScore = 1.44f;

		// Token: 0x040013D6 RID: 5078
		private const float MinimumDistanceScore = 0.2f;

		// Token: 0x040013D7 RID: 5079
		private const float MaximumDistanceScore = 1f;

		// Token: 0x040013D8 RID: 5080
		private IDisbandPartyCampaignBehavior _disbandPartyCampaignBehavior;
	}
}
