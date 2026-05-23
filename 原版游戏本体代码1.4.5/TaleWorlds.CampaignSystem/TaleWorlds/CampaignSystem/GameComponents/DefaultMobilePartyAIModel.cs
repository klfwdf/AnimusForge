using System;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMobilePartyAIModel : MobilePartyAIModel
{
	public override float AiCheckInterval => 0.25f;

	public override float FleeToNearbyPartyRadius => Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * Campaign.Current.EstimatedMaximumLordPartySpeedExceptPlayer * AiCheckInterval * 1.5f;

	public override float FleeToNearbySettlementRadius => FleeToNearbyPartyRadius * 2f;

	public override float HideoutPatrolDistanceAsDays => 0.5f;

	public override float FortificationPatrolDistanceAsDays => 0.5f;

	public override float FortificationPortPatrolDistanceAsDays => 0f;

	public override float VillagePatrolDistanceAsDays => 0.25f;

	public override float SettlementDefendingNearbyPartyCheckRadius => SettlementDefendingWaitingPositionRadius * 3f;

	public override float SettlementDefendingWaitingPositionRadius => 3f;

	public override float NeededFoodsInDaysThresholdForSiege => 12f;

	public override float NeededFoodsInDaysThresholdForRaid => 8f;

	public override bool ShouldConsiderAttacking(MobileParty party, MobileParty targetParty)
	{
		bool num = targetParty != MobileParty.MainParty || !MobileParty.MainParty.ShouldBeIgnored;
		bool flag = targetParty != MobileParty.MainParty || party.Ai.DoNotAttackMainPartyUntil.IsPast;
		bool flag2 = party.IsCurrentlyAtSea == targetParty.IsCurrentlyAtSea;
		bool flag3 = party.CurrentSettlement != null && party.CurrentSettlement.IsFortification && party.CurrentSettlement.HasPort && party.HasNavalNavigationCapability;
		if (num && flag && (flag2 || flag3))
		{
			return MobilePartyHelper.CanPartyAttackWithCurrentMorale(party);
		}
		return false;
	}

	public override bool ShouldConsiderAvoiding(MobileParty party, MobileParty targetParty)
	{
		if (targetParty.SiegeEvent != null && targetParty.SiegeEvent.BesiegedSettlement.HasPort && !targetParty.SiegeEvent.IsBlockadeActive && party.IsTargetingPort)
		{
			return false;
		}
		if (!targetParty.IsMainParty && !MobilePartyHelper.CanPartyAttackWithCurrentMorale(targetParty))
		{
			return false;
		}
		if (!(targetParty.Aggressiveness > 0.01f) || targetParty.IsInRaftState)
		{
			return targetParty.IsGarrison;
		}
		return true;
	}

	public override float GetPatrolRadius(MobileParty mobileParty, CampaignVec2 patrolPoint)
	{
		float num = 0f;
		if (mobileParty.TargetSettlement != null)
		{
			num = GetPatrolRadiusInternal(mobileParty.TargetSettlement, mobileParty._lastCalculatedSpeed, !patrolPoint.IsOnLand);
			if (mobileParty.IsPatrolParty)
			{
				num *= 0.5f;
			}
		}
		return num;
	}

	public override float GetSettlementNearbyThreatAndAllyCheckRadius(Settlement settlement, bool isPort)
	{
		float num = (isPort ? Campaign.Current.EstimatedAverageLordPartyNavalSpeed : Campaign.Current.EstimatedAverageLordPartySpeed);
		return GetPatrolRadiusInternal(settlement, num * 1.3f, isPort);
	}

	public override bool ShouldPartyCheckInitiativeBehavior(MobileParty mobileParty)
	{
		if (mobileParty.CurrentSettlement != null && (mobileParty.IsGarrison || mobileParty.IsMilitia || mobileParty.IsBandit || (mobileParty.IsLordParty && mobileParty.LeaderHero == null)))
		{
			return false;
		}
		if (mobileParty != MobileParty.MainParty && mobileParty.BesiegedSettlement == null)
		{
			if (mobileParty.Army != null)
			{
				return !mobileParty.Army.LeaderParty.AttachedParties.Contains(mobileParty);
			}
			return true;
		}
		return false;
	}

	public override void GetBestInitiativeBehavior(MobileParty mobileParty, out AiBehavior bestInitiativeBehavior, out MobileParty bestInitiativeTargetParty, out float bestInitiativeBehaviorScore, out Vec2 averageEnemyVec)
	{
		MobilePartyAi.DangerousPartiesAndTheirVecs.Clear();
		bestInitiativeBehaviorScore = 0f;
		bestInitiativeTargetParty = null;
		bestInitiativeBehavior = AiBehavior.None;
		averageEnemyVec = Vec2.Zero;
		float getEncounterJoiningRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
		float num = 2f;
		if (mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && !mobileParty.AiBehaviorTarget.IsOnLand && mobileParty.IsCurrentlyAtSea)
		{
			num *= 2f;
		}
		float radius = getEncounterJoiningRadius * (num + 1f);
		CampaignVec2 position = mobileParty.Position;
		if (mobileParty.CurrentSettlement != null)
		{
			position = mobileParty.CurrentSettlement.Position;
		}
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(position.ToVec2(), radius);
		MobileParty mobileParty2 = MobileParty.FindNextLocatable(ref data);
		while (mobileParty2 != null)
		{
			if (mobileParty2.MapEvent != null && MobileParty.MainParty.MapEvent == mobileParty2.MapEvent && (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && mobileParty2 != MobileParty.MainParty)
			{
				mobileParty2 = MobileParty.FindNextLocatable(ref data);
				continue;
			}
			if (!mobileParty2.IsGarrison)
			{
				if ((mobileParty.CurrentSettlement == null || !mobileParty.CurrentSettlement.HasPort) && mobileParty.IsCurrentlyAtSea != mobileParty2.IsCurrentlyAtSea)
				{
					mobileParty2 = MobileParty.FindNextLocatable(ref data);
					continue;
				}
				if ((mobileParty2.IsCurrentlyAtSea && !mobileParty.HasNavalNavigationCapability) || (!mobileParty2.IsCurrentlyAtSea && !mobileParty.HasLandNavigationCapability))
				{
					mobileParty2 = MobileParty.FindNextLocatable(ref data);
					continue;
				}
			}
			if (mobileParty.IsLordParty && mobileParty2.IsBandit && mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && (mobileParty.TargetPosition.IsOnLand == mobileParty.IsCurrentlyAtSea || mobileParty.IsTransitionInProgress))
			{
				mobileParty2 = MobileParty.FindNextLocatable(ref data);
				continue;
			}
			if (mobileParty2 == mobileParty || !mobileParty2.IsActive || !IsEnemy(mobileParty2.Party, mobileParty) || mobileParty2.ShouldBeIgnored || (mobileParty2.CurrentSettlement != null && !mobileParty2.IsGarrison && !mobileParty2.IsLordParty) || mobileParty.CurrentSettlement?.SiegeEvent != null || (mobileParty2.IsGarrison && !mobileParty.IsBandit) || (mobileParty2.BesiegerCamp != null && mobileParty2.BesiegerCamp.LeaderParty != mobileParty2) || (mobileParty2.Army != null && mobileParty2.Army.LeaderParty != mobileParty2 && mobileParty2.AttachedTo != null) || (mobileParty2.MapEvent != null && mobileParty2 != MobileParty.MainParty && mobileParty2.Party.MapEvent.MapEventSettlement == null && mobileParty2.Party != mobileParty2.Party.MapEvent.GetLeaderParty(BattleSideEnum.Attacker) && mobileParty2.Party != mobileParty2.Party.MapEvent.GetLeaderParty(BattleSideEnum.Defender)) || (mobileParty2.MapEvent != null && IsEnemy(mobileParty2.MapEvent.AttackerSide.LeaderParty, mobileParty) == IsEnemy(mobileParty2.MapEvent.DefenderSide.LeaderParty, mobileParty)) || (mobileParty2.CurrentSettlement != null && mobileParty2.CurrentSettlement.IsHideout && mobileParty.IsBandit))
			{
				mobileParty2 = MobileParty.FindNextLocatable(ref data);
				continue;
			}
			if (mobileParty.Army != null && mobileParty.AttachedTo == null && mobileParty.Army.LeaderParty != mobileParty && mobileParty2.MapEvent != null && mobileParty2.MapEventSide.OtherSide.LeaderParty.IsMobile && mobileParty2.MapEventSide.OtherSide.LeaderParty.MobileParty.Army != null && mobileParty2.MapEventSide.OtherSide.LeaderParty.MobileParty.Army == mobileParty.Army)
			{
				mobileParty2 = MobileParty.FindNextLocatable(ref data);
				continue;
			}
			CampaignVec2 v;
			if (mobileParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty.IsCurrentlyAtSea && mobileParty.IsTargetingPort)
			{
				v = mobileParty.TargetSettlement.PortPosition;
				num = Campaign.Current.Models.MobilePartyAIModel.SettlementDefendingNearbyPartyCheckRadius;
			}
			else
			{
				v = mobileParty2.Position;
				if (!DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(mobileParty, mobileParty2, mobileParty.NavigationCapability, getEncounterJoiningRadius * num * 10f, out var _, out var _))
				{
					mobileParty2 = MobileParty.FindNextLocatable(ref data);
					continue;
				}
			}
			float num2 = mobileParty.Position.Distance(v);
			if (num2 >= getEncounterJoiningRadius * num * 3f)
			{
				mobileParty2 = MobileParty.FindNextLocatable(ref data);
				continue;
			}
			if (bestInitiativeTargetParty != null && mobileParty.IsLordParty && !mobileParty2.IsLordParty && bestInitiativeBehavior == AiBehavior.EngageParty && bestInitiativeTargetParty.IsLordParty)
			{
				mobileParty2 = MobileParty.FindNextLocatable(ref data);
				continue;
			}
			if (mobileParty2.SiegeEvent != null && mobileParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty.TargetSettlement == mobileParty2.SiegeEvent.BesiegedSettlement && mobileParty2.MapEvent != null && mobileParty2.MapEvent.IsBlockade && !mobileParty.IsCurrentlyAtSea)
			{
				mobileParty2 = MobileParty.FindNextLocatable(ref data);
				continue;
			}
			if (mobileParty.Army != null && mobileParty.AttachedTo == null)
			{
				if (mobileParty.Army.LeaderParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty2.SiegeEvent != null && mobileParty2.SiegeEvent.BesiegedSettlement == mobileParty.Army.LeaderParty.TargetSettlement)
				{
					mobileParty2 = MobileParty.FindNextLocatable(ref data);
					continue;
				}
				if (mobileParty2.IsBandit)
				{
					mobileParty2 = MobileParty.FindNextLocatable(ref data);
					continue;
				}
				MobileParty mobileParty3 = mobileParty2.AttachedTo ?? mobileParty2;
				if (mobileParty.Army.LeaderParty != mobileParty && ((mobileParty3.IsFleeing() && mobileParty3.ShortTermTargetParty == mobileParty.Army.LeaderParty) || (mobileParty3.Army?.EstimatedStrength ?? mobileParty3.Party.EstimatedStrength) < mobileParty.Army.EstimatedStrength))
				{
					mobileParty2 = MobileParty.FindNextLocatable(ref data);
					continue;
				}
			}
			float num3 = 1f + TaleWorlds.Library.MathF.Max(0f, (num2 - 1f) / ((getEncounterJoiningRadius - 1f) * 2f));
			num3 = ((num3 > num) ? num : num3);
			float num4 = ((mobileParty.Army != null && (mobileParty.AttachedTo != null || mobileParty.Army.LeaderParty == mobileParty)) ? mobileParty.Army.EstimatedStrength : mobileParty.Party.EstimatedStrength) + 0.01f;
			if (mobileParty2.IsCurrentlyAtSea != mobileParty.IsCurrentlyAtSea)
			{
				num4 = ((mobileParty.Army != null && (mobileParty.AttachedTo != null || mobileParty.Army.LeaderParty == mobileParty)) ? mobileParty.Army.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.SeaBattle) : mobileParty.Party.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.SeaBattle)) + 0.01f;
			}
			float aggressiveness = mobileParty.Aggressiveness;
			float num5 = 0f;
			float num6 = 0.01f;
			if (mobileParty2.BesiegerCamp != null)
			{
				bool isCurrentlyAtSea = mobileParty.IsCurrentlyAtSea;
				MapEvent.PowerCalculationContext context = (isCurrentlyAtSea ? MapEvent.PowerCalculationContext.SeaBattle : Campaign.Current.Models.MilitaryPowerModel.GetContextForPosition(mobileParty2.SiegeEvent.BesiegerCamp.LeaderParty.Position));
				foreach (PartyBase item in mobileParty2.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType(isCurrentlyAtSea ? MapEvent.BattleTypes.BlockadeBattle : MapEvent.BattleTypes.Siege))
				{
					num6 += item.GetCustomStrength(BattleSideEnum.Defender, context);
				}
			}
			else if (mobileParty2.CurrentSettlement == null || !mobileParty2.CurrentSettlement.IsUnderSiege)
			{
				num6 += ((mobileParty2.Army != null && (mobileParty2.AttachedTo != null || mobileParty2.Army.LeaderParty == mobileParty2)) ? mobileParty2.Army.EstimatedStrength : mobileParty2.Party.EstimatedStrength);
			}
			bool flag = false;
			LocatableSearchData<MobileParty> data2 = MobileParty.StartFindingLocatablesAroundPosition(mobileParty.Position.ToVec2(), getEncounterJoiningRadius * (num + 1f));
			MobileParty mobileParty4 = MobileParty.FindNextLocatable(ref data2);
			float num7 = 0f;
			while (mobileParty4 != null)
			{
				if ((mobileParty.MapFaction == mobileParty4.MapFaction && mobileParty4.BesiegedSettlement != null) || (mobileParty4.MapEvent != null && mobileParty4.MapEvent != mobileParty2.MapEvent))
				{
					mobileParty4 = MobileParty.FindNextLocatable(ref data2);
					continue;
				}
				if (mobileParty4.AttachedTo != null)
				{
					mobileParty4 = MobileParty.FindNextLocatable(ref data2);
					continue;
				}
				if (mobileParty4.IsCurrentlyAtSea != mobileParty.IsCurrentlyAtSea)
				{
					mobileParty4 = MobileParty.FindNextLocatable(ref data2);
					continue;
				}
				if (mobileParty4.IsInRaftState)
				{
					mobileParty4 = MobileParty.FindNextLocatable(ref data2);
					continue;
				}
				if (mobileParty4.CurrentSettlement != null && mobileParty4.CurrentSettlement.SiegeEvent != null)
				{
					mobileParty4 = MobileParty.FindNextLocatable(ref data2);
					continue;
				}
				if (mobileParty4.ShortTermBehavior == AiBehavior.EngageParty && mobileParty4.ShortTermTargetParty == mobileParty && mobileParty4.MapFaction != mobileParty2.MapFaction)
				{
					flag = true;
					break;
				}
				if (mobileParty4 != mobileParty && mobileParty4 != mobileParty2)
				{
					Vec2 v2 = ((mobileParty4.BesiegedSettlement != null) ? mobileParty4.VisualPosition2DWithoutError : mobileParty4.Position.ToVec2());
					float num8 = ((mobileParty4 != mobileParty2) ? v2.Distance(v.ToVec2()) : mobileParty.Position.Distance(v2));
					if (num8 > num * getEncounterJoiningRadius)
					{
						mobileParty4 = MobileParty.FindNextLocatable(ref data2);
						continue;
					}
					if (mobileParty4.BesiegerCamp != null && mobileParty4.BesiegerCamp.LeaderParty != mobileParty4)
					{
						mobileParty4 = MobileParty.FindNextLocatable(ref data2);
						continue;
					}
					if (mobileParty4.IsGarrison || mobileParty4.IsMilitia)
					{
						mobileParty4 = MobileParty.FindNextLocatable(ref data2);
						continue;
					}
					PartyBase aiBehaviorPartyBase = mobileParty4.Ai.AiBehaviorPartyBase;
					if (mobileParty4.Army != null)
					{
						aiBehaviorPartyBase = mobileParty4.Army.LeaderParty.Ai.AiBehaviorPartyBase;
					}
					bool flag2 = aiBehaviorPartyBase != null && (aiBehaviorPartyBase == mobileParty2.Party || (aiBehaviorPartyBase.MapEvent != null && aiBehaviorPartyBase.MapEvent == mobileParty2.Party.MapEvent));
					bool flag3 = (mobileParty.Army != null && mobileParty.Army == mobileParty4.Army && mobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(mobileParty)) || (mobileParty2.Army != null && mobileParty2.Army == mobileParty4.Army) || (mobileParty2.BesiegedSettlement != null && mobileParty2.BesiegedSettlement == mobileParty4.BesiegedSettlement) || (num2 > getEncounterJoiningRadius && flag2) || (num8 > getEncounterJoiningRadius && flag2 && mobileParty2 != MobileParty.MainParty && (MobileParty.MainParty.Army == null || mobileParty2 != MobileParty.MainParty.Army.LeaderParty));
					if (flag3 || num8 < getEncounterJoiningRadius * num3)
					{
						float b = (flag3 ? 1f : ((num8 < getEncounterJoiningRadius) ? 1f : (1f - (num8 - getEncounterJoiningRadius) / (getEncounterJoiningRadius * (num3 - 1f)))));
						b = TaleWorlds.Library.MathF.Min(1f, b);
						bool flag4 = mobileParty2.MapEvent != null && mobileParty2.MapEvent == mobileParty4.MapEvent;
						float num9 = ((mobileParty4.Army != null && (mobileParty4.AttachedTo != null || mobileParty4.Army.LeaderParty == mobileParty4)) ? mobileParty4.Army.EstimatedStrength : mobileParty4.Party.EstimatedStrength);
						if (mobileParty4.IsGarrison && !mobileParty.IsLordParty)
						{
							num7 += TaleWorlds.Library.MathF.Max(mobileParty4.Party.EstimatedStrength, 250f);
						}
						if ((mobileParty4.Aggressiveness > 0.01f || mobileParty4.IsGarrison || flag4) && mobileParty4.MapFaction == mobileParty2.MapFaction)
						{
							if (mobileParty4.BesiegerCamp != null)
							{
								foreach (PartyBase item2 in mobileParty4.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType(mobileParty.IsCurrentlyAtSea ? MapEvent.BattleTypes.BlockadeBattle : MapEvent.BattleTypes.Siege))
								{
									bool flag5 = mobileParty.DefaultBehavior == AiBehavior.DefendSettlement && item2.SiegeEvent.BesiegedSettlement == mobileParty.TargetSettlement;
									num7 += item2.EstimatedStrength * (flag5 ? 0.2f : 1f);
								}
							}
							else
							{
								num7 += num9 * b;
							}
						}
						if (mobileParty.MapFaction == mobileParty4.MapFaction && !mobileParty4.IsMainParty)
						{
							bool flag6 = mobileParty4.Aggressiveness > 0.01f || (mobileParty4.CurrentSettlement != null && mobileParty4.CurrentSettlement == mobileParty2.CurrentSettlement);
							bool flag7 = mobileParty2 != MobileParty.MainParty || Campaign.Current.Models.MobilePartyAIModel.ShouldConsiderAttacking(mobileParty4, MobileParty.MainParty);
							bool flag8 = mobileParty4.CurrentSettlement == null || !mobileParty4.CurrentSettlement.IsHideout;
							if ((flag4 || (flag6 && flag7 && flag8)) && (mobileParty4.CurrentSettlement?.SiegeEvent == null || mobileParty2 != mobileParty4.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty))
							{
								if (mobileParty4.BesiegerCamp != null)
								{
									foreach (PartyBase item3 in mobileParty4.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType())
									{
										num4 += item3.EstimatedStrength;
										if (item3.MobileParty.Aggressiveness > aggressiveness)
										{
											aggressiveness = item3.MobileParty.Aggressiveness;
										}
									}
								}
								else
								{
									num4 += num9 * b;
									if (mobileParty4.Aggressiveness > aggressiveness)
									{
										aggressiveness = mobileParty4.Aggressiveness;
									}
									if (mobileParty4.CurrentSettlement != null)
									{
										num5 += num9 * b;
									}
								}
							}
						}
					}
				}
				mobileParty4 = MobileParty.FindNextLocatable(ref data2);
			}
			num6 += num7 * 0.9f;
			if (mobileParty.CurrentSettlement != null)
			{
				num4 -= num5;
			}
			if (mobileParty2.LastVisitedSettlement != null && mobileParty2.LastVisitedSettlement.IsVillage && mobileParty2.Position.DistanceSquared(mobileParty2.LastVisitedSettlement.Position) < 1f && mobileParty2.LastVisitedSettlement.MapFaction.IsAtWarWith(mobileParty.MapFaction))
			{
				num6 += 20f;
			}
			float num10 = num4 / num6;
			num10 *= (((mobileParty.IsCaravan || mobileParty.IsVillager) && mobileParty2 == MobileParty.MainParty) ? 0.6f : 1f);
			num10 *= ((!mobileParty.IsPatrolParty) ? 1f : (mobileParty2.IsBandit ? 1.2f : (mobileParty2.IsLordParty ? 0.9f : (mobileParty2.IsPatrolParty ? 0.8f : 1f))));
			if (mobileParty2.IsCaravan && mobileParty.LeaderHero != null && mobileParty.LeaderHero.IsMinorFactionHero)
			{
				num10 *= 1.5f;
			}
			if (mobileParty2.MapEvent != null && mobileParty2.MapEvent.IsSiegeAssault && mobileParty2 == mobileParty2.MapEvent.AttackerSide.LeaderParty.MobileParty)
			{
				float settlementAdvantage = Campaign.Current.Models.CombatSimulationModel.GetSettlementAdvantage(mobileParty2.MapEvent.MapEventSettlement);
				if (num5 * TaleWorlds.Library.MathF.Sqrt(settlementAdvantage) > num6)
				{
					mobileParty2 = MobileParty.FindNextLocatable(ref data);
					continue;
				}
			}
			CalculateInitiativeScoresForEnemy(mobileParty, mobileParty2, out var avoidScore, out var attackScore, num10, aggressiveness);
			if (flag)
			{
				attackScore = 0f;
			}
			if (mobileParty2.CurrentSettlement != null && mobileParty2.MapEvent == null)
			{
				attackScore = 0f;
			}
			if (num10 > 2f && mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && mobileParty2.AttachedParties.Count == 0 && !mobileParty.Army.IsWaitingForArmyMembers() && (mobileParty.DefaultBehavior != AiBehavior.GoAroundParty || mobileParty.TargetParty != mobileParty2))
			{
				attackScore = 0f;
				avoidScore = 0f;
			}
			if (avoidScore > 1f)
			{
				MobilePartyAi.DangerousPartiesAndTheirVecs.Add((avoidScore, (v.ToVec2() - mobileParty.Position.ToVec2()).Normalized()));
			}
			if (avoidScore > bestInitiativeBehaviorScore || (avoidScore * 0.75f > bestInitiativeBehaviorScore && bestInitiativeBehavior == AiBehavior.EngageParty))
			{
				bestInitiativeBehavior = AiBehavior.FleeToPoint;
				bestInitiativeTargetParty = mobileParty2;
				bestInitiativeBehaviorScore = avoidScore;
			}
			if (attackScore > bestInitiativeBehaviorScore && (bestInitiativeBehaviorScore < attackScore * 0.75f || bestInitiativeBehavior == AiBehavior.EngageParty))
			{
				bestInitiativeBehavior = AiBehavior.EngageParty;
				bestInitiativeTargetParty = mobileParty2;
				bestInitiativeBehaviorScore = attackScore;
			}
			mobileParty2 = MobileParty.FindNextLocatable(ref data);
		}
		if (bestInitiativeBehavior != AiBehavior.FleeToPoint && bestInitiativeBehavior != AiBehavior.FleeToGate)
		{
			return;
		}
		float num11 = 0f;
		for (int i = 0; i < 8; i++)
		{
			Vec2 vec = new Vec2(TaleWorlds.Library.MathF.Sin((float)i / 8f * System.MathF.PI * 2f), TaleWorlds.Library.MathF.Cos((float)i / 8f * System.MathF.PI * 2f));
			float num12 = 0f;
			for (int j = 0; j < MobilePartyAi.DangerousPartiesAndTheirVecs.Count; j++)
			{
				float num13 = MobilePartyAi.DangerousPartiesAndTheirVecs[j].Item2.DistanceSquared(vec);
				if (num13 > 1f)
				{
					num13 = 1f + (num13 - 1f) * 0.5f;
				}
				num12 += num13 * MobilePartyAi.DangerousPartiesAndTheirVecs[j].Item1;
			}
			if (num12 > num11)
			{
				averageEnemyVec = -vec;
				num11 = num12;
			}
		}
	}

	private bool IsEnemy(PartyBase party, MobileParty mobileParty)
	{
		return FactionManager.IsAtWarAgainstFaction(party.MapFaction, mobileParty.MapFaction);
	}

	private void CalculateInitiativeScoresForEnemy(MobileParty mobileParty, MobileParty enemyParty, out float avoidScore, out float attackScore, float localAdvantage, float maxAggressiveness)
	{
		attackScore = 0f;
		avoidScore = 0f;
		float num = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 1.2f;
		CampaignVec2 campaignVec = mobileParty.Position;
		if (!mobileParty.IsCurrentlyAtSea && enemyParty.IsCurrentlyAtSea)
		{
			campaignVec = mobileParty.CurrentSettlement.PortPosition;
		}
		float length = (enemyParty.Position.ToVec2() - campaignVec.ToVec2()).Length;
		float num2 = CalculateStanceScore(mobileParty, enemyParty);
		float num3 = MBMath.ClampFloat(0.5f * (1f + localAdvantage), 0.05f, 3f);
		float num4 = MBMath.ClampFloat((localAdvantage < 1f) ? MBMath.ClampFloat(1f / localAdvantage, 0.05f, 3f) : 0f, 0.05f, 3f);
		if (Campaign.Current.Models.MobilePartyAIModel.ShouldConsiderAttacking(mobileParty, enemyParty) && num3 > num4)
		{
			float initiativeDistanceForAttack = GetInitiativeDistanceForAttack(mobileParty, enemyParty, num);
			float num5 = 1f;
			float num6 = ((mobileParty.IsBandit && mobileParty.HasNavalNavigationCapability) ? 10f : 5f);
			if (mobileParty.IsCurrentlyAtSea && mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty)
			{
				num6 *= 0.5f;
			}
			float num7 = (mobileParty.IsCurrentlyAtSea ? Campaign.Current.Models.EncounterModel.NeededMaximumNavalDistanceForEncounteringMobileParty : Campaign.Current.Models.EncounterModel.NeededMaximumLandDistanceForEncounteringMobileParty);
			if (length < num7 * num6 || (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && enemyParty.Army != null && enemyParty.Army.LeaderParty == enemyParty && initiativeDistanceForAttack * 2f > length))
			{
				num5 = 100f;
			}
			else if (enemyParty.IsMoving && enemyParty.SiegeEvent == null && enemyParty.MapEvent == null)
			{
				float num8 = mobileParty.LastCalculatedBaseSpeed - enemyParty.LastCalculatedBaseSpeed;
				if (num8 > 0.01f)
				{
					float num9 = initiativeDistanceForAttack / num8;
					float num10 = (float)CampaignTime.HoursInDay * 0.75f;
					if (num9 < num10)
					{
						num5 = num10 / num9;
					}
				}
				else
				{
					num5 = 0f;
				}
			}
			float num11 = ((enemyParty.IsLordParty && enemyParty.LeaderHero != null && enemyParty.LeaderHero.IsLord) ? 1f : mobileParty.Ai.AttackInitiative);
			if ((double)mobileParty.Aggressiveness < 0.01)
			{
				maxAggressiveness = mobileParty.Aggressiveness;
			}
			float num12 = ((enemyParty.MapEvent != null && maxAggressiveness > 0.1f) ? TaleWorlds.Library.MathF.Max(1f + (enemyParty.MapEvent.IsSallyOut ? 0.3f : 0f), maxAggressiveness) : maxAggressiveness);
			float num13 = ((mobileParty.DefaultBehavior == AiBehavior.DefendSettlement && ((enemyParty.BesiegedSettlement != null && mobileParty.Ai.AiBehaviorPartyBase == enemyParty.BesiegedSettlement.Party) || (enemyParty.MapEvent != null && enemyParty.MapEvent.MapEventSettlement != null && mobileParty.Ai.AiBehaviorPartyBase == enemyParty.MapEvent.MapEventSettlement.Party))) ? 1.1f : 1f);
			float num14 = 1f;
			if (mobileParty.IsLordParty && mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && num3 * 0.8f > num4)
			{
				MobileParty.NavigationType navigationType = ((!mobileParty.HasNavalNavigationCapability) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.All);
				num14 += 0.2f * (Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(navigationType) * 0.5f) / mobileParty.AiBehaviorTarget.Distance(mobileParty.Position);
			}
			float num15 = ((enemyParty.MapEvent != null && enemyParty.MapEventSide.OtherSide.Parties.ContainsQ((MapEventParty x) => x.Party.IsMobile && x.Party.MapFaction == mobileParty.MapFaction)) ? 1.2f : 1f);
			attackScore = 1.06f * num13 * num3 * num2 * num5 * num12 * num14 * num11 * num15;
		}
		if (!(attackScore < 1f))
		{
			return;
		}
		if (enemyParty.IsGarrison)
		{
			attackScore = 0f;
			if (enemyParty == mobileParty.ShortTermTargetParty)
			{
				mobileParty.RecalculateShortTermBehavior();
			}
		}
		if (Campaign.Current.Models.MobilePartyAIModel.ShouldConsiderAvoiding(mobileParty, enemyParty))
		{
			float num16 = ((mobileParty.IsCaravan || mobileParty.IsVillager) ? 0.9f : ((enemyParty.IsGarrison || enemyParty.IsMilitia || enemyParty.CurrentSettlement != null) ? 0.4f : 0.7f));
			float num17 = num * num16;
			if (enemyParty.MapEvent != null || enemyParty.BesiegedSettlement != null || (mobileParty.DefaultBehavior == AiBehavior.EngageParty && mobileParty.TargetParty == enemyParty) || (mobileParty.DefaultBehavior == AiBehavior.GoAroundParty && mobileParty.TargetParty == enemyParty))
			{
				num17 = num * 0.6f;
			}
			num17 *= (1f + mobileParty.Ai.AvoidInitiative) / 2f;
			float num18 = 1f;
			if (length < num17 * 4f)
			{
				float num19 = length / (num17 + 1E-05f);
				num18 = 4f - num19;
			}
			float num20 = ((enemyParty.IsLordParty && enemyParty.LeaderHero != null && enemyParty.LeaderHero.IsLord) ? 1f : mobileParty.Ai.AvoidInitiative);
			avoidScore = 0.9433963f * num20 * num18 * ((num2 > 0.01f) ? 1f : 0f) * num4;
		}
	}

	private float GetInitiativeDistanceForAttack(MobileParty mobileParty, MobileParty enemyParty, float reasonableDistance)
	{
		float num = 1f;
		if (enemyParty.IsCaravan)
		{
			num = (mobileParty.IsBandit ? 2f : ((mobileParty.Army == null) ? 1.5f : 1f));
		}
		else if (enemyParty.Aggressiveness < 0.1f)
		{
			num = 0.7f;
		}
		else if (enemyParty.IsBandit || enemyParty.IsLordParty)
		{
			num = ((mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint) ? 3.5f : 1f);
		}
		else if ((mobileParty.DefaultBehavior == AiBehavior.GoAroundParty || mobileParty.ShortTermBehavior == AiBehavior.GoAroundParty) && enemyParty != mobileParty.TargetParty)
		{
			num = 0.7f;
		}
		if (enemyParty.MapEvent == null && mobileParty._lastCalculatedSpeed < enemyParty._lastCalculatedSpeed * 1.1f && (mobileParty.DefaultBehavior != AiBehavior.GoAroundParty || mobileParty.TargetParty != enemyParty) && (mobileParty.DefaultBehavior != AiBehavior.DefendSettlement || enemyParty != mobileParty.TargetSettlement.LastAttackerParty))
		{
			float b = TaleWorlds.Library.MathF.Max(0.5f, (mobileParty._lastCalculatedSpeed + 0.1f) / (enemyParty._lastCalculatedSpeed + 0.1f)) / 1.1f;
			num *= TaleWorlds.Library.MathF.Max(0.8f, b) * TaleWorlds.Library.MathF.Max(0.8f, b);
		}
		float num2 = reasonableDistance * num;
		num2 *= (1f + ((mobileParty.Army != null && mobileParty.Army.LeaderParty != null && (enemyParty.BesiegedSettlement == mobileParty.Army.LeaderParty.TargetSettlement || (mobileParty.Army.LeaderParty.TargetSettlement != null && enemyParty == mobileParty.Army.LeaderParty.TargetSettlement.LastAttackerParty))) ? 1f : mobileParty.Ai.AttackInitiative)) / 2f;
		num2 *= ((enemyParty.Army != null) ? TaleWorlds.Library.MathF.Pow(enemyParty.Army.Parties.Count, 0.33f) : 1f);
		if (enemyParty.MapEvent != null || enemyParty.BesiegedSettlement != null || (mobileParty.DefaultBehavior == AiBehavior.EngageParty && mobileParty.TargetParty == enemyParty) || (mobileParty.DefaultBehavior == AiBehavior.GoAroundParty && mobileParty.TargetParty == enemyParty))
		{
			num2 = reasonableDistance * 1.5f;
		}
		return num2;
	}

	private float CalculateStanceScore(MobileParty mobileParty, MobileParty otherParty)
	{
		if (FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, otherParty.MapFaction))
		{
			return 1f;
		}
		if (DiplomacyHelper.IsSameFactionAndNotEliminated(mobileParty.MapFaction, otherParty.MapFaction))
		{
			return -1f;
		}
		return 0f;
	}

	private float GetPatrolRadiusInternal(Settlement settlement, float speed, bool isPort)
	{
		if (settlement.IsHideout)
		{
			return Campaign.Current.Models.MobilePartyAIModel.HideoutPatrolDistanceAsDays * speed * (float)CampaignTime.HoursInDay;
		}
		if (settlement.IsFortification)
		{
			return (isPort ? Campaign.Current.Models.MobilePartyAIModel.FortificationPortPatrolDistanceAsDays : Campaign.Current.Models.MobilePartyAIModel.FortificationPatrolDistanceAsDays) * speed * (float)CampaignTime.HoursInDay;
		}
		if (settlement.IsVillage)
		{
			return Campaign.Current.Models.MobilePartyAIModel.VillagePatrolDistanceAsDays * speed * (float)CampaignTime.HoursInDay;
		}
		return 0f;
	}
}
