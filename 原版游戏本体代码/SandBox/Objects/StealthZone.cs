using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects
{
	// Token: 0x0200003E RID: 62
	public class StealthZone
	{
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x0600022F RID: 559 RVA: 0x0000D6E0 File Offset: 0x0000B8E0
		// (set) Token: 0x06000230 RID: 560 RVA: 0x0000D6E8 File Offset: 0x0000B8E8
		public bool AreAgentsActive { get; private set; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x06000231 RID: 561 RVA: 0x0000D6F1 File Offset: 0x0000B8F1
		// (set) Token: 0x06000232 RID: 562 RVA: 0x0000D6F9 File Offset: 0x0000B8F9
		public bool UseVolumeBox { get; private set; }

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x06000233 RID: 563 RVA: 0x0000D702 File Offset: 0x0000B902
		// (set) Token: 0x06000234 RID: 564 RVA: 0x0000D70A File Offset: 0x0000B90A
		public int EliminatedAgents { get; private set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x06000235 RID: 565 RVA: 0x0000D713 File Offset: 0x0000B913
		// (set) Token: 0x06000236 RID: 566 RVA: 0x0000D71B File Offset: 0x0000B91B
		private Timer ActivationTimer { get; set; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x06000237 RID: 567 RVA: 0x0000D724 File Offset: 0x0000B924
		// (set) Token: 0x06000238 RID: 568 RVA: 0x0000D72C File Offset: 0x0000B92C
		private Timer DeactivationTimer { get; set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x06000239 RID: 569 RVA: 0x0000D735 File Offset: 0x0000B935
		// (set) Token: 0x0600023A RID: 570 RVA: 0x0000D73D File Offset: 0x0000B93D
		private Timer ForceTargetTimer { get; set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x0600023B RID: 571 RVA: 0x0000D746 File Offset: 0x0000B946
		// (set) Token: 0x0600023C RID: 572 RVA: 0x0000D74E File Offset: 0x0000B94E
		public List<Agent> Agents { get; private set; }

		// Token: 0x14000008 RID: 8
		// (add) Token: 0x0600023D RID: 573 RVA: 0x0000D758 File Offset: 0x0000B958
		// (remove) Token: 0x0600023E RID: 574 RVA: 0x0000D790 File Offset: 0x0000B990
		public event Action OnActivated;

		// Token: 0x14000009 RID: 9
		// (add) Token: 0x0600023F RID: 575 RVA: 0x0000D7C8 File Offset: 0x0000B9C8
		// (remove) Token: 0x06000240 RID: 576 RVA: 0x0000D800 File Offset: 0x0000BA00
		public event Action OnDisactivated;

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x06000241 RID: 577 RVA: 0x0000D835 File Offset: 0x0000BA35
		// (set) Token: 0x06000242 RID: 578 RVA: 0x0000D83D File Offset: 0x0000BA3D
		public VolumeBox VolumeBox { get; private set; }

		// Token: 0x06000243 RID: 579 RVA: 0x0000D848 File Offset: 0x0000BA48
		public StealthZone(Agent targetAgent, bool useVolumeBox)
		{
			this.TargetAgent = targetAgent;
			this.EliminatedAgents = 0;
			this.AreAgentsActive = false;
			this.UseVolumeBox = useVolumeBox;
			if (this.UseVolumeBox)
			{
				GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("stealth_zone_volume");
				this.VolumeBox = ((gameEntity != null) ? gameEntity.GetFirstScriptOfType<VolumeBox>() : null);
			}
			this.IsZoneUsable = false;
		}

		// Token: 0x06000244 RID: 580 RVA: 0x0000D8AC File Offset: 0x0000BAAC
		public void SetStealthAgents(List<Agent> agents)
		{
			this.Agents = agents;
		}

		// Token: 0x06000245 RID: 581 RVA: 0x0000D8B8 File Offset: 0x0000BAB8
		public void Tick()
		{
			bool flag = this.TargetAgent != null && this.TargetAgent.IsActive() && (!this.UseVolumeBox || this.VolumeBox.IsPointIn(this.TargetAgent.Position));
			if (this.IsZoneUsable)
			{
				if (!this.AreAgentsActive && flag && this.ActivationTimer == null)
				{
					this.ActivationTimer = new Timer(Mission.Current.CurrentTime, 2f, true);
				}
				if (this.AreAgentsActive && !flag && this.DeactivationTimer == null)
				{
					this.DeactivationTimer = new Timer(Mission.Current.CurrentTime, 2f, true);
				}
				if (!flag)
				{
					this.ActivationTimer = null;
				}
				else
				{
					this.DeactivationTimer = null;
				}
				if (this.AreAgentsActive && !flag && this.DeactivationTimer.Check(Mission.Current.CurrentTime))
				{
					this.DeactivateStealthZone();
				}
				if (flag && !this.AreAgentsActive && this.ActivationTimer.Check(Mission.Current.CurrentTime) && this.Agents != null && this.Agents.Count > 0)
				{
					this.ActivateStealthZone();
					return;
				}
			}
			else
			{
				if (flag && this.ForceTargetTimer == null)
				{
					this.ForceTargetTimer = new Timer(Mission.Current.CurrentTime, 5f, true);
				}
				else if (!flag)
				{
					this.ForceTargetTimer = null;
				}
				if (flag && this.ForceTargetTimer.Check(Mission.Current.CurrentTime))
				{
					StealthZone.StealthZoneEvent onTargetInZone = this.OnTargetInZone;
					if (onTargetInZone != null)
					{
						onTargetInZone();
					}
					this.ForceTargetTimer.Reset(Mission.Current.CurrentTime);
				}
			}
		}

		// Token: 0x06000246 RID: 582 RVA: 0x0000DA60 File Offset: 0x0000BC60
		private void ActivateStealthZone()
		{
			this.AreAgentsActive = true;
			this.ActivationTimer = null;
			this.DeactivationTimer = null;
			this.ActivateStealthZoneInternal();
			this.OnActivated();
		}

		// Token: 0x06000247 RID: 583 RVA: 0x0000DA88 File Offset: 0x0000BC88
		private void DeactivateStealthZone()
		{
			this.AreAgentsActive = false;
			this.ActivationTimer = null;
			this.DeactivationTimer = null;
			this.DeactivateStealthZoneInternal();
			this.OnDisactivated();
		}

		// Token: 0x06000248 RID: 584 RVA: 0x0000DAB0 File Offset: 0x0000BCB0
		private void ActivateStealthZoneInternal()
		{
			Mission.Current.SetMissionMode(MissionMode.Stealth, false);
			Mission.Current.IsInventoryAccessible = false;
			Mission.Current.IsQuestScreenAccessible = false;
			this.EliminatedAgents = 0;
			foreach (Agent agent in this.Agents)
			{
				if (agent.IsActive())
				{
					agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanGetAlarmed);
					this.SetStealthMode(agent, true);
					agent.SetTeam(Mission.Current.PlayerEnemyTeam, true);
				}
			}
		}

		// Token: 0x06000249 RID: 585 RVA: 0x0000DB58 File Offset: 0x0000BD58
		private void DeactivateStealthZoneInternal()
		{
			Mission.Current.SetMissionMode(MissionMode.StartUp, false);
			Mission.Current.IsInventoryAccessible = true;
			Mission.Current.IsQuestScreenAccessible = true;
			foreach (Agent agent in this.Agents)
			{
				if (agent.IsActive())
				{
					agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanGetAlarmed);
					this.SetStealthMode(agent, false);
					agent.Health = agent.HealthLimit;
					agent.SetTeam(Team.Invalid, true);
				}
			}
			this.EliminatedAgents = 0;
		}

		// Token: 0x0600024A RID: 586 RVA: 0x0000DC08 File Offset: 0x0000BE08
		public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent)
		{
			if (!affectedAgent.IsMainAgent || this.Agents == null || !this.Agents.Contains(affectorAgent))
			{
				if (affectedAgent.IsAIControlled && affectedAgent.Team == Mission.Current.PlayerEnemyTeam && this.Agents.Contains(affectedAgent))
				{
					int eliminatedAgents = this.EliminatedAgents;
					this.EliminatedAgents = eliminatedAgents + 1;
					this.Agents.Remove(affectedAgent);
				}
				return;
			}
			StealthZone.StealthZoneEvent onTargetEliminated = this.OnTargetEliminated;
			if (onTargetEliminated == null)
			{
				return;
			}
			onTargetEliminated();
		}

		// Token: 0x0600024B RID: 587 RVA: 0x0000DC89 File Offset: 0x0000BE89
		public bool IsAgentInside(Agent agent)
		{
			VolumeBox volumeBox = this.VolumeBox;
			return volumeBox != null && volumeBox.IsPointIn(agent.Position);
		}

		// Token: 0x0600024C RID: 588 RVA: 0x0000DCA2 File Offset: 0x0000BEA2
		public void OnPlayerFlees()
		{
			if (this.IsAnyoneAlarmed() || this.IsAgentInside(this.TargetAgent))
			{
				StealthZone.StealthZoneEvent onTargetFlees = this.OnTargetFlees;
				if (onTargetFlees == null)
				{
					return;
				}
				onTargetFlees();
			}
		}

		// Token: 0x0600024D RID: 589 RVA: 0x0000DCCC File Offset: 0x0000BECC
		private void SetStealthMode(Agent agent, bool isActive)
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AlarmedBehaviorGroup alarmedBehaviorGroup = ((component != null) ? component.AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>() : null);
			if (alarmedBehaviorGroup != null)
			{
				alarmedBehaviorGroup.DoNotCheckForAlarmFactorIncrease = !isActive;
				alarmedBehaviorGroup.ResetAlarmFactor();
				if (isActive)
				{
					alarmedBehaviorGroup.DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy = false;
				}
				alarmedBehaviorGroup.IsActive = isActive;
				agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().IsActive = !isActive;
			}
		}

		// Token: 0x0600024E RID: 590 RVA: 0x0000DD2E File Offset: 0x0000BF2E
		public void ResetEvents()
		{
			this.OnTargetInZone = null;
			this.OnTargetFlees = null;
			this.OnTargetEliminated = null;
		}

		// Token: 0x0600024F RID: 591 RVA: 0x0000DD45 File Offset: 0x0000BF45
		public void DisableAll()
		{
			this.SetStealthAgents(null);
			this.ResetEvents();
			this.IsZoneUsable = false;
			this.AreAgentsActive = false;
			this.TargetAgent = null;
		}

		// Token: 0x06000250 RID: 592 RVA: 0x0000DD69 File Offset: 0x0000BF69
		private bool IsAnyoneAlarmed()
		{
			if (this.Agents != null)
			{
				return this.Agents.Any((Agent x) => x.IsAlarmed());
			}
			return false;
		}

		// Token: 0x040000DD RID: 221
		public const string VolumeBoxId = "stealth_zone_volume";

		// Token: 0x040000DF RID: 223
		public bool IsZoneUsable;

		// Token: 0x040000E6 RID: 230
		public StealthZone.StealthZoneEvent OnTargetFlees;

		// Token: 0x040000E7 RID: 231
		public StealthZone.StealthZoneEvent OnTargetEliminated;

		// Token: 0x040000E8 RID: 232
		public StealthZone.StealthZoneEvent OnTargetInZone;

		// Token: 0x040000EC RID: 236
		public Agent TargetAgent;

		// Token: 0x02000143 RID: 323
		// (Invoke) Token: 0x06000DED RID: 3565
		public delegate void StealthZoneEvent();
	}
}
