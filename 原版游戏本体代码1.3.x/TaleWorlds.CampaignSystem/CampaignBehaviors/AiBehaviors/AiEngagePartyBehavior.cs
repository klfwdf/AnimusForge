using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors
{
	// Token: 0x0200046C RID: 1132
	public class AiEngagePartyBehavior : CampaignBehaviorBase
	{
		// Token: 0x060047B6 RID: 18358 RVA: 0x00167536 File Offset: 0x00165736
		public override void RegisterEvents()
		{
			CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(this.AiHourlyTick));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x060047B7 RID: 18359 RVA: 0x00167566 File Offset: 0x00165766
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this._disbandPartyCampaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
		}

		// Token: 0x060047B8 RID: 18360 RVA: 0x00167578 File Offset: 0x00165778
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060047B9 RID: 18361 RVA: 0x0016757C File Offset: 0x0016577C
		private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
		{
			if (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.SiegeEvent != null)
			{
				return;
			}
			if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && mobileParty.Army.ArmyType != Army.ArmyTypes.Defender)
			{
				return;
			}
			float num = Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringMobileParty * 45f;
			if ((mobileParty.MapFaction.IsKingdomFaction || mobileParty.MapFaction == Hero.MainHero.MapFaction) && !mobileParty.IsCaravan && (mobileParty.Army == null || mobileParty.Army.LeaderParty == mobileParty) && mobileParty.LeaderHero != null)
			{
				bool flag = !mobileParty.MapFaction.Settlements.Any<Settlement>();
				float num2 = 0f;
				if (!flag)
				{
					float num3 = Campaign.MapDiagonalSquared;
					float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(mobileParty.NavigationCapability);
					LocatableSearchData<Settlement> locatableSearchData = Settlement.StartFindingLocatablesAroundPosition(mobileParty.Position.ToVec2(), averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.76f);
					for (Settlement settlement = Settlement.FindNextLocatable(ref locatableSearchData); settlement != null; settlement = Settlement.FindNextLocatable(ref locatableSearchData))
					{
						if (settlement.MapFaction == mobileParty.MapFaction)
						{
							float num4 = settlement.Position.DistanceSquared(mobileParty.Position);
							if (num4 < num3)
							{
								num3 = num4;
							}
						}
					}
					float num5 = MathF.Sqrt(num3);
					float num6 = Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
					num2 = ((num5 < num6 * 0.5f) ? (1f - MathF.Max(0f, num5 - num6 * 0.15f) / num6 * 0.3f) : 0f);
				}
				if (flag || num2 > 0f)
				{
					float num7 = mobileParty.PartySizeRatio;
					foreach (MobileParty mobileParty2 in mobileParty.AttachedParties)
					{
						num7 += mobileParty2.PartySizeRatio;
					}
					float num8 = MathF.Min(1f, num7 / ((float)mobileParty.AttachedParties.Count + 1f));
					float num9 = num8 * ((num8 <= 0.5f) ? num8 : (0.5f + 0.707f * MathF.Sqrt(num8 - 0.5f)));
					LocatableSearchData<MobileParty> locatableSearchData2 = MobileParty.StartFindingLocatablesAroundPosition(mobileParty.Position.ToVec2(), num);
					for (MobileParty mobileParty3 = MobileParty.FindNextLocatable(ref locatableSearchData2); mobileParty3 != null; mobileParty3 = MobileParty.FindNextLocatable(ref locatableSearchData2))
					{
						if (mobileParty3.IsActive && (mobileParty3.IsLordParty || mobileParty3.IsCurrentlyAtSea == mobileParty.IsCurrentlyAtSea))
						{
							IFaction mapFaction = mobileParty3.MapFaction;
							if (mapFaction != null && mapFaction.IsAtWarWith(mobileParty.MapFaction) && (mobileParty3.Army == null || mobileParty3 == mobileParty3.Army.LeaderParty))
							{
								IFaction mapFaction2 = mobileParty3.MapFaction;
								if (((mapFaction2 != null && mapFaction2.IsKingdomFaction) || mobileParty3.MapFaction == Hero.MainHero.MapFaction) && (mobileParty3.CurrentSettlement == null || !mobileParty3.CurrentSettlement.IsFortification) && mobileParty3.Aggressiveness > 0.1f && !mobileParty3.ShouldBeIgnored)
								{
									MobileParty.NavigationType navigationType = MobileParty.NavigationType.None;
									float num10 = Campaign.MapDiagonal;
									bool flag2 = mobileParty.HasNavalNavigationCapability && mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.HasPort && mobileParty3.IsCurrentlyAtSea;
									if (mobileParty.CurrentSettlement == null && mobileParty3.CurrentSettlement == null)
									{
										AiHelper.GetBestNavigationTypeAndDistanceOfMobilePartyForMobileParty(mobileParty, mobileParty3, out navigationType, out num10);
									}
									else if (mobileParty3.CurrentSettlement == null)
									{
										bool flag3;
										AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty3, mobileParty.CurrentSettlement, false, out navigationType, out num10, out flag3);
									}
									else
									{
										navigationType = (flag2 ? MobileParty.NavigationType.Naval : MobileParty.NavigationType.Default);
										num10 = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty3.CurrentSettlement, mobileParty.CurrentSettlement, flag2, flag2, navigationType);
									}
									if (num10 < num)
									{
										float estimatedStrength;
										if (mobileParty3.Army != null)
										{
											estimatedStrength = mobileParty3.Army.EstimatedStrength;
										}
										else
										{
											estimatedStrength = mobileParty3.Party.EstimatedStrength;
										}
										float num11 = 1f - num10 / num;
										float num12 = 1f;
										if (mobileParty3.LeaderHero != null)
										{
											int relation = mobileParty3.LeaderHero.GetRelation(mobileParty.LeaderHero);
											if (relation < 0)
											{
												num12 = 1f + MathF.Sqrt((float)(-(float)relation)) / 20f;
											}
											else
											{
												num12 = 1f - MathF.Sqrt((float)relation) / 10f;
											}
										}
										float num13 = 0f;
										LocatableSearchData<MobileParty> locatableSearchData3 = MobileParty.StartFindingLocatablesAroundPosition(mobileParty.Position.ToVec2(), num);
										for (MobileParty mobileParty4 = MobileParty.FindNextLocatable(ref locatableSearchData3); mobileParty4 != null; mobileParty4 = MobileParty.FindNextLocatable(ref locatableSearchData3))
										{
											if (mobileParty4 != mobileParty && mobileParty4.MapFaction == mobileParty.MapFaction && (mobileParty4.Army == null || mobileParty4.Army.LeaderParty == mobileParty4) && ((mobileParty4.DefaultBehavior == AiBehavior.GoAroundParty && mobileParty4.TargetParty == mobileParty3) || (mobileParty4.ShortTermBehavior == AiBehavior.EngageParty && mobileParty4.ShortTermTargetParty == mobileParty3)))
											{
												float num14 = num13;
												Army army = mobileParty4.Army;
												num13 = num14 + ((army != null) ? army.EstimatedStrength : mobileParty4.Party.EstimatedStrength);
											}
										}
										Army army2 = mobileParty.Army;
										float num15 = ((army2 != null) ? army2.EstimatedStrength : mobileParty.Party.EstimatedStrength);
										float num16 = (num13 + num15) / estimatedStrength;
										float num17 = 0f;
										if (mobileParty.Army == null || mobileParty.Army.LeaderParty != mobileParty || num15 <= estimatedStrength * 2f)
										{
											float num18 = ((mobileParty3.CurrentSettlement != null && mobileParty3.CurrentSettlement.IsFortification && mobileParty3.CurrentSettlement.MapFaction != mobileParty.MapFaction) ? 0.25f : 1f);
											float num19 = 1f;
											if (num13 + (num15 + 30f) > estimatedStrength * 1.5f)
											{
												float num20 = estimatedStrength * 1.5f + 10f + ((mobileParty3.MapEvent != null || mobileParty3.SiegeEvent != null) ? 30f : 0f);
												float num21 = num13 + (num15 + 30f);
												num19 = MathF.Pow(num20 / num21, 0.8f);
											}
											float lastCalculatedSpeed = mobileParty._lastCalculatedSpeed;
											float lastCalculatedSpeed2 = mobileParty3._lastCalculatedSpeed;
											float num22 = lastCalculatedSpeed / lastCalculatedSpeed2;
											float num23 = num22 * num22 * num22 * num22;
											float num24 = ((lastCalculatedSpeed > lastCalculatedSpeed2 && mobileParty.Army == null) ? 1f : ((num13 + num15 > estimatedStrength) ? (0.5f + 0.5f * num23 * num19) : (0.5f * num23)));
											float num25 = ((mobileParty.DefaultBehavior == AiBehavior.GoAroundParty && mobileParty3 == mobileParty.TargetParty) ? 1.1f : 1f);
											float num26 = ((mobileParty.Army != null) ? 0.9f : 1f);
											float num27 = ((mobileParty3 == MobileParty.MainParty) ? 1.2f : 1f);
											float num28 = 1f;
											if (mobileParty.Objective == MobileParty.PartyObjective.Defensive)
											{
												num28 = 1.2f;
											}
											float num29 = 1f;
											if (mobileParty.MapFaction != null && mobileParty.MapFaction.IsKingdomFaction && mobileParty.MapFaction.Leader == Hero.MainHero)
											{
												StanceLink stanceWith = Hero.MainHero.MapFaction.GetStanceWith(mobileParty3.MapFaction);
												if (stanceWith != null && stanceWith.BehaviorPriority == 1)
												{
													num29 = 1.2f;
												}
											}
											num17 = num11 * num2 * num12 * num16 * num27 * num9 * num24 * num19 * num18 * num25 * num26 * num28 * num29 * 2f;
										}
										if (num17 > 0.05f && mobileParty3.CurrentSettlement == null)
										{
											float averageDistanceBetweenClosestTwoTownsWithNavigationType2 = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(mobileParty.NavigationCapability);
											float num30 = Campaign.MapDiagonalSquared;
											LocatableSearchData<Settlement> locatableSearchData4 = Settlement.StartFindingLocatablesAroundPosition(mobileParty3.Position.ToVec2(), averageDistanceBetweenClosestTwoTownsWithNavigationType2 * 0.38f);
											for (Settlement settlement2 = Settlement.FindNextLocatable(ref locatableSearchData4); settlement2 != null; settlement2 = Settlement.FindNextLocatable(ref locatableSearchData4))
											{
												if (settlement2.MapFaction == mobileParty3.MapFaction)
												{
													float num31 = settlement2.Position.DistanceSquared(mobileParty.Position);
													if (num31 < num30)
													{
														num30 = num31;
													}
												}
											}
											if (num30 < averageDistanceBetweenClosestTwoTownsWithNavigationType2 * 9.6f)
											{
												float num32 = MathF.Sqrt(num30);
												num17 *= 0.25f + 0.75f * (MathF.Max(0f, num32 - 5f) / 20f);
												if (!mobileParty.IsDisbanding)
												{
													IDisbandPartyCampaignBehavior disbandPartyCampaignBehavior = this._disbandPartyCampaignBehavior;
													if (disbandPartyCampaignBehavior == null || !disbandPartyCampaignBehavior.IsPartyWaitingForDisband(mobileParty))
													{
														goto IL_857;
													}
												}
												num17 *= 0.25f;
											}
										}
										IL_857:
										p.CurrentObjectiveValue = num17;
										AiBehavior aiBehavior = AiBehavior.GoAroundParty;
										AIBehaviorData item = new AIBehaviorData(mobileParty3, aiBehavior, navigationType, false, flag2, false);
										ValueTuple<AIBehaviorData, float> valueTuple = new ValueTuple<AIBehaviorData, float>(item, num17);
										p.AddBehaviorScore(valueTuple);
									}
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x040013CB RID: 5067
		private IDisbandPartyCampaignBehavior _disbandPartyCampaignBehavior;
	}
}
