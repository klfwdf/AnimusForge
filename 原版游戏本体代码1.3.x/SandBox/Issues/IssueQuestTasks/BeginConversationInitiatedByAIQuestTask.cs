using System;
using SandBox.Conversation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Issues.IssueQuestTasks
{
	// Token: 0x020000B8 RID: 184
	public class BeginConversationInitiatedByAIQuestTask : QuestTaskBase
	{
		// Token: 0x0600079F RID: 1951 RVA: 0x00033EA2 File Offset: 0x000320A2
		public BeginConversationInitiatedByAIQuestTask(Agent agent, Action onSucceededAction, Action onFailedAction, Action onCanceledAction, DialogFlow dialogFlow = null)
			: base(dialogFlow, onSucceededAction, onFailedAction, onCanceledAction)
		{
			this._conversationAgent = agent;
			base.IsLogged = false;
		}

		// Token: 0x060007A0 RID: 1952 RVA: 0x00033EBE File Offset: 0x000320BE
		public void MissionTick(float dt)
		{
			if (Mission.Current.MainAgent == null || this._conversationAgent == null)
			{
				return;
			}
			if (!this._conversationOpened && Mission.Current.Mode != MissionMode.Conversation)
			{
				this.OpenConversation(this._conversationAgent);
			}
		}

		// Token: 0x060007A1 RID: 1953 RVA: 0x00033EF6 File Offset: 0x000320F6
		private void OpenConversation(Agent agent)
		{
			ConversationMission.StartConversationWithAgent(agent);
		}

		// Token: 0x060007A2 RID: 1954 RVA: 0x00033EFE File Offset: 0x000320FE
		protected override void OnFinished()
		{
			this._conversationAgent = null;
		}

		// Token: 0x060007A3 RID: 1955 RVA: 0x00033F07 File Offset: 0x00032107
		public override void SetReferences()
		{
			CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, new Action<float>(this.MissionTick));
		}

		// Token: 0x0400040D RID: 1037
		private bool _conversationOpened;

		// Token: 0x0400040E RID: 1038
		private Agent _conversationAgent;
	}
}
