using SandBox.Conversation.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class TalkBehavior : AgentBehavior
{
	private bool _doNotMove;

	private bool _startConversation;

	public TalkBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
		_startConversation = true;
		_doNotMove = true;
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (!_startConversation || base.Mission.MainAgent == null || !base.Mission.MainAgent.IsActive() || base.Mission.Mode == MissionMode.Conversation || base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Barter)
		{
			return;
		}
		float interactionDistanceToUsable = base.OwnerAgent.GetInteractionDistanceToUsable(base.Mission.MainAgent);
		if (base.OwnerAgent.Position.DistanceSquared(base.Mission.MainAgent.Position) < (interactionDistanceToUsable + 3f) * (interactionDistanceToUsable + 3f) && base.Navigator.CanSeeAgent(base.Mission.MainAgent))
		{
			base.Navigator.SetTargetFrame(base.OwnerAgent.GetWorldPosition(), base.OwnerAgent.Frame.rotation.f.AsVec2.RotationInRadians, 1f, -10f, Agent.AIScriptedFrameFlags.DoNotRun);
			MissionConversationLogic missionBehavior = base.Mission.GetMissionBehavior<MissionConversationLogic>();
			if (missionBehavior != null && missionBehavior.IsReadyForConversation)
			{
				missionBehavior.OnAgentInteraction(base.Mission.MainAgent, base.OwnerAgent, -1);
				_startConversation = false;
			}
		}
		else if (!_doNotMove)
		{
			base.Navigator.SetTargetFrame(Agent.Main.GetWorldPosition(), Agent.Main.Frame.rotation.f.AsVec2.RotationInRadians, 1f, -10f, Agent.AIScriptedFrameFlags.DoNotRun);
		}
	}

	public override float GetAvailability(bool isSimulation)
	{
		if (isSimulation)
		{
			return 0f;
		}
		if (_startConversation && base.Mission.MainAgent != null && base.Mission.MainAgent.IsActive())
		{
			float num = base.OwnerAgent.GetInteractionDistanceToUsable(base.Mission.MainAgent) + 3f;
			if (base.OwnerAgent.Position.DistanceSquared(base.Mission.MainAgent.Position) < num * num && base.Mission.Mode != MissionMode.Conversation && !base.Mission.MainAgent.IsEnemyOf(base.OwnerAgent))
			{
				return 1f;
			}
		}
		return 0f;
	}

	public override string GetDebugInfo()
	{
		return "Talk";
	}

	protected override void OnDeactivate()
	{
		base.Navigator.ClearTarget();
		Disable();
	}

	public void Disable()
	{
		_startConversation = false;
		_doNotMove = true;
	}

	public void Enable(bool doNotMove)
	{
		_startConversation = true;
		_doNotMove = doNotMove;
	}
}
