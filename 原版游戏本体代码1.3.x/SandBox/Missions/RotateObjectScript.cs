using System;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.Missions
{
	// Token: 0x0200005E RID: 94
	public class RotateObjectScript : ScriptComponentBehavior
	{
		// Token: 0x060003A7 RID: 935 RVA: 0x00015698 File Offset: 0x00013898
		public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
		{
			return ScriptComponentBehavior.TickRequirement.Tick;
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x0001569B File Offset: 0x0001389B
		protected override void OnTick(float dt)
		{
			if (this._state == RotateObjectScript.State.None)
			{
				this._state = RotateObjectScript.State.Start;
			}
			this.OnTickInternal(dt);
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x000156B3 File Offset: 0x000138B3
		protected override void OnEditorTick(float dt)
		{
			this.OnTickInternal(dt);
		}

		// Token: 0x060003AA RID: 938 RVA: 0x000156BC File Offset: 0x000138BC
		private void OnTickInternal(float dt)
		{
			if (this._rotationAxis.Equals("X", StringComparison.OrdinalIgnoreCase) || this._rotationAxis.Equals("Y", StringComparison.OrdinalIgnoreCase) || this._rotationAxis.Equals("Z", StringComparison.OrdinalIgnoreCase))
			{
				this._rotationAxis = "X";
			}
			switch (this._state)
			{
			case RotateObjectScript.State.Start:
				if (this._waitBeforeRotateAsSeconds > 0f)
				{
					this._initialFrameCacheForPreviewRotateObjectButton = base.GameEntity.GetFrame();
					this._state = RotateObjectScript.State.WaitBeforeRotate;
					return;
				}
				this._state = RotateObjectScript.State.Rotating;
				return;
			case RotateObjectScript.State.WaitBeforeRotate:
				this._currentTimeDt += dt;
				if (this._currentTimeDt >= this._waitBeforeRotateAsSeconds)
				{
					this._state = RotateObjectScript.State.Rotating;
					return;
				}
				break;
			case RotateObjectScript.State.Rotating:
			{
				int num = MathF.Sign(this._rotateAngle);
				MatrixFrame frame = base.GameEntity.GetFrame();
				float radian = this._rotationSpeed * (float)num * dt * 0.017453292f;
				Vec3 rotationAxis = this.GetRotationAxis();
				frame.Rotate(radian, rotationAxis);
				base.GameEntity.SetFrame(ref frame, true);
				this._currentRotationAngle += this._rotationSpeed * (float)num * dt;
				if (Math.Abs(this._currentRotationAngle) >= Math.Abs(this._rotateAngle))
				{
					this._state = RotateObjectScript.State.End;
				}
				break;
			}
			default:
				return;
			}
		}

		// Token: 0x060003AB RID: 939 RVA: 0x00015808 File Offset: 0x00013A08
		private Vec3 GetRotationAxis()
		{
			if (this._rotationAxis.Equals("X", StringComparison.OrdinalIgnoreCase))
			{
				return Vec3.Side;
			}
			if (this._rotationAxis.Equals("Y", StringComparison.OrdinalIgnoreCase))
			{
				return Vec3.Forward;
			}
			if (this._rotationAxis.Equals("Z", StringComparison.OrdinalIgnoreCase))
			{
				return Vec3.Up;
			}
			Debug.FailedAssert("Wrong rotation axis!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\RotateObjectScript.cs", "GetRotationAxis", 123);
			return Vec3.Forward;
		}

		// Token: 0x060003AC RID: 940 RVA: 0x0001587C File Offset: 0x00013A7C
		protected override void OnEditorVariableChanged(string variableName)
		{
			base.OnEditorVariableChanged(variableName);
			if (variableName == "PreviewRotateObject")
			{
				if (this._state != RotateObjectScript.State.None && this._state != RotateObjectScript.State.End)
				{
					Debug.FailedAssert("The rotation is already started, please click the \"StopMovement\" button first!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\RotateObjectScript.cs", "OnEditorVariableChanged", 135);
				}
				else
				{
					this._initialFrameCacheForPreviewRotateObjectButton = base.GameEntity.GetFrame();
					this._currentRotationAngle = 0f;
					this._currentTimeDt = 0f;
					this._state = RotateObjectScript.State.Start;
				}
			}
			if (variableName == "StopMovement")
			{
				base.GameEntity.SetFrame(ref this._initialFrameCacheForPreviewRotateObjectButton, true);
				this._currentRotationAngle = 0f;
				this._currentTimeDt = 0f;
				this._state = RotateObjectScript.State.None;
			}
		}

		// Token: 0x040001E8 RID: 488
		[EditableScriptComponentVariable(true, "RotationAxis")]
		private string _rotationAxis = "X";

		// Token: 0x040001E9 RID: 489
		[EditableScriptComponentVariable(true, "WaitBeforeRotateAsSeconds")]
		private float _waitBeforeRotateAsSeconds = 2f;

		// Token: 0x040001EA RID: 490
		[EditableScriptComponentVariable(true, "RotateAngle")]
		private float _rotateAngle = 90f;

		// Token: 0x040001EB RID: 491
		[EditableScriptComponentVariable(true, "RotationSpeed")]
		private float _rotationSpeed = 1f;

		// Token: 0x040001EC RID: 492
		public SimpleButton PreviewRotateObject;

		// Token: 0x040001ED RID: 493
		public SimpleButton StopMovement;

		// Token: 0x040001EE RID: 494
		private MatrixFrame _initialFrameCacheForPreviewRotateObjectButton;

		// Token: 0x040001EF RID: 495
		private RotateObjectScript.State _state;

		// Token: 0x040001F0 RID: 496
		private float _currentRotationAngle;

		// Token: 0x040001F1 RID: 497
		private float _currentTimeDt;

		// Token: 0x02000159 RID: 345
		private enum State
		{
			// Token: 0x040006C5 RID: 1733
			None,
			// Token: 0x040006C6 RID: 1734
			Start,
			// Token: 0x040006C7 RID: 1735
			WaitBeforeRotate,
			// Token: 0x040006C8 RID: 1736
			Rotating,
			// Token: 0x040006C9 RID: 1737
			End
		}
	}
}
