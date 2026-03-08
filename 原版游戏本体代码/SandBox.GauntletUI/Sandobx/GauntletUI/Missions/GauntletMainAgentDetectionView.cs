using System;
using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.View.Missions;
using SandBox.ViewModelCollection;
using SandBox.ViewModelCollection.Missions.MainAgentDetection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace Sandobx.GauntletUI.Missions
{
	// Token: 0x02000004 RID: 4
	[OverrideView(typeof(MissionMainAgentDetectionView))]
	public class GauntletMainAgentDetectionView : MissionMainAgentDetectionView
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			this._detectionDataSource = new MainAgentDetectionVM();
			this._losingTargetDataSource = new MissionLosingTargetVM();
			this._markersDataSource = new MissionDisguiseMarkersVM();
			this._detectionBarGauntletLayer = new GauntletLayer("MissionMainAgentDetection", 10, false);
			this._detectionBarGauntletLayer.LoadMovie("MissionMainAgentDetection", this._detectionDataSource);
			this._losingTargetGauntletLayer = new GauntletLayer("MissionLosingTarget", 11, false);
			this._losingTargetGauntletLayer.LoadMovie("MissionLosingTarget", this._losingTargetDataSource);
			this._markersGauntletLayer = new GauntletLayer("MissionDetectionMarkers", 12, false);
			this._markersGauntletLayer.LoadMovie("MissionDetectionMarkers", this._markersDataSource);
			base.MissionScreen.AddLayer(this._detectionBarGauntletLayer);
			base.MissionScreen.AddLayer(this._losingTargetGauntletLayer);
			base.MissionScreen.AddLayer(this._markersGauntletLayer);
		}

		// Token: 0x06000004 RID: 4 RVA: 0x0000213D File Offset: 0x0000033D
		public override void AfterStart()
		{
			this._disguiseMissionLogic = base.Mission.GetMissionBehavior<DisguiseMissionLogic>();
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002150 File Offset: 0x00000350
		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			this._detectionDataSource.OnFinalize();
			base.MissionScreen.RemoveLayer(this._detectionBarGauntletLayer);
			this._detectionBarGauntletLayer = null;
			this._detectionDataSource = null;
			this._disguiseMissionLogic = null;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002189 File Offset: 0x00000389
		private void UpdateSuspicion(float dt)
		{
			this._detectionDataSource.UpdateDetectionValues(0f, 1f, this._disguiseMissionLogic.PlayerSuspiciousLevel);
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000021AC File Offset: 0x000003AC
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (this._disguiseMissionLogic != null)
			{
				if (this._losingTargetDataSource != null)
				{
					this.UpdateLosingTarget(dt);
				}
				if (this._detectionDataSource != null)
				{
					this.UpdateSuspicion(dt);
				}
				if (this._markersDataSource != null)
				{
					this.UpdateMarkers(dt);
				}
				this._lastSuspicousLevel = this._disguiseMissionLogic.PlayerSuspiciousLevel;
			}
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002206 File Offset: 0x00000406
		private void UpdateLosingTarget(float dt)
		{
			this._losingTargetDataSource.UpdateLosingTargetValues(false, 0f, 1f);
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002220 File Offset: 0x00000420
		private void UpdateMarkers(float dt)
		{
			bool isInStealthMode = this._disguiseMissionLogic.IsInStealthMode;
			bool isSuspicious = isInStealthMode || this._lastSuspicousLevel < this._disguiseMissionLogic.PlayerSuspiciousLevel;
			List<MissionDisguiseMarkerItemVM> list = new List<MissionDisguiseMarkerItemVM>();
			foreach (MissionDisguiseMarkerItemVM missionDisguiseMarkerItemVM in this._markersDataSource.HostileAgents)
			{
				if (this._disguiseMissionLogic.GetAgentOffenseInfo((missionDisguiseMarkerItemVM != null) ? missionDisguiseMarkerItemVM.OffenseInfo.Agent : null) == null)
				{
					list.Add(missionDisguiseMarkerItemVM);
				}
			}
			foreach (KeyValuePair<Agent, DisguiseMissionLogic.ShadowingAgentOffenseInfo> keyValuePair in this._disguiseMissionLogic.ThreatAgentInfos)
			{
				bool flag = true;
				foreach (MissionDisguiseMarkerItemVM missionDisguiseMarkerItemVM2 in this._markersDataSource.HostileAgents)
				{
					if (keyValuePair.Key != null)
					{
						DisguiseMissionLogic.ShadowingAgentOffenseInfo offenseInfo = missionDisguiseMarkerItemVM2.OffenseInfo;
						if (((offenseInfo != null) ? offenseInfo.Agent : null) == keyValuePair.Key)
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					this._markersDataSource.HostileAgents.Add(new MissionDisguiseMarkerItemVM(base.MissionScreen.CombatCamera, keyValuePair.Value));
				}
			}
			foreach (MissionDisguiseMarkerItemVM item in list)
			{
				this._markersDataSource.HostileAgents.Remove(item);
			}
			foreach (MissionDisguiseMarkerItemVM missionDisguiseMarkerItemVM3 in this._markersDataSource.HostileAgents)
			{
				if (missionDisguiseMarkerItemVM3.OffenseInfo.Agent.IsActive())
				{
					Vec3 origin = base.MissionScreen.CombatCamera.Frame.origin;
					Vec3 eyeGlobalPosition = missionDisguiseMarkerItemVM3.OffenseInfo.Agent.GetEyeGlobalPosition();
					float num;
					bool flag2 = !base.Mission.Scene.RayCastForClosestEntityOrTerrain(origin, eyeGlobalPosition, out num, 0.035f, BodyFlags.CommonFocusRayCastExcludeFlags);
					missionDisguiseMarkerItemVM3.OffenseInfo.SetCanPlayerCameraSeeTheAgent(flag2);
					missionDisguiseMarkerItemVM3.IsInVision = flag2;
					missionDisguiseMarkerItemVM3.IsInVisibilityRange = SandBoxUIHelper.IsAgentInVisibilityRangeApproximate(Agent.Main, missionDisguiseMarkerItemVM3.OffenseInfo.Agent);
					missionDisguiseMarkerItemVM3.IsStealthModeEnabled = isInStealthMode;
					missionDisguiseMarkerItemVM3.IsSuspicious = isSuspicious;
				}
				else
				{
					missionDisguiseMarkerItemVM3.IsInVision = false;
					missionDisguiseMarkerItemVM3.IsInVisibilityRange = false;
				}
				missionDisguiseMarkerItemVM3.UpdatePosition();
				missionDisguiseMarkerItemVM3.RefreshVisuals();
			}
		}

		// Token: 0x04000001 RID: 1
		private GauntletLayer _markersGauntletLayer;

		// Token: 0x04000002 RID: 2
		private GauntletLayer _losingTargetGauntletLayer;

		// Token: 0x04000003 RID: 3
		private GauntletLayer _detectionBarGauntletLayer;

		// Token: 0x04000004 RID: 4
		private MainAgentDetectionVM _detectionDataSource;

		// Token: 0x04000005 RID: 5
		private MissionDisguiseMarkersVM _markersDataSource;

		// Token: 0x04000006 RID: 6
		private MissionLosingTargetVM _losingTargetDataSource;

		// Token: 0x04000007 RID: 7
		private DisguiseMissionLogic _disguiseMissionLogic;

		// Token: 0x04000008 RID: 8
		private float _lastSuspicousLevel;
	}
}
