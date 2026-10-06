using NavalDLC.Missions.AI.Behaviors;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.Tactics;

public class TacticNavalBalancedOffense : NavalTacticComponent
{
	private readonly TeamAINavalComponent _teamAINavalComponent;

	private readonly NavalShipsLogic _navalShipsLogic;

	public TacticNavalBalancedOffense(Team team)
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

	private void NavalEngage()
	{
		int num = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count / 2;
		int num2 = num - 1;
		bool flag = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count > _teamAINavalComponent.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count;
		Formation formation = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num];
		formation.AI.ResetBehaviorWeights();
		NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation);
		formation.AI.SetBehaviorWeight<BehaviorNavalEngageCorrespondingEnemy>(1f).SetTargetShipSideAndOrder(!flag, num);
		formation.AI.SetBehaviorWeight<BehaviorNavalSkirmish>(1f);
		formation.AI.SetBehaviorWeight<BehaviorNavalRamming>(1f);
		for (int i = num + 1; i < _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count; i++)
		{
			Formation formation2 = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[i];
			formation2.AI.ResetBehaviorWeights();
			NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation2);
			formation2.AI.SetBehaviorWeight<BehaviorNavalEngageCorrespondingEnemy>(1f).SetTargetShipSideAndOrder(!flag, i);
			formation2.AI.SetBehaviorWeight<BehaviorNavalSkirmish>(1f);
			formation2.AI.SetBehaviorWeight<BehaviorNavalRamming>(1f);
		}
		if (num2 >= 0 && num2 < _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count)
		{
			Formation formation3 = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num2];
			formation3.AI.ResetBehaviorWeights();
			NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation3);
			formation3.AI.SetBehaviorWeight<BehaviorNavalEngageCorrespondingEnemy>(1f).SetTargetShipSideAndOrder(flag, num2);
			formation3.AI.SetBehaviorWeight<BehaviorNavalSkirmish>(1f);
			formation3.AI.SetBehaviorWeight<BehaviorNavalRamming>(1f);
			for (int num3 = num2 - 1; num3 >= 0; num3--)
			{
				Formation formation4 = _teamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num3];
				formation4.AI.ResetBehaviorWeights();
				NavalTacticComponent.SetDefaultNavalBehaviorWeights(formation4);
				formation4.AI.SetBehaviorWeight<BehaviorNavalEngageCorrespondingEnemy>(1f).SetTargetShipSideAndOrder(flag, num3);
				formation4.AI.SetBehaviorWeight<BehaviorNavalSkirmish>(1f);
				formation4.AI.SetBehaviorWeight<BehaviorNavalRamming>(1f);
			}
		}
		foreach (MissionShip item in TeamAINavalComponent.TeamNavalQuerySystem.TeamShipsWithFormationsInLeftToRightOrder)
		{
			item.ShipOrder.SetEnforcedSailUsage(0);
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
						NavalEngage();
					}
					else if (!_teamAINavalComponent.UseSpawnPathApproachPosition || flag || IsTacticReapplyNeeded)
					{
						NavalApproach();
					}
				}
				IsTacticReapplyNeeded = false;
			}
		}
		base.TickOccasionally();
	}

	protected override float GetTacticWeight()
	{
		return MathF.Max(base.Team.QuerySystem.TotalPowerRatio, 0.1f);
	}
}
