using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions
{
	// Token: 0x02000017 RID: 23
	public class MissionCustomCameraView : MissionView
	{
		// Token: 0x06000099 RID: 153 RVA: 0x00005CA8 File Offset: 0x00003EA8
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			foreach (GameEntity gameEntity in base.Mission.Scene.FindEntitiesWithTag(this.tag))
			{
				Camera camera = Camera.CreateCamera();
				gameEntity.GetCameraParamsFromCameraScript(camera, ref this._dofParams);
				this._cameras.Add(camera);
			}
			base.MissionScreen.CustomCamera = this._cameras[0];
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00005D38 File Offset: 0x00003F38
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (base.DebugInput.IsHotKeyReleased("CustomCameraMissionViewHotkeyIncreaseCustomCameraIndex"))
			{
				this._currentCameraIndex++;
				if (this._currentCameraIndex >= this._cameras.Count)
				{
					this._currentCameraIndex = 0;
				}
				base.MissionScreen.CustomCamera = this._cameras[this._currentCameraIndex];
			}
		}

		// Token: 0x04000031 RID: 49
		public string tag = "customcamera";

		// Token: 0x04000032 RID: 50
		private readonly List<Camera> _cameras = new List<Camera>();

		// Token: 0x04000033 RID: 51
		public Vec3 _dofParams;

		// Token: 0x04000034 RID: 52
		private int _currentCameraIndex;
	}
}
