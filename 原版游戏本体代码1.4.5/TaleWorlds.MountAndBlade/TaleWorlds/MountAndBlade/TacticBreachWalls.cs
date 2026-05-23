using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticBreachWalls : TacticComponent
{
	private class BreachWallsProgressIndicators
	{
		public float StartingPowerRatio;

		public int InitialLaneCount;

		public int InitialUnitCount;

		private readonly float _insideFormationEffect;

		private readonly float _openLaneEffect;

		private readonly float _existingLaneEffect;

		public BreachWallsProgressIndicators(Team team, List<SiegeLane> lanes)
		{
			StartingPowerRatio = team.QuerySystem.RemainingPowerRatio;
			InitialUnitCount = team.QuerySystem.AllyUnitCount;
			InitialLaneCount = ((InitialUnitCount <= 100) ? 1 : lanes.Count);
			_insideFormationEffect = 1f / (float)InitialLaneCount;
			_openLaneEffect = 0.7f / (float)InitialLaneCount;
			_existingLaneEffect = 0.4f / (float)InitialLaneCount;
		}

		public float GetRetreatThresholdRatio(List<SiegeLane> lanes, int insideFormationCount)
		{
			float num = 1f;
			num -= (float)insideFormationCount * _insideFormationEffect;
			int num2 = lanes.Count((SiegeLane l) => !l.IsOpen);
			int num3 = lanes.Count - num2 - insideFormationCount;
			if (num3 > 0)
			{
				num -= (float)num3 * _openLaneEffect;
			}
			return num - (float)num2 * _existingLaneEffect;
		}
	}

	private enum TacticState
	{
		Unset,
		AssaultUnderRangedCover,
		TotalAttack,
		Retreating
	}

	public const float SameBehaviorFactor = 3f;

	public const float SameSideFactor = 5f;

	private const int ShockAssaultThresholdCount = 100;

	private readonly TeamAISiegeAttacker _teamAISiegeAttacker;

	private BreachWallsProgressIndicators _indicators;

	private List<Formation> _meleeFormations;

	private List<Formation> _rangedFormations;

	private int _laneCount;

	private List<SiegeLane> _cachedUsedSiegeLanes;

	private int _lanesInUse;

	private List<ArcherPosition> _cachedUsedArcherPositions;

	private TacticState _tacticState;

	private bool _isShockAssault;

	public TacticBreachWalls(Team team)
		: base(team)
	{
		_ = Mission.Current;
		_teamAISiegeAttacker = team.TeamAI as TeamAISiegeAttacker;
		_meleeFormations = new List<Formation>();
		_rangedFormations = new List<Formation>();
		_cachedUsedSiegeLanes = new List<SiegeLane>();
		_cachedUsedArcherPositions = new List<ArcherPosition>();
	}

	private void BalanceAssaultLanes(List<Formation> attackerFormations)
	{
		if (attackerFormations.Count < 2)
		{
			return;
		}
		int num = attackerFormations.Sum((Formation f) => f.CountOfUnitsWithoutDetachedOnes);
		int idealCount = num / attackerFormations.Count;
		int num2 = MathF.Max((int)((float)num * 0.2f), 1);
		foreach (Formation attackerFormation in attackerFormations)
		{
			int num3 = 0;
			while (idealCount - attackerFormation.CountOfUnitsWithoutDetachedOnes > num2 && attackerFormations.Any((Formation af) => af.CountOfUnitsWithoutDetachedOnes > idealCount) && num3 < attackerFormations.Count)
			{
				int a = idealCount - attackerFormation.CountOfUnitsWithoutDetachedOnes;
				Formation formation = TaleWorlds.Core.Extensions.MaxBy(attackerFormations, (Formation df) => df.CountOfUnitsWithoutDetachedOnes - idealCount);
				a = MathF.Min(a, formation.CountOfUnitsWithoutDetachedOnes - idealCount);
				formation.TransferUnits(attackerFormation, a);
				num3++;
			}
		}
	}

	private bool ShouldRetreat(List<SiegeLane> lanes, int insideFormationCount)
	{
		if (_indicators != null)
		{
			float num = base.Team.QuerySystem.RemainingPowerRatio / _indicators.StartingPowerRatio;
			float retreatThresholdRatio = _indicators.GetRetreatThresholdRatio(lanes, insideFormationCount);
			return num < retreatThresholdRatio;
		}
		return false;
	}

	private void AssignMeleeFormationsToLanes(List<Formation> meleeFormationsSource, List<SiegeLane> currentLanes)
	{
		List<Formation> list = new List<Formation>(meleeFormationsSource.Count);
		list.AddRange(meleeFormationsSource);
		List<SiegeLane> list2 = currentLanes.ToList();
		for (int i = 0; i < currentLanes.Count; i++)
		{
			SiegeLane siegeLane = currentLanes[i];
			Formation lastAssignedFormation = currentLanes[i].GetLastAssignedFormation(base.Team.TeamIndex);
			if (lastAssignedFormation != null && list.Contains(lastAssignedFormation))
			{
				lastAssignedFormation.AI.Side = siegeLane.LaneSide;
				lastAssignedFormation.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(lastAssignedFormation);
				lastAssignedFormation.AI.SetBehaviorWeight<BehaviorAssaultWalls>(1f);
				lastAssignedFormation.AI.SetBehaviorWeight<BehaviorUseSiegeMachines>(1f);
				lastAssignedFormation.AI.SetBehaviorWeight<BehaviorWaitForLadders>(1f);
				list2.Remove(siegeLane);
				list.Remove(lastAssignedFormation);
			}
		}
		while (list.Count > 0 && list2.Count > 0)
		{
			Formation largestFormation = TaleWorlds.Core.Extensions.MaxBy(list, (Formation mf) => mf.CountOfUnitsWithoutLooseDetachedOnes);
			SiegeLane siegeLane2 = TaleWorlds.Core.Extensions.MinBy(list2, (SiegeLane l) => l.GetCurrentAttackerPosition().DistanceSquaredWithLimit(largestFormation.CachedMedianPosition.GetNavMeshVec3(), 10000f));
			largestFormation.AI.Side = siegeLane2.LaneSide;
			largestFormation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(largestFormation);
			largestFormation.AI.SetBehaviorWeight<BehaviorAssaultWalls>(1f);
			largestFormation.AI.SetBehaviorWeight<BehaviorUseSiegeMachines>(1f);
			largestFormation.AI.SetBehaviorWeight<BehaviorWaitForLadders>(1f);
			siegeLane2.SetLastAssignedFormation(base.Team.TeamIndex, largestFormation);
			list.Remove(largestFormation);
			list2.Remove(siegeLane2);
		}
		bool flag = true;
		while (list.Count > 0)
		{
			if (list2.IsEmpty())
			{
				list2.AddRange(currentLanes);
				flag = false;
			}
			Formation nextBiggest = TaleWorlds.Core.Extensions.MaxBy(list, (Formation mf) => mf.CountOfUnitsWithoutLooseDetachedOnes);
			SiegeLane siegeLane3 = TaleWorlds.Core.Extensions.MinBy(list2, (SiegeLane l) => l.GetCurrentAttackerPosition().DistanceSquaredWithLimit(nextBiggest.CachedMedianPosition.GetNavMeshVec3(), 10000f));
			nextBiggest.AI.Side = siegeLane3.LaneSide;
			nextBiggest.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(nextBiggest);
			nextBiggest.AI.SetBehaviorWeight<BehaviorAssaultWalls>(1f);
			nextBiggest.AI.SetBehaviorWeight<BehaviorUseSiegeMachines>(1f);
			nextBiggest.AI.SetBehaviorWeight<BehaviorWaitForLadders>(1f);
			if (flag)
			{
				siegeLane3.SetLastAssignedFormation(base.Team.TeamIndex, nextBiggest);
			}
			list.Remove(nextBiggest);
			list2.Remove(siegeLane3);
		}
	}

	private void WellRoundedAssault(ref List<SiegeLane> currentLanes, ref List<ArcherPosition> archerPositions)
	{
		if (currentLanes.Count == 0)
		{
			Debug.Print("TeamAISiegeComponent.SiegeLanes.Count" + TeamAISiegeComponent.SiegeLanes.Count);
			for (int i = 0; i < TeamAISiegeComponent.SiegeLanes.Count; i++)
			{
				SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes[i];
				Debug.Print("lane " + i + " is breach " + siegeLane.IsBreach.ToString() + " is unusable " + siegeLane.CalculateIsLaneUnusable().ToString() + " has gate " + siegeLane.HasGate.ToString());
			}
			Debug.Print("_teamAISiegeAttacker.PrimarySiegeWeapons.Count " + _teamAISiegeAttacker.PrimarySiegeWeapons.Count);
			List<SiegeLadder> list = Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeLadder>().ToList();
			Debug.Print("ladders.Count = " + list.Count);
			List<SiegeTower> list2 = Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeTower>().ToList();
			Debug.Print("towers.Count = " + list2.Count);
			BatteringRam batteringRam = Mission.Current.ActiveMissionObjects.FindAllWithType<BatteringRam>().FirstOrDefault();
			Debug.Print("ram = " + batteringRam);
		}
		AssignMeleeFormationsToLanes(_meleeFormations, currentLanes);
		foreach (Formation rangedFormation2 in _rangedFormations)
		{
			rangedFormation2.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(rangedFormation2);
			rangedFormation2.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
		}
		if (archerPositions.Count <= 0)
		{
			return;
		}
		foreach (Formation rangedFormation in _rangedFormations.OrderByDescending((Formation rf) => rf.CountOfUnits))
		{
			if (archerPositions.IsEmpty())
			{
				archerPositions.AddRange(_teamAISiegeAttacker.ArcherPositions);
			}
			ArcherPosition archerPosition = null;
			if (rangedFormation.AI.ActiveBehavior is BehaviorSparseSkirmish)
			{
				archerPosition = archerPositions.FirstOrDefault((ArcherPosition ap) => ap.Entity == (rangedFormation.AI.ActiveBehavior as BehaviorSparseSkirmish).ArcherPosition);
			}
			if (archerPosition != null)
			{
				rangedFormation.AI.SetBehaviorWeight<BehaviorSparseSkirmish>(1f);
				archerPositions.Remove(archerPosition);
				continue;
			}
			ArcherPosition archerPosition2 = TaleWorlds.Core.Extensions.MinBy(archerPositions, (ArcherPosition ap) => ap.Entity.GlobalPosition.AsVec2.DistanceSquared(rangedFormation.CachedAveragePosition));
			rangedFormation.AI.SetBehaviorWeight<BehaviorSparseSkirmish>(1f);
			rangedFormation.AI.GetBehavior<BehaviorSparseSkirmish>().ArcherPosition = archerPosition2.Entity;
			archerPosition2.SetLastAssignedFormation(base.Team.TeamIndex, rangedFormation);
			archerPositions.Remove(archerPosition2);
		}
	}

	private void AllInAssault()
	{
		List<Formation> meleeFormationsSource = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0).ToList();
		List<SiegeLane> currentLanes = DetermineCurrentLanes();
		AssignMeleeFormationsToLanes(meleeFormationsSource, currentLanes);
	}

	private void StartTacticalRetreat()
	{
		StopUsingAllMachines();
		foreach (Formation item in base.FormationsIncludingSpecialAndEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				item.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(item);
				item.AI.SetBehaviorWeight<BehaviorRetreatToKeep>(1f);
			}
		}
	}

	protected override bool CheckAndSetAvailableFormationsChanged()
	{
		bool flag = false;
		int count = DetermineCurrentLanes().Count;
		if (_laneCount != count)
		{
			_laneCount = count;
			flag = true;
		}
		int aIControlledFormationCount = base.Team.GetAIControlledFormationCount();
		bool flag2 = aIControlledFormationCount != _AIControlledFormationCount;
		if (flag2)
		{
			_AIControlledFormationCount = aIControlledFormationCount;
			IsTacticReapplyNeeded = true;
		}
		bool flag3 = false;
		bool flag4 = false;
		if (_tacticState == TacticState.AssaultUnderRangedCover)
		{
			int num = 0;
			int num2 = 0;
			foreach (Formation item in base.Team.FormationsIncludingEmpty)
			{
				if (item.CountOfUnitsWithoutDetachedOnes > 0)
				{
					if (item.QuerySystem.IsInfantryFormation)
					{
						num++;
					}
					if (item.QuerySystem.IsRangedFormation)
					{
						num2++;
					}
				}
			}
			if (_meleeFormations.Count != num || _rangedFormations.Count != num2)
			{
				flag3 = true;
				_meleeFormations.Clear();
				_rangedFormations.Clear();
				foreach (Formation item2 in base.Team.FormationsIncludingEmpty)
				{
					if (item2.CountOfUnitsWithoutDetachedOnes > 0)
					{
						if (item2.QuerySystem.IsInfantryFormation)
						{
							_meleeFormations.Add(item2);
						}
						if (item2.QuerySystem.IsRangedFormation)
						{
							_rangedFormations.Add(item2);
						}
					}
				}
			}
		}
		else if (_tacticState == TacticState.TotalAttack)
		{
			int formationCount = base.Team.GetFormationCount();
			if ((formationCount < count && aIControlledFormationCount > 0) || (formationCount > count && (formationCount - aIControlledFormationCount < count || aIControlledFormationCount > 1)))
			{
				flag4 = true;
			}
		}
		return flag || flag2 || flag3 || flag4;
	}

	private void MergeFormationsIfLanesBecameUnavailable(ref List<SiegeLane> currentLanes)
	{
		int count = currentLanes.Count;
		if (_laneCount > count)
		{
			List<Formation> list = new List<Formation>();
			int num = 0;
			List<Formation> list2 = new List<Formation>();
			int num2 = 0;
			for (int i = 0; i < _cachedUsedSiegeLanes.Count; i++)
			{
				bool flag = false;
				SiegeLane siegeLane = _cachedUsedSiegeLanes[i];
				for (int j = 0; j < currentLanes.Count; j++)
				{
					if (siegeLane == currentLanes[j])
					{
						flag = true;
						break;
					}
				}
				Formation formation = null;
				if (!flag)
				{
					formation = siegeLane.GetLastAssignedFormation(base.Team.TeamIndex);
					if (formation != null && formation.IsSplittableByAI)
					{
						num += formation.CountOfUnits;
						list.Add(formation);
					}
				}
				else
				{
					formation = siegeLane.GetLastAssignedFormation(base.Team.TeamIndex);
					if (formation != null)
					{
						num2 += formation.CountOfUnits;
						list2.Add(formation);
					}
				}
			}
			int num3 = MathF.Ceiling((float)(num + num2) / (float)list2.Count);
			for (int k = 0; k < list.Count; k++)
			{
				Formation formation2 = list[k];
				int num4 = formation2.CountOfUnits;
				for (int l = 0; l < list2.Count; l++)
				{
					Formation formation3 = list2[l];
					int num5 = num3 - formation3.CountOfUnits;
					if (num5 > 0)
					{
						int num6 = MathF.Min(num4, num5);
						num4 -= num6;
						formation2.TransferUnits(formation3, num6);
					}
				}
			}
			_AIControlledFormationCount -= num;
		}
		_cachedUsedSiegeLanes = currentLanes;
		_laneCount = currentLanes.Count;
	}

	private void MergeFormationsIfArcherPositionsBecameUnavailable(ref List<ArcherPosition> currentArcherPositions)
	{
		int count = currentArcherPositions.Count;
		if (_cachedUsedArcherPositions.Count > count)
		{
			List<Formation> list = new List<Formation>();
			int num = 0;
			List<Formation> list2 = new List<Formation>();
			int num2 = 0;
			for (int i = 0; i < _cachedUsedArcherPositions.Count; i++)
			{
				bool flag = false;
				ArcherPosition archerPosition = _cachedUsedArcherPositions[i];
				for (int j = 0; j < currentArcherPositions.Count; j++)
				{
					if (archerPosition == currentArcherPositions[j])
					{
						flag = true;
						break;
					}
				}
				Formation formation = null;
				if (!flag)
				{
					formation = archerPosition.GetLastAssignedFormation(base.Team.TeamIndex);
					if (formation != null && formation.IsSplittableByAI)
					{
						num += formation.CountOfUnits;
						list.Add(formation);
					}
				}
				else
				{
					formation = archerPosition.GetLastAssignedFormation(base.Team.TeamIndex);
					if (formation != null)
					{
						num2 += formation.CountOfUnits;
						list2.Add(formation);
					}
				}
			}
			int num3 = MathF.Ceiling((float)(num + num2) / (float)list2.Count);
			for (int k = 0; k < list.Count; k++)
			{
				Formation formation2 = list[k];
				int num4 = formation2.CountOfUnits;
				for (int l = 0; l < list2.Count; l++)
				{
					Formation formation3 = list2[l];
					int num5 = num3 - formation3.CountOfUnits;
					if (num5 > 0)
					{
						int num6 = MathF.Min(num4, num5);
						num4 -= num6;
						formation2.TransferUnits(formation3, num6);
					}
				}
			}
			_AIControlledFormationCount -= num;
		}
		_cachedUsedArcherPositions = currentArcherPositions;
	}

	protected override void ManageFormationCounts()
	{
		List<SiegeLane> list = DetermineCurrentLanes();
		if (_indicators == null && base.Team.QuerySystem.EnemyUnitCount > 0)
		{
			_indicators = new BreachWallsProgressIndicators(base.Team, list);
		}
		if (_tacticState == TacticState.Retreating)
		{
			return;
		}
		int count = list.Count;
		if (_tacticState == TacticState.AssaultUnderRangedCover)
		{
			int rangedCount = MathF.Min(DetermineCurrentArcherPositions(list).Count, 8 - count);
			ManageFormationCounts(count, rangedCount, 0, 0);
			_meleeFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.QuerySystem.IsInfantryFormation && f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
			_rangedFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.QuerySystem.IsRangedFormation && f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
		}
		else if (_tacticState == TacticState.TotalAttack)
		{
			SplitFormationClassIntoGivenNumber((Formation f) => true, count);
		}
	}

	private void CheckAndChangeState()
	{
		if (_tacticState == TacticState.Retreating)
		{
			return;
		}
		bool isShockAssault = _isShockAssault;
		List<SiegeLane> currentLanes = DetermineCurrentLanes();
		int num = 0;
		foreach (Formation item in base.Team.FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0 && TeamAISiegeComponent.IsFormationInsideCastle(item, includeOnlyPositionedUnits: true))
			{
				num++;
			}
		}
		if (ShouldRetreat(currentLanes, num))
		{
			_tacticState = TacticState.Retreating;
			StartTacticalRetreat();
			IsTacticReapplyNeeded = false;
			return;
		}
		TacticState tacticState = TacticState.TotalAttack;
		List<ArcherPosition> archerPositions = null;
		if (_tacticState != TacticState.TotalAttack)
		{
			archerPositions = DetermineCurrentArcherPositions(currentLanes);
			if (archerPositions.Count > 0)
			{
				int num2 = MathF.Max(_meleeFormations.Sum((Formation mf) => mf.CountOfUnits), 1);
				int num3 = MathF.Max(_rangedFormations.Sum((Formation rf) => rf.CountOfUnits), 1);
				int num4 = num2 + num3;
				int num5 = num4 - base.Team.FormationsIncludingEmpty.Sum((Formation f) => f.CountOfUnitsWithoutDetachedOnes);
				tacticState = (((float)num2 / (float)num3 > 0.5f && (float)num5 / (float)num4 < 0.2f) ? TacticState.AssaultUnderRangedCover : TacticState.TotalAttack);
			}
		}
		if (tacticState != _tacticState || isShockAssault != _isShockAssault)
		{
			switch (tacticState)
			{
			case TacticState.AssaultUnderRangedCover:
				_tacticState = TacticState.AssaultUnderRangedCover;
				ManageFormationCounts();
				WellRoundedAssault(ref currentLanes, ref archerPositions);
				IsTacticReapplyNeeded = false;
				break;
			case TacticState.TotalAttack:
				_tacticState = TacticState.TotalAttack;
				ManageFormationCounts();
				AllInAssault();
				IsTacticReapplyNeeded = false;
				break;
			}
		}
	}

	private List<SiegeLane> DetermineCurrentLanes()
	{
		List<SiegeLane> list = TeamAISiegeComponent.SiegeLanes.Where((SiegeLane sl) => sl.IsBreach).ToList();
		if (list.Count >= 2)
		{
			if (_indicators == null || _indicators.InitialUnitCount > 100)
			{
				return list;
			}
			if (!_isShockAssault)
			{
				StopUsingAllMachines();
				_isShockAssault = true;
			}
			return list.Take(1).ToList();
		}
		List<SiegeLane> list2 = TeamAISiegeComponent.SiegeLanes.Where((SiegeLane sl) => !sl.CalculateIsLaneUnusable()).ToList();
		if (list2.Count > 0)
		{
			if (_indicators == null || _indicators.InitialUnitCount > 100)
			{
				if (list.Count >= 1)
				{
					return list2.Where((SiegeLane l) => l.IsBreach || l.PrimarySiegeWeapons.Any((IPrimarySiegeWeapon psw) => !(psw is SiegeLadder))).ToList();
				}
				return list2;
			}
			List<SiegeLane> result = new List<SiegeLane> { TaleWorlds.Core.Extensions.MaxBy(list2, (SiegeLane ul) => ul.CalculateLaneCapacity()) };
			if (!_isShockAssault)
			{
				StopUsingAllMachines();
				_isShockAssault = true;
			}
			return result;
		}
		return TeamAISiegeComponent.SiegeLanes.Where((SiegeLane sl) => sl.HasGate).ToList();
	}

	private List<ArcherPosition> DetermineCurrentArcherPositions(List<SiegeLane> currentLanes)
	{
		return _teamAISiegeAttacker.ArcherPositions.Where((ArcherPosition ap) => currentLanes.Any((SiegeLane cl) => ap.IsArcherPositionRelatedToSide(cl.LaneSide))).ToList();
	}

	public override void TickOccasionally()
	{
		if (!base.AreFormationsCreated)
		{
			return;
		}
		_meleeFormations.RemoveAll((Formation mf) => mf.CountOfUnitsWithoutDetachedOnes == 0);
		_rangedFormations.RemoveAll((Formation rf) => rf.CountOfUnitsWithoutDetachedOnes == 0);
		List<SiegeLane> currentLanes = DetermineCurrentLanes();
		MergeFormationsIfLanesBecameUnavailable(ref currentLanes);
		bool flag = CheckAndSetAvailableFormationsChanged();
		if (_indicators == null && base.Team.QuerySystem.EnemyUnitCount > 0)
		{
			_indicators = new BreachWallsProgressIndicators(base.Team, currentLanes);
			_indicators.StartingPowerRatio = base.Team.QuerySystem.TotalPowerRatio;
			_indicators.InitialLaneCount = currentLanes.Count;
			_indicators.InitialUnitCount = base.Team.QuerySystem.AllyUnitCount;
		}
		int num = 0;
		foreach (SiegeLane item in currentLanes)
		{
			num |= MathF.PowTwo32((int)item.LaneSide);
		}
		IsTacticReapplyNeeded = num != _lanesInUse;
		_lanesInUse = num;
		if (flag)
		{
			ManageFormationCounts();
		}
		CheckAndChangeState();
		switch (_tacticState)
		{
		case TacticState.AssaultUnderRangedCover:
		{
			List<ArcherPosition> currentArcherPositions = DetermineCurrentArcherPositions(currentLanes);
			if (flag || IsTacticReapplyNeeded)
			{
				_cachedUsedArcherPositions = currentArcherPositions;
				WellRoundedAssault(ref currentLanes, ref currentArcherPositions);
				IsTacticReapplyNeeded = false;
			}
			else if (_cachedUsedArcherPositions.Count != currentArcherPositions.Count)
			{
				MergeFormationsIfArcherPositionsBecameUnavailable(ref currentArcherPositions);
			}
			BalanceAssaultLanes(_meleeFormations.Where((Formation mf) => mf.IsAIControlled && mf.IsAITickedAfterSplit && (mf.AI.ActiveBehavior is BehaviorUseSiegeMachines || mf.AI.ActiveBehavior is BehaviorWaitForLadders)).ToList());
			break;
		}
		case TacticState.TotalAttack:
			if (flag || IsTacticReapplyNeeded)
			{
				AllInAssault();
				IsTacticReapplyNeeded = false;
			}
			BalanceAssaultLanes(base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.IsAIControlled && f.IsAITickedAfterSplit && (f.AI.ActiveBehavior is BehaviorUseSiegeMachines || f.AI.ActiveBehavior is BehaviorWaitForLadders)).ToList());
			break;
		case TacticState.Retreating:
			if (flag || IsTacticReapplyNeeded)
			{
				StartTacticalRetreat();
				IsTacticReapplyNeeded = false;
			}
			break;
		}
		_teamAISiegeAttacker.SetAreLaddersReady(currentLanes.Count((SiegeLane l) => l.IsBreach) > 1 || !currentLanes.Any((SiegeLane l) => l.PrimarySiegeWeapons.Any((IPrimarySiegeWeapon psw) => psw.HoldLadders)) || currentLanes.Any((SiegeLane l) => l.PrimarySiegeWeapons.Any((IPrimarySiegeWeapon psw) => psw.SendLadders)));
		CheckAndSetAvailableFormationsChanged();
		base.TickOccasionally();
	}

	protected internal override float GetTacticWeight()
	{
		return 10f;
	}
}
