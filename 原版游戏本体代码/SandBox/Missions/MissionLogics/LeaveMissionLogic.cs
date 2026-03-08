using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000070 RID: 112
	public class LeaveMissionLogic : MissionLogic
	{
		// Token: 0x06000479 RID: 1145 RVA: 0x0001AE71 File Offset: 0x00019071
		public LeaveMissionLogic(string leaveMenuId = "settlement_player_unconscious")
		{
			this._menuId = leaveMenuId;
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x0001AE80 File Offset: 0x00019080
		public override bool MissionEnded(ref MissionResult missionResult)
		{
			return base.Mission.MainAgent != null && !base.Mission.MainAgent.IsActive();
		}

		// Token: 0x0600047B RID: 1147 RVA: 0x0001AEA4 File Offset: 0x000190A4
		public override void OnMissionTick(float dt)
		{
			if (Agent.Main == null || !Agent.Main.IsActive())
			{
				if (this._isAgentDeadTimer == null)
				{
					this._isAgentDeadTimer = new Timer(Mission.Current.CurrentTime, 5f, true);
				}
				if (this._isAgentDeadTimer.Check(Mission.Current.CurrentTime))
				{
					Mission.Current.NextCheckTimeEndMission = 0f;
					Mission.Current.EndMission();
					Campaign.Current.GameMenuManager.SetNextMenu(this._menuId);
					return;
				}
			}
			else if (this._isAgentDeadTimer != null)
			{
				this._isAgentDeadTimer = null;
			}
		}

		// Token: 0x04000266 RID: 614
		private string _menuId;

		// Token: 0x04000267 RID: 615
		private Timer _isAgentDeadTimer;
	}
}
