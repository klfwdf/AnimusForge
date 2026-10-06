using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine;

public class ScreenManagerEngineConnection : IScreenManagerEngineConnection
{
	float IScreenManagerEngineConnection.RealScreenResolutionWidth => Screen.RealScreenResolutionWidth;

	float IScreenManagerEngineConnection.RealScreenResolutionHeight => Screen.RealScreenResolutionHeight;

	float IScreenManagerEngineConnection.AspectRatio => Screen.AspectRatio;

	Vec2 IScreenManagerEngineConnection.DesktopResolution => Screen.DesktopResolution;

	void IScreenManagerEngineConnection.ActivateMouseCursor(CursorType mouseId)
	{
		MouseManager.ActivateMouseCursor(mouseId);
	}

	void IScreenManagerEngineConnection.SetMouseVisible(bool value)
	{
		EngineApplicationInterface.IScreen.SetMouseVisible(value);
	}

	bool IScreenManagerEngineConnection.GetMouseVisible()
	{
		return EngineApplicationInterface.IScreen.GetMouseVisible();
	}

	bool IScreenManagerEngineConnection.GetIsEnterButtonRDown()
	{
		return EngineApplicationInterface.IScreen.IsEnterButtonCross();
	}

	void IScreenManagerEngineConnection.BeginDebugPanel(string panelTitle)
	{
		Imgui.BeginMainThreadScope();
		Imgui.Begin(panelTitle);
	}

	void IScreenManagerEngineConnection.EndDebugPanel()
	{
		Imgui.End();
		Imgui.EndMainThreadScope();
	}

	void IScreenManagerEngineConnection.DrawDebugText(string text)
	{
		Imgui.Text(text);
	}

	bool IScreenManagerEngineConnection.DrawDebugTreeNode(string text)
	{
		return Imgui.TreeNode(text);
	}

	void IScreenManagerEngineConnection.PopDebugTreeNode()
	{
		Imgui.TreePop();
	}
}
