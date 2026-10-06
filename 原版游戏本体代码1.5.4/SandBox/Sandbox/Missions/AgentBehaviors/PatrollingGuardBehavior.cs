using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class PatrollingGuardBehavior : AgentBehavior
{
	private readonly MissionAgentHandler _missionAgentHandler;

	private UsableMachine _target;

	public PatrollingGuardBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
		_missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (_target == null)
		{
			UsableMachine usableMachine = ((base.Navigator.SpecialTargetTag == null || base.Navigator.SpecialTargetTag.IsEmpty()) ? _missionAgentHandler.FindUnusedPointWithTagForAgent(base.OwnerAgent, "npc_common") : _missionAgentHandler.FindUnusedPointWithTagForAgent(base.OwnerAgent, base.Navigator.SpecialTargetTag));
			if (usableMachine != null)
			{
				_target = usableMachine;
				base.Navigator.SetTarget(_target);
			}
		}
		else if (base.Navigator.TargetUsableMachine == null)
		{
			base.Navigator.SetTarget(_target);
		}
	}

	public override float GetAvailability(bool isSimulation)
	{
		if (_missionAgentHandler.GetAllUsablePointsWithTag(base.Navigator.SpecialTargetTag).Count <= 0)
		{
			return 0f;
		}
		return 1f;
	}

	protected override void OnDeactivate()
	{
		_target = null;
		base.Navigator.ClearTarget();
	}

	public override string GetDebugInfo()
	{
		return "Guard patrol";
	}
}
