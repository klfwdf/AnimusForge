using System;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects.Usables;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Source.Missions.AgentBehaviors
{
	// Token: 0x02000056 RID: 86
	public class BoardGameAgentBehavior : AgentBehavior
	{
		// Token: 0x06000369 RID: 873 RVA: 0x00013E18 File Offset: 0x00012018
		public BoardGameAgentBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
		}

		// Token: 0x0600036A RID: 874 RVA: 0x00013E24 File Offset: 0x00012024
		public override void Tick(float dt, bool isSimulation)
		{
			switch (this._state)
			{
			case BoardGameAgentBehavior.State.Idle:
				if (base.Navigator.TargetUsableMachine != this._chair && !this._chair.IsAgentFullySitting(base.OwnerAgent))
				{
					base.Navigator.SetTarget(this._chair, false, Agent.AIScriptedFrameFlags.None);
					this._state = BoardGameAgentBehavior.State.MovingToChair;
					return;
				}
				break;
			case BoardGameAgentBehavior.State.MovingToChair:
				if (this._chair.IsAgentFullySitting(base.OwnerAgent))
				{
					this._state = BoardGameAgentBehavior.State.Idle;
					return;
				}
				break;
			case BoardGameAgentBehavior.State.Finish:
				if (base.OwnerAgent.IsUsingGameObject && this._waitTimer == null)
				{
					base.Navigator.ClearTarget();
					this._waitTimer = new Timer(base.Mission.CurrentTime, 3f, true);
					return;
				}
				if (this._waitTimer != null)
				{
					if (this._waitTimer.Check(base.Mission.CurrentTime))
					{
						this.RemoveBoardGameBehaviorInternal();
						return;
					}
				}
				else
				{
					this.RemoveBoardGameBehaviorInternal();
				}
				break;
			default:
				return;
			}
		}

		// Token: 0x0600036B RID: 875 RVA: 0x00013F17 File Offset: 0x00012117
		protected override void OnDeactivate()
		{
			base.Navigator.ClearTarget();
			this._chair = null;
			this._state = BoardGameAgentBehavior.State.Idle;
			this._waitTimer = null;
		}

		// Token: 0x0600036C RID: 876 RVA: 0x00013F39 File Offset: 0x00012139
		public override string GetDebugInfo()
		{
			return "BoardGameAgentBehavior";
		}

		// Token: 0x0600036D RID: 877 RVA: 0x00013F40 File Offset: 0x00012140
		public override float GetAvailability(bool isSimulation)
		{
			return 1f;
		}

		// Token: 0x0600036E RID: 878 RVA: 0x00013F48 File Offset: 0x00012148
		private void RemoveBoardGameBehaviorInternal()
		{
			InterruptingBehaviorGroup behaviorGroup = base.OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>();
			if (behaviorGroup.GetBehavior<BoardGameAgentBehavior>() != null)
			{
				behaviorGroup.RemoveBehavior<BoardGameAgentBehavior>();
			}
		}

		// Token: 0x0600036F RID: 879 RVA: 0x00013F7C File Offset: 0x0001217C
		public static void AddTargetChair(Agent ownerAgent, Chair chair)
		{
			InterruptingBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>();
			bool flag = behaviorGroup.GetBehavior<BoardGameAgentBehavior>() == null;
			BoardGameAgentBehavior boardGameAgentBehavior = behaviorGroup.GetBehavior<BoardGameAgentBehavior>() ?? behaviorGroup.AddBehavior<BoardGameAgentBehavior>();
			boardGameAgentBehavior._chair = chair;
			boardGameAgentBehavior._state = BoardGameAgentBehavior.State.Idle;
			boardGameAgentBehavior._waitTimer = null;
			if (flag)
			{
				behaviorGroup.SetScriptedBehavior<BoardGameAgentBehavior>();
			}
		}

		// Token: 0x06000370 RID: 880 RVA: 0x00013FD0 File Offset: 0x000121D0
		public static void RemoveBoardGameBehaviorOfAgent(Agent ownerAgent)
		{
			BoardGameAgentBehavior behavior = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>().GetBehavior<BoardGameAgentBehavior>();
			if (behavior != null)
			{
				behavior._chair = null;
				behavior._state = BoardGameAgentBehavior.State.Finish;
			}
		}

		// Token: 0x06000371 RID: 881 RVA: 0x00014004 File Offset: 0x00012204
		public static bool IsAgentMovingToChair(Agent ownerAgent)
		{
			if (ownerAgent == null)
			{
				return false;
			}
			InterruptingBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>();
			BoardGameAgentBehavior boardGameAgentBehavior = ((behaviorGroup != null) ? behaviorGroup.GetBehavior<BoardGameAgentBehavior>() : null);
			return boardGameAgentBehavior != null && boardGameAgentBehavior._state == BoardGameAgentBehavior.State.MovingToChair;
		}

		// Token: 0x040001B4 RID: 436
		private const int FinishDelayAsSeconds = 3;

		// Token: 0x040001B5 RID: 437
		private Chair _chair;

		// Token: 0x040001B6 RID: 438
		private BoardGameAgentBehavior.State _state;

		// Token: 0x040001B7 RID: 439
		private Timer _waitTimer;

		// Token: 0x02000154 RID: 340
		private enum State
		{
			// Token: 0x040006B0 RID: 1712
			Idle,
			// Token: 0x040006B1 RID: 1713
			MovingToChair,
			// Token: 0x040006B2 RID: 1714
			Finish
		}
	}
}
