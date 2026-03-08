using System;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000AE RID: 174
	public class TalkBehavior : AgentBehavior
	{
		// Token: 0x06000746 RID: 1862 RVA: 0x00032126 File Offset: 0x00030326
		public TalkBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
			this._startConversation = true;
			this._doNotMove = true;
		}

		// Token: 0x06000747 RID: 1863 RVA: 0x00032140 File Offset: 0x00030340
		public override void Tick(float dt, bool isSimulation)
		{
			if (!this._startConversation || base.Mission.MainAgent == null || !base.Mission.MainAgent.IsActive() || base.Mission.Mode == MissionMode.Conversation || base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Barter)
			{
				return;
			}
			float interactionDistanceToUsable = base.OwnerAgent.GetInteractionDistanceToUsable(base.Mission.MainAgent);
			if (base.OwnerAgent.Position.DistanceSquared(base.Mission.MainAgent.Position) < (interactionDistanceToUsable + 3f) * (interactionDistanceToUsable + 3f) && base.Navigator.CanSeeAgent(base.Mission.MainAgent))
			{
				AgentNavigator navigator = base.Navigator;
				WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
				MatrixFrame frame = base.OwnerAgent.Frame;
				navigator.SetTargetFrame(worldPosition, frame.rotation.f.AsVec2.RotationInRadians, 1f, -10f, Agent.AIScriptedFrameFlags.DoNotRun, false);
				MissionConversationLogic missionBehavior = base.Mission.GetMissionBehavior<MissionConversationLogic>();
				if (missionBehavior != null && missionBehavior.IsReadyForConversation)
				{
					missionBehavior.OnAgentInteraction(base.Mission.MainAgent, base.OwnerAgent, -1);
					this._startConversation = false;
					return;
				}
			}
			else if (!this._doNotMove)
			{
				AgentNavigator navigator2 = base.Navigator;
				WorldPosition worldPosition2 = Agent.Main.GetWorldPosition();
				MatrixFrame frame = Agent.Main.Frame;
				navigator2.SetTargetFrame(worldPosition2, frame.rotation.f.AsVec2.RotationInRadians, 1f, -10f, Agent.AIScriptedFrameFlags.DoNotRun, false);
			}
		}

		// Token: 0x06000748 RID: 1864 RVA: 0x000322D8 File Offset: 0x000304D8
		public override float GetAvailability(bool isSimulation)
		{
			if (isSimulation)
			{
				return 0f;
			}
			if (this._startConversation && base.Mission.MainAgent != null && base.Mission.MainAgent.IsActive())
			{
				float num = base.OwnerAgent.GetInteractionDistanceToUsable(base.Mission.MainAgent) + 3f;
				if (base.OwnerAgent.Position.DistanceSquared(base.Mission.MainAgent.Position) < num * num && base.Mission.Mode != MissionMode.Conversation && !base.Mission.MainAgent.IsEnemyOf(base.OwnerAgent))
				{
					return 1f;
				}
			}
			return 0f;
		}

		// Token: 0x06000749 RID: 1865 RVA: 0x00032391 File Offset: 0x00030591
		public override string GetDebugInfo()
		{
			return "Talk";
		}

		// Token: 0x0600074A RID: 1866 RVA: 0x00032398 File Offset: 0x00030598
		protected override void OnDeactivate()
		{
			base.Navigator.ClearTarget();
			this.Disable();
		}

		// Token: 0x0600074B RID: 1867 RVA: 0x000323AB File Offset: 0x000305AB
		public void Disable()
		{
			this._startConversation = false;
			this._doNotMove = true;
		}

		// Token: 0x0600074C RID: 1868 RVA: 0x000323BB File Offset: 0x000305BB
		public void Enable(bool doNotMove)
		{
			this._startConversation = true;
			this._doNotMove = doNotMove;
		}

		// Token: 0x040003EC RID: 1004
		private bool _doNotMove;

		// Token: 0x040003ED RID: 1005
		private bool _startConversation;
	}
}
