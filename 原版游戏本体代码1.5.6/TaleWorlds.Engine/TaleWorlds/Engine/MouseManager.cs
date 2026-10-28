using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine;

public static class MouseManager
{
	public static void ActivateMouseCursor(CursorType mouseId)
	{
		EngineApplicationInterface.IMouseManager.ActivateMouseCursor((int)mouseId);
	}

	public static void SetMouseCursor(CursorType mouseId, string mousePath)
	{
		EngineApplicationInterface.IMouseManager.SetMouseCursor((int)mouseId, mousePath);
	}

	public static void ShowCursor(bool show)
	{
		EngineApplicationInterface.IMouseManager.ShowCursor(show);
	}

	public static void LockCursorAtCurrentPosition(bool lockCursor)
	{
		EngineApplicationInterface.IMouseManager.LockCursorAtCurrentPosition(lockCursor);
	}

	public static void LockCursorAtPosition(float x, float y)
	{
		EngineApplicationInterface.IMouseManager.LockCursorAtPosition(x, y);
	}

	public static void UnlockCursor()
	{
		EngineApplicationInterface.IMouseManager.UnlockCursor();
	}
}
