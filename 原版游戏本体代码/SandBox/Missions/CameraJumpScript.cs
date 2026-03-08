using System;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.Missions
{
	// Token: 0x02000057 RID: 87
	public class CameraJumpScript : ScriptComponentBehavior
	{
		// Token: 0x06000372 RID: 882 RVA: 0x00014041 File Offset: 0x00012241
		public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
		{
			return ScriptComponentBehavior.TickRequirement.Tick;
		}

		// Token: 0x06000373 RID: 883 RVA: 0x00014044 File Offset: 0x00012244
		protected override void OnInit()
		{
			this._elapsedDuration = 0f;
		}

		// Token: 0x06000374 RID: 884 RVA: 0x00014054 File Offset: 0x00012254
		protected override void OnEditorInit()
		{
			this._initialGlobalFrame = base.GameEntity.GetGlobalFrame();
		}

		// Token: 0x06000375 RID: 885 RVA: 0x00014075 File Offset: 0x00012275
		protected override void OnTick(float dt)
		{
			this.OnJumpTick(dt);
		}

		// Token: 0x06000376 RID: 886 RVA: 0x0001407E File Offset: 0x0001227E
		protected override void OnEditorTick(float dt)
		{
			this.OnJumpTick(dt);
		}

		// Token: 0x06000377 RID: 887 RVA: 0x00014088 File Offset: 0x00012288
		private void OnJumpTick(float dt)
		{
			if (this._elapsedDuration >= 0f)
			{
				this._elapsedDuration += dt;
				if (this._elapsedDuration >= this._waitBeforeCameraJump)
				{
					Mat3 identity = Mat3.Identity;
					identity.ApplyEulerAngles(this._cameraJumpRotation);
					WeakGameEntity gameEntity = base.GameEntity;
					MatrixFrame matrixFrame = new MatrixFrame(ref identity, ref this._cameraJumpPosition);
					gameEntity.SetGlobalFrame(matrixFrame, true);
				}
			}
		}

		// Token: 0x06000378 RID: 888 RVA: 0x000140F0 File Offset: 0x000122F0
		protected override void OnEditorVariableChanged(string variableName)
		{
			if (variableName == "Preview")
			{
				this._elapsedDuration = 0f;
			}
			if (variableName == "Reset")
			{
				base.GameEntity.SetGlobalFrame(this._initialGlobalFrame, true);
				this._elapsedDuration = -1f;
			}
			if (variableName == "SetCurrentCameraTransform")
			{
				MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
				this._cameraJumpPosition = globalFrame.origin;
				this._cameraJumpRotation = globalFrame.rotation.GetEulerAngles();
			}
		}

		// Token: 0x040001B8 RID: 440
		[EditableScriptComponentVariable(true, "WaitBeforeCameraJump")]
		private float _waitBeforeCameraJump = 2f;

		// Token: 0x040001B9 RID: 441
		[EditableScriptComponentVariable(true, "CameraJumpPosition")]
		private Vec3 _cameraJumpPosition;

		// Token: 0x040001BA RID: 442
		[EditableScriptComponentVariable(true, "CameraJumpRotation")]
		private Vec3 _cameraJumpRotation;

		// Token: 0x040001BB RID: 443
		public SimpleButton SetCurrentCameraTransform;

		// Token: 0x040001BC RID: 444
		public SimpleButton Preview;

		// Token: 0x040001BD RID: 445
		public SimpleButton Reset;

		// Token: 0x040001BE RID: 446
		private MatrixFrame _initialGlobalFrame;

		// Token: 0x040001BF RID: 447
		private float _elapsedDuration = -1f;
	}
}
