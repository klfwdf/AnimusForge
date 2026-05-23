using System;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine
{
	// Token: 0x02000046 RID: 70
	public class ScreenManagerEngineConnection : IScreenManagerEngineConnection
	{
		// Token: 0x17000016 RID: 22
		// (get) Token: 0x060006D6 RID: 1750 RVA: 0x0000417F File Offset: 0x0000237F
		float IScreenManagerEngineConnection.RealScreenResolutionWidth
		{
			get
			{
				return Screen.RealScreenResolutionWidth;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x060006D7 RID: 1751 RVA: 0x00004186 File Offset: 0x00002386
		float IScreenManagerEngineConnection.RealScreenResolutionHeight
		{
			get
			{
				return Screen.RealScreenResolutionHeight;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x060006D8 RID: 1752 RVA: 0x0000418D File Offset: 0x0000238D
		float IScreenManagerEngineConnection.AspectRatio
		{
			get
			{
				return Screen.AspectRatio;
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x060006D9 RID: 1753 RVA: 0x00004194 File Offset: 0x00002394
		Vec2 IScreenManagerEngineConnection.DesktopResolution
		{
			get
			{
				return Screen.DesktopResolution;
			}
		}

		// Token: 0x060006DA RID: 1754 RVA: 0x0000419B File Offset: 0x0000239B
		void IScreenManagerEngineConnection.ActivateMouseCursor(CursorType mouseId)
		{
			MouseManager.ActivateMouseCursor(mouseId);
		}

		// Token: 0x060006DB RID: 1755 RVA: 0x000041A3 File Offset: 0x000023A3
		void IScreenManagerEngineConnection.SetMouseVisible(bool value)
		{
			EngineApplicationInterface.IScreen.SetMouseVisible(value);
		}

		// Token: 0x060006DC RID: 1756 RVA: 0x000041B0 File Offset: 0x000023B0
		bool IScreenManagerEngineConnection.GetMouseVisible()
		{
			return EngineApplicationInterface.IScreen.GetMouseVisible();
		}

		// Token: 0x060006DD RID: 1757 RVA: 0x000041BC File Offset: 0x000023BC
		bool IScreenManagerEngineConnection.GetIsEnterButtonRDown()
		{
			return EngineApplicationInterface.IScreen.IsEnterButtonCross();
		}

		// Token: 0x060006DE RID: 1758 RVA: 0x000041C8 File Offset: 0x000023C8
		void IScreenManagerEngineConnection.BeginDebugPanel(string panelTitle)
		{
			Imgui.BeginMainThreadScope();
			Imgui.Begin(panelTitle);
		}

		// Token: 0x060006DF RID: 1759 RVA: 0x000041D5 File Offset: 0x000023D5
		void IScreenManagerEngineConnection.EndDebugPanel()
		{
			Imgui.End();
			Imgui.EndMainThreadScope();
		}

		// Token: 0x060006E0 RID: 1760 RVA: 0x000041E1 File Offset: 0x000023E1
		void IScreenManagerEngineConnection.DrawDebugText(string text)
		{
			Imgui.Text(text);
		}

		// Token: 0x060006E1 RID: 1761 RVA: 0x000041E9 File Offset: 0x000023E9
		bool IScreenManagerEngineConnection.DrawDebugTreeNode(string text)
		{
			return Imgui.TreeNode(text);
		}

		// Token: 0x060006E2 RID: 1762 RVA: 0x000041F1 File Offset: 0x000023F1
		void IScreenManagerEngineConnection.PopDebugTreeNode()
		{
			Imgui.TreePop();
		}
	}
}
