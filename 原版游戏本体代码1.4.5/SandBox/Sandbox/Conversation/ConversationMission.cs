using System.Collections.Generic;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Conversation;

public static class ConversationMission
{
	public static Agent OneToOneConversationAgent => Campaign.Current.ConversationManager.OneToOneConversationAgent as Agent;

	public static CharacterObject OneToOneConversationCharacter => Campaign.Current.ConversationManager.OneToOneConversationCharacter;

	public static Agent CurrentSpeakerAgent => Campaign.Current.ConversationManager.SpeakerAgent as Agent;

	public static IEnumerable<Agent> ConversationAgents
	{
		get
		{
			foreach (IAgent conversationAgent in Campaign.Current.ConversationManager.ConversationAgents)
			{
				yield return conversationAgent as Agent;
			}
		}
	}

	public static void StartConversationWithAgent(Agent agent)
	{
		Mission.Current.GetMissionBehavior<MissionConversationLogic>()?.StartConversation(agent, setActionsInstantly: true);
	}
}
