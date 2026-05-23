using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticDefendCastle : TacticComponent
{
	public enum TacticState
	{
		ProperDefense,
		DesperateDefense,
		RetreatToKeep,
		SallyOut
	}

	private const float InfantrySallyOutEffectiveness = 1f;

	private const float RangedSallyOutEffectiveness = 0.3f;

	private const float CavalrySallyOutEffectiveness = 2f;

	private const float SallyOutDecisionPenalty = 3f;

	private readonly TeamAISiegeDefender _teamAISiegeDefender;

	private readonly List<MissionObject> _castleKeyPositions;

	private readonly List<SiegeLane> _lanes;

	private float _startingPowerRatio;

	private float _meleeDefenderPower;

	private float _laneThreatCapacity;

	private float _initialLaneDefensePowerRatio = -1f;

	private bool _isSallyingOut;

	private bool _areRangedNeededForLaneDefense;

	private bool _isTacticFailing;

	private bool _areSiegeWeaponsAbandoned;

	private Formation _invadingEnemyFormation;

	private Formation _emergencyFormation;

	private List<Formation> _meleeFormations;

	private List<Formation> _laneDefendingFormations = new List<Formation>();

	private List<Formation> _rangedFormations;

	private int _laneCount;

	public TacticState CurrentTacticState { get; private set; }

	public TacticDefendCastle(Team team)
		: base(team)
	{
		Mission current = Mission.Current;
		_castleKeyPositions = new List<MissionObject>();
		IEnumerable<CastleGate> enumerable = current.ActiveMissionObjects.FindAllWithType<CastleGate>();
		IEnumerable<WallSegment> collection = current.ActiveMissionObjects.FindAllWithType<WallSegment>();
		enumerable.FirstOrDefault();
		_castleKeyPositions.AddRange(enumerable);
		_castleKeyPositions.AddRange(collection);
		_teamAISiegeDefender = team.TeamAI as TeamAISiegeDefender;
		_lanes = TeamAISiegeComponent.SiegeLanes;
	}

	private static float GetFormationSallyOutPower(Formation formation)
	{
		if (formation.CountOfUnits > 0)
		{
			float typeMultiplier = (formation.PhysicalClass.IsMeleeCavalry() ? 2f : (formation.PhysicalClass.IsRanged() ? 0.3f : 1f));
			float sum = 0f;
			formation.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				sum += agent.CharacterPowerCached * typeMultiplier;
			});
			return sum;
		}
		return 0f;
	}

	private Formation GetStrongestSallyOutFormation()
	{
		float num = 0f;
		Formation result = null;
		foreach (Formation meleeFormation in _meleeFormations)
		{
			if (TeamAISiegeComponent.IsFormationInsideCastle(meleeFormation, includeOnlyPositionedUnits: true))
			{
				float formationSallyOutPower = GetFormationSallyOutPower(meleeFormation);
				if (formationSallyOutPower > num)
				{
					result = meleeFormation;
					num = formationSallyOutPower;
				}
			}
		}
		return result;
	}

	private bool MustRetreatToCastle()
	{
		return false;
	}

	private bool IsSallyOutApplicable()
	{
		float num = base.FormationsIncludingEmpty.Sum((Formation formation) => GetFormationSallyOutPower(formation));
		float num2 = 0f;
		foreach (Team team in Mission.Current.Teams)
		{
			if (team.Side.GetOppositeSide() != BattleSideEnum.Defender)
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					num2 += GetFormationSallyOutPower(item);
				}
			}
		}
		if (num > num2 * 3f)
		{
			return base.Team.QuerySystem.RemainingPowerRatio / _startingPowerRatio > 3f;
		}
		return false;
	}

	private void BalanceLaneDefenders(List<Formation> defenderFormations, out bool transferOccurred)
	{
		transferOccurred = false;
		int num = 3;
		SiegeLane[] array = new SiegeLane[num];
		int i;
		for (i = 0; i < num; i++)
		{
			array[i] = _lanes.FirstOrDefault((SiegeLane l) => l.LaneSide == (FormationAI.BehaviorSide)i);
		}
		float[] array2 = new float[num];
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			SiegeLane siegeLane = array[num2];
			array2[num2] = ((siegeLane == null || siegeLane.GetDefenseState() == SiegeLane.LaneDefenseStates.Token) ? 0f : siegeLane.CalculateLaneCapacity());
		}
		float num3 = array2.Sum();
		float[] array3 = new float[num];
		for (int num4 = 0; num4 < num; num4++)
		{
			array3[num4] = array2[num4] / num3;
		}
		int num5 = array.Count((SiegeLane l) => l != null && l.GetDefenseState() == SiegeLane.LaneDefenseStates.Token);
		int num6 = 15;
		int num7 = num5 * 15;
		int num8 = defenderFormations.Sum((Formation f) => f.CountOfUnitsWithoutDetachedOnes);
		int num9 = num8 - num7;
		IEnumerable<float> source = array3.Where((float ltp) => ltp > 0f);
		if (source.Any() && (float)num9 * source.Min() <= (float)num6)
		{
			num9 = num8;
			num6 = TaleWorlds.Library.MathF.Max((int)((float)num9 * 0.1f), 1);
		}
		int[] array4 = new int[num];
		for (int num10 = 0; num10 < num; num10++)
		{
			int num11 = (int)(array3[num10] * (float)num9);
			array4[num10] = ((num11 == 0) ? num6 : num11);
		}
		int[] array5 = new int[num];
		foreach (Formation defenderFormation in defenderFormations)
		{
			int side = (int)defenderFormation.AI.Side;
			array5[side] = defenderFormation.UnitsWithoutLooseDetachedOnes.Count - array4[side];
		}
		int num12 = TaleWorlds.Library.MathF.Max((int)((float)defenderFormations.Sum((Formation df) => df.CountOfUnits) * 0.2f), 1);
		foreach (Formation receiverDefenderFormation in defenderFormations)
		{
			int side2 = (int)receiverDefenderFormation.AI.Side;
			if (array5[side2] >= -num12)
			{
				continue;
			}
			foreach (Formation item in defenderFormations.Where((Formation df) => df != receiverDefenderFormation))
			{
				int side3 = (int)item.AI.Side;
				if (array5[side3] > num12)
				{
					int num13 = TaleWorlds.Library.MathF.Min(-array5[side2], array5[side3]);
					array5[side2] += num13;
					array5[side3] -= num13;
					item.TransferUnits(receiverDefenderFormation, num13);
					transferOccurred = true;
					if (array5[side2] == 0)
					{
						break;
					}
				}
			}
			if (!transferOccurred)
			{
				break;
			}
		}
	}

	private void ArcherShiftAround(List<Formation> p_RangedFormations)
	{
		List<Formation> list = p_RangedFormations.Where((Formation rf) => rf.AI.ActiveBehavior is BehaviorShootFromCastleWalls).ToList();
		if (list.Count < 2)
		{
			return;
		}
		float smallerFormationUnitPercentage = 0.1f;
		float mediumFormationUnitPercentage = 0.2f;
		float largerFormationUnitPercentage = 0.4f;
		float num = list.Sum((Formation f) => (!(f.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("many")) ? ((!(f.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("few")) ? mediumFormationUnitPercentage : smallerFormationUnitPercentage) : largerFormationUnitPercentage);
		smallerFormationUnitPercentage /= num;
		mediumFormationUnitPercentage /= num;
		largerFormationUnitPercentage /= num;
		int num2 = list.Sum((Formation f) => f.CountOfUnitsWithoutDetachedOnes);
		int smallFormationCount = TaleWorlds.Library.MathF.Max((int)((float)num2 * smallerFormationUnitPercentage), 1);
		int mediumFormationCount = TaleWorlds.Library.MathF.Max((int)((float)num2 * mediumFormationUnitPercentage), 1);
		int largeFormationCount = TaleWorlds.Library.MathF.Max((int)((float)num2 * largerFormationUnitPercentage), 1);
		int num3 = TaleWorlds.Library.MathF.Max((int)((float)num2 * 0.1f), 1);
		foreach (Formation item in list)
		{
			int num4 = ((item.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("many") ? largeFormationCount : ((item.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("few") ? smallFormationCount : mediumFormationCount));
			int num5 = 0;
			while (num4 - item.CountOfUnitsWithoutDetachedOnes > num3 && list.Any((Formation rf) => rf.CountOfUnitsWithoutDetachedOnes > ((rf.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("many") ? largeFormationCount : ((rf.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("few") ? smallFormationCount : mediumFormationCount))) && num5 < list.Count)
			{
				int a = num4 - item.CountOfUnitsWithoutDetachedOnes;
				Formation formation = TaleWorlds.Core.Extensions.MaxBy(list, (Formation rf) => rf.CountOfUnitsWithoutDetachedOnes - ((rf.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("many") ? largeFormationCount : ((rf.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("few") ? smallFormationCount : mediumFormationCount)));
				a = TaleWorlds.Library.MathF.Min(a, formation.CountOfUnitsWithoutDetachedOnes - ((formation.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("many") ? largeFormationCount : ((formation.AI.ActiveBehavior as BehaviorShootFromCastleWalls).ArcherPosition.HasTag("few") ? smallFormationCount : mediumFormationCount)));
				formation.TransferUnits(item, a);
				num5++;
			}
		}
	}

	protected override bool CheckAndSetAvailableFormationsChanged()
	{
		bool flag = false;
		if (_laneCount != _lanes.Count)
		{
			_laneCount = _lanes.Count;
			flag = true;
		}
		int aIControlledFormationCount = base.Team.GetAIControlledFormationCount();
		bool flag2 = aIControlledFormationCount != _AIControlledFormationCount;
		if (flag2)
		{
			_AIControlledFormationCount = aIControlledFormationCount;
			IsTacticReapplyNeeded = true;
		}
		return flag || flag2;
	}

	private int GetRequiredMeleeDefenderCount()
	{
		return _lanes.Count((SiegeLane l) => l.IsOpen || l.PrimarySiegeWeapons.Any((IPrimarySiegeWeapon lsw) => lsw is SiegeLadder) || l.PrimarySiegeWeapons.Any((IPrimarySiegeWeapon psw) => psw.HasCompletedAction() || (psw as SiegeWeapon).IsUsed));
	}

	protected override void ManageFormationCounts()
	{
		if (_startingPowerRatio == 0f)
		{
			_startingPowerRatio = base.Team.QuerySystem.RemainingPowerRatio;
		}
		switch (CurrentTacticState)
		{
		case TacticState.ProperDefense:
		{
			int requiredMeleeDefenderCount2 = GetRequiredMeleeDefenderCount();
			int rangedCount2 = TaleWorlds.Library.MathF.Min(_teamAISiegeDefender.ArcherPositions.Count, 8 - requiredMeleeDefenderCount2);
			ManageFormationCounts(requiredMeleeDefenderCount2, rangedCount2, 0, 0);
			_meleeFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation).ToList();
			_rangedFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation).ToList();
			break;
		}
		case TacticState.DesperateDefense:
		{
			int formationCount = base.Team.GetFormationCount();
			int aIControlledFormationCount = base.Team.GetAIControlledFormationCount();
			int num2 = formationCount - aIControlledFormationCount;
			int requiredMeleeDefenderCount = GetRequiredMeleeDefenderCount();
			if (aIControlledFormationCount <= 0 || formationCount == requiredMeleeDefenderCount || num2 > requiredMeleeDefenderCount)
			{
				break;
			}
			List<Formation> source = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.IsAIControlled).ToList();
			Formation biggestFormation = TaleWorlds.Core.Extensions.MaxBy(source, (Formation f) => f.CountOfUnitsWithoutDetachedOnes);
			foreach (Formation item in source.Where((Formation f) => f != biggestFormation))
			{
				item.TransferUnits(biggestFormation, item.CountOfUnits);
			}
			if (aIControlledFormationCount > 1)
			{
				biggestFormation.Split(aIControlledFormationCount);
			}
			break;
		}
		case TacticState.SallyOut:
		{
			int num = 1;
			int rangedCount = TaleWorlds.Library.MathF.Min(_teamAISiegeDefender.ArcherPositions.Count, 8 - num);
			ManageFormationCounts(num, rangedCount, 0, 0);
			_meleeFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation).ToList();
			_rangedFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation).ToList();
			break;
		}
		}
		if (_initialLaneDefensePowerRatio != -1f)
		{
			return;
		}
		_meleeDefenderPower = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0).Sum((Formation f) => f.QuerySystem.FormationMeleeFightingPower);
		_laneThreatCapacity = _lanes.Sum((SiegeLane l) => l.CalculateLaneCapacity());
		float num3 = 0f;
		foreach (Team team in base.Team.Mission.Teams)
		{
			if (!team.IsEnemyOf(base.Team))
			{
				continue;
			}
			foreach (Formation item2 in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item2.CountOfUnits > 0)
				{
					num3 += item2.QuerySystem.FormationPower;
				}
			}
		}
		int enemyUnitCount = base.Team.QuerySystem.EnemyUnitCount;
		float num4 = ((enemyUnitCount == 0) ? 0f : (num3 / (float)enemyUnitCount));
		_laneThreatCapacity = TaleWorlds.Library.MathF.Min(_lanes.Where((SiegeLane l) => l.IsOpen || l.PrimarySiegeWeapons.Any((IPrimarySiegeWeapon psw) => !(psw as SiegeWeapon).IsDeactivated)).Sum((SiegeLane l) => l.CalculateLaneCapacity()) * num4, num3);
		_initialLaneDefensePowerRatio = _meleeDefenderPower / _laneThreatCapacity;
	}

	protected override void StopUsingAllMachines()
	{
		base.StopUsingAllMachines();
		StopUsingStrategicAreas();
	}

	private void StopUsingStrategicAreas()
	{
		foreach (var item in base.Team.DetachmentManager.Detachments.Where(((IDetachment, DetachmentData) d) => d.Item1 is StrategicArea).ToList())
		{
			base.Team.DetachmentManager.DestroyDetachment(item.Item1);
		}
	}

	private void StartRetreatToKeep()
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

	private void DistributeRangedFormations()
	{
		List<Tuple<Formation, ArcherPosition>> list = _rangedFormations.CombineWith(_teamAISiegeDefender.ArcherPositions);
		while (list.Count > 0)
		{
			Tuple<Formation, ArcherPosition> tuple = TaleWorlds.Core.Extensions.MinBy(list, (Tuple<Formation, ArcherPosition> c) => c.Item1.CachedMedianPosition.AsVec2.DistanceSquared(c.Item2.Entity.GlobalPosition.AsVec2));
			Formation bestFormation = tuple.Item1;
			ArcherPosition bestArcherPosition = tuple.Item2;
			bestFormation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(bestFormation);
			bestFormation.AI.SetBehaviorWeight<BehaviorShootFromCastleWalls>(1f);
			bestFormation.AI.GetBehavior<BehaviorShootFromCastleWalls>().ArcherPosition = bestArcherPosition.Entity;
			list.RemoveAll((Tuple<Formation, ArcherPosition> c) => c.Item1 == bestFormation || c.Item2 == bestArcherPosition);
		}
	}

	private void ManageGatesForSallyingOut()
	{
		if ((_teamAISiegeDefender.InnerGate.IsGateOpen && _teamAISiegeDefender.OuterGate.IsGateOpen) || !_meleeFormations.Any((Formation mf) => TeamAISiegeComponent.IsFormationInsideCastle(mf, includeOnlyPositionedUnits: true)))
		{
			return;
		}
		CastleGate castleGate = ((!_teamAISiegeDefender.InnerGate.IsGateOpen) ? _teamAISiegeDefender.InnerGate : _teamAISiegeDefender.OuterGate);
		bool flag = false;
		foreach (Formation item in base.FormationsIncludingEmpty)
		{
			if (item.CountOfUnits != 0 && castleGate.IsUsedByFormation(item))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			GetStrongestSallyOutFormation()?.StartUsingMachine(castleGate);
		}
	}

	private void StartSallyOut()
	{
		DistributeRangedFormations();
		foreach (Formation meleeFormation in _meleeFormations)
		{
			meleeFormation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(meleeFormation);
			meleeFormation.AI.SetBehaviorWeight<BehaviorSallyOut>(1000f);
		}
	}

	private void CarryOutDefense(List<SiegeLane> defendedLanes, List<SiegeLane> lanesToBeRetaken, bool isEnemyInside, bool doRangedJoinMelee, out bool hasTransferOccurred)
	{
		hasTransferOccurred = false;
		List<Formation> list = new List<Formation>();
		List<Formation> list2 = new List<Formation>();
		int num = defendedLanes.Count + TaleWorlds.Library.MathF.Max(lanesToBeRetaken.Count, isEnemyInside ? 1 : 0);
		List<Formation> list3 = new List<Formation>();
		foreach (Formation meleeFormation in _meleeFormations)
		{
			if (meleeFormation.CountOfUnitsWithoutDetachedOnes > 0)
			{
				list3.Add(meleeFormation);
			}
		}
		if (list3.Count <= 0)
		{
			foreach (Formation item2 in base.FormationsIncludingEmpty)
			{
				if (item2.CountOfUnits > 0 && item2.QuerySystem.IsMeleeFormation)
				{
					list3.Add(item2);
				}
			}
		}
		int num2 = list3.Count;
		List<ArcherPosition> list4 = new List<ArcherPosition>();
		foreach (ArcherPosition archerPosition2 in _teamAISiegeDefender.ArcherPositions)
		{
			foreach (SiegeLane lane in _lanes)
			{
				if (archerPosition2.IsArcherPositionRelatedToSide(lane.LaneSide) && lane.LaneState > SiegeLane.LaneStateEnum.Used && lane.LaneState < SiegeLane.LaneStateEnum.Conceited)
				{
					list4.Add(archerPosition2);
				}
			}
		}
		List<Formation> list5 = new List<Formation>();
		foreach (Formation rangedFormation2 in _rangedFormations)
		{
			if (rangedFormation2.CountOfUnitsWithoutDetachedOnes > 0 && !list3.Contains(rangedFormation2))
			{
				list5.Add(rangedFormation2);
			}
		}
		if (list5.Count <= 0)
		{
			foreach (Formation item3 in base.FormationsIncludingEmpty)
			{
				if (item3.CountOfUnits > 0 && item3.QuerySystem.IsRangedFormation && !list3.Contains(item3))
				{
					list5.Add(item3);
				}
			}
		}
		foreach (Formation item4 in base.FormationsIncludingEmpty)
		{
			if (item4.CountOfUnits != 0 && !list3.Contains(item4) && !list5.Contains(item4) && item4.CountOfUnitsWithoutDetachedOnes > 0)
			{
				if (item4.QuerySystem.IsRangedFormation)
				{
					list5.Add(item4);
					continue;
				}
				list3.Add(item4);
				num2++;
			}
		}
		List<ArcherPosition> list6 = new List<ArcherPosition>();
		if (doRangedJoinMelee)
		{
			foreach (ArcherPosition item5 in list4)
			{
				foreach (SiegeLane lane2 in _lanes)
				{
					if (lane2.LaneState > SiegeLane.LaneStateEnum.Unused && lane2.IsUnderAttack() && item5.IsArcherPositionRelatedToSide(lane2.LaneSide))
					{
						list6.Add(item5);
					}
				}
			}
		}
		else
		{
			foreach (ArcherPosition item6 in list4)
			{
				bool flag = false;
				foreach (SiegeLane lane3 in _lanes)
				{
					if (lane3.LaneSide == item6.GetArcherPositionClosestSide() && lane3.LaneState >= SiegeLane.LaneStateEnum.Conceited)
					{
						list6.Add(item6);
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
				bool flag2 = false;
				foreach (SiegeLane lane4 in _lanes)
				{
					if (item6.IsArcherPositionRelatedToSide(lane4.LaneSide) && lane4.LaneState >= SiegeLane.LaneStateEnum.Conceited)
					{
						list6.Add(item6);
						flag2 = true;
						break;
					}
				}
				if (flag2 || _lanes.Count <= 0)
				{
					continue;
				}
				int num3 = int.MaxValue;
				SiegeLane siegeLane = null;
				foreach (SiegeLane lane5 in _lanes)
				{
					int num4 = SiegeQuerySystem.SideDistance(item6.ConnectedSides, 1 << (int)lane5.LaneSide);
					if (num4 < num3)
					{
						siegeLane = lane5;
						num3 = num4;
					}
				}
				if (siegeLane.LaneState >= SiegeLane.LaneStateEnum.Conceited)
				{
					list6.Add(item6);
					break;
				}
			}
		}
		List<Formation> list7 = new List<Formation>();
		foreach (ArcherPosition item7 in list6)
		{
			Formation lastAssignedFormation = item7.GetLastAssignedFormation(base.Team.TeamIndex);
			if (lastAssignedFormation != null && list5.Contains(lastAssignedFormation))
			{
				list7.Add(lastAssignedFormation);
			}
		}
		int count = list7.Count;
		if (num2 > num)
		{
			List<Formation> list8 = new List<Formation>();
			foreach (SiegeLane defendedLane in defendedLanes)
			{
				if (defendedLane.GetLastAssignedFormation(base.Team.TeamIndex) != null)
				{
					list8.Add(defendedLane.GetLastAssignedFormation(base.Team.TeamIndex));
				}
			}
			foreach (SiegeLane item8 in lanesToBeRetaken)
			{
				if (item8.GetLastAssignedFormation(base.Team.TeamIndex) != null)
				{
					list8.Add(item8.GetLastAssignedFormation(base.Team.TeamIndex));
				}
			}
			if (list8.Count > 0)
			{
				Formation formation = null;
				foreach (Formation excessFormation in list3)
				{
					if (!excessFormation.IsAIControlled || list8.Contains(excessFormation))
					{
						continue;
					}
					Formation formation2 = list8.FirstOrDefault((Formation aff) => aff.AI.Side == excessFormation.AI.Side);
					if (formation2 != null)
					{
						excessFormation.TransferUnits(formation2, excessFormation.CountOfUnits);
						hasTransferOccurred = true;
						formation = excessFormation;
						break;
					}
					float num5 = (float)(list8.Sum((Formation aff) => aff.CountOfUnits) + excessFormation.CountOfUnits) / (float)list8.Count;
					foreach (Formation item9 in list8)
					{
						int b = (int)Math.Ceiling(num5 - (float)item9.CountOfUnits);
						int num6 = TaleWorlds.Library.MathF.Min(excessFormation.CountOfUnits, b);
						if (num6 > 0)
						{
							excessFormation.TransferUnits(item9, num6);
						}
					}
					hasTransferOccurred = true;
					formation = excessFormation;
					break;
				}
				if (formation != null)
				{
					list3.Remove(formation);
				}
			}
			else
			{
				list3 = ConsolidateFormations(list3, num);
				hasTransferOccurred = true;
			}
			num2 = list3.Count;
		}
		List<Formation> list9 = list3.Concat(list7).ToList();
		if (list9.Count <= 0)
		{
			list9 = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0).ToList();
			list5.Clear();
		}
		if (num2 + count < num)
		{
			List<Formation> list10 = new List<Formation>();
			foreach (Formation item10 in list9)
			{
				if (item10.IsSplittableByAI)
				{
					list10.Add(item10);
				}
			}
			if (list10.Count > 0)
			{
				int num7 = 0;
				while (num2 + count + num7 < num && !hasTransferOccurred)
				{
					Formation largestFormation = TaleWorlds.Core.Extensions.MaxBy(list10, (Formation rf) => rf.UnitsWithoutLooseDetachedOnes.Count);
					List<Formation> list11 = largestFormation.Split().ToList();
					hasTransferOccurred = true;
					if (list11.Count < 2)
					{
						break;
					}
					num7++;
					Formation item = list11.FirstOrDefault((Formation rf) => rf != largestFormation);
					list10.Add(item);
					list9.Add(item);
				}
			}
		}
		List<SiegeLane> list12 = new List<SiegeLane>();
		List<Formation> list13 = new List<Formation>();
		foreach (SiegeLane toBeDefendedLane in defendedLanes)
		{
			Formation formation3 = list9.FirstOrDefault((Formation affml) => affml == toBeDefendedLane.GetLastAssignedFormation(base.Team.TeamIndex));
			if (formation3 != null)
			{
				formation3.AI.Side = toBeDefendedLane.LaneSide;
				formation3.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation3);
				formation3.AI.SetBehaviorWeight<BehaviorDefendCastleKeyPosition>(1f);
				toBeDefendedLane.SetLastAssignedFormation(base.Team.TeamIndex, formation3);
				list9.Remove(formation3);
				list12.Add(toBeDefendedLane);
				list13.Add(formation3);
				list.Add(formation3);
			}
		}
		List<SiegeLane> list14 = defendedLanes.Except(list12).ToList();
		List<SiegeLane> list15 = new List<SiegeLane>();
		foreach (SiegeLane toBeRetakenLane in lanesToBeRetaken)
		{
			Formation formation4 = list9.FirstOrDefault((Formation affml) => affml == toBeRetakenLane.GetLastAssignedFormation(base.Team.TeamIndex));
			if (formation4 != null)
			{
				formation4.AI.Side = toBeRetakenLane.LaneSide;
				formation4.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation4);
				formation4.AI.SetBehaviorWeight<BehaviorRetakeCastleKeyPosition>(1f);
				toBeRetakenLane.SetLastAssignedFormation(base.Team.TeamIndex, formation4);
				list9.Remove(formation4);
				list15.Add(toBeRetakenLane);
				list13.Add(formation4);
				list.Add(formation4);
				list12.Add(toBeRetakenLane);
			}
		}
		bool flag3 = false;
		while (list14.Count > 0)
		{
			SiegeLane firstToDefend = TaleWorlds.Core.Extensions.MaxBy(list14, (SiegeLane tbdl) => tbdl.CalculateLaneCapacity());
			Formation formation5 = list9.FirstOrDefault((Formation affml) => affml.AI.Side == firstToDefend.LaneSide);
			if (formation5 != null)
			{
				formation5.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation5);
				formation5.AI.SetBehaviorWeight<BehaviorDefendCastleKeyPosition>(1f);
				firstToDefend.SetLastAssignedFormation(base.Team.TeamIndex, formation5);
				list9.Remove(formation5);
				list13.Add(formation5);
				list.Add(formation5);
				list12.Add(firstToDefend);
			}
			else
			{
				if (list9.Count <= 0)
				{
					flag3 = true;
					list14.Clear();
					break;
				}
				Formation formation6 = TaleWorlds.Core.Extensions.MaxBy(list9, (Formation f) => f.QuerySystem.FormationPower);
				formation6.AI.Side = firstToDefend.LaneSide;
				formation6.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation6);
				formation6.AI.SetBehaviorWeight<BehaviorDefendCastleKeyPosition>(1f);
				firstToDefend.SetLastAssignedFormation(base.Team.TeamIndex, formation6);
				list9.Remove(formation6);
				list13.Add(formation6);
				list.Add(formation6);
				list12.Add(firstToDefend);
			}
			list14.Remove(firstToDefend);
		}
		List<SiegeLane> list16 = (flag3 ? new List<SiegeLane>() : lanesToBeRetaken.Except(list15).ToList());
		while (list16.Count > 0 && list9.Count > 0)
		{
			SiegeLane firstToRetake = TaleWorlds.Core.Extensions.MaxBy(lanesToBeRetaken, (SiegeLane ltbr) => ltbr.CalculateLaneCapacity());
			Formation formation7 = list9.FirstOrDefault((Formation affml) => affml.AI.Side == firstToRetake.LaneSide);
			if (formation7 != null)
			{
				formation7.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation7);
				formation7.AI.SetBehaviorWeight<BehaviorRetakeCastleKeyPosition>(1f);
				firstToRetake.SetLastAssignedFormation(base.Team.TeamIndex, formation7);
				list9.Remove(formation7);
				list13.Add(formation7);
				list.Add(formation7);
				list12.Add(firstToRetake);
			}
			else
			{
				if (list9.Count <= 0)
				{
					break;
				}
				Formation formation8 = TaleWorlds.Core.Extensions.MaxBy(list9, (Formation f) => f.QuerySystem.FormationPower);
				formation8.AI.Side = firstToRetake.LaneSide;
				formation8.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation8);
				formation8.AI.SetBehaviorWeight<BehaviorRetakeCastleKeyPosition>(1f);
				firstToRetake.SetLastAssignedFormation(base.Team.TeamIndex, formation8);
				list9.Remove(formation8);
				list13.Add(formation8);
				list.Add(formation8);
				list12.Add(firstToRetake);
			}
			list16.Remove(firstToRetake);
		}
		Formation formation9 = null;
		if (isEnemyInside && list9.Count > 0)
		{
			Formation formation10 = null;
			formation10 = ((_emergencyFormation == null || !list9.Contains(_emergencyFormation)) ? TaleWorlds.Core.Extensions.MaxBy(list9, (Formation affml) => affml.QuerySystem.FormationPower) : _emergencyFormation);
			formation10.AI.Side = FormationAI.BehaviorSide.BehaviorSideNotSet;
			formation10.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(formation10);
			formation10.AI.SetBehaviorWeight<BehaviorEliminateEnemyInsideCastle>(1f);
			list9.Remove(formation10);
			list13.Add(formation10);
			list.Add(formation10);
			formation9 = formation10;
		}
		IEnumerable<Formation> enumerable = list7.Except(list13);
		if (!hasTransferOccurred)
		{
			using IEnumerator<Formation> enumerator5 = enumerable.GetEnumerator();
			if (enumerator5.MoveNext())
			{
				Formation rangedFormation = enumerator5.Current;
				ArcherPosition archerPosition = list6.FirstOrDefault((ArcherPosition aptba) => aptba.GetLastAssignedFormation(base.Team.TeamIndex) == rangedFormation);
				List<SiegeLane> list17 = new List<SiegeLane>();
				foreach (SiegeLane item11 in defendedLanes.Union(lanesToBeRetaken))
				{
					if (item11.GetLastAssignedFormation(base.Team.TeamIndex) != null)
					{
						list17.Add(item11);
					}
				}
				bool flag4 = list17.Count > 0;
				SiegeLane siegeLane2 = null;
				if (archerPosition == null)
				{
					if (flag4)
					{
						siegeLane2 = TaleWorlds.Core.Extensions.MaxBy(list17, (SiegeLane arl) => arl.LaneState);
					}
				}
				else if (flag4)
				{
					List<SiegeLane> list18 = new List<SiegeLane>();
					foreach (SiegeLane item12 in list17)
					{
						if (archerPosition.IsArcherPositionRelatedToSide(item12.LaneSide))
						{
							list18.Add(item12);
						}
					}
					siegeLane2 = ((list18.Count <= 0) ? TaleWorlds.Core.Extensions.MaxBy(list17, (SiegeLane arl) => arl.LaneState) : TaleWorlds.Core.Extensions.MaxBy(list18, (SiegeLane rl) => rl.LaneState));
				}
				Formation target = ((siegeLane2 != null) ? siegeLane2.GetLastAssignedFormation(base.Team.TeamIndex) : formation9);
				rangedFormation.TransferUnits(target, rangedFormation.CountOfUnits);
				hasTransferOccurred = true;
			}
		}
		List<ArcherPosition> list19 = list4.Except(list6).ToList();
		List<Formation> list20 = list5.Except(list13).Except(enumerable).ToList();
		List<ArcherPosition> list21 = new List<ArcherPosition>();
		if (list20.Count > list19.Count)
		{
			if (list19.Count > 0 && !hasTransferOccurred)
			{
				list20 = ConsolidateFormations(list20, list19.Count);
				hasTransferOccurred = true;
			}
		}
		else if (list20.Count < list19.Count && list20.Count > 0 && !hasTransferOccurred)
		{
			int num8 = list19.Count - list20.Count;
			Formation formation11 = TaleWorlds.Core.Extensions.MaxBy(list20, (Formation rrf) => rrf.CountOfUnits);
			List<Formation> list22 = formation11.Split(num8 + 1).ToList();
			list22.Remove(formation11);
			hasTransferOccurred = true;
			list20.AddRange(list22);
		}
		foreach (ArcherPosition remainingArcherPosition in list19)
		{
			if (remainingArcherPosition.GetLastAssignedFormation(base.Team.TeamIndex) == null)
			{
				continue;
			}
			if (list20.Contains(remainingArcherPosition.GetLastAssignedFormation(base.Team.TeamIndex)))
			{
				Formation lastAssignedFormation2 = remainingArcherPosition.GetLastAssignedFormation(base.Team.TeamIndex);
				lastAssignedFormation2.AI.Side = remainingArcherPosition.GetArcherPositionClosestSide();
				lastAssignedFormation2.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(lastAssignedFormation2);
				lastAssignedFormation2.AI.SetBehaviorWeight<BehaviorShootFromCastleWalls>(1f).ArcherPosition = remainingArcherPosition.Entity;
				remainingArcherPosition.SetLastAssignedFormation(base.Team.TeamIndex, lastAssignedFormation2);
				list20.Remove(lastAssignedFormation2);
				list13.Add(remainingArcherPosition.GetLastAssignedFormation(base.Team.TeamIndex));
				list2.Add(remainingArcherPosition.GetLastAssignedFormation(base.Team.TeamIndex));
				list21.Add(remainingArcherPosition);
				continue;
			}
			Formation formation12 = list20.FirstOrDefault((Formation rrf) => remainingArcherPosition.IsArcherPositionRelatedToSide(rrf.AI.Side));
			if (formation12 == null)
			{
				formation12 = list20.FirstOrDefault();
			}
			if (formation12 != null)
			{
				formation12.AI.Side = remainingArcherPosition.GetArcherPositionClosestSide();
				formation12.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation12);
				formation12.AI.SetBehaviorWeight<BehaviorShootFromCastleWalls>(1f).ArcherPosition = remainingArcherPosition.Entity;
				remainingArcherPosition.SetLastAssignedFormation(base.Team.TeamIndex, formation12);
				list20.Remove(formation12);
				list13.Add(formation12);
				list2.Add(formation12);
				list21.Add(remainingArcherPosition);
				continue;
			}
			break;
		}
		list19 = list19.Except(list21).ToList();
		foreach (ArcherPosition remainingArcherPosition2 in list19)
		{
			Formation formation13 = list20.FirstOrDefault((Formation rrf) => remainingArcherPosition2.IsArcherPositionRelatedToSide(rrf.AI.Side));
			if (formation13 == null)
			{
				formation13 = list20.FirstOrDefault();
			}
			if (formation13 != null)
			{
				formation13.AI.Side = remainingArcherPosition2.GetArcherPositionClosestSide();
				formation13.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation13);
				formation13.AI.SetBehaviorWeight<BehaviorShootFromCastleWalls>(1f).ArcherPosition = remainingArcherPosition2.Entity;
				remainingArcherPosition2.SetLastAssignedFormation(base.Team.TeamIndex, formation13);
				list20.Remove(formation13);
				list13.Add(formation13);
				list2.Add(formation13);
				list21.Add(remainingArcherPosition2);
				continue;
			}
			break;
		}
		_meleeFormations = list;
		_laneDefendingFormations = new List<Formation>();
		foreach (Formation item13 in list)
		{
			if (item13.AI.Side < FormationAI.BehaviorSide.BehaviorSideNotSet)
			{
				_laneDefendingFormations.Add(item13);
			}
		}
		_rangedFormations = list2;
		foreach (SiegeLane item14 in _lanes.Except(list12))
		{
			item14.SetLastAssignedFormation(base.Team.TeamIndex, null);
		}
		_emergencyFormation = formation9;
		foreach (ArcherPosition item15 in _teamAISiegeDefender.ArcherPositions.Except(list21))
		{
			item15.SetLastAssignedFormation(base.Team.TeamIndex, null);
		}
	}

	private void CheckAndChangeState()
	{
		if (MustRetreatToCastle())
		{
			if (CurrentTacticState != TacticState.RetreatToKeep)
			{
				CurrentTacticState = TacticState.RetreatToKeep;
				ManageFormationCounts();
				StartRetreatToKeep();
			}
			return;
		}
		if (IsSallyOutApplicable())
		{
			if (CurrentTacticState == TacticState.SallyOut)
			{
				if (!_isSallyingOut)
				{
					ManageGatesForSallyingOut();
					if (_teamAISiegeDefender.InnerGate.IsGateOpen && _teamAISiegeDefender.OuterGate.IsGateOpen)
					{
						StartSallyOut();
						_isSallyingOut = true;
					}
				}
			}
			else
			{
				CurrentTacticState = TacticState.SallyOut;
			}
			return;
		}
		bool flag = false;
		if (_invadingEnemyFormation != null)
		{
			flag = TeamAISiegeComponent.IsFormationInsideCastle(_invadingEnemyFormation, includeOnlyPositionedUnits: true);
			if (!flag)
			{
				_invadingEnemyFormation = null;
			}
		}
		if (!flag)
		{
			flag = TeamAISiegeComponent.QuerySystem.InsideAttackerCount > 30;
			if (flag)
			{
				Formation formation = null;
				foreach (Team team in base.Team.Mission.Teams)
				{
					if (!team.IsEnemyOf(base.Team))
					{
						continue;
					}
					for (int i = 0; i < Math.Min(team.FormationsIncludingSpecialAndEmpty.Count, 8); i++)
					{
						Formation formation2 = team.FormationsIncludingSpecialAndEmpty[i];
						if (formation2.CountOfUnits > 0 && TeamAISiegeComponent.IsFormationInsideCastle(formation2, includeOnlyPositionedUnits: true))
						{
							formation = formation2;
							break;
						}
					}
				}
				if (formation != null)
				{
					_invadingEnemyFormation = formation;
				}
				else
				{
					flag = false;
				}
			}
		}
		List<SiegeLane> list = _lanes.Where((SiegeLane l) => l.LaneState == SiegeLane.LaneStateEnum.Conceited).ToList();
		List<SiegeLane> activeLanes = (from l in _lanes.Except(list)
			where l.GetDefenseState() != SiegeLane.LaneDefenseStates.Empty
			select l).ToList();
		if (flag)
		{
			list.Clear();
		}
		bool num = list.Count > 0;
		if (!num && !flag && activeLanes.Count == 0)
		{
			activeLanes = _lanes.Where((SiegeLane l) => l.HasGate).ToList();
		}
		if (num && activeLanes.Count > 0)
		{
			SiegeLane item = TaleWorlds.Core.Extensions.MinBy(list, (SiegeLane cl) => activeLanes.Min((SiegeLane al) => SiegeQuerySystem.SideDistance(1 << (int)al.LaneSide, 1 << (int)cl.LaneSide)));
			list.Clear();
			list.Add(item);
		}
		bool num2 = num || flag;
		_meleeFormations = _meleeFormations.Where((Formation mf) => mf.CountOfUnits > 0).ToList();
		_rangedFormations = _rangedFormations.Where((Formation rf) => rf.CountOfUnits > 0).ToList();
		int num3 = TaleWorlds.Library.MathF.Max(_meleeFormations.Sum((Formation mf) => mf.CountOfUnits), 1);
		int num4 = TaleWorlds.Library.MathF.Max(_rangedFormations.Sum((Formation rf) => rf.CountOfUnits), 1);
		int num5 = num3 + num4;
		if (!_areRangedNeededForLaneDefense)
		{
			_areRangedNeededForLaneDefense = (float)num3 < (float)num5 * 0.33f;
		}
		int num6 = 0;
		if (num2)
		{
			float num7 = (float)num3 - _lanes.Sum((SiegeLane l) => l.CalculateLaneCapacity());
			num6 = ((!flag) ? TaleWorlds.Library.MathF.Min((int)num7 / 15, list.Count) : ((num7 >= 15f) ? 1 : 0));
		}
		if (activeLanes.Count + list.Count + num6 <= 0)
		{
			_isTacticFailing = true;
			num6 = 1;
		}
		CarryOutDefense(activeLanes, list, flag && num6 > 0, _areRangedNeededForLaneDefense, out var hasTransferOccurred);
		if (!hasTransferOccurred)
		{
			BalanceLaneDefenders(_laneDefendingFormations.Where((Formation ldf) => ldf.IsAIControlled && ldf.AI.ActiveBehavior is BehaviorDefendCastleKeyPosition).ToList(), out hasTransferOccurred);
			if (!hasTransferOccurred)
			{
				ArcherShiftAround(_rangedFormations);
			}
		}
	}

	public override void TickOccasionally()
	{
		if (!base.AreFormationsCreated)
		{
			return;
		}
		if (!_areSiegeWeaponsAbandoned)
		{
			foreach (Team team in base.Team.Mission.Teams)
			{
				if (team.IsEnemyOf(base.Team))
				{
					if (team.QuerySystem.InsideWallsRatio > 0.5f)
					{
						StopUsingAllRangedSiegeWeapons();
						_areSiegeWeaponsAbandoned = true;
					}
					break;
				}
			}
		}
		CheckAndChangeState();
		CheckAndSetAvailableFormationsChanged();
		base.TickOccasionally();
	}

	protected internal override float GetTacticWeight()
	{
		if (_isTacticFailing)
		{
			return 5f;
		}
		return 10f;
	}
}
