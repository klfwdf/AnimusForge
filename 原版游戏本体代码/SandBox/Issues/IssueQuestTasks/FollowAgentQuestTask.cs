using System;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Issues.IssueQuestTasks
{
	// Token: 0x020000B9 RID: 185
	public class FollowAgentQuestTask : QuestTaskBase
	{
		// Token: 0x060007A4 RID: 1956 RVA: 0x00033F20 File Offset: 0x00032120
		public FollowAgentQuestTask(Agent followedAgent, GameEntity targetEntity, Action onSucceededAction, Action onCanceledAction, DialogFlow dialogFlow = null)
			: base(dialogFlow, onSucceededAction, null, onCanceledAction)
		{
			this._followedAgent = followedAgent;
			this._followedAgentChar = (CharacterObject)this._followedAgent.Character;
			this._targetEntity = targetEntity;
			this.StartAgentMovement();
		}

		// Token: 0x060007A5 RID: 1957 RVA: 0x00033F58 File Offset: 0x00032158
		public FollowAgentQuestTask(Agent followedAgent, Agent targetAgent, Action onSucceededAction, Action onCanceledAction, DialogFlow dialogFlow = null)
			: base(dialogFlow, onSucceededAction, null, onCanceledAction)
		{
			this._followedAgent = followedAgent;
			this._targetAgent = targetAgent;
			this.StartAgentMovement();
		}

		// Token: 0x060007A6 RID: 1958 RVA: 0x00033F7C File Offset: 0x0003217C
		private void StartAgentMovement()
		{
			if (this._targetEntity != null)
			{
				UsableMachine firstScriptOfType = this._targetEntity.GetFirstScriptOfType<UsableMachine>();
				ScriptBehavior.AddUsableMachineTarget(this._followedAgent, firstScriptOfType);
				return;
			}
			if (this._targetAgent != null)
			{
				ScriptBehavior.AddAgentTarget(this._followedAgent, this._targetAgent);
			}
		}

		// Token: 0x060007A7 RID: 1959 RVA: 0x00033FCC File Offset: 0x000321CC
		public void MissionTick(float dt)
		{
			ScriptBehavior scriptBehavior = (ScriptBehavior)this._followedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehavior<ScriptBehavior>();
			if (scriptBehavior != null && scriptBehavior.IsNearTarget(this._targetAgent) && this._followedAgent.GetCurrentVelocity().LengthSquared < 0.0001f && this._followedAgent.Position.DistanceSquared(Mission.Current.MainAgent.Position) < 16f)
			{
				base.Finish(QuestTaskBase.FinishStates.Success);
			}
		}

		// Token: 0x060007A8 RID: 1960 RVA: 0x00034054 File Offset: 0x00032254
		protected override void OnFinished()
		{
			this._followedAgent = null;
			this._followedAgentChar = null;
			this._targetEntity = null;
			this._targetAgent = null;
		}

		// Token: 0x060007A9 RID: 1961 RVA: 0x00034072 File Offset: 0x00032272
		public override void SetReferences()
		{
			CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, new Action<float>(this.MissionTick));
		}

		// Token: 0x0400040F RID: 1039
		private Agent _followedAgent;

		// Token: 0x04000410 RID: 1040
		private CharacterObject _followedAgentChar;

		// Token: 0x04000411 RID: 1041
		private GameEntity _targetEntity;

		// Token: 0x04000412 RID: 1042
		private Agent _targetAgent;
	}
}
