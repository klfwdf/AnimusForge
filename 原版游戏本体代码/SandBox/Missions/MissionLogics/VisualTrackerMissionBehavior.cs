using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200008B RID: 139
	public class VisualTrackerMissionBehavior : MissionLogic
	{
		// Token: 0x06000560 RID: 1376 RVA: 0x00023C57 File Offset: 0x00021E57
		public override void OnAgentCreated(Agent agent)
		{
		}

		// Token: 0x06000561 RID: 1377 RVA: 0x00023C59 File Offset: 0x00021E59
		public override void AfterStart()
		{
			this.Refresh();
		}

		// Token: 0x06000562 RID: 1378 RVA: 0x00023C61 File Offset: 0x00021E61
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (this._visualTrackerManager.TrackedObjectsVersion != this._trackedObjectsVersion)
			{
				this.Refresh();
			}
		}

		// Token: 0x06000563 RID: 1379 RVA: 0x00023C83 File Offset: 0x00021E83
		private void Refresh()
		{
			if (PlayerEncounter.LocationEncounter != null)
			{
				this.RefreshCommonAreas();
			}
			this._trackedObjectsVersion = this._visualTrackerManager.TrackedObjectsVersion;
		}

		// Token: 0x06000564 RID: 1380 RVA: 0x00023CA4 File Offset: 0x00021EA4
		public void RegisterLocalOnlyObject(ITrackableBase obj)
		{
			using (List<TrackedObject>.Enumerator enumerator = this._currentTrackedObjects.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Object == obj)
					{
						return;
					}
				}
			}
			this._currentTrackedObjects.Add(new TrackedObject(obj));
		}

		// Token: 0x06000565 RID: 1381 RVA: 0x00023D0C File Offset: 0x00021F0C
		private void RefreshCommonAreas()
		{
			Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
			foreach (CommonAreaMarker commonAreaMarker in base.Mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>().ToList<CommonAreaMarker>())
			{
				if (settlement.Alleys.Count >= commonAreaMarker.AreaIndex)
				{
					this.RegisterLocalOnlyObject(commonAreaMarker);
				}
			}
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x00023D8C File Offset: 0x00021F8C
		public override List<CompassItemUpdateParams> GetCompassTargets()
		{
			List<CompassItemUpdateParams> list = new List<CompassItemUpdateParams>();
			foreach (TrackedObject trackedObject in this._currentTrackedObjects)
			{
				list.Add(new CompassItemUpdateParams(trackedObject.Object, TargetIconType.Flag_A, trackedObject.Position, 4288256409U, uint.MaxValue));
			}
			return list;
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x00023E00 File Offset: 0x00022000
		private void RemoveLocalObject(ITrackableBase obj)
		{
			this._currentTrackedObjects.RemoveAll((TrackedObject x) => x.Object == obj);
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x00023E32 File Offset: 0x00022032
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			this.RemoveLocalObject(affectedAgent);
		}

		// Token: 0x06000569 RID: 1385 RVA: 0x00023E3B File Offset: 0x0002203B
		public override void OnAgentDeleted(Agent affectedAgent)
		{
			this.RemoveLocalObject(affectedAgent);
		}

		// Token: 0x040002CE RID: 718
		private List<TrackedObject> _currentTrackedObjects = new List<TrackedObject>();

		// Token: 0x040002CF RID: 719
		private int _trackedObjectsVersion = -1;

		// Token: 0x040002D0 RID: 720
		private readonly VisualTrackerManager _visualTrackerManager = Campaign.Current.VisualTrackerManager;

		// Token: 0x02000186 RID: 390
		public enum AgentTrackTypes
		{
			// Token: 0x04000750 RID: 1872
			AvailableIssue,
			// Token: 0x04000751 RID: 1873
			ActiveIssue,
			// Token: 0x04000752 RID: 1874
			ActiveStoryQuest,
			// Token: 0x04000753 RID: 1875
			TrackedIssue,
			// Token: 0x04000754 RID: 1876
			TrackedStoryQuest
		}
	}
}
