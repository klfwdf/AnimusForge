using System;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Issues.IssueQuestTasks;

public class FollowAgentQuestTask : QuestTaskBase
{
	private Agent _followedAgent;

	private CharacterObject _followedAgentChar;

	private GameEntity _targetEntity;

	private Agent _targetAgent;

	public FollowAgentQuestTask(Agent followedAgent, GameEntity targetEntity, Action onSucceededAction, Action onCanceledAction, DialogFlow dialogFlow = null)
		: base(dialogFlow, onSucceededAction, null, onCanceledAction)
	{
		_followedAgent = followedAgent;
		_followedAgentChar = (CharacterObject)_followedAgent.Character;
		_targetEntity = targetEntity;
		StartAgentMovement();
	}

	public FollowAgentQuestTask(Agent followedAgent, Agent targetAgent, Action onSucceededAction, Action onCanceledAction, DialogFlow dialogFlow = null)
		: base(dialogFlow, onSucceededAction, null, onCanceledAction)
	{
		_followedAgent = followedAgent;
		_targetAgent = targetAgent;
		StartAgentMovement();
	}

	private void StartAgentMovement()
	{
		if (_targetEntity != null)
		{
			UsableMachine firstScriptOfType = _targetEntity.GetFirstScriptOfType<UsableMachine>();
			ScriptBehavior.AddUsableMachineTarget(_followedAgent, firstScriptOfType);
		}
		else if (_targetAgent != null)
		{
			ScriptBehavior.AddAgentTarget(_followedAgent, _targetAgent);
		}
	}

	public void MissionTick(float dt)
	{
		ScriptBehavior scriptBehavior = (ScriptBehavior)_followedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehavior<ScriptBehavior>();
		if (scriptBehavior != null && scriptBehavior.IsNearTarget(_targetAgent) && _followedAgent.GetCurrentVelocity().LengthSquared < 0.0001f && _followedAgent.Position.DistanceSquared(Mission.Current.MainAgent.Position) < 16f)
		{
			Finish(FinishStates.Success);
		}
	}

	protected override void OnFinished()
	{
		_followedAgent = null;
		_followedAgentChar = null;
		_targetEntity = null;
		_targetAgent = null;
	}

	public override void SetReferences()
	{
		CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, MissionTick);
	}
}
