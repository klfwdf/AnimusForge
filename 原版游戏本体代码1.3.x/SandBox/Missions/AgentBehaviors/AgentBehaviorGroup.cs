using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x0200009D RID: 157
	public abstract class AgentBehaviorGroup
	{
		// Token: 0x1700008F RID: 143
		// (get) Token: 0x06000688 RID: 1672 RVA: 0x0002C4CF File Offset: 0x0002A6CF
		public Agent OwnerAgent
		{
			get
			{
				return this.Navigator.OwnerAgent;
			}
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x06000689 RID: 1673 RVA: 0x0002C4DC File Offset: 0x0002A6DC
		// (set) Token: 0x0600068A RID: 1674 RVA: 0x0002C4E4 File Offset: 0x0002A6E4
		public AgentBehavior ScriptedBehavior { get; private set; }

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x0600068B RID: 1675 RVA: 0x0002C4ED File Offset: 0x0002A6ED
		// (set) Token: 0x0600068C RID: 1676 RVA: 0x0002C4F5 File Offset: 0x0002A6F5
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

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x0600068D RID: 1677 RVA: 0x0002C51C File Offset: 0x0002A71C
		// (set) Token: 0x0600068E RID: 1678 RVA: 0x0002C524 File Offset: 0x0002A724
		public Mission Mission { get; private set; }

		// Token: 0x0600068F RID: 1679 RVA: 0x0002C52D File Offset: 0x0002A72D
		protected AgentBehaviorGroup(AgentNavigator navigator, Mission mission)
		{
			this.Mission = mission;
			this.Behaviors = new List<AgentBehavior>();
			this.Navigator = navigator;
			this._isActive = false;
			this.ScriptedBehavior = null;
		}

		// Token: 0x06000690 RID: 1680 RVA: 0x0002C568 File Offset: 0x0002A768
		public T AddBehavior<T>() where T : AgentBehavior
		{
			T t = Activator.CreateInstance(typeof(T), new object[] { this }) as T;
			if (t != null)
			{
				foreach (AgentBehavior agentBehavior in this.Behaviors)
				{
					if (agentBehavior.GetType() == t.GetType())
					{
						return agentBehavior as T;
					}
				}
				this.Behaviors.Add(t);
				return t;
			}
			return t;
		}

		// Token: 0x06000691 RID: 1681 RVA: 0x0002C61C File Offset: 0x0002A81C
		public T GetBehavior<T>() where T : AgentBehavior
		{
			foreach (AgentBehavior agentBehavior in this.Behaviors)
			{
				if (agentBehavior is T)
				{
					return (T)((object)agentBehavior);
				}
			}
			return default(T);
		}

		// Token: 0x06000692 RID: 1682 RVA: 0x0002C684 File Offset: 0x0002A884
		public bool HasBehavior<T>() where T : AgentBehavior
		{
			using (List<AgentBehavior>.Enumerator enumerator = this.Behaviors.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current is T)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000693 RID: 1683 RVA: 0x0002C6E0 File Offset: 0x0002A8E0
		public void RemoveBehavior<T>() where T : AgentBehavior
		{
			for (int i = 0; i < this.Behaviors.Count; i++)
			{
				if (this.Behaviors[i] is T)
				{
					bool isActive = this.Behaviors[i].IsActive;
					this.Behaviors[i].IsActive = false;
					if (this.ScriptedBehavior == this.Behaviors[i])
					{
						this.ScriptedBehavior = null;
					}
					this.Behaviors.RemoveAt(i);
					if (isActive)
					{
						this.ForceThink(0f);
					}
				}
			}
		}

		// Token: 0x06000694 RID: 1684 RVA: 0x0002C770 File Offset: 0x0002A970
		public void SetScriptedBehavior<T>() where T : AgentBehavior
		{
			foreach (AgentBehavior agentBehavior in this.Behaviors)
			{
				if (agentBehavior is T)
				{
					this.ScriptedBehavior = agentBehavior;
					this.ForceThink(0f);
					break;
				}
			}
			foreach (AgentBehavior agentBehavior2 in this.Behaviors)
			{
				if (agentBehavior2 != this.ScriptedBehavior)
				{
					agentBehavior2.IsActive = false;
				}
			}
		}

		// Token: 0x06000695 RID: 1685 RVA: 0x0002C824 File Offset: 0x0002AA24
		public void DisableScriptedBehavior()
		{
			if (this.ScriptedBehavior != null)
			{
				this.ScriptedBehavior.IsActive = false;
				this.ScriptedBehavior = null;
				this.ForceThink(0f);
			}
		}

		// Token: 0x06000696 RID: 1686 RVA: 0x0002C84C File Offset: 0x0002AA4C
		public void DisableAllBehaviors()
		{
			foreach (AgentBehavior agentBehavior in this.Behaviors)
			{
				agentBehavior.IsActive = false;
			}
		}

		// Token: 0x06000697 RID: 1687 RVA: 0x0002C8A0 File Offset: 0x0002AAA0
		public AgentBehavior GetActiveBehavior()
		{
			foreach (AgentBehavior agentBehavior in this.Behaviors)
			{
				if (agentBehavior.IsActive)
				{
					return agentBehavior;
				}
			}
			return null;
		}

		// Token: 0x06000698 RID: 1688 RVA: 0x0002C8FC File Offset: 0x0002AAFC
		public virtual void Tick(float dt, bool isSimulation)
		{
		}

		// Token: 0x06000699 RID: 1689 RVA: 0x0002C8FE File Offset: 0x0002AAFE
		public virtual void ConversationTick()
		{
		}

		// Token: 0x0600069A RID: 1690 RVA: 0x0002C900 File Offset: 0x0002AB00
		public virtual void OnAgentRemoved(Agent agent)
		{
		}

		// Token: 0x0600069B RID: 1691 RVA: 0x0002C902 File Offset: 0x0002AB02
		protected virtual void OnActivate()
		{
		}

		// Token: 0x0600069C RID: 1692 RVA: 0x0002C904 File Offset: 0x0002AB04
		protected virtual void OnDeactivate()
		{
			foreach (AgentBehavior agentBehavior in this.Behaviors)
			{
				agentBehavior.IsActive = false;
			}
		}

		// Token: 0x0600069D RID: 1693 RVA: 0x0002C958 File Offset: 0x0002AB58
		public virtual float GetScore(bool isSimulation)
		{
			return 0f;
		}

		// Token: 0x0600069E RID: 1694 RVA: 0x0002C95F File Offset: 0x0002AB5F
		public virtual void ForceThink(float inSeconds)
		{
		}

		// Token: 0x04000380 RID: 896
		public AgentNavigator Navigator;

		// Token: 0x04000381 RID: 897
		public List<AgentBehavior> Behaviors;

		// Token: 0x04000382 RID: 898
		protected float CheckBehaviorTime = 5f;

		// Token: 0x04000383 RID: 899
		protected Timer CheckBehaviorTimer;

		// Token: 0x04000385 RID: 901
		private bool _isActive;
	}
}
