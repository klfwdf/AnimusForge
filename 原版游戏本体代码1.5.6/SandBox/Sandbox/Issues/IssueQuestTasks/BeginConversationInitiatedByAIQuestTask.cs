using System;
using SandBox.Conversation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Issues.IssueQuestTasks;

public class BeginConversationInitiatedByAIQuestTask : QuestTaskBase
{
	private bool _conversationOpened;

	private Agent _conversationAgent;

	public BeginConversationInitiatedByAIQuestTask(Agent agent, Action onSucceededAction, Action onFailedAction, Action onCanceledAction, DialogFlow dialogFlow = null)
		: base(dialogFlow, onSucceededAction, onFailedAction, onCanceledAction)
	{
		_conversationAgent = agent;
		base.IsLogged = false;
	}

	public void MissionTick(float dt)
	{
		if (Mission.Current.MainAgent != null && _conversationAgent != null && !_conversationOpened && Mission.Current.Mode != MissionMode.Conversation)
		{
			OpenConversation(_conversationAgent);
		}
	}

	private void OpenConversation(Agent agent)
	{
		ConversationMission.StartConversationWithAgent(agent);
	}

	protected override void OnFinished()
	{
		_conversationAgent = null;
	}

	public override void SetReferences()
	{
		CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, MissionTick);
	}
}
