using System;
using System.Collections.Generic;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Conversation
{
	// Token: 0x020000C5 RID: 197
	public static class ConversationMission
	{
		// Token: 0x1700009B RID: 155
		// (get) Token: 0x06000812 RID: 2066 RVA: 0x0003A332 File Offset: 0x00038532
		public static Agent OneToOneConversationAgent
		{
			get
			{
				return Campaign.Current.ConversationManager.OneToOneConversationAgent as Agent;
			}
		}

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x06000813 RID: 2067 RVA: 0x0003A348 File Offset: 0x00038548
		public static CharacterObject OneToOneConversationCharacter
		{
			get
			{
				return Campaign.Current.ConversationManager.OneToOneConversationCharacter;
			}
		}

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x06000814 RID: 2068 RVA: 0x0003A359 File Offset: 0x00038559
		public static Agent CurrentSpeakerAgent
		{
			get
			{
				return Campaign.Current.ConversationManager.SpeakerAgent as Agent;
			}
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x06000815 RID: 2069 RVA: 0x0003A36F File Offset: 0x0003856F
		public static IEnumerable<Agent> ConversationAgents
		{
			get
			{
				foreach (IAgent agent in Campaign.Current.ConversationManager.ConversationAgents)
				{
					yield return agent as Agent;
				}
				IEnumerator<IAgent> enumerator = null;
				yield break;
				yield break;
			}
		}

		// Token: 0x06000816 RID: 2070 RVA: 0x0003A378 File Offset: 0x00038578
		public static void StartConversationWithAgent(Agent agent)
		{
			MissionConversationLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionConversationLogic>();
			if (missionBehavior == null)
			{
				return;
			}
			missionBehavior.StartConversation(agent, true, false);
		}
	}
}
