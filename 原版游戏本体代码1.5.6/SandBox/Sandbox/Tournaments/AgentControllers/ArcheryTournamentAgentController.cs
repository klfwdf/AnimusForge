using System.Collections.Generic;
using System.Linq;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.AgentControllers;

public class ArcheryTournamentAgentController : AgentController
{
	private List<DestructableComponent> _targetList;

	private DestructableComponent _target;

	private TournamentArcheryMissionController _missionController;

	public override void OnInitialize()
	{
		_missionController = Mission.Current.GetMissionBehavior<TournamentArcheryMissionController>();
	}

	public void OnTick()
	{
		if (base.Owner.IsAIControlled)
		{
			UpdateTarget();
		}
	}

	public void SetTargets(List<DestructableComponent> targetList)
	{
		_targetList = targetList;
		_target = null;
	}

	private void UpdateTarget()
	{
		if (_target == null || _target.IsDestroyed)
		{
			SelectNewTarget();
		}
	}

	private void SelectNewTarget()
	{
		List<KeyValuePair<float, DestructableComponent>> list = new List<KeyValuePair<float, DestructableComponent>>();
		foreach (DestructableComponent target in _targetList)
		{
			float score = GetScore(target);
			if (score > 0f)
			{
				list.Add(new KeyValuePair<float, DestructableComponent>(score, target));
			}
		}
		if (list.Count == 0)
		{
			_target = null;
			base.Owner.DisableScriptedCombatMovement();
			WorldPosition position = base.Owner.GetWorldPosition();
			base.Owner.SetScriptedPosition(ref position, addHumanLikeDelay: false);
		}
		else
		{
			List<KeyValuePair<float, DestructableComponent>> list2 = list.OrderByDescending((KeyValuePair<float, DestructableComponent> x) => x.Key).ToList();
			int maxValue = MathF.Min(list2.Count, 5);
			_target = list2[MBRandom.RandomInt(maxValue)].Value;
		}
		if (_target != null)
		{
			base.Owner.SetScriptedTargetEntity(_target.GameEntity);
		}
	}

	private float GetScore(DestructableComponent target)
	{
		if (!target.IsDestroyed)
		{
			return 1f / base.Owner.Position.DistanceSquared(target.GameEntity.GlobalPosition);
		}
		return 0f;
	}

	public void OnTargetHit(Agent agent, DestructableComponent target)
	{
		if (agent == base.Owner || target == _target)
		{
			SelectNewTarget();
		}
	}
}
