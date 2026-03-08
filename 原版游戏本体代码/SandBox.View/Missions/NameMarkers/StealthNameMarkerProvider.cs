using System;
using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.Usables;
using SandBox.ViewModelCollection;
using SandBox.ViewModelCollection.Missions.NameMarker;
using SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace SandBox.View.Missions.NameMarkers
{
	// Token: 0x02000031 RID: 49
	public class StealthNameMarkerProvider : MissionNameMarkerProvider
	{
		// Token: 0x0600019B RID: 411 RVA: 0x00012F27 File Offset: 0x00011127
		protected override void OnInitialize(Mission mission)
		{
			base.OnInitialize(mission);
			this._stealthAreaMissionLogic = mission.GetMissionBehavior<StealthAreaMissionLogic>();
		}

		// Token: 0x0600019C RID: 412 RVA: 0x00012F3C File Offset: 0x0001113C
		protected override void OnDestroy(Mission mission)
		{
			base.OnDestroy(mission);
			this._stealthAreaMissionLogic = null;
		}

		// Token: 0x0600019D RID: 413 RVA: 0x00012F4C File Offset: 0x0001114C
		public override void CreateMarkers(List<MissionNameMarkerTargetBaseVM> markers)
		{
			this.CreateStealthAreaMarkers(markers);
		}

		// Token: 0x0600019E RID: 414 RVA: 0x00012F58 File Offset: 0x00011158
		private void CreateStealthAreaMarkers(List<MissionNameMarkerTargetBaseVM> markers)
		{
			if (this._stealthAreaMissionLogic == null)
			{
				return;
			}
			Mission mission = Mission.Current;
			if (mission == null)
			{
				return;
			}
			if (Agent.Main != null)
			{
				foreach (StealthAreaUsePoint stealthAreaUsePoint in Mission.Current.ActiveMissionObjects.FindAllWithType<StealthAreaUsePoint>())
				{
					if (stealthAreaUsePoint.IsUsableByAgent(Agent.Main))
					{
						MissionStealthAreaUsePointNameMarkerTargetVM item = new MissionStealthAreaUsePointNameMarkerTargetVM(stealthAreaUsePoint);
						markers.Add(item);
					}
				}
			}
			AgentReadOnlyList allAgents = mission.AllAgents;
			for (int i = 0; i < allAgents.Count; i++)
			{
				Agent agent = allAgents[i];
				bool flag;
				if (SandBoxUIHelper.CanAgentBeAlarmed(agent))
				{
					StealthAreaMissionLogic stealthAreaMissionLogic = this._stealthAreaMissionLogic;
					flag = stealthAreaMissionLogic != null && stealthAreaMissionLogic.IsSentry(agent);
				}
				else
				{
					flag = false;
				}
				if (flag)
				{
					MissionStealthSentryNameMarkerTargetVM item2 = new MissionStealthSentryNameMarkerTargetVM(agent);
					markers.Add(item2);
				}
			}
		}

		// Token: 0x040000FB RID: 251
		private StealthAreaMissionLogic _stealthAreaMissionLogic;
	}
}
