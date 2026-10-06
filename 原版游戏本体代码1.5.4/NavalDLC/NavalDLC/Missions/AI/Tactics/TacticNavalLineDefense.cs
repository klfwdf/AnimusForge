using NavalDLC.Missions.AI.Behaviors;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.Tactics;

public class TacticNavalLineDefense : NavalTacticComponent
{
	private readonly TeamAINavalComponent _teamAINavalComponent;

	private readonly NavalShipsLogic _navalShipsLogic;

	public TacticNavalLineDefense(Team team)
		: base(team)
	{
		_teamAINavalComponent = team.TeamAI as TeamAINavalComponent;
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
	}

	protected override bool CheckAndSetAvailableFormationsChanged()
	{
		int aIControlledFormationCount = base.Team.GetAIControlledFormationCount();
		bool num = aIControlledFormationCount != _AIControlledFormationCount;
		if (num)
		{
			_AIControlledFormationCount = aIControlledFormationCount;
			IsTacticReapplyNeeded = true;
		}
		return num;
	}

	private void NavalDefensiveEngage()
	{
		int num = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count / 2;
		int num2 = num - 1;
		bool flag = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count > _teamAINavalComponent.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count;
		Formation formation = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num];
		formation.AI.ResetBehaviorWeights();
		NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation);
		formation.AI.SetBehaviorWeight<BehaviorNavalEngageCorrespondingEnemy>(1f).SetTargetShipSideAndOrder(!flag, num);
		formation.AI.SetBehaviorWeight<BehaviorNavalDefendInLine>(1f).SetTargetShipSideAndOrder(rightSide: true, num, isAnchor: true);
		formation.AI.SetBehaviorWeight<BehaviorNavalSkirmish>(1f);
		formation.AI.SetBehaviorWeight<BehaviorNavalRamming>(1f);
		for (int i = num + 1; i < _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count; i++)
		{
			Formation formation2 = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[i];
			formation2.AI.ResetBehaviorWeights();
			NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation2);
			formation2.AI.SetBehaviorWeight<BehaviorNavalEngageCorrespondingEnemy>(1f).SetTargetShipSideAndOrder(!flag, i);
			BehaviorNavalDefendInLine behaviorNavalDefendInLine = formation2.AI.SetBehaviorWeight<BehaviorNavalDefendInLine>(1f);
			_navalShipsLogic.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var _);
			behaviorNavalDefendInLine.SetTargetShipSideAndOrder(rightSide: true, i, isAnchor: false);
			formation2.AI.SetBehaviorWeight<BehaviorNavalSkirmish>(1f);
			formation2.AI.SetBehaviorWeight<BehaviorNavalRamming>(1f);
			formation = formation2;
		}
		if (num2 >= 0 && num2 < _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count)
		{
			formation = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num2];
			formation.AI.ResetBehaviorWeights();
			NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation);
			formation.AI.SetBehaviorWeight<BehaviorNavalEngageCorrespondingEnemy>(1f).SetTargetShipSideAndOrder(flag, num2);
			formation.AI.SetBehaviorWeight<BehaviorNavalDefendInLine>(1f).SetTargetShipSideAndOrder(rightSide: false, num2, isAnchor: false);
			formation.AI.SetBehaviorWeight<BehaviorNavalSkirmish>(1f);
			formation.AI.SetBehaviorWeight<BehaviorNavalRamming>(1f);
			for (int num3 = num2 - 1; num3 >= 0; num3--)
			{
				Formation formation3 = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num3];
				formation3.AI.ResetBehaviorWeights();
				NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation3);
				formation3.AI.SetBehaviorWeight<BehaviorNavalEngageCorrespondingEnemy>(1f).SetTargetShipSideAndOrder(flag, num3);
				BehaviorNavalDefendInLine behaviorNavalDefendInLine2 = formation3.AI.SetBehaviorWeight<BehaviorNavalDefendInLine>(1f);
				_navalShipsLogic.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var _);
				behaviorNavalDefendInLine2.SetTargetShipSideAndOrder(rightSide: false, num3, isAnchor: false);
				formation3.AI.SetBehaviorWeight<BehaviorNavalSkirmish>(1f);
				formation3.AI.SetBehaviorWeight<BehaviorNavalRamming>(1f);
				formation = formation3;
			}
		}
	}

	private void NavalDefensivePositioning()
	{
		int num = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count / 2;
		int num2 = num - 1;
		_ = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count;
		_ = TeamAINavalComponent.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count;
		Formation formation = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num];
		formation.AI.ResetBehaviorWeights();
		NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation);
		formation.AI.SetBehaviorWeight<BehaviorNavalDefendInLine>(1f).SetTargetShipSideAndOrder(rightSide: true, num, isAnchor: true);
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		for (int i = num + 1; i < TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count; i++)
		{
			Formation formation2 = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[i];
			formation2.AI.ResetBehaviorWeights();
			NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation2);
			BehaviorNavalDefendInLine behaviorNavalDefendInLine = formation2.AI.SetBehaviorWeight<BehaviorNavalDefendInLine>(1f);
			missionBehavior.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var _);
			behaviorNavalDefendInLine.SetTargetShipSideAndOrder(rightSide: true, i, isAnchor: false);
			formation = formation2;
		}
		if (num2 >= 0 && num2 < TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count)
		{
			formation = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num2];
			formation.AI.ResetBehaviorWeights();
			NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation);
			formation.AI.SetBehaviorWeight<BehaviorNavalDefendInLine>(1f).SetTargetShipSideAndOrder(rightSide: false, num2, isAnchor: false);
			for (int num3 = num2 - 1; num3 >= 0; num3--)
			{
				Formation formation3 = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num3];
				formation3.AI.ResetBehaviorWeights();
				NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation3);
				BehaviorNavalDefendInLine behaviorNavalDefendInLine2 = formation3.AI.SetBehaviorWeight<BehaviorNavalDefendInLine>(1f);
				missionBehavior.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var _);
				behaviorNavalDefendInLine2.SetTargetShipSideAndOrder(rightSide: false, num3, isAnchor: false);
				formation = formation3;
			}
		}
	}

	public override void TickOccasionally()
	{
		if (base.AreFormationsCreated && _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count > 0 && _teamAINavalComponent.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count > 0)
		{
			bool flag = CheckAndSetAvailableFormationsChanged();
			bool flag2 = flag || HasShipOrderChanged();
			if (!HasBattleBeenJoined)
			{
				CheckAndSetHasBattleBeenJoined();
				IsTacticReapplyNeeded |= HasBattleBeenJoined;
			}
			if (flag || flag2 || IsTacticReapplyNeeded)
			{
				if (flag)
				{
					ManageFormationCounts();
				}
				if (flag2)
				{
					_shipOrderCached = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.ToMBList();
				}
				if (_teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count > 0)
				{
					if (HasBattleBeenJoined)
					{
						NavalDefensiveEngage();
					}
					else if (!_teamAINavalComponent.UseSpawnPathApproachPosition || flag || IsTacticReapplyNeeded)
					{
						NavalDefensivePositioning();
					}
				}
				IsTacticReapplyNeeded = false;
			}
		}
		base.TickOccasionally();
	}

	protected override float GetTacticWeight()
	{
		if (base.Team.TeamAI.IsDefenseApplicable)
		{
			return 1.5f;
		}
		return 0f;
	}
}
