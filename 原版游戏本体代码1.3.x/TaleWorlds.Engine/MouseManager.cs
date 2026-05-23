using System;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine
{
	// Token: 0x0200006D RID: 109
	public static class MouseManager
	{
		// Token: 0x06000A2F RID: 2607 RVA: 0x0000A51E File Offset: 0x0000871E
		public static void ActivateMouseCursor(CursorType mouseId)
		{
			EngineApplicationInterface.IMouseManager.ActivateMouseCursor((int)mouseId);
		}

		// Token: 0x06000A30 RID: 2608 RVA: 0x0000A52B File Offset: 0x0000872B
		public static void SetMouseCursor(CursorType mouseId, string mousePath)
		{
			EngineApplicationInterface.IMouseManager.SetMouseCursor((int)mouseId, mousePath);
		}

		// Token: 0x06000A31 RID: 2609 RVA: 0x0000A539 File Offset: 0x00008739
		public static void ShowCursor(bool show)
		{
			EngineApplicationInterface.IMouseManager.ShowCursor(show);
		}

		// Token: 0x06000A32 RID: 2610 RVA: 0x0000A546 File Offset: 0x00008746
		public static void LockCursorAtCurrentPosition(bool lockCursor)
		{
			EngineApplicationInterface.IMouseManager.LockCursorAtCurrentPosition(lockCursor);
		}

		// Token: 0x06000A33 RID: 2611 RVA: 0x0000A553 File Offset: 0x00008753
		public static void LockCursorAtPosition(float x, float y)
		{
			EngineApplicationInterface.IMouseManager.LockCursorAtPosition(x, y);
		}

		// Token: 0x06000A34 RID: 2612 RVA: 0x0000A561 File Offset: 0x00008761
		public static void UnlockCursor()
		{
			EngineApplicationInterface.IMouseManager.UnlockCursor();
		}
	}
}
