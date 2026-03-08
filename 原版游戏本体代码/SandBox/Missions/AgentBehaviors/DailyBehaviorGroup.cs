using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000A2 RID: 162
	public class DailyBehaviorGroup : AgentBehaviorGroup
	{
		// Token: 0x060006D3 RID: 1747 RVA: 0x0002E954 File Offset: 0x0002CB54
		public DailyBehaviorGroup(AgentNavigator navigator, Mission mission)
			: base(navigator, mission)
		{
		}

		// Token: 0x060006D4 RID: 1748 RVA: 0x0002E960 File Offset: 0x0002CB60
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
				else if (this.CheckBehaviorTimer == null || this.CheckBehaviorTimer.Check(base.Mission.CurrentTime))
				{
					this.Think(isSimulation);
				}
				this.TickActiveBehaviors(dt, isSimulation);
			}
		}

		// Token: 0x060006D5 RID: 1749 RVA: 0x0002E9D0 File Offset: 0x0002CBD0
		public override void ConversationTick()
		{
			foreach (AgentBehavior agentBehavior in this.Behaviors)
			{
				if (agentBehavior.IsActive)
				{
					agentBehavior.ConversationTick();
				}
			}
		}

		// Token: 0x060006D6 RID: 1750 RVA: 0x0002EA2C File Offset: 0x0002CC2C
		private void Think(bool isSimulation)
		{
			float num = 0f;
			float[] array = new float[this.Behaviors.Count];
			for (int i = 0; i < this.Behaviors.Count; i++)
			{
				array[i] = this.Behaviors[i].GetAvailability(isSimulation);
				num += array[i];
			}
			if (num > 0f)
			{
				float num2 = MBRandom.RandomFloat * num;
				int j = 0;
				while (j < array.Length)
				{
					float num3 = array[j];
					num2 -= num3;
					if (num2 < 0f)
					{
						if (!this.Behaviors[j].IsActive)
						{
							base.DisableAllBehaviors();
							this.Behaviors[j].IsActive = true;
							this.CheckBehaviorTime = this.Behaviors[j].CheckTime;
							this.SetCheckBehaviorTimer(this.CheckBehaviorTime);
							return;
						}
						break;
					}
					else
					{
						j++;
					}
				}
			}
		}

		// Token: 0x060006D7 RID: 1751 RVA: 0x0002EB08 File Offset: 0x0002CD08
		private void TickActiveBehaviors(float dt, bool isSimulation)
		{
			foreach (AgentBehavior agentBehavior in this.Behaviors)
			{
				if (agentBehavior.IsActive)
				{
					agentBehavior.Tick(dt, isSimulation);
				}
			}
		}

		// Token: 0x060006D8 RID: 1752 RVA: 0x0002EB64 File Offset: 0x0002CD64
		private void SetCheckBehaviorTimer(float time)
		{
			if (this.CheckBehaviorTimer == null)
			{
				this.CheckBehaviorTimer = new Timer(base.Mission.CurrentTime, time, true);
				return;
			}
			this.CheckBehaviorTimer.Reset(base.Mission.CurrentTime, time);
		}

		// Token: 0x060006D9 RID: 1753 RVA: 0x0002EB9E File Offset: 0x0002CD9E
		public override float GetScore(bool isSimulation)
		{
			return 0.5f;
		}

		// Token: 0x060006DA RID: 1754 RVA: 0x0002EBA8 File Offset: 0x0002CDA8
		public override void OnAgentRemoved(Agent agent)
		{
			if (base.IsActive)
			{
				foreach (AgentBehavior agentBehavior in this.Behaviors)
				{
					if (agentBehavior.IsActive)
					{
						agentBehavior.OnAgentRemoved(agent);
					}
				}
			}
		}

		// Token: 0x060006DB RID: 1755 RVA: 0x0002EC0C File Offset: 0x0002CE0C
		protected override void OnActivate()
		{
			if (CampaignMission.Current.Location != null)
			{
				LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(base.OwnerAgent.Origin);
				if (locationCharacter != null && locationCharacter.ActionSetCode != locationCharacter.AlarmedActionSetCode)
				{
					AnimationSystemData animationSystemData = locationCharacter.GetAgentBuildData().AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSet(locationCharacter.ActionSetCode), locationCharacter.Character.GetStepSize(), false);
					base.OwnerAgent.SetActionSet(ref animationSystemData);
				}
			}
			this.Navigator.SetItemsVisibility(true);
			this.Navigator.SetSpecialItem();
		}

		// Token: 0x060006DC RID: 1756 RVA: 0x0002ECA2 File Offset: 0x0002CEA2
		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			this.CheckBehaviorTimer = null;
		}

		// Token: 0x060006DD RID: 1757 RVA: 0x0002ECB1 File Offset: 0x0002CEB1
		public override void ForceThink(float inSeconds)
		{
			if (MathF.Abs(inSeconds) < 1E-45f)
			{
				this.Think(false);
				return;
			}
			this.SetCheckBehaviorTimer(inSeconds);
		}
	}
}
