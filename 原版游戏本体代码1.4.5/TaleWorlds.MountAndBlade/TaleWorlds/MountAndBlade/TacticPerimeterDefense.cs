using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticPerimeterDefense : TacticComponent
{
	private class DefenseFront
	{
		public Formation MeleeFormation;

		public Formation RangedFormation;

		public EnemyCluster MatchedEnemyCluster;

		public DefenseFront(EnemyCluster matchedEnemyCluster, Formation meleeFormation)
		{
			MatchedEnemyCluster = matchedEnemyCluster;
			MeleeFormation = meleeFormation;
			RangedFormation = null;
		}
	}

	private class EnemyCluster
	{
		private readonly MBList<Formation> _enemyFormations = new MBList<Formation>();

		private float _totalPower;

		public Vec2 AggregatePosition { get; private set; }

		public WorldPosition MedianAggregatePosition { get; private set; }

		public MBReadOnlyList<Formation> EnemyFormations => _enemyFormations;

		public void UpdateClusterData()
		{
			_totalPower = _enemyFormations.Sum((Formation ef) => ef.QuerySystem.FormationPower);
			AggregatePosition = Vec2.Zero;
			foreach (Formation enemyFormation in _enemyFormations)
			{
				AggregatePosition += enemyFormation.CachedAveragePosition * (enemyFormation.QuerySystem.FormationPower / _totalPower);
			}
			UpdateMedianPosition();
		}

		public void AddToCluster(Formation formation)
		{
			_enemyFormations.Add(formation);
			float totalPower = _totalPower;
			_totalPower += formation.QuerySystem.FormationPower;
			AggregatePosition = AggregatePosition * (totalPower / _totalPower) + formation.CachedAveragePosition * (formation.QuerySystem.FormationPower / _totalPower);
			UpdateMedianPosition();
		}

		public void RemoveFromCluster(Formation formation)
		{
			_enemyFormations.Remove(formation);
			float totalPower = _totalPower;
			_totalPower -= formation.QuerySystem.FormationPower;
			AggregatePosition -= formation.CachedAveragePosition * (formation.QuerySystem.FormationPower / totalPower);
			AggregatePosition *= totalPower / _totalPower;
			UpdateMedianPosition();
		}

		private void UpdateMedianPosition()
		{
			float num = float.MaxValue;
			foreach (Formation enemyFormation in _enemyFormations)
			{
				float num2 = enemyFormation.CachedMedianPosition.AsVec2.DistanceSquared(AggregatePosition);
				if (num2 < num)
				{
					num = num2;
					MedianAggregatePosition = enemyFormation.CachedMedianPosition;
				}
			}
		}
	}

	private WorldPosition _defendPosition;

	private readonly List<EnemyCluster> _enemyClusters;

	private readonly List<DefenseFront> _defenseFronts;

	private const float RetreatThresholdValue = 2f;

	private List<Formation> _meleeFormations;

	private List<Formation> _rangedFormations;

	private bool _isRetreatingToKeep;

	public TacticPerimeterDefense(Team team)
		: base(team)
	{
		_ = Mission.Current.Scene;
		FleePosition fleePosition = Mission.Current.GetFleePositionsForSide(BattleSideEnum.Defender).FirstOrDefault((FleePosition fp) => fp.GetSide() == BattleSideEnum.Defender);
		if (fleePosition != null)
		{
			_defendPosition = fleePosition.GameEntity.GlobalPosition.ToWorldPosition();
		}
		else
		{
			_defendPosition = WorldPosition.Invalid;
		}
		_enemyClusters = new List<EnemyCluster>();
		_defenseFronts = new List<DefenseFront>();
	}

	private void DetermineEnemyClusters()
	{
		List<Formation> list = new List<Formation>();
		float num = 0f;
		foreach (Team team in base.Team.Mission.Teams)
		{
			if (team.IsEnemyOf(base.Team))
			{
				num += team.QuerySystem.TeamPower;
			}
		}
		foreach (Team team2 in base.Team.Mission.Teams)
		{
			if (!team2.IsEnemyOf(base.Team))
			{
				continue;
			}
			for (int i = 0; i < Math.Min(team2.FormationsIncludingSpecialAndEmpty.Count, 8); i++)
			{
				Formation enemyFormation = team2.FormationsIncludingSpecialAndEmpty[i];
				if (enemyFormation.CountOfUnits > 0 && enemyFormation.QuerySystem.FormationPower < TaleWorlds.Library.MathF.Min(base.Team.QuerySystem.TeamPower, num) / 4f)
				{
					if (!_enemyClusters.Any((EnemyCluster ec) => ec.EnemyFormations.IndexOf(enemyFormation) >= 0))
					{
						list.Add(enemyFormation);
					}
					continue;
				}
				EnemyCluster enemyCluster = _enemyClusters.FirstOrDefault((EnemyCluster ec) => ec.EnemyFormations.IndexOf(enemyFormation) >= 0);
				if (enemyCluster != null)
				{
					if ((double)(_defendPosition.AsVec2 - enemyCluster.AggregatePosition).DotProduct(_defendPosition.AsVec2 - enemyFormation.CachedAveragePosition) >= 0.70710678118)
					{
						continue;
					}
					enemyCluster.RemoveFromCluster(enemyFormation);
				}
				List<EnemyCluster> list2 = _enemyClusters.Where((EnemyCluster c) => (double)(_defendPosition.AsVec2 - c.AggregatePosition).DotProduct(_defendPosition.AsVec2 - enemyFormation.CachedMedianPosition.AsVec2) >= 0.70710678118).ToList();
				if (list2.Count > 0)
				{
					TaleWorlds.Core.Extensions.MaxBy(list2, (EnemyCluster ec) => (_defendPosition.AsVec2 - ec.AggregatePosition).DotProduct(_defendPosition.AsVec2 - enemyFormation.CachedMedianPosition.AsVec2)).AddToCluster(enemyFormation);
				}
				else
				{
					EnemyCluster enemyCluster2 = new EnemyCluster();
					enemyCluster2.AddToCluster(enemyFormation);
					_enemyClusters.Add(enemyCluster2);
				}
			}
		}
		if (_enemyClusters.Count <= 0)
		{
			return;
		}
		foreach (Formation skippedFormation in list)
		{
			TaleWorlds.Core.Extensions.MaxBy(_enemyClusters, (EnemyCluster ec) => (_defendPosition.AsVec2 - ec.AggregatePosition).DotProduct(_defendPosition.AsVec2 - skippedFormation.CachedMedianPosition.AsVec2)).AddToCluster(skippedFormation);
		}
	}

	private bool MustRetreatToCastle()
	{
		return base.Team.QuerySystem.TotalPowerRatio / base.Team.QuerySystem.RemainingPowerRatio > 2f;
	}

	private void StartRetreatToKeep()
	{
		foreach (Formation item in base.FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				item.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(item);
				item.AI.SetBehaviorWeight<BehaviorRetreatToKeep>(1f);
			}
		}
	}

	private void CheckAndChangeState()
	{
		if (MustRetreatToCastle() && !_isRetreatingToKeep)
		{
			_isRetreatingToKeep = true;
			StartRetreatToKeep();
		}
	}

	private void ArrangeDefenseFronts()
	{
		_meleeFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && (f.QuerySystem.IsInfantryFormation || f.QuerySystem.IsCavalryFormation)).ToList();
		_rangedFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && (f.QuerySystem.IsRangedFormation || f.QuerySystem.IsRangedCavalryFormation)).ToList();
		int num = TaleWorlds.Library.MathF.Min(8 - _rangedFormations.Count, _enemyClusters.Count);
		if (_meleeFormations.Count != num)
		{
			SplitFormationClassIntoGivenNumber((Formation f) => f.QuerySystem.IsInfantryFormation || f.QuerySystem.IsCavalryFormation, num);
			_meleeFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && (f.QuerySystem.IsInfantryFormation || f.QuerySystem.IsCavalryFormation)).ToList();
		}
		int num2 = TaleWorlds.Library.MathF.Min(8 - num, _enemyClusters.Count);
		if (_rangedFormations.Count != num2)
		{
			SplitFormationClassIntoGivenNumber((Formation f) => f.QuerySystem.IsRangedFormation || f.QuerySystem.IsRangedCavalryFormation, num2);
			_rangedFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && (f.QuerySystem.IsRangedFormation || f.QuerySystem.IsRangedCavalryFormation)).ToList();
		}
		foreach (DefenseFront defenseFront2 in _defenseFronts)
		{
			defenseFront2.MatchedEnemyCluster.UpdateClusterData();
			BehaviorDefendKeyPosition behaviorDefendKeyPosition = defenseFront2.MeleeFormation.AI.SetBehaviorWeight<BehaviorDefendKeyPosition>(1f);
			behaviorDefendKeyPosition.EnemyClusterPosition = defenseFront2.MatchedEnemyCluster.MedianAggregatePosition;
			behaviorDefendKeyPosition.EnemyClusterPosition.SetVec2(defenseFront2.MatchedEnemyCluster.AggregatePosition);
		}
		IEnumerable<EnemyCluster> enumerable = _enemyClusters.Where((EnemyCluster ec) => _defenseFronts.All((DefenseFront df) => df.MatchedEnemyCluster != ec));
		List<Formation> list = _meleeFormations.Where((Formation mf) => _defenseFronts.All((DefenseFront df) => df.MeleeFormation != mf)).ToList();
		List<Formation> list2 = _rangedFormations.Where((Formation rf) => _defenseFronts.All((DefenseFront df) => df.RangedFormation != rf)).ToList();
		foreach (EnemyCluster item in enumerable)
		{
			if (list.IsEmpty())
			{
				break;
			}
			Formation formation = list[list.Count - 1];
			DefenseFront defenseFront = new DefenseFront(item, formation);
			formation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(formation);
			BehaviorDefendKeyPosition behaviorDefendKeyPosition2 = formation.AI.SetBehaviorWeight<BehaviorDefendKeyPosition>(1f);
			behaviorDefendKeyPosition2.DefensePosition = _defendPosition;
			behaviorDefendKeyPosition2.EnemyClusterPosition = item.MedianAggregatePosition;
			behaviorDefendKeyPosition2.EnemyClusterPosition.SetVec2(item.AggregatePosition);
			list.Remove(formation);
			if (!list2.IsEmpty())
			{
				Formation formation2 = list2[list2.Count - 1];
				formation2.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation2);
				formation2.AI.SetBehaviorWeight<BehaviorSkirmishBehindFormation>(1f).ReferenceFormation = formation;
				defenseFront.RangedFormation = formation2;
				list2.Remove(formation2);
				_defenseFronts.Add(defenseFront);
			}
		}
	}

	public override void TickOccasionally()
	{
		if (base.AreFormationsCreated)
		{
			CheckAndChangeState();
			if (!_isRetreatingToKeep)
			{
				DetermineEnemyClusters();
				ArrangeDefenseFronts();
			}
		}
	}

	protected internal override float GetTacticWeight()
	{
		if (_defendPosition.IsValid)
		{
			return 10f;
		}
		return 0f;
	}
}
