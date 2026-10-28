using NavalDLC.Missions.AI.Behaviors;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.Tactics;

public abstract class NavalTacticComponent : TacticComponent
{
	private const float EngagementDistanceSquared = 40000f;

	protected readonly TeamAINavalComponent TeamAINavalComponent;

	protected bool HasBattleBeenJoined;

	protected MBReadOnlyList<Formation> _shipOrderCached;

	public NavalTacticComponent(Team team)
		: base(team)
	{
		TeamAINavalComponent = team.TeamAI as TeamAINavalComponent;
		_shipOrderCached = new MBReadOnlyList<Formation>();
	}

	public static void SetDefaultNavalBehaviorWeights(Formation f)
	{
		f.AI.SetBehaviorWeight<BehaviorNavalRemoveConnection>(1f);
	}

	protected void NavalApproach()
	{
		int num = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count / 2;
		int num2 = num - 1;
		_ = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count;
		_ = TeamAINavalComponent.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count;
		Formation formation = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num];
		formation.AI.ResetBehaviorWeights();
		SetDefaultNavalBehaviorWeights(formation);
		formation.AI.SetBehaviorWeight<BehaviorNavalApproachInLine>(1f).SetTargetShipSideAndOrder(rightSide: true, num, isAnchor: true);
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		for (int i = num + 1; i < TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count; i++)
		{
			Formation formation2 = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[i];
			formation2.AI.ResetBehaviorWeights();
			SetDefaultNavalBehaviorWeights(formation2);
			BehaviorNavalApproachInLine behaviorNavalApproachInLine = formation2.AI.SetBehaviorWeight<BehaviorNavalApproachInLine>(1f);
			missionBehavior.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var _);
			behaviorNavalApproachInLine.SetTargetShipSideAndOrder(rightSide: true, i, isAnchor: false);
			formation = formation2;
		}
		if (num2 >= 0 && num2 < TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count)
		{
			formation = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num2];
			formation.AI.ResetBehaviorWeights();
			SetDefaultNavalBehaviorWeights(formation);
			formation.AI.SetBehaviorWeight<BehaviorNavalApproachInLine>(1f).SetTargetShipSideAndOrder(rightSide: false, num2, isAnchor: false);
			for (int num3 = num2 - 1; num3 >= 0; num3--)
			{
				Formation formation3 = TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[num3];
				formation3.AI.ResetBehaviorWeights();
				SetDefaultNavalBehaviorWeights(formation3);
				BehaviorNavalApproachInLine behaviorNavalApproachInLine2 = formation3.AI.SetBehaviorWeight<BehaviorNavalApproachInLine>(1f);
				missionBehavior.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var _);
				behaviorNavalApproachInLine2.SetTargetShipSideAndOrder(rightSide: false, num3, isAnchor: false);
				formation = formation3;
			}
		}
		if (TeamAINavalComponent.UseSpawnPathApproachPosition || !base.Team.IsAttacker)
		{
			return;
		}
		Vec2 globalWindVelocity = Mission.Current.Scene.GetGlobalWindVelocity();
		Vec2 v = (TeamAINavalComponent.TeamNavalQuerySystem.AverageEnemyShipPosition - TeamAINavalComponent.TeamNavalQuerySystem.AverageShipPosition).Normalized();
		if (!(globalWindVelocity.Normalized().DotProduct(v) > 0.5f))
		{
			return;
		}
		foreach (MissionShip item in TeamAINavalComponent.TeamNavalQuerySystem.TeamShipsWithFormationsInLeftToRightOrder)
		{
			item.ShipOrder.SetEnforcedSailUsage(1);
		}
	}

	protected void CheckAndSetHasBattleBeenJoined()
	{
		if (TeamAINavalComponent.TeamNavalQuerySystem.ClosestDistanceSquaredToEnemyShip <= 40000f || base.Team.QuerySystem.DeathByRangedCount > 10 || (float)base.Team.QuerySystem.DeathByRangedCount > (float)base.Team.QuerySystem.AllyUnitCount * 0.1f)
		{
			HasBattleBeenJoined = true;
			return;
		}
		foreach (MissionShip item in TeamAINavalComponent.TeamNavalQuerySystem.TeamShipsWithFormationsInLeftToRightOrder)
		{
			if (item.GetIsConnectedToEnemy())
			{
				HasBattleBeenJoined = true;
				break;
			}
		}
	}

	protected bool HasShipOrderChanged()
	{
		for (int i = 0; i < _shipOrderCached.Count && i < TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count; i++)
		{
			if (_shipOrderCached[i] != TeamAINavalComponent.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder[i])
			{
				return true;
			}
		}
		return false;
	}

	protected override void ManageFormationCounts()
	{
		base.ManageFormationCounts();
		TeamAINavalComponent.TeamNavalQuerySystem.ForceExpireSameSideShipLists();
	}
}
