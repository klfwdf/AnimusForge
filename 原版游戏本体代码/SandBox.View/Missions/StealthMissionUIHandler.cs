using System;
using SandBox.Objects.Usables;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions
{
	// Token: 0x02000026 RID: 38
	public class StealthMissionUIHandler : MissionView
	{
		// Token: 0x06000107 RID: 263 RVA: 0x0000CB45 File Offset: 0x0000AD45
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (!this._isInitialized)
			{
				this.InitializeView();
			}
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000CB5C File Offset: 0x0000AD5C
		public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
		{
			base.OnObjectUsed(userAgent, usedObject);
			if (this._isInitialized && usedObject is StealthAreaUsePoint)
			{
				this.CameraFadeInFadeOut(0.5f, 0.5f, 1f);
			}
		}

		// Token: 0x06000109 RID: 265 RVA: 0x0000CB8B File Offset: 0x0000AD8B
		private void InitializeView()
		{
			this._cameraFadeViewController = base.Mission.GetMissionBehavior<MissionCameraFadeView>();
			this._isInitialized = true;
		}

		// Token: 0x0600010A RID: 266 RVA: 0x0000CBA5 File Offset: 0x0000ADA5
		private void CameraFadeInFadeOut(float fadeOutTime, float blackTime, float fadeInTime)
		{
			if (this._cameraFadeViewController.FadeState == MissionCameraFadeView.CameraFadeState.White)
			{
				this._cameraFadeViewController.BeginFadeOutAndIn(fadeOutTime, blackTime, fadeInTime);
			}
		}

		// Token: 0x04000081 RID: 129
		private MissionCameraFadeView _cameraFadeViewController;

		// Token: 0x04000082 RID: 130
		private bool _isInitialized;
	}
}
