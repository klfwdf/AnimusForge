using System;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000A8 RID: 168
	public class InterruptingBehaviorGroup : AgentBehaviorGroup
	{
		// Token: 0x06000717 RID: 1815 RVA: 0x00030CA7 File Offset: 0x0002EEA7
		public InterruptingBehaviorGroup(AgentNavigator navigator, Mission mission)
			: base(navigator, mission)
		{
		}

		// Token: 0x06000718 RID: 1816 RVA: 0x00030CB4 File Offset: 0x0002EEB4
		public override void Tick(float dt, bool isSimulation)
		{
			if (base.IsActive)
			{
				if (base.ScriptedBehavior != null)
				{
					if (!base.ScriptedBehavior.IsActive)
					{
						base.DisableAllBehaviors();
						base.ScriptedBehavior.IsActive = true;
					}
				}
				else
				{
					int bestBehaviorIndex = this.GetBestBehaviorIndex(isSimulation);
					if (bestBehaviorIndex != -1 && !this.Behaviors[bestBehaviorIndex].IsActive)
					{
						base.DisableAllBehaviors();
						this.Behaviors[bestBehaviorIndex].IsActive = true;
					}
				}
				this.TickActiveBehaviors(dt, isSimulation);
			}
		}

		// Token: 0x06000719 RID: 1817 RVA: 0x00030D34 File Offset: 0x0002EF34
		private void TickActiveBehaviors(float dt, bool isSimulation)
		{
			for (int i = this.Behaviors.Count - 1; i >= 0; i--)
			{
				AgentBehavior agentBehavior = this.Behaviors[i];
				if (agentBehavior.IsActive)
				{
					agentBehavior.Tick(dt, isSimulation);
				}
			}
		}

		// Token: 0x0600071A RID: 1818 RVA: 0x00030D76 File Offset: 0x0002EF76
		public override float GetScore(bool isSimulation)
		{
			if (this.GetBestBehaviorIndex(isSimulation) != -1)
			{
				return 0.75f;
			}
			return 0f;
		}

		// Token: 0x0600071B RID: 1819 RVA: 0x00030D90 File Offset: 0x0002EF90
		private int GetBestBehaviorIndex(bool isSimulation)
		{
			float num = 0f;
			int result = -1;
			for (int i = 0; i < this.Behaviors.Count; i++)
			{
				float availability = this.Behaviors[i].GetAvailability(isSimulation);
				if (availability > num)
				{
					num = availability;
					result = i;
				}
			}
			return result;
		}

		// Token: 0x0600071C RID: 1820 RVA: 0x00030DD7 File Offset: 0x0002EFD7
		public override void ForceThink(float inSeconds)
		{
			this.Navigator.RefreshBehaviorGroups(false);
		}
	}
}
