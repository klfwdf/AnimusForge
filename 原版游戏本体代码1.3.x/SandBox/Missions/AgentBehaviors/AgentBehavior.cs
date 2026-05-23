using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x0200009C RID: 156
	public abstract class AgentBehavior
	{
		// Token: 0x1700008B RID: 139
		// (get) Token: 0x06000677 RID: 1655 RVA: 0x0002C40B File Offset: 0x0002A60B
		public AgentNavigator Navigator
		{
			get
			{
				return this.BehaviorGroup.Navigator;
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x06000678 RID: 1656 RVA: 0x0002C418 File Offset: 0x0002A618
		// (set) Token: 0x06000679 RID: 1657 RVA: 0x0002C420 File Offset: 0x0002A620
		public bool IsActive
		{
			get
			{
				return this._isActive;
			}
			set
			{
				if (this._isActive != value)
				{
					this._isActive = value;
					if (this._isActive)
					{
						this.OnActivate();
						return;
					}
					this.OnDeactivate();
				}
			}
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x0600067A RID: 1658 RVA: 0x0002C447 File Offset: 0x0002A647
		public Agent OwnerAgent
		{
			get
			{
				return this.Navigator.OwnerAgent;
			}
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x0600067B RID: 1659 RVA: 0x0002C454 File Offset: 0x0002A654
		// (set) Token: 0x0600067C RID: 1660 RVA: 0x0002C45C File Offset: 0x0002A65C
		public Mission Mission { get; private set; }

		// Token: 0x0600067D RID: 1661 RVA: 0x0002C468 File Offset: 0x0002A668
		protected AgentBehavior(AgentBehaviorGroup behaviorGroup)
		{
			this.Mission = behaviorGroup.Mission;
			this.CheckTime = 40f + MBRandom.RandomFloat * 20f;
			this.BehaviorGroup = behaviorGroup;
			this._isActive = false;
		}

		// Token: 0x0600067E RID: 1662 RVA: 0x0002C4B7 File Offset: 0x0002A6B7
		public virtual float GetAvailability(bool isSimulation)
		{
			return 0f;
		}

		// Token: 0x0600067F RID: 1663 RVA: 0x0002C4BE File Offset: 0x0002A6BE
		public virtual void Tick(float dt, bool isSimulation)
		{
		}

		// Token: 0x06000680 RID: 1664 RVA: 0x0002C4C0 File Offset: 0x0002A6C0
		public virtual void ConversationTick()
		{
		}

		// Token: 0x06000681 RID: 1665 RVA: 0x0002C4C2 File Offset: 0x0002A6C2
		protected virtual void OnActivate()
		{
		}

		// Token: 0x06000682 RID: 1666 RVA: 0x0002C4C4 File Offset: 0x0002A6C4
		protected virtual void OnDeactivate()
		{
		}

		// Token: 0x06000683 RID: 1667 RVA: 0x0002C4C6 File Offset: 0x0002A6C6
		public virtual bool CheckStartWithBehavior()
		{
			return false;
		}

		// Token: 0x06000684 RID: 1668 RVA: 0x0002C4C9 File Offset: 0x0002A6C9
		public virtual void OnSpecialTargetChanged()
		{
		}

		// Token: 0x06000685 RID: 1669 RVA: 0x0002C4CB File Offset: 0x0002A6CB
		public virtual void SetCustomWanderTarget(UsableMachine customUsableMachine)
		{
		}

		// Token: 0x06000686 RID: 1670 RVA: 0x0002C4CD File Offset: 0x0002A6CD
		public virtual void OnAgentRemoved(Agent agent)
		{
		}

		// Token: 0x06000687 RID: 1671
		public abstract string GetDebugInfo();

		// Token: 0x0400037C RID: 892
		public float CheckTime = 15f;

		// Token: 0x0400037D RID: 893
		protected readonly AgentBehaviorGroup BehaviorGroup;

		// Token: 0x0400037E RID: 894
		private bool _isActive;
	}
}
