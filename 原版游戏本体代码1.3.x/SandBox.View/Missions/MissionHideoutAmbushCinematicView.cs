using System;
using SandBox.Missions.MissionLogics.Hideout;
using SandBox.Objects.Cinematics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions
{
	// Token: 0x0200001A RID: 26
	public class MissionHideoutAmbushCinematicView : MissionView
	{
		// Token: 0x060000B7 RID: 183 RVA: 0x000095A0 File Offset: 0x000077A0
		protected virtual void SetPlayerMovementEnabled(bool isPlayerMovementEnabled)
		{
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x000095A4 File Offset: 0x000077A4
		public override void AfterStart()
		{
			base.AfterStart();
			this._cameraEntity = base.Mission.Scene.FindEntityWithTag("hideout_ambush_cutscene_camera");
			this._arrowPath = base.Mission.Scene.FindEntityWithTag("hideout_ambush_cutscene_arrow_path");
			this._hideoutAmbushMissionController = base.Mission.GetMissionBehavior<HideoutAmbushMissionController>();
			this._missionCameraFadeView = base.Mission.GetMissionBehavior<MissionCameraFadeView>();
			Vec3 invalid = Vec3.Invalid;
			this._camera = Camera.CreateCamera();
			this._cameraEntity.GetCameraParamsFromCameraScript(this._camera, ref invalid);
			this._camera.SetFovVertical(this._camera.GetFovVertical(), Screen.AspectRatio, this._camera.Near, this._camera.Far);
			this._arrowPath.SetVisibilityExcludeParents(false);
			this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.None;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00009678 File Offset: 0x00007878
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			switch (this._currentHideoutAmbushCinematicState)
			{
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.None:
			{
				HideoutAmbushMissionController hideoutAmbushMissionController = this._hideoutAmbushMissionController;
				if (hideoutAmbushMissionController != null && hideoutAmbushMissionController.IsReadyForCallTroopsCinematic)
				{
					this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.FirstFadeOut;
					this.SetPlayerMovementEnabled(false);
					return;
				}
				break;
			}
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.FirstFadeOut:
				this._missionCameraFadeView.BeginFadeOutAndIn(0.5f, 0.5f, 0.5f);
				this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.ChangeToCustomCamera;
				return;
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.ChangeToCustomCamera:
				if (this._missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.Black)
				{
					base.MissionScreen.CustomCamera = this._camera;
					Agent.Main.AgentVisuals.SetVisible(false);
					this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.FirstFadeIn;
					return;
				}
				break;
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.FirstFadeIn:
				if (this._missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.White)
				{
					this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.SendArrow;
					return;
				}
				break;
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.SendArrow:
				this._arrowPath.SetVisibilityExcludeParents(true);
				this._timer = new Timer(base.Mission.CurrentTime, 5f, true);
				this._arrowPath.GetFirstScriptOfType<CinematicBurningArrow>().StartMovement();
				this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.Wait;
				return;
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.Wait:
				if (this._timer.Check(base.Mission.CurrentTime))
				{
					this._timer = null;
					this._arrowPath.SetVisibilityExcludeParents(false);
					this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.SecondFadeOut;
					return;
				}
				break;
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.SecondFadeOut:
				this._missionCameraFadeView.BeginFadeOutAndIn(0.5f, 0.5f, 0.5f);
				this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.ChangeBackToDefaultCamera;
				return;
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.ChangeBackToDefaultCamera:
				if (this._missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.Black)
				{
					base.MissionScreen.CustomCamera = null;
					Agent.Main.AgentVisuals.SetVisible(true);
					this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.SecondFadeIn;
					return;
				}
				break;
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.SecondFadeIn:
				if (this._missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.White)
				{
					this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.Ending;
					return;
				}
				break;
			case MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.Ending:
				this.SetPlayerMovementEnabled(true);
				this._hideoutAmbushMissionController.OnAgentsShouldBeEnabled();
				this._currentHideoutAmbushCinematicState = MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState.Ended;
				break;
			default:
				return;
			}
		}

		// Token: 0x04000066 RID: 102
		private const string CameraTag = "hideout_ambush_cutscene_camera";

		// Token: 0x04000067 RID: 103
		private const string ArrowBarrelTag = "hideout_ambush_cutscene_arrow_barrel";

		// Token: 0x04000068 RID: 104
		private const string ArrowPathTag = "hideout_ambush_cutscene_arrow_path";

		// Token: 0x04000069 RID: 105
		private Camera _camera;

		// Token: 0x0400006A RID: 106
		private GameEntity _cameraEntity;

		// Token: 0x0400006B RID: 107
		private GameEntity _arrowPath;

		// Token: 0x0400006C RID: 108
		private HideoutAmbushMissionController _hideoutAmbushMissionController;

		// Token: 0x0400006D RID: 109
		private MissionCameraFadeView _missionCameraFadeView;

		// Token: 0x0400006E RID: 110
		private MissionHideoutAmbushCinematicView.HideoutAmbushCinematicState _currentHideoutAmbushCinematicState;

		// Token: 0x0400006F RID: 111
		private Timer _timer;

		// Token: 0x0200008B RID: 139
		private enum HideoutAmbushCinematicState
		{
			// Token: 0x040002BD RID: 701
			None,
			// Token: 0x040002BE RID: 702
			FirstFadeOut,
			// Token: 0x040002BF RID: 703
			ChangeToCustomCamera,
			// Token: 0x040002C0 RID: 704
			FirstFadeIn,
			// Token: 0x040002C1 RID: 705
			SendArrow,
			// Token: 0x040002C2 RID: 706
			Wait,
			// Token: 0x040002C3 RID: 707
			SecondFadeOut,
			// Token: 0x040002C4 RID: 708
			ChangeBackToDefaultCamera,
			// Token: 0x040002C5 RID: 709
			SecondFadeIn,
			// Token: 0x040002C6 RID: 710
			Ending,
			// Token: 0x040002C7 RID: 711
			Ended
		}
	}
}
