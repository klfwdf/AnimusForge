using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public class CampaignTickCacheDataStore
{
	private struct PartyTickCachePerParty
	{
		internal MobileParty MobileParty;

		internal MobileParty.CachedPartyVariables LocalVariables;
	}

	private class MobilePartyComparer : IComparer<MobileParty>
	{
		public int Compare(MobileParty x, MobileParty y)
		{
			return x.Id.InternalValue.CompareTo(y.Id.InternalValue);
		}
	}

	private PartyTickCachePerParty[] _cacheData;

	private MobileParty[] _gridChangeMobilePartyList;

	private MobileParty[] _exitingSettlementMobilePartyList;

	private MobileParty[] _navigationTransitionedMobilePartyList;

	private int[] _movingPartyIndices;

	private int _currentFrameMovingPartyCount;

	private int[] _stationaryPartyIndices;

	private int _currentFrameStationaryPartyCount;

	private int[] _transitioningArmyLeaderPartyIndices;

	private int _currentFrameTransitioningArmyLeaderCount;

	private int[] _transitioningPartyIndices;

	private int _currentFrameTransitioningCount;

	private int[] _movingArmyLeaderPartyIndices;

	private int _currentFrameMovingArmyLeaderCount;

	private int[] _stationaryArmyLeaderPartyIndices;

	private int _currentFrameStationaryArmyLeaderCount;

	private int _currentTotalMobilePartyCapacity;

	private int _gridChangeCount;

	private int _exitingSettlementCount;

	private int _navigationTransitionedCount;

	private float _currentDt;

	private float _currentRealDt;

	private readonly TWParallel.ParallelForAuxPredicate _parallelInitializeCachedPartyVariablesPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelCacheTargetPartyVariablesAtFrameStartPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelArrangePartyIndicesPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelTickMovingArmiesPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelTickTransitioningArmiesPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelTickTransitioningPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelTickMovingPartiesPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelTickStationaryPartiesPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelCheckExitingSettlementsPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelTickStationaryArmyLeaderPredicate;

	private readonly MobilePartyComparer _mobilePartyComparer;

	internal CampaignTickCacheDataStore()
	{
		_mobilePartyComparer = new MobilePartyComparer();
		_parallelInitializeCachedPartyVariablesPredicate = ParallelInitializeCachedPartyVariables;
		_parallelCacheTargetPartyVariablesAtFrameStartPredicate = ParallelCacheTargetPartyVariablesAtFrameStart;
		_parallelArrangePartyIndicesPredicate = ParallelArrangePartyIndices;
		_parallelTickMovingArmiesPredicate = ParallelTickMovingArmies;
		_parallelTickMovingPartiesPredicate = ParallelTickMovingParties;
		_parallelTickStationaryArmyLeaderPredicate = ParallelTickStationaryArmyLeaderParties;
		_parallelTickStationaryPartiesPredicate = ParallelTickStationaryParties;
		_parallelCheckExitingSettlementsPredicate = ParallelCheckExitingSettlements;
		_parallelTickTransitioningArmiesPredicate = ParallelTickTransitioningArmyLeaders;
		_parallelTickTransitioningPredicate = ParallelTickTransitioningParties;
	}

	internal void ValidateMobilePartyTickDataCache(int currentTotalMobilePartyCount)
	{
		if (_currentTotalMobilePartyCapacity <= currentTotalMobilePartyCount)
		{
			InitializeCacheArrays();
		}
		_currentFrameMovingPartyCount = -1;
		_currentFrameStationaryPartyCount = -1;
		_currentFrameMovingArmyLeaderCount = -1;
		_gridChangeCount = -1;
		_exitingSettlementCount = -1;
		_currentFrameStationaryArmyLeaderCount = -1;
		_navigationTransitionedCount = -1;
		_currentFrameTransitioningArmyLeaderCount = -1;
		_currentFrameTransitioningCount = -1;
	}

	private void InitializeCacheArrays()
	{
		int num = (int)((float)_currentTotalMobilePartyCapacity * 2f);
		_cacheData = new PartyTickCachePerParty[num];
		_gridChangeMobilePartyList = new MobileParty[num];
		_exitingSettlementMobilePartyList = new MobileParty[num];
		_currentTotalMobilePartyCapacity = num;
		_navigationTransitionedMobilePartyList = new MobileParty[num];
		_movingPartyIndices = new int[num];
		_stationaryPartyIndices = new int[num];
		_transitioningArmyLeaderPartyIndices = new int[num];
		_transitioningPartyIndices = new int[num];
		_movingArmyLeaderPartyIndices = new int[num];
		_stationaryArmyLeaderPartyIndices = new int[num];
	}

	internal void InitializeDataCache()
	{
		_currentFrameMovingArmyLeaderCount = Campaign.Current.MobileParties.Count;
		_currentTotalMobilePartyCapacity = Campaign.Current.MobileParties.Count;
		_currentFrameStationaryPartyCount = Campaign.Current.MobileParties.Count;
		InitializeCacheArrays();
	}

	private void ParallelTickTransitioningArmyLeaders(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			int num = _transitioningArmyLeaderPartyIndices[i];
			MobileParty.CachedPartyVariables variables = _cacheData[num].LocalVariables;
			Campaign.Current.MobileParties[num].FillCurrentTickMoveDataForMovingArmyLeader(ref variables, _currentDt, _currentRealDt);
			Campaign.Current.MobileParties[num].CommonTransitioningPartyTick(ref variables, ref _navigationTransitionedCount, ref _navigationTransitionedMobilePartyList, _currentDt);
		}
	}

	private void ParallelTickTransitioningParties(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			int num = _transitioningPartyIndices[i];
			MobileParty.CachedPartyVariables variables = _cacheData[num].LocalVariables;
			Campaign.Current.MobileParties[num].FillCurrentTickMoveDataForMovingMobileParty(ref variables, _currentDt, _currentRealDt);
			Campaign.Current.MobileParties[num].CommonTransitioningPartyTick(ref variables, ref _navigationTransitionedCount, ref _navigationTransitionedMobilePartyList, _currentDt);
		}
	}

	private void ParallelCheckExitingSettlements(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			Campaign.Current.MobileParties[i].CheckExitingSettlementParallel(ref _exitingSettlementCount, ref _exitingSettlementMobilePartyList, ref _gridChangeCount, ref _gridChangeMobilePartyList);
		}
	}

	private void ParallelInitializeCachedPartyVariables(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			MobileParty mobileParty = Campaign.Current.MobileParties[i];
			_cacheData[i].MobileParty = mobileParty;
			mobileParty.InitializeCachedPartyVariables(ref _cacheData[i].LocalVariables);
		}
	}

	private void ParallelCacheTargetPartyVariablesAtFrameStart(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			_cacheData[i].MobileParty.CacheTargetPartyVariablesAtFrameStart(ref _cacheData[i].LocalVariables);
		}
	}

	private void ParallelArrangePartyIndices(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			MobileParty mobileParty = _cacheData[i].MobileParty;
			MobileParty.CachedPartyVariables localVariables = _cacheData[i].LocalVariables;
			if (!mobileParty.IsActive)
			{
				continue;
			}
			if (localVariables.IsMoving)
			{
				if (localVariables.IsArmyLeader)
				{
					int num = Interlocked.Increment(ref _currentFrameMovingArmyLeaderCount);
					_movingArmyLeaderPartyIndices[num] = i;
				}
				else
				{
					int num2 = Interlocked.Increment(ref _currentFrameMovingPartyCount);
					_movingPartyIndices[num2] = i;
				}
			}
			else if (localVariables.IsArmyLeader)
			{
				if (localVariables.IsTransitionInProgress)
				{
					int num3 = Interlocked.Increment(ref _currentFrameTransitioningArmyLeaderCount);
					_transitioningArmyLeaderPartyIndices[num3] = i;
				}
				else
				{
					int num4 = Interlocked.Increment(ref _currentFrameStationaryArmyLeaderCount);
					_stationaryArmyLeaderPartyIndices[num4] = i;
				}
			}
			else if (localVariables.IsTransitionInProgress && !localVariables.IsAttachedArmyMember)
			{
				int num5 = Interlocked.Increment(ref _currentFrameTransitioningCount);
				_transitioningPartyIndices[num5] = i;
			}
			else
			{
				int num6 = Interlocked.Increment(ref _currentFrameStationaryPartyCount);
				_stationaryPartyIndices[num6] = i;
			}
		}
	}

	private void ParallelTickMovingArmies(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			int num = _movingArmyLeaderPartyIndices[i];
			PartyTickCachePerParty partyTickCachePerParty = _cacheData[num];
			MobileParty mobileParty = partyTickCachePerParty.MobileParty;
			MobileParty.CachedPartyVariables variables = partyTickCachePerParty.LocalVariables;
			mobileParty.FillCurrentTickMoveDataForMovingArmyLeader(ref variables, _currentDt, _currentRealDt);
			mobileParty.TryToMoveThePartyWithCurrentTickMoveData(ref variables, ref _gridChangeCount, ref _gridChangeMobilePartyList);
			_cacheData[num].LocalVariables = variables;
			mobileParty.ValidateSpeed();
		}
	}

	private void ParallelTickMovingParties(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			int num = _movingPartyIndices[i];
			PartyTickCachePerParty partyTickCachePerParty = _cacheData[num];
			MobileParty mobileParty = partyTickCachePerParty.MobileParty;
			MobileParty.CachedPartyVariables variables = partyTickCachePerParty.LocalVariables;
			mobileParty.FillCurrentTickMoveDataForMovingMobileParty(ref variables, _currentDt, _currentRealDt);
			mobileParty.TryToMoveThePartyWithCurrentTickMoveData(ref variables, ref _gridChangeCount, ref _gridChangeMobilePartyList);
			_cacheData[num].LocalVariables = variables;
		}
	}

	private void ParallelTickStationaryParties(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			int num = _stationaryPartyIndices[i];
			PartyTickCachePerParty partyTickCachePerParty = _cacheData[num];
			MobileParty mobileParty = partyTickCachePerParty.MobileParty;
			MobileParty.CachedPartyVariables variables = partyTickCachePerParty.LocalVariables;
			mobileParty.TickForStationaryMobileParty(ref variables, _currentDt, _currentRealDt);
			_cacheData[num].LocalVariables = variables;
		}
	}

	private void ParallelTickStationaryArmyLeaderParties(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			int num = _stationaryArmyLeaderPartyIndices[i];
			PartyTickCachePerParty partyTickCachePerParty = _cacheData[num];
			MobileParty mobileParty = partyTickCachePerParty.MobileParty;
			MobileParty.CachedPartyVariables variables = partyTickCachePerParty.LocalVariables;
			mobileParty.TickForStationaryMobileParty(ref variables, _currentDt, _currentRealDt);
			_cacheData[num].LocalVariables = variables;
		}
	}

	internal void Tick()
	{
		TWParallel.For(0, Campaign.Current.MobileParties.Count, _parallelCheckExitingSettlementsPredicate);
		Array.Sort(_exitingSettlementMobilePartyList, 0, _exitingSettlementCount + 1, _mobilePartyComparer);
		for (int i = 0; i < _exitingSettlementCount + 1; i++)
		{
			LeaveSettlementAction.ApplyForParty(_exitingSettlementMobilePartyList[i]);
		}
	}

	internal void RealTick(float dt, float realDt)
	{
		_currentDt = dt;
		_currentRealDt = realDt;
		ValidateMobilePartyTickDataCache(Campaign.Current.MobileParties.Count);
		int count = Campaign.Current.MobileParties.Count;
		TWParallel.For(0, count, _parallelInitializeCachedPartyVariablesPredicate);
		TWParallel.For(0, count, _parallelCacheTargetPartyVariablesAtFrameStartPredicate);
		TWParallel.For(0, count, _parallelArrangePartyIndicesPredicate);
		TWParallel.For(0, _currentFrameMovingArmyLeaderCount + 1, _parallelTickMovingArmiesPredicate);
		TWParallel.For(0, _currentFrameTransitioningArmyLeaderCount + 1, _parallelTickTransitioningArmiesPredicate);
		TWParallel.For(0, _currentFrameMovingPartyCount + 1, _parallelTickMovingPartiesPredicate);
		TWParallel.For(0, _currentFrameTransitioningCount + 1, _parallelTickTransitioningPredicate);
		TWParallel.For(0, _currentFrameStationaryArmyLeaderCount + 1, _parallelTickStationaryArmyLeaderPredicate);
		TWParallel.For(0, _currentFrameStationaryPartyCount + 1, _parallelTickStationaryPartiesPredicate);
		UpdateVisibilitiesAroundMainParty();
		Array.Sort(_gridChangeMobilePartyList, 0, _gridChangeCount + 1, _mobilePartyComparer);
		Campaign current = Campaign.Current;
		for (int i = 0; i < _gridChangeCount + 1; i++)
		{
			current.MobilePartyLocator.UpdateLocator(_gridChangeMobilePartyList[i]);
		}
		Array.Sort(_navigationTransitionedMobilePartyList, 0, _navigationTransitionedCount + 1, _mobilePartyComparer);
		for (int j = 0; j < _navigationTransitionedCount + 1; j++)
		{
			_navigationTransitionedMobilePartyList[j].FinishNavigationTransitionInternal();
		}
	}

	private void UpdateVisibilitiesAroundMainParty()
	{
		if (!MobileParty.MainParty.Position.IsValid() || Campaign.Current.GetSimplifiedTimeControlMode() == CampaignTimeControlMode.Stop)
		{
			return;
		}
		if (MobileParty.MainParty.SiegeEvent != null && MobileParty.MainParty.SiegeEvent.BesiegedSettlement.HasPort)
		{
			UpdateVisibilitiesBasedOnPoint(MobileParty.MainParty.SiegeEvent.BesiegedSettlement.Position, MobileParty.MainParty.SeeingRange * 1.35f);
			return;
		}
		MapEvent mapEvent = MobileParty.MainParty.MapEvent;
		if (mapEvent != null && mapEvent.IsRaid && MobileParty.MainParty.MapEvent.MapEventSettlement.HasPort)
		{
			UpdateVisibilitiesBasedOnPoint(MobileParty.MainParty.MapEvent.MapEventSettlement.Position, MobileParty.MainParty.SeeingRange * 1.35f);
		}
		else
		{
			UpdateVisibilitiesBasedOnPoint(MobileParty.MainParty.Position, MobileParty.MainParty.SeeingRange);
		}
	}

	private void UpdateVisibilitiesBasedOnPoint(CampaignVec2 point, float mainPartyVisibilityRange)
	{
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(point.ToVec2(), Campaign.Current.Models.MapVisibilityModel.MaximumSeeingRange() + 5f);
		for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref data); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref data))
		{
			if (!mobileParty.IsMilitia && !mobileParty.IsGarrison)
			{
				mobileParty.Party.UpdateVisibilityAndInspected(point, mainPartyVisibilityRange);
			}
		}
		LocatableSearchData<Settlement> data2 = Settlement.StartFindingLocatablesAroundPosition(point.ToVec2(), Campaign.Current.Models.MapVisibilityModel.MaximumSeeingRange() + 5f);
		for (Settlement settlement = Settlement.FindNextLocatable(ref data2); settlement != null; settlement = Settlement.FindNextLocatable(ref data2))
		{
			settlement.Party.UpdateVisibilityAndInspected(point, mainPartyVisibilityRange);
		}
	}
}
