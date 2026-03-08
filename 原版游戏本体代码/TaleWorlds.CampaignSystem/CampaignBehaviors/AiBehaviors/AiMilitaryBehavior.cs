using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors
{
	// Token: 0x0200046E RID: 1134
	public class AiMilitaryBehavior : CampaignBehaviorBase
	{
		// Token: 0x060047BF RID: 18367 RVA: 0x00167FF8 File Offset: 0x001661F8
		public override void RegisterEvents()
		{
			CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(this.AiHourlyTick));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventStarted));
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
		}

		// Token: 0x060047C0 RID: 18368 RVA: 0x00168078 File Offset: 0x00166278
		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (mapEvent.MapEventSettlement != null && mapEvent.MapEventSettlement.IsFortification && mapEvent.MapEventSettlement.HasPort && mapEvent.MapEventSettlement.SiegeEvent != null && mapEvent.MapEventSettlement.SiegeEvent.IsBlockadeActive)
			{
				bool isNavalMapEvent = mapEvent.IsNavalMapEvent;
				foreach (MobileParty mobileParty in MobileParty.AllLordParties)
				{
					bool flag = mobileParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty.TargetSettlement == mapEvent.MapEventSettlement;
					if (((mobileParty.ShortTermBehavior == AiBehavior.EngageParty && mobileParty.ShortTermTargetParty.SiegeEvent != null && mobileParty.ShortTermTargetParty.MapFaction.IsAtWarWith(mobileParty.MapFaction)) || flag) && isNavalMapEvent != mobileParty.IsTargetingPort)
					{
						mobileParty.SetMoveModeHold();
					}
				}
			}
		}

		// Token: 0x060047C1 RID: 18369 RVA: 0x00168178 File Offset: 0x00166378
		private void OnSiegeEventStarted(SiegeEvent siegeEvent)
		{
			foreach (MobileParty mobileParty in MobileParty.All)
			{
				if (mobileParty.DefaultBehavior == AiBehavior.GoToSettlement && mobileParty.TargetSettlement == siegeEvent.BesiegedSettlement && mobileParty.CurrentSettlement != siegeEvent.BesiegedSettlement && !mobileParty.IsTargetingPort)
				{
					mobileParty.SetMoveModeHold();
				}
			}
		}

		// Token: 0x060047C2 RID: 18370 RVA: 0x001681F8 File Offset: 0x001663F8
		private void OnMapEventEnded(MapEvent mapEvent)
		{
			if (mapEvent.RetreatingSide != BattleSideEnum.None)
			{
				MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.RetreatingSide.GetOppositeSide());
				using (List<MapEventParty>.Enumerator enumerator = mapEvent.GetMapEventSide(mapEvent.RetreatingSide).Parties.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						MapEventParty mapEventParty = enumerator.Current;
						MobileParty mobileParty = mapEventParty.Party.MobileParty;
						if (mobileParty != null && mobileParty.AttachedTo == null)
						{
							mobileParty.TeleportPartyToOutSideOfEncounterRadius();
							CampaignVec2 point;
							mobileParty.Ai.CalculateFleePosition(out point, mapEventSide.LeaderParty.MobileParty, mapEventSide.LeaderParty.MobileParty.Position.ToVec2());
							mobileParty.SetMoveGoToPoint(point, mobileParty.IsCurrentlyAtSea ? MobileParty.NavigationType.Naval : MobileParty.NavigationType.Default);
						}
					}
					return;
				}
			}
			MobileParty mobileParty2 = mapEvent.AttackerSide.LeaderParty.MobileParty;
			bool flag = mapEvent.IsRaid && mapEvent.BattleState == BattleState.AttackerVictory && !mapEvent.MapEventSettlement.SettlementHitPoints.ApproximatelyEqualsTo(0f, 1E-05f);
			Settlement mapEventSettlement = mapEvent.MapEventSettlement;
			if (mobileParty2 != MobileParty.MainParty && flag)
			{
				mobileParty2.SetMoveRaidSettlement(mapEventSettlement, mobileParty2.NavigationCapability);
				mobileParty2.RecalculateShortTermBehavior();
			}
		}

		// Token: 0x060047C3 RID: 18371 RVA: 0x00168344 File Offset: 0x00166544
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this._disbandPartyCampaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
		}

		// Token: 0x060047C4 RID: 18372 RVA: 0x00168356 File Offset: 0x00166556
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060047C5 RID: 18373 RVA: 0x00168358 File Offset: 0x00166558
		public void FindBestTargetAndItsValueForFaction(Army.ArmyTypes missionType, PartyThinkParams p, float ourStrength, float newArmyCreatingAdditionalConstant = 1f)
		{
			MobileParty mobilePartyOf = p.MobilePartyOf;
			IFaction mapFaction = mobilePartyOf.MapFaction;
			if (mobilePartyOf.Army != null && mobilePartyOf.Army.LeaderParty != mobilePartyOf)
			{
				return;
			}
			if ((mobilePartyOf.Objective != MobileParty.PartyObjective.Defensive || (missionType != Army.ArmyTypes.Besieger && missionType != Army.ArmyTypes.Raider)) && (mobilePartyOf.Objective != MobileParty.PartyObjective.Aggressive || missionType != Army.ArmyTypes.Defender))
			{
				float num = 1f;
				if (mobilePartyOf.Army != null && mobilePartyOf.Army.Cohesion < 40f)
				{
					num *= mobilePartyOf.Army.Cohesion / 40f;
				}
				if (num > 0.25f)
				{
					float partySizeScore = this.GetPartySizeScore(mobilePartyOf, missionType);
					AiBehavior aiBehavior = AiBehavior.Hold;
					switch (missionType)
					{
					case Army.ArmyTypes.Besieger:
						aiBehavior = AiBehavior.BesiegeSettlement;
						break;
					case Army.ArmyTypes.Raider:
						aiBehavior = AiBehavior.RaidSettlement;
						break;
					case Army.ArmyTypes.Defender:
						aiBehavior = AiBehavior.DefendSettlement;
						break;
					}
					float foodScoreForActionType = this.GetFoodScoreForActionType(p, missionType);
					if (foodScoreForActionType > 0f)
					{
						if (missionType == Army.ArmyTypes.Defender)
						{
							this.CalculateMilitaryBehaviorForFactionSettlements(mapFaction, p, missionType, aiBehavior, ourStrength, partySizeScore, num, foodScoreForActionType, newArmyCreatingAdditionalConstant);
							return;
						}
						if (missionType != Army.ArmyTypes.Raider || (mobilePartyOf.Army == null && !p.WillGatherAnArmy))
						{
							for (int i = 0; i < mapFaction.FactionsAtWarWith.Count; i++)
							{
								IFaction faction = mapFaction.FactionsAtWarWith[i];
								if (faction.Leader != null && faction.IsMapFaction)
								{
									this.CalculateMilitaryBehaviorForFactionSettlements(faction, p, missionType, aiBehavior, ourStrength, partySizeScore, num, foodScoreForActionType, newArmyCreatingAdditionalConstant);
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x060047C6 RID: 18374 RVA: 0x001684AC File Offset: 0x001666AC
		private float GetFoodScoreForActionType(PartyThinkParams p, Army.ArmyTypes type)
		{
			float num = ((type == Army.ArmyTypes.Raider) ? Campaign.Current.Models.MobilePartyAIModel.NeededFoodsInDaysThresholdForRaid : Campaign.Current.Models.MobilePartyAIModel.NeededFoodsInDaysThresholdForSiege);
			MobileParty mobilePartyOf = p.MobilePartyOf;
			int num2 = mobilePartyOf.GetNumDaysForFoodToLast();
			if (p.WillGatherAnArmy)
			{
				foreach (MobileParty mobileParty in p.PossibleArmyMembersUponArmyCreation)
				{
					num2 += mobileParty.GetNumDaysForFoodToLast();
				}
				num2 /= p.PossibleArmyMembersUponArmyCreation.Count + 1;
			}
			else if (mobilePartyOf.Army != null && mobilePartyOf == mobilePartyOf.Army.LeaderParty)
			{
				foreach (MobileParty mobileParty2 in mobilePartyOf.Army.LeaderParty.AttachedParties)
				{
					num2 += mobileParty2.GetNumDaysForFoodToLast();
				}
				num2 /= mobilePartyOf.Army.LeaderParty.AttachedParties.Count + 1;
			}
			if ((p.WillGatherAnArmy || type == Army.ArmyTypes.Raider) && num > (float)num2)
			{
				return 0f;
			}
			if ((float)num2 >= num)
			{
				return 1f;
			}
			return 0.1f + 0.9f * ((float)num2 / num);
		}

		// Token: 0x060047C7 RID: 18375 RVA: 0x0016860C File Offset: 0x0016680C
		private float GetPartySizeScore(MobileParty mobileParty, Army.ArmyTypes missionType)
		{
			float x2;
			if (mobileParty.Army != null)
			{
				float num = 0f;
				foreach (MobileParty mobileParty2 in mobileParty.Army.Parties)
				{
					float num2 = PartyBaseHelper.FindPartySizeNormalLimit(mobileParty2);
					float num3 = mobileParty2.PartySizeRatio / num2;
					num += num3;
				}
				x2 = num / (float)mobileParty.Army.Parties.Count;
			}
			else
			{
				float num4 = PartyBaseHelper.FindPartySizeNormalLimit(mobileParty);
				x2 = mobileParty.PartySizeRatio / num4;
			}
			float num5 = MathF.Max(1f, MathF.Min((float)mobileParty.MapFaction.Fiefs.Count((Town x) => x.IsTown) / 5f, 2.5f));
			if (missionType == Army.ArmyTypes.Defender)
			{
				num5 = MathF.Pow(num5, 0.75f);
			}
			else if (missionType == Army.ArmyTypes.Raider)
			{
				num5 *= 0.75f;
			}
			return MathF.Min(1f, MathF.Pow(x2, num5));
		}

		// Token: 0x060047C8 RID: 18376 RVA: 0x0016872C File Offset: 0x0016692C
		private void CalculateMilitaryBehaviorForFactionSettlements(IFaction faction, PartyThinkParams p, Army.ArmyTypes missionType, AiBehavior aiBehavior, float ourStrength, float partySizeScore, float cohesionScore, float foodScore, float newArmyCreatingAdditionalConstant)
		{
			MobileParty mobilePartyOf = p.MobilePartyOf;
			for (int i = 0; i < faction.Settlements.Count; i++)
			{
				Settlement settlement = faction.Settlements[i];
				if (this.CheckIfSettlementIsSuitableForMilitaryAction(settlement, mobilePartyOf, missionType, p.WillGatherAnArmy))
				{
					this.CalculateMilitaryBehaviorForSettlement(settlement, missionType, aiBehavior, p, ourStrength, partySizeScore, cohesionScore, foodScore, newArmyCreatingAdditionalConstant);
				}
			}
		}

		// Token: 0x060047C9 RID: 18377 RVA: 0x0016878C File Offset: 0x0016698C
		private bool CheckIfSettlementIsSuitableForMilitaryAction(Settlement settlement, MobileParty mobileParty, Army.ArmyTypes missionType, bool isCalculatingForNewArmyCreation)
		{
			return (!MobileParty.MainParty.ShouldBeIgnored || mobileParty.IsMainParty || mobileParty.AttachedParties.Contains(MobileParty.MainParty) || ((settlement.Party.MapEvent == null || settlement.Party.MapEvent != MapEvent.PlayerMapEvent) && (settlement.SiegeEvent == null || !settlement.SiegeEvent.IsPlayerSiegeEvent))) && ((mobileParty.Army == null && !isCalculatingForNewArmyCreation) || missionType != Army.ArmyTypes.Defender || !settlement.IsVillage);
		}

		// Token: 0x060047CA RID: 18378 RVA: 0x00168814 File Offset: 0x00166A14
		private void CalculateDistanceScoreForBesieging(Settlement targetSettlement, MobileParty mobileParty, out MobileParty.NavigationType bestNavigationType, out float bestDistanceScore, out bool isFromPort, out bool isTargetingPort)
		{
			this._checkedNeighbors.Clear();
			float num = 0.01f;
			float num2 = 0.0001f;
			IFaction mapFaction = mobileParty.MapFaction;
			MBReadOnlyList<Settlement> neighborFortifications = targetSettlement.Town.GetNeighborFortifications(MobileParty.NavigationType.All);
			foreach (Settlement settlement in neighborFortifications)
			{
				this._checkedNeighbors.Add(settlement);
				if (settlement.MapFaction != targetSettlement.MapFaction)
				{
					num += 1f;
					if (settlement.MapFaction == mapFaction)
					{
						num2 += 1f;
					}
				}
			}
			float num3 = 0.01f;
			float num4 = 0.0001f;
			foreach (Settlement settlement2 in neighborFortifications)
			{
				foreach (Settlement settlement3 in settlement2.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
				{
					if (settlement3 != targetSettlement && !this._checkedNeighbors.Contains(settlement3))
					{
						this._checkedNeighbors.Add(settlement3);
						if (settlement3.MapFaction != targetSettlement.MapFaction)
						{
							num3 += 1f;
							if (settlement3.MapFaction == mapFaction)
							{
								num4 += 1f;
							}
						}
					}
				}
			}
			bestDistanceScore = 0f + num2 / num * 1f + num4 / num3 * 0.25f;
			if (bestDistanceScore < 0.1f)
			{
				bestDistanceScore = 0f;
			}
			isTargetingPort = false;
			float num5;
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort, out bestNavigationType, out num5, out isFromPort);
		}

		// Token: 0x060047CB RID: 18379 RVA: 0x001689E0 File Offset: 0x00166BE0
		private void GetDistanceScoreForRaiding(Settlement targetSettlement, MobileParty mobileParty, out MobileParty.NavigationType bestNavigationType, out float bestDistanceScore, out bool isFromPort, out bool isTargetingPort)
		{
			isTargetingPort = false;
			float maxValue;
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort, out bestNavigationType, out maxValue, out isFromPort);
			float num = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(mobileParty.NavigationCapability) * 3f;
			if (maxValue > num)
			{
				bestNavigationType = MobileParty.NavigationType.None;
				maxValue = float.MaxValue;
				isTargetingPort = false;
			}
			bestDistanceScore = MBMath.Map(0.75f - maxValue / num, 0f, 1f, 0.1f, 1f);
		}

		// Token: 0x060047CC RID: 18380 RVA: 0x00168A50 File Offset: 0x00166C50
		private void GetDistanceScoreForDefending(Settlement targetSettlement, MobileParty mobileParty, out MobileParty.NavigationType bestNavigationType, out float bestDistanceScore, out bool isFromPort, out bool isTargetingPort)
		{
			isTargetingPort = false;
			bool flag = targetSettlement.HasPort && mobileParty.HasNavalNavigationCapability;
			bool flag2 = flag && targetSettlement.SiegeEvent != null && (!targetSettlement.SiegeEvent.IsBlockadeActive || (targetSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent != null && (targetSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsBlockade || targetSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsBlockadeSallyOut)));
			if (flag2)
			{
				isTargetingPort = true;
			}
			float num;
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort, out bestNavigationType, out num, out isFromPort);
			if (!flag2 && flag)
			{
				AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, true, out bestNavigationType, out num, out isFromPort);
			}
			float num2 = ((bestNavigationType == MobileParty.NavigationType.Naval) ? Campaign.Current.EstimatedAverageLordPartyNavalSpeed : Campaign.Current.EstimatedAverageLordPartySpeed);
			if (bestNavigationType == MobileParty.NavigationType.All)
			{
				num2 = (Campaign.Current.EstimatedAverageLordPartyNavalSpeed + Campaign.Current.EstimatedAverageLordPartySpeed) * 0.5f;
			}
			float num3 = num / (num2 * (float)CampaignTime.HoursInDay);
			float num4 = 2.865f;
			if (targetSettlement.IsVillage)
			{
				MapEvent mapEvent = targetSettlement.Party.MapEvent;
				RaidEventComponent raidEventComponent;
				if (mapEvent != null && (raidEventComponent = mapEvent.Component as RaidEventComponent) != null && raidEventComponent.RaidDamage > 0f)
				{
					float num5 = raidEventComponent.RaidDamage / mapEvent.BattleStartTime.ElapsedDaysUntilNow;
					num4 = targetSettlement.SettlementHitPoints / num5;
				}
			}
			else if (targetSettlement.IsFortification && targetSettlement.Party.SiegeEvent != null)
			{
				num4 = 5.73f;
			}
			if (num3 >= num4)
			{
				bestNavigationType = MobileParty.NavigationType.None;
				bestDistanceScore = 0f;
				isTargetingPort = false;
			}
			else if (targetSettlement.Party.MapEventSide == null && targetSettlement.SiegeEvent != null && mobileParty.NavigationCapability == MobileParty.NavigationType.All)
			{
				bool flag3 = false;
				bool flag4 = mobileParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty.ShortTermTargetParty != null && !mobileParty.ShortTermTargetParty.MapFaction.IsAtWarWith(mobileParty.MapFaction);
				if (flag4)
				{
					flag3 = !mobileParty.ShortTermTargetParty.IsCurrentlyAtSea;
					bestNavigationType = MobileParty.NavigationType.All;
				}
				else
				{
					bool flag5;
					MobileParty mobileParty2;
					MobileParty mobileParty3;
					mobileParty.Ai.GetNearbyPartyDataWhileDefendingSettlement(targetSettlement, out flag4, out flag3, out flag5, out mobileParty2, out mobileParty3);
				}
				isTargetingPort = !flag3 && targetSettlement.HasPort;
			}
			bestDistanceScore = MBMath.Map(1f - num3 / (num4 + 0.01f), 0f, 1f, 0.1f, 1f);
		}

		// Token: 0x060047CD RID: 18381 RVA: 0x00168CB4 File Offset: 0x00166EB4
		private void CalculateMilitaryBehaviorForSettlement(Settlement settlement, Army.ArmyTypes missionType, AiBehavior aiBehavior, PartyThinkParams p, float ourStrength, float partySizeScore, float cohesionScore, float foodScore, float newArmyCreatingAdditionalConstant = 1f)
		{
			if ((missionType == Army.ArmyTypes.Defender && settlement.LastAttackerParty != null && settlement.LastAttackerParty.IsActive) || (missionType == Army.ArmyTypes.Raider && settlement.IsVillage && settlement.Village.VillageState != Village.VillageStates.Looted) || (missionType == Army.ArmyTypes.Besieger && settlement.IsFortification && (settlement.SiegeEvent == null || settlement.SiegeEvent.BesiegerCamp.MapFaction == p.MobilePartyOf.MapFaction)))
			{
				MobileParty mobilePartyOf = p.MobilePartyOf;
				if ((missionType == Army.ArmyTypes.Raider && (settlement.Village.VillageState != Village.VillageStates.Normal || settlement.Party.MapEvent != null) && (mobilePartyOf.MapEvent == null || mobilePartyOf.MapEvent.MapEventSettlement != settlement)) || (missionType == Army.ArmyTypes.Besieger && (settlement.Party.MapEvent != null || settlement.SiegeEvent != null) && (settlement.SiegeEvent == null || settlement.SiegeEvent.BesiegerCamp.MapFaction != mobilePartyOf.MapFaction) && (mobilePartyOf.MapEvent == null || mobilePartyOf.MapEvent.MapEventSettlement != settlement)) || (missionType == Army.ArmyTypes.Defender && (settlement.LastAttackerParty == null || !settlement.LastAttackerParty.IsActive || !settlement.LastAttackerParty.MapFaction.IsAtWarWith(mobilePartyOf.MapFaction))))
				{
					return;
				}
				if (mobilePartyOf.Army == null && missionType == Army.ArmyTypes.Besieger && ((settlement.Party.MapEvent != null && settlement.Party.MapEvent.AttackerSide.LeaderParty != mobilePartyOf.Party) || (settlement.Party.SiegeEvent != null && mobilePartyOf.BesiegedSettlement != settlement)))
				{
					return;
				}
				MobileParty.NavigationType navigationType = MobileParty.NavigationType.None;
				float maxValue = float.MaxValue;
				bool isTargetingPort = false;
				bool isFromPort = false;
				switch (missionType)
				{
				case Army.ArmyTypes.Besieger:
					this.CalculateDistanceScoreForBesieging(settlement, mobilePartyOf, out navigationType, out maxValue, out isFromPort, out isTargetingPort);
					break;
				case Army.ArmyTypes.Raider:
					this.GetDistanceScoreForRaiding(settlement, mobilePartyOf, out navigationType, out maxValue, out isFromPort, out isTargetingPort);
					break;
				case Army.ArmyTypes.Defender:
					this.GetDistanceScoreForDefending(settlement, mobilePartyOf, out navigationType, out maxValue, out isFromPort, out isTargetingPort);
					break;
				}
				if (maxValue > 0f)
				{
					if (mobilePartyOf.SiegeEvent != null && mobilePartyOf.BesiegerCamp != null && mobilePartyOf.SiegeEvent.BesiegedSettlement == settlement)
					{
						ourStrength = mobilePartyOf.BesiegerCamp.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege).Sum((PartyBase x) => x.EstimatedStrength);
					}
					float num = Campaign.Current.Models.TargetScoreCalculatingModel.GetTargetScoreForFaction(settlement, missionType, mobilePartyOf, ourStrength);
					num *= maxValue * cohesionScore * partySizeScore * foodScore * newArmyCreatingAdditionalConstant;
					if (mobilePartyOf.Objective == MobileParty.PartyObjective.Defensive)
					{
						if (aiBehavior == AiBehavior.DefendSettlement)
						{
							num *= 1.2f;
						}
						else
						{
							num *= 0.8f;
						}
					}
					else if (mobilePartyOf.Objective == MobileParty.PartyObjective.Aggressive)
					{
						if (aiBehavior == AiBehavior.BesiegeSettlement || aiBehavior == AiBehavior.RaidSettlement)
						{
							num *= 1.2f;
						}
						else
						{
							num *= 0.8f;
						}
					}
					if (!mobilePartyOf.IsDisbanding)
					{
						IDisbandPartyCampaignBehavior disbandPartyCampaignBehavior = this._disbandPartyCampaignBehavior;
						if (disbandPartyCampaignBehavior == null || !disbandPartyCampaignBehavior.IsPartyWaitingForDisband(mobilePartyOf))
						{
							goto IL_2D4;
						}
					}
					num *= 0.25f;
					IL_2D4:
					if (navigationType != MobileParty.NavigationType.None)
					{
						AIBehaviorData item = new AIBehaviorData(settlement, aiBehavior, navigationType, p.WillGatherAnArmy, isFromPort, isTargetingPort);
						ValueTuple<AIBehaviorData, float> valueTuple = new ValueTuple<AIBehaviorData, float>(item, num);
						p.AddBehaviorScore(valueTuple);
					}
				}
			}
		}

		// Token: 0x060047CE RID: 18382 RVA: 0x00168FC0 File Offset: 0x001671C0
		private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
		{
			if (mobileParty.IsMilitia || mobileParty.IsCaravan || mobileParty.IsVillager || mobileParty.IsBandit || mobileParty.IsPatrolParty || mobileParty.IsDisbanding || mobileParty.LeaderHero == null || (mobileParty.MapFaction != Clan.PlayerClan.MapFaction && !mobileParty.MapFaction.IsKingdomFaction))
			{
				return;
			}
			Settlement currentSettlement = mobileParty.CurrentSettlement;
			if (((currentSettlement != null) ? currentSettlement.SiegeEvent : null) != null)
			{
				return;
			}
			if (mobileParty.Army != null)
			{
				mobileParty.Ai.SetInitiative(0.33f, 0.33f, 24f);
				if (mobileParty.Army.LeaderParty == mobileParty && mobileParty.Army.LeaderParty.Army.IsWaitingForArmyMembers())
				{
					mobileParty.Ai.SetInitiative(0.33f, 1f, 24f);
					p.DoNotChangeBehavior = true;
				}
				else if (mobileParty.Army.LeaderParty.DefaultBehavior == AiBehavior.PatrolAroundPoint)
				{
					mobileParty.Ai.SetInitiative(1f, 1f, 24f);
				}
				else if (mobileParty.Army.LeaderParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty.Army.LeaderParty == mobileParty && mobileParty.Army.AiBehaviorObject != null && mobileParty.Army.AiBehaviorObject is Settlement && ((Settlement)mobileParty.Army.AiBehaviorObject).Position.DistanceSquared(mobileParty.Position) < Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(mobileParty.NavigationCapability) * 1.53f)
				{
					mobileParty.Ai.SetInitiative(1f, 1f, 24f);
				}
				if (mobileParty.Army.LeaderParty != mobileParty)
				{
					return;
				}
			}
			else if (mobileParty.DefaultBehavior == AiBehavior.DefendSettlement || mobileParty.Objective == MobileParty.PartyObjective.Defensive)
			{
				mobileParty.Ai.SetInitiative(0.33f, 1f, 2f);
			}
			float totalLandStrengthWithFollowers = mobileParty.GetTotalLandStrengthWithFollowers(true);
			p.Initialization();
			bool flag = false;
			float newArmyCreatingAdditionalConstant = 1f;
			float num = totalLandStrengthWithFollowers;
			if (mobileParty.LeaderHero != null && mobileParty.Army == null && mobileParty.LeaderHero.Clan != null && mobileParty.PartySizeRatio > 0.6f && (mobileParty.LeaderHero.Clan.Leader == mobileParty.LeaderHero || (mobileParty.LeaderHero.Clan.Leader.PartyBelongedTo == null && mobileParty.LeaderHero.Clan.WarPartyComponents != null && mobileParty.LeaderHero.Clan.WarPartyComponents.FirstOrDefault<WarPartyComponent>() == mobileParty.WarPartyComponent)))
			{
				int traitLevel = mobileParty.LeaderHero.GetTraitLevel(DefaultTraits.Calculating);
				IFaction mapFaction = mobileParty.MapFaction;
				Kingdom kingdom = (Kingdom)mapFaction;
				int num2 = ((Kingdom)mapFaction).Armies.CountQ((Army x) => !x.Parties.Contains(MobileParty.MainParty));
				int num3 = 50 + num2 * num2 * 20 + mobileParty.LeaderHero.RandomInt(20) + traitLevel * 20;
				float num4 = 1f - (float)num2 * 0.4f;
				bool flag2;
				if (!mobileParty.IsCurrentlyAtSea && mobileParty.MapEvent == null && mobileParty.LeaderHero.Clan.Influence > (float)num3 && mobileParty.MapFaction.IsKingdomFaction && !mobileParty.LeaderHero.Clan.IsUnderMercenaryService && (float)mobileParty.GetNumDaysForFoodToLast() > Campaign.Current.Models.MobilePartyAIModel.NeededFoodsInDaysThresholdForSiege)
				{
					flag2 = mobileParty.MapFaction.FactionsAtWarWith.AnyQ((IFaction x) => x.Fiefs.Any<Town>());
				}
				else
				{
					flag2 = false;
				}
				flag = flag2;
				if (flag)
				{
					float num5 = ((kingdom.Armies.Count == 0) ? (1f + MathF.Sqrt((float)((int)CampaignTime.Now.ToDays - kingdom.LastArmyCreationDay)) * 0.15f) : 1f);
					float num6 = (10f + MathF.Sqrt(MathF.Min(900f, mobileParty.LeaderHero.Clan.Influence))) / 50f;
					float num7 = MathF.Sqrt(mobileParty.PartySizeRatio);
					newArmyCreatingAdditionalConstant = num5 * num6 * num4 * num7;
					num = mobileParty.Party.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.Siege);
					List<MobileParty> mobilePartiesToCallToArmy = Campaign.Current.Models.ArmyManagementCalculationModel.GetMobilePartiesToCallToArmy(mobileParty);
					if (mobilePartiesToCallToArmy.Count == 0)
					{
						flag = false;
					}
					else
					{
						foreach (MobileParty mobileParty2 in mobilePartiesToCallToArmy)
						{
							p.AddPotentialArmyMember(mobileParty2);
							num += mobileParty2.Party.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.Siege);
						}
					}
				}
			}
			for (int i = 0; i < 4; i++)
			{
				Army.ArmyTypes armyTypes = (Army.ArmyTypes)i;
				if (flag && armyTypes != Army.ArmyTypes.Raider)
				{
					p.WillGatherAnArmy = true;
					this.FindBestTargetAndItsValueForFaction(armyTypes, p, num, newArmyCreatingAdditionalConstant);
				}
				p.WillGatherAnArmy = false;
				this.FindBestTargetAndItsValueForFaction(armyTypes, p, totalLandStrengthWithFollowers, 1f);
			}
		}

		// Token: 0x040013CC RID: 5068
		private const int MinimumInfluenceNeededToCreateArmy = 50;

		// Token: 0x040013CD RID: 5069
		private const float MeaningfulCohesionThresholdForArmy = 40f;

		// Token: 0x040013CE RID: 5070
		private const float MinimumCohesionScoreThreshold = 0.25f;

		// Token: 0x040013CF RID: 5071
		private const float AverageSiegeDurationAsDays = 5.73f;

		// Token: 0x040013D0 RID: 5072
		private IDisbandPartyCampaignBehavior _disbandPartyCampaignBehavior;

		// Token: 0x040013D1 RID: 5073
		private readonly HashSet<Settlement> _checkedNeighbors = new HashSet<Settlement>();
	}
}
