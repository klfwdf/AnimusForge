using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IMouseManager
{
	[EngineMethod("activate_mouse_cursor", false, null, false)]
	void ActivateMouseCursor(int id);

	[EngineMethod("set_mouse_cursor", false, null, false)]
	void SetMouseCursor(int id, string mousePath);

	[EngineMethod("show_cursor", false, null, false)]
	void ShowCursor(bool show);

	[EngineMethod("lock_cursor_at_current_pos", false, null, false)]
	void LockCursorAtCurrentPosition(bool lockCursor);

	[EngineMethod("lock_cursor_at_position", false, null, false)]
	void LockCursorAtPosition(float x, float y);

	[EngineMethod("unlock_cursor", false, null, false)]
	void UnlockCursor();
}
