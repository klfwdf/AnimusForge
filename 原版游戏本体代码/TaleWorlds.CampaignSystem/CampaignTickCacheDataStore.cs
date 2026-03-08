using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000079 RID: 121
	public class CampaignTickCacheDataStore
	{
		// Token: 0x0600104F RID: 4175 RVA: 0x0004D0E0 File Offset: 0x0004B2E0
		internal CampaignTickCacheDataStore()
		{
			this._mobilePartyComparer = new CampaignTickCacheDataStore.MobilePartyComparer();
			this._parallelInitializeCachedPartyVariablesPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelInitializeCachedPartyVariables);
			this._parallelCacheTargetPartyVariablesAtFrameStartPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelCacheTargetPartyVariablesAtFrameStart);
			this._parallelArrangePartyIndicesPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelArrangePartyIndices);
			this._parallelTickMovingArmiesPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelTickMovingArmies);
			this._parallelTickMovingPartiesPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelTickMovingParties);
			this._parallelTickStationaryArmyLeaderPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelTickStationaryArmyLeaderParties);
			this._parallelTickStationaryPartiesPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelTickStationaryParties);
			this._parallelCheckExitingSettlementsPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelCheckExitingSettlements);
			this._parallelTickTransitioningArmiesPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelTickTransitioningArmyLeaders);
			this._parallelTickTransitioningPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelTickTransitioningParties);
		}

		// Token: 0x06001050 RID: 4176 RVA: 0x0004D1B4 File Offset: 0x0004B3B4
		internal void ValidateMobilePartyTickDataCache(int currentTotalMobilePartyCount)
		{
			if (this._currentTotalMobilePartyCapacity <= currentTotalMobilePartyCount)
			{
				this.InitializeCacheArrays();
			}
			this._currentFrameMovingPartyCount = -1;
			this._currentFrameStationaryPartyCount = -1;
			this._currentFrameMovingArmyLeaderCount = -1;
			this._gridChangeCount = -1;
			this._exitingSettlementCount = -1;
			this._currentFrameStationaryArmyLeaderCount = -1;
			this._navigationTransitionedCount = -1;
			this._currentFrameTransitioningArmyLeaderCount = -1;
			this._currentFrameTransitioningCount = -1;
		}

		// Token: 0x06001051 RID: 4177 RVA: 0x0004D210 File Offset: 0x0004B410
		private void InitializeCacheArrays()
		{
			int num = (int)((float)this._currentTotalMobilePartyCapacity * 2f);
			this._cacheData = new CampaignTickCacheDataStore.PartyTickCachePerParty[num];
			this._gridChangeMobilePartyList = new MobileParty[num];
			this._exitingSettlementMobilePartyList = new MobileParty[num];
			this._currentTotalMobilePartyCapacity = num;
			this._navigationTransitionedMobilePartyList = new MobileParty[num];
			this._movingPartyIndices = new int[num];
			this._stationaryPartyIndices = new int[num];
			this._transitioningArmyLeaderPartyIndices = new int[num];
			this._transitioningPartyIndices = new int[num];
			this._movingArmyLeaderPartyIndices = new int[num];
			this._stationaryArmyLeaderPartyIndices = new int[num];
		}

		// Token: 0x06001052 RID: 4178 RVA: 0x0004D2AC File Offset: 0x0004B4AC
		internal void InitializeDataCache()
		{
			this._currentFrameMovingArmyLeaderCount = Campaign.Current.MobileParties.Count;
			this._currentTotalMobilePartyCapacity = Campaign.Current.MobileParties.Count;
			this._currentFrameStationaryPartyCount = Campaign.Current.MobileParties.Count;
			this.InitializeCacheArrays();
		}

		// Token: 0x06001053 RID: 4179 RVA: 0x0004D300 File Offset: 0x0004B500
		private void ParallelTickTransitioningArmyLeaders(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				int num = this._transitioningArmyLeaderPartyIndices[i];
				MobileParty.CachedPartyVariables localVariables = this._cacheData[num].LocalVariables;
				Campaign.Current.MobileParties[num].FillCurrentTickMoveDataForMovingArmyLeader(ref localVariables, this._currentDt, this._currentRealDt);
				Campaign.Current.MobileParties[num].CommonTransitioningPartyTick(ref localVariables, ref this._navigationTransitionedCount, ref this._navigationTransitionedMobilePartyList, this._currentDt);
			}
		}

		// Token: 0x06001054 RID: 4180 RVA: 0x0004D380 File Offset: 0x0004B580
		private void ParallelTickTransitioningParties(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				int num = this._transitioningPartyIndices[i];
				MobileParty.CachedPartyVariables localVariables = this._cacheData[num].LocalVariables;
				Campaign.Current.MobileParties[num].FillCurrentTickMoveDataForMovingMobileParty(ref localVariables, this._currentDt, this._currentRealDt);
				Campaign.Current.MobileParties[num].CommonTransitioningPartyTick(ref localVariables, ref this._navigationTransitionedCount, ref this._navigationTransitionedMobilePartyList, this._currentDt);
			}
		}

		// Token: 0x06001055 RID: 4181 RVA: 0x0004D400 File Offset: 0x0004B600
		private void ParallelCheckExitingSettlements(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				Campaign.Current.MobileParties[i].CheckExitingSettlementParallel(ref this._exitingSettlementCount, ref this._exitingSettlementMobilePartyList, ref this._gridChangeCount, ref this._gridChangeMobilePartyList);
			}
		}

		// Token: 0x06001056 RID: 4182 RVA: 0x0004D448 File Offset: 0x0004B648
		private void ParallelInitializeCachedPartyVariables(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				MobileParty mobileParty = Campaign.Current.MobileParties[i];
				this._cacheData[i].MobileParty = mobileParty;
				mobileParty.InitializeCachedPartyVariables(ref this._cacheData[i].LocalVariables);
			}
		}

		// Token: 0x06001057 RID: 4183 RVA: 0x0004D49C File Offset: 0x0004B69C
		private void ParallelCacheTargetPartyVariablesAtFrameStart(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this._cacheData[i].MobileParty.CacheTargetPartyVariablesAtFrameStart(ref this._cacheData[i].LocalVariables);
			}
		}

		// Token: 0x06001058 RID: 4184 RVA: 0x0004D4DC File Offset: 0x0004B6DC
		private void ParallelArrangePartyIndices(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				MobileParty mobileParty = this._cacheData[i].MobileParty;
				MobileParty.CachedPartyVariables localVariables = this._cacheData[i].LocalVariables;
				if (mobileParty.IsActive)
				{
					if (localVariables.IsMoving)
					{
						if (localVariables.IsArmyLeader)
						{
							int num = Interlocked.Increment(ref this._currentFrameMovingArmyLeaderCount);
							this._movingArmyLeaderPartyIndices[num] = i;
						}
						else
						{
							int num2 = Interlocked.Increment(ref this._currentFrameMovingPartyCount);
							this._movingPartyIndices[num2] = i;
						}
					}
					else if (localVariables.IsArmyLeader)
					{
						if (localVariables.IsTransitionInProgress)
						{
							int num3 = Interlocked.Increment(ref this._currentFrameTransitioningArmyLeaderCount);
							this._transitioningArmyLeaderPartyIndices[num3] = i;
						}
						else
						{
							int num4 = Interlocked.Increment(ref this._currentFrameStationaryArmyLeaderCount);
							this._stationaryArmyLeaderPartyIndices[num4] = i;
						}
					}
					else if (localVariables.IsTransitionInProgress && !localVariables.IsAttachedArmyMember)
					{
						int num5 = Interlocked.Increment(ref this._currentFrameTransitioningCount);
						this._transitioningPartyIndices[num5] = i;
					}
					else
					{
						int num6 = Interlocked.Increment(ref this._currentFrameStationaryPartyCount);
						this._stationaryPartyIndices[num6] = i;
					}
				}
			}
		}

		// Token: 0x06001059 RID: 4185 RVA: 0x0004D5F0 File Offset: 0x0004B7F0
		private void ParallelTickMovingArmies(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				int num = this._movingArmyLeaderPartyIndices[i];
				CampaignTickCacheDataStore.PartyTickCachePerParty partyTickCachePerParty = this._cacheData[num];
				MobileParty mobileParty = partyTickCachePerParty.MobileParty;
				MobileParty.CachedPartyVariables localVariables = partyTickCachePerParty.LocalVariables;
				mobileParty.FillCurrentTickMoveDataForMovingArmyLeader(ref localVariables, this._currentDt, this._currentRealDt);
				mobileParty.TryToMoveThePartyWithCurrentTickMoveData(ref localVariables, ref this._gridChangeCount, ref this._gridChangeMobilePartyList);
				this._cacheData[num].LocalVariables = localVariables;
				mobileParty.ValidateSpeed();
			}
		}

		// Token: 0x0600105A RID: 4186 RVA: 0x0004D66C File Offset: 0x0004B86C
		private void ParallelTickMovingParties(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				int num = this._movingPartyIndices[i];
				CampaignTickCacheDataStore.PartyTickCachePerParty partyTickCachePerParty = this._cacheData[num];
				MobileParty mobileParty = partyTickCachePerParty.MobileParty;
				MobileParty.CachedPartyVariables localVariables = partyTickCachePerParty.LocalVariables;
				mobileParty.FillCurrentTickMoveDataForMovingMobileParty(ref localVariables, this._currentDt, this._currentRealDt);
				mobileParty.TryToMoveThePartyWithCurrentTickMoveData(ref localVariables, ref this._gridChangeCount, ref this._gridChangeMobilePartyList);
				this._cacheData[num].LocalVariables = localVariables;
			}
		}

		// Token: 0x0600105B RID: 4187 RVA: 0x0004D6E4 File Offset: 0x0004B8E4
		private void ParallelTickStationaryParties(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				int num = this._stationaryPartyIndices[i];
				CampaignTickCacheDataStore.PartyTickCachePerParty partyTickCachePerParty = this._cacheData[num];
				MobileParty mobileParty = partyTickCachePerParty.MobileParty;
				MobileParty.CachedPartyVariables localVariables = partyTickCachePerParty.LocalVariables;
				mobileParty.TickForStationaryMobileParty(ref localVariables, this._currentDt, this._currentRealDt);
				this._cacheData[num].LocalVariables = localVariables;
			}
		}

		// Token: 0x0600105C RID: 4188 RVA: 0x0004D748 File Offset: 0x0004B948
		private void ParallelTickStationaryArmyLeaderParties(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				int num = this._stationaryArmyLeaderPartyIndices[i];
				CampaignTickCacheDataStore.PartyTickCachePerParty partyTickCachePerParty = this._cacheData[num];
				MobileParty mobileParty = partyTickCachePerParty.MobileParty;
				MobileParty.CachedPartyVariables localVariables = partyTickCachePerParty.LocalVariables;
				mobileParty.TickForStationaryMobileParty(ref localVariables, this._currentDt, this._currentRealDt);
				this._cacheData[num].LocalVariables = localVariables;
			}
		}

		// Token: 0x0600105D RID: 4189 RVA: 0x0004D7AC File Offset: 0x0004B9AC
		internal void Tick()
		{
			TWParallel.For(0, Campaign.Current.MobileParties.Count, this._parallelCheckExitingSettlementsPredicate, 16);
			Array.Sort<MobileParty>(this._exitingSettlementMobilePartyList, 0, this._exitingSettlementCount + 1, this._mobilePartyComparer);
			for (int i = 0; i < this._exitingSettlementCount + 1; i++)
			{
				LeaveSettlementAction.ApplyForParty(this._exitingSettlementMobilePartyList[i]);
			}
		}

		// Token: 0x0600105E RID: 4190 RVA: 0x0004D810 File Offset: 0x0004BA10
		internal void RealTick(float dt, float realDt)
		{
			this._currentDt = dt;
			this._currentRealDt = realDt;
			this.ValidateMobilePartyTickDataCache(Campaign.Current.MobileParties.Count);
			int count = Campaign.Current.MobileParties.Count;
			TWParallel.For(0, count, this._parallelInitializeCachedPartyVariablesPredicate, 16);
			TWParallel.For(0, count, this._parallelCacheTargetPartyVariablesAtFrameStartPredicate, 16);
			TWParallel.For(0, count, this._parallelArrangePartyIndicesPredicate, 16);
			TWParallel.For(0, this._currentFrameMovingArmyLeaderCount + 1, this._parallelTickMovingArmiesPredicate, 16);
			TWParallel.For(0, this._currentFrameTransitioningArmyLeaderCount + 1, this._parallelTickTransitioningArmiesPredicate, 16);
			TWParallel.For(0, this._currentFrameMovingPartyCount + 1, this._parallelTickMovingPartiesPredicate, 16);
			TWParallel.For(0, this._currentFrameTransitioningCount + 1, this._parallelTickTransitioningPredicate, 16);
			TWParallel.For(0, this._currentFrameStationaryArmyLeaderCount + 1, this._parallelTickStationaryArmyLeaderPredicate, 16);
			TWParallel.For(0, this._currentFrameStationaryPartyCount + 1, this._parallelTickStationaryPartiesPredicate, 16);
			this.UpdateVisibilitiesAroundMainParty();
			Array.Sort<MobileParty>(this._gridChangeMobilePartyList, 0, this._gridChangeCount + 1, this._mobilePartyComparer);
			Campaign campaign = Campaign.Current;
			for (int i = 0; i < this._gridChangeCount + 1; i++)
			{
				campaign.MobilePartyLocator.UpdateLocator(this._gridChangeMobilePartyList[i]);
			}
			Array.Sort<MobileParty>(this._navigationTransitionedMobilePartyList, 0, this._navigationTransitionedCount + 1, this._mobilePartyComparer);
			for (int j = 0; j < this._navigationTransitionedCount + 1; j++)
			{
				this._navigationTransitionedMobilePartyList[j].FinishNavigationTransitionInternal();
			}
		}

		// Token: 0x0600105F RID: 4191 RVA: 0x0004D988 File Offset: 0x0004BB88
		private void UpdateVisibilitiesAroundMainParty()
		{
			if (MobileParty.MainParty.Position.IsValid() && Campaign.Current.GetSimplifiedTimeControlMode() != CampaignTimeControlMode.Stop)
			{
				if (MobileParty.MainParty.SiegeEvent != null && MobileParty.MainParty.SiegeEvent.BesiegedSettlement.HasPort)
				{
					this.UpdateVisibilitiesBasedOnPoint(MobileParty.MainParty.SiegeEvent.BesiegedSettlement.Position, MobileParty.MainParty.SeeingRange * 1.35f);
					return;
				}
				this.UpdateVisibilitiesBasedOnPoint(MobileParty.MainParty.Position, MobileParty.MainParty.SeeingRange);
			}
		}

		// Token: 0x06001060 RID: 4192 RVA: 0x0004DA1C File Offset: 0x0004BC1C
		private void UpdateVisibilitiesBasedOnPoint(CampaignVec2 point, float mainPartyVisibilityRange)
		{
			LocatableSearchData<MobileParty> locatableSearchData = MobileParty.StartFindingLocatablesAroundPosition(point.ToVec2(), Campaign.Current.Models.MapVisibilityModel.MaximumSeeingRange() + 5f);
			for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData))
			{
				if (!mobileParty.IsMilitia && !mobileParty.IsGarrison)
				{
					mobileParty.Party.UpdateVisibilityAndInspected(point, mainPartyVisibilityRange);
				}
			}
			LocatableSearchData<Settlement> locatableSearchData2 = Settlement.StartFindingLocatablesAroundPosition(point.ToVec2(), Campaign.Current.Models.MapVisibilityModel.MaximumSeeingRange() + 5f);
			for (Settlement settlement = Settlement.FindNextLocatable(ref locatableSearchData2); settlement != null; settlement = Settlement.FindNextLocatable(ref locatableSearchData2))
			{
				settlement.Party.UpdateVisibilityAndInspected(point, mainPartyVisibilityRange);
			}
		}

		// Token: 0x04000477 RID: 1143
		private CampaignTickCacheDataStore.PartyTickCachePerParty[] _cacheData;

		// Token: 0x04000478 RID: 1144
		private MobileParty[] _gridChangeMobilePartyList;

		// Token: 0x04000479 RID: 1145
		private MobileParty[] _exitingSettlementMobilePartyList;

		// Token: 0x0400047A RID: 1146
		private MobileParty[] _navigationTransitionedMobilePartyList;

		// Token: 0x0400047B RID: 1147
		private int[] _movingPartyIndices;

		// Token: 0x0400047C RID: 1148
		private int _currentFrameMovingPartyCount;

		// Token: 0x0400047D RID: 1149
		private int[] _stationaryPartyIndices;

		// Token: 0x0400047E RID: 1150
		private int _currentFrameStationaryPartyCount;

		// Token: 0x0400047F RID: 1151
		private int[] _transitioningArmyLeaderPartyIndices;

		// Token: 0x04000480 RID: 1152
		private int _currentFrameTransitioningArmyLeaderCount;

		// Token: 0x04000481 RID: 1153
		private int[] _transitioningPartyIndices;

		// Token: 0x04000482 RID: 1154
		private int _currentFrameTransitioningCount;

		// Token: 0x04000483 RID: 1155
		private int[] _movingArmyLeaderPartyIndices;

		// Token: 0x04000484 RID: 1156
		private int _currentFrameMovingArmyLeaderCount;

		// Token: 0x04000485 RID: 1157
		private int[] _stationaryArmyLeaderPartyIndices;

		// Token: 0x04000486 RID: 1158
		private int _currentFrameStationaryArmyLeaderCount;

		// Token: 0x04000487 RID: 1159
		private int _currentTotalMobilePartyCapacity;

		// Token: 0x04000488 RID: 1160
		private int _gridChangeCount;

		// Token: 0x04000489 RID: 1161
		private int _exitingSettlementCount;

		// Token: 0x0400048A RID: 1162
		private int _navigationTransitionedCount;

		// Token: 0x0400048B RID: 1163
		private float _currentDt;

		// Token: 0x0400048C RID: 1164
		private float _currentRealDt;

		// Token: 0x0400048D RID: 1165
		private readonly TWParallel.ParallelForAuxPredicate _parallelInitializeCachedPartyVariablesPredicate;

		// Token: 0x0400048E RID: 1166
		private readonly TWParallel.ParallelForAuxPredicate _parallelCacheTargetPartyVariablesAtFrameStartPredicate;

		// Token: 0x0400048F RID: 1167
		private readonly TWParallel.ParallelForAuxPredicate _parallelArrangePartyIndicesPredicate;

		// Token: 0x04000490 RID: 1168
		private readonly TWParallel.ParallelForAuxPredicate _parallelTickMovingArmiesPredicate;

		// Token: 0x04000491 RID: 1169
		private readonly TWParallel.ParallelForAuxPredicate _parallelTickTransitioningArmiesPredicate;

		// Token: 0x04000492 RID: 1170
		private readonly TWParallel.ParallelForAuxPredicate _parallelTickTransitioningPredicate;

		// Token: 0x04000493 RID: 1171
		private readonly TWParallel.ParallelForAuxPredicate _parallelTickMovingPartiesPredicate;

		// Token: 0x04000494 RID: 1172
		private readonly TWParallel.ParallelForAuxPredicate _parallelTickStationaryPartiesPredicate;

		// Token: 0x04000495 RID: 1173
		private readonly TWParallel.ParallelForAuxPredicate _parallelCheckExitingSettlementsPredicate;

		// Token: 0x04000496 RID: 1174
		private readonly TWParallel.ParallelForAuxPredicate _parallelTickStationaryArmyLeaderPredicate;

		// Token: 0x04000497 RID: 1175
		private readonly CampaignTickCacheDataStore.MobilePartyComparer _mobilePartyComparer;

		// Token: 0x02000539 RID: 1337
		private struct PartyTickCachePerParty
		{
			// Token: 0x04001634 RID: 5684
			internal MobileParty MobileParty;

			// Token: 0x04001635 RID: 5685
			internal MobileParty.CachedPartyVariables LocalVariables;
		}

		// Token: 0x0200053A RID: 1338
		private class MobilePartyComparer : IComparer<MobileParty>
		{
			// Token: 0x06004C1F RID: 19487 RVA: 0x00179414 File Offset: 0x00177614
			public int Compare(MobileParty x, MobileParty y)
			{
				return x.Id.InternalValue.CompareTo(y.Id.InternalValue);
			}
		}
	}
}
