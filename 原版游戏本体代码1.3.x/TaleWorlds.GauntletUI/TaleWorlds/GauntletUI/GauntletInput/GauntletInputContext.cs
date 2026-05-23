using System;
using System.Numerics;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.GauntletInput
{
	// Token: 0x0200004A RID: 74
	public class GauntletInputContext : IReadonlyInputContext
	{
		// Token: 0x06000457 RID: 1111 RVA: 0x00011BCF File Offset: 0x0000FDCF
		public GauntletInputContext(IInputContext inputContext)
		{
			this._inputContext = inputContext;
		}

		// Token: 0x06000458 RID: 1112 RVA: 0x00011BDE File Offset: 0x0000FDDE
		public bool GetIsMouseActive()
		{
			return this._inputContext.GetIsMouseActive();
		}

		// Token: 0x06000459 RID: 1113 RVA: 0x00011BEB File Offset: 0x0000FDEB
		public Vector2 GetMousePosition()
		{
			if (this._isMousePositionOverridden)
			{
				return this._overrideMousePosition;
			}
			return this._inputContext.GetPointerPosition();
		}

		// Token: 0x0600045A RID: 1114 RVA: 0x00011C07 File Offset: 0x0000FE07
		public Vector2 GetMouseMovement()
		{
			return new Vector2(this._inputContext.GetMouseMoveX(), this._inputContext.GetMouseMoveY());
		}

		// Token: 0x0600045B RID: 1115 RVA: 0x00011C24 File Offset: 0x0000FE24
		public InputKey[] GetClickKeys()
		{
			return Input.GetClickKeys();
		}

		// Token: 0x0600045C RID: 1116 RVA: 0x00011C2B File Offset: 0x0000FE2B
		public InputKey[] GetAlternateClickKeys()
		{
			return new InputKey[] { InputKey.RightMouseButton };
		}

		// Token: 0x0600045D RID: 1117 RVA: 0x00011C3B File Offset: 0x0000FE3B
		public float GetMouseScrollDelta()
		{
			return this._inputContext.GetDeltaMouseScroll();
		}

		// Token: 0x0600045E RID: 1118 RVA: 0x00011C48 File Offset: 0x0000FE48
		public Vector2 GetControllerLeftStickState()
		{
			return (Vector2)this._inputContext.GetControllerLeftStickState();
		}

		// Token: 0x0600045F RID: 1119 RVA: 0x00011C5A File Offset: 0x0000FE5A
		public Vector2 GetControllerRightStickState()
		{
			return (Vector2)this._inputContext.GetControllerRightStickState();
		}

		// Token: 0x06000460 RID: 1120 RVA: 0x00011C6C File Offset: 0x0000FE6C
		public void SetMousePositionOverride(Vector2 mousePosition)
		{
			this._isMousePositionOverridden = true;
			this._overrideMousePosition = mousePosition;
		}

		// Token: 0x06000461 RID: 1121 RVA: 0x00011C7C File Offset: 0x0000FE7C
		public void ResetMousePositionOverride()
		{
			this._isMousePositionOverridden = false;
		}

		// Token: 0x04000225 RID: 549
		private readonly IInputContext _inputContext;

		// Token: 0x04000226 RID: 550
		private bool _isMousePositionOverridden;

		// Token: 0x04000227 RID: 551
		private Vector2 _overrideMousePosition;
	}
}
