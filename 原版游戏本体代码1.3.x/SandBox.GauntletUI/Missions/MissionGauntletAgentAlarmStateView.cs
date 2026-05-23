using System;
using SandBox.View.Missions;
using SandBox.ViewModelCollection.Missions;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x02000018 RID: 24
	[OverrideView(typeof(MissionAgentAlarmStateView))]
	public class MissionGauntletAgentAlarmStateView : MissionAgentAlarmStateView
	{
		// Token: 0x0600016A RID: 362 RVA: 0x0000A193 File Offset: 0x00008393
		public MissionGauntletAgentAlarmStateView()
		{
			this._dataSource = new MissionAgentAlarmStateVM();
		}

		// Token: 0x0600016B RID: 363 RVA: 0x0000A1A8 File Offset: 0x000083A8
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			this._dataSource.Initialize(base.Mission, base.MissionScreen.CombatCamera);
			this._layer = new GauntletLayer("MissionAlarmState", 10, false);
			this._layer.LoadMovie("AgentAlarmStateMissionView", this._dataSource);
			base.MissionScreen.AddLayer(this._layer);
		}

		// Token: 0x0600016C RID: 364 RVA: 0x0000A212 File Offset: 0x00008412
		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			base.MissionScreen.RemoveLayer(this._layer);
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._layer = null;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x0000A244 File Offset: 0x00008444
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			base.OnAgentBuild(agent, banner);
			MissionAgentAlarmStateVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.OnAgentBuild(agent, banner);
		}

		// Token: 0x0600016E RID: 366 RVA: 0x0000A260 File Offset: 0x00008460
		public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
		{
			base.OnAgentTeamChanged(prevTeam, newTeam, agent);
			MissionAgentAlarmStateVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.OnAgentTeamChanged(prevTeam, newTeam, agent);
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0000A27E File Offset: 0x0000847E
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
			MissionAgentAlarmStateVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.OnAgentRemoved(affectedAgent);
		}

		// Token: 0x06000170 RID: 368 RVA: 0x0000A29C File Offset: 0x0000849C
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			MissionAgentAlarmStateVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.Update();
		}

		// Token: 0x06000171 RID: 369 RVA: 0x0000A2B5 File Offset: 0x000084B5
		protected override void OnResumeView()
		{
			base.OnResumeView();
			ScreenManager.SetSuspendLayer(this._layer, false);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x0000A2C9 File Offset: 0x000084C9
		protected override void OnSuspendView()
		{
			base.OnSuspendView();
			ScreenManager.SetSuspendLayer(this._layer, true);
		}

		// Token: 0x04000070 RID: 112
		private GauntletLayer _layer;

		// Token: 0x04000071 RID: 113
		private MissionAgentAlarmStateVM _dataSource;
	}
}
