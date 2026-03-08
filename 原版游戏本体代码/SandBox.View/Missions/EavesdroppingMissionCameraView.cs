using System;
using SandBox.Missions;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions
{
	// Token: 0x0200000F RID: 15
	public class EavesdroppingMissionCameraView : MissionView
	{
		// Token: 0x0600006D RID: 109 RVA: 0x000046B0 File Offset: 0x000028B0
		protected virtual void SetPlayerMovementEnabled(bool isPlayerMovementEnabled)
		{
		}

		// Token: 0x0600006E RID: 110 RVA: 0x000046B4 File Offset: 0x000028B4
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._cameraSwitchState = EavesdroppingMissionCameraView.CameraSwitchState.None;
			foreach (MissionBehavior missionBehavior in base.Mission.MissionBehaviors)
			{
				if (missionBehavior is EavesdroppingMissionLogic)
				{
					this._eavesdroppingMissionLogic = missionBehavior as EavesdroppingMissionLogic;
				}
				if (missionBehavior is MissionCameraFadeView)
				{
					this._missionCameraFadeView = missionBehavior as MissionCameraFadeView;
				}
			}
		}

		// Token: 0x0600006F RID: 111 RVA: 0x0000473C File Offset: 0x0000293C
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (this._eavesdroppingMissionLogic != null)
			{
				switch (this._cameraSwitchState)
				{
				case EavesdroppingMissionCameraView.CameraSwitchState.None:
					if ((this._eavesdroppingMissionLogic.EavesdropStarted && base.MissionScreen.CustomCamera == null) || (!this._eavesdroppingMissionLogic.EavesdropStarted && base.MissionScreen.CustomCamera != null))
					{
						if (this._eavesdroppingMissionLogic.EavesdropStarted && base.MissionScreen.CustomCamera == null)
						{
							this.SetPlayerMovementEnabled(false);
						}
						this._cameraSwitchState = EavesdroppingMissionCameraView.CameraSwitchState.ReadyForFadeOut;
						return;
					}
					break;
				case EavesdroppingMissionCameraView.CameraSwitchState.ReadyForFadeOut:
					this._missionCameraFadeView.BeginFadeOutAndIn(0.5f, 0.5f, 0.5f);
					this._cameraSwitchState = EavesdroppingMissionCameraView.CameraSwitchState.FadeOutAndInStarted;
					return;
				case EavesdroppingMissionCameraView.CameraSwitchState.FadeOutAndInStarted:
					if (this._missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.Black)
					{
						base.MissionScreen.CustomCamera = ((base.MissionScreen.CustomCamera == null) ? this._eavesdroppingMissionLogic.CurrentEavesdroppingCamera : null);
						if (base.MissionScreen.CustomCamera == null)
						{
							this.SetPlayerMovementEnabled(true);
						}
						this._cameraSwitchState = EavesdroppingMissionCameraView.CameraSwitchState.WaitingForFadeInToEnd;
						return;
					}
					break;
				case EavesdroppingMissionCameraView.CameraSwitchState.WaitingForFadeInToEnd:
					if (this._missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.White)
					{
						this._cameraSwitchState = EavesdroppingMissionCameraView.CameraSwitchState.None;
					}
					break;
				default:
					return;
				}
			}
		}

		// Token: 0x04000014 RID: 20
		private EavesdroppingMissionCameraView.CameraSwitchState _cameraSwitchState;

		// Token: 0x04000015 RID: 21
		private EavesdroppingMissionLogic _eavesdroppingMissionLogic;

		// Token: 0x04000016 RID: 22
		private MissionCameraFadeView _missionCameraFadeView;

		// Token: 0x02000083 RID: 131
		private enum CameraSwitchState
		{
			// Token: 0x04000299 RID: 665
			None,
			// Token: 0x0400029A RID: 666
			ReadyForFadeOut,
			// Token: 0x0400029B RID: 667
			FadeOutAndInStarted,
			// Token: 0x0400029C RID: 668
			WaitingForFadeInToEnd
		}
	}
}
