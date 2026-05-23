using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000037 RID: 55
	[ApplicationInterfaceBase]
	internal interface IMouseManager
	{
		// Token: 0x06000572 RID: 1394
		[EngineMethod("activate_mouse_cursor", false, null, false)]
		void ActivateMouseCursor(int id);

		// Token: 0x06000573 RID: 1395
		[EngineMethod("set_mouse_cursor", false, null, false)]
		void SetMouseCursor(int id, string mousePath);

		// Token: 0x06000574 RID: 1396
		[EngineMethod("show_cursor", false, null, false)]
		void ShowCursor(bool show);

		// Token: 0x06000575 RID: 1397
		[EngineMethod("lock_cursor_at_current_pos", false, null, false)]
		void LockCursorAtCurrentPosition(bool lockCursor);

		// Token: 0x06000576 RID: 1398
		[EngineMethod("lock_cursor_at_position", false, null, false)]
		void LockCursorAtPosition(float x, float y);

		// Token: 0x06000577 RID: 1399
		[EngineMethod("unlock_cursor", false, null, false)]
		void UnlockCursor();
	}
}
