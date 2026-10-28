using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.AI.AgentComponents;

public class ScriptedMovementComponent : AgentComponent
{
	private bool _isInDialogueRange;

	private readonly bool _isCharacterToTalkTo;

	private readonly float _dialogueTriggerProximity = 10f;

	private readonly float _agentSpeedLimit;

	private Agent _targetAgent;

	public ScriptedMovementComponent(Agent agent, bool isCharacterToTalkTo = false, float dialogueProximityOffset = 0f)
		: base(agent)
	{
		_isCharacterToTalkTo = isCharacterToTalkTo;
		_agentSpeedLimit = Agent.GetMaximumSpeedLimit();
		if (!_isCharacterToTalkTo)
		{
			Agent.SetMaximumSpeedLimit(MBRandom.RandomFloatRanged(0.2f, 0.3f), isMultiplier: true);
			_dialogueTriggerProximity += dialogueProximityOffset;
		}
	}

	public void SetTargetAgent(Agent targetAgent)
	{
		_targetAgent = targetAgent;
	}

	public override void OnTick(float dt)
	{
		if (!Agent.Mission.AllowAiTicking || !Agent.IsAIControlled || _targetAgent == null)
		{
			return;
		}
		bool flag = _targetAgent.State != AgentState.Routed && _targetAgent.State != AgentState.Deleted;
		if (!_isInDialogueRange)
		{
			float num = _targetAgent.Position.DistanceSquared(Agent.Position);
			_isInDialogueRange = num <= _dialogueTriggerProximity * _dialogueTriggerProximity;
			if (_isInDialogueRange)
			{
				Agent.SetScriptedFlags(Agent.GetScriptedFlags() & ~Agent.AIScriptedFrameFlags.DoNotRun);
				Agent.DisableScriptedMovement();
				if (flag)
				{
					Agent.SetLookAgent(_targetAgent);
				}
				Agent.SetMaximumSpeedLimit(_agentSpeedLimit, isMultiplier: false);
			}
			else
			{
				WorldPosition position = _targetAgent.Position.ToWorldPosition();
				Agent.SetScriptedPosition(ref position, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.DoNotRun);
			}
		}
		else if (!flag)
		{
			Agent.SetLookAgent(null);
		}
	}

	public bool ShouldConversationStartWithAgent()
	{
		if (_targetAgent != null && _isInDialogueRange)
		{
			return _isCharacterToTalkTo;
		}
		return false;
	}

	public void Reset()
	{
		_targetAgent = null;
		_isInDialogueRange = false;
	}
}
