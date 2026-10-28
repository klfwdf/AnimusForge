using TaleWorlds.Library;

namespace TaleWorlds.ScreenSystem;

public interface IScreenManagerEngineConnection
{
	float RealScreenResolutionWidth { get; }

	float RealScreenResolutionHeight { get; }

	float AspectRatio { get; }

	Vec2 DesktopResolution { get; }

	void ActivateMouseCursor(CursorType mouseId);

	void SetMouseVisible(bool value);

	bool GetMouseVisible();

	bool GetIsEnterButtonRDown();

	void BeginDebugPanel(string panelTitle);

	void EndDebugPanel();

	void DrawDebugText(string text);

	bool DrawDebugTreeNode(string text);

	void PopDebugTreeNode();
}
