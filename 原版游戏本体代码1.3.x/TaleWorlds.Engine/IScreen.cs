using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200003D RID: 61
	[ApplicationInterfaceBase]
	internal interface IScreen
	{
		// Token: 0x06000637 RID: 1591
		[EngineMethod("get_real_screen_resolution_width", false, null, false)]
		float GetRealScreenResolutionWidth();

		// Token: 0x06000638 RID: 1592
		[EngineMethod("get_real_screen_resolution_height", false, null, false)]
		float GetRealScreenResolutionHeight();

		// Token: 0x06000639 RID: 1593
		[EngineMethod("get_desktop_width", false, null, false)]
		float GetDesktopWidth();

		// Token: 0x0600063A RID: 1594
		[EngineMethod("get_desktop_height", false, null, false)]
		float GetDesktopHeight();

		// Token: 0x0600063B RID: 1595
		[EngineMethod("get_aspect_ratio", false, null, false)]
		float GetAspectRatio();

		// Token: 0x0600063C RID: 1596
		[EngineMethod("get_mouse_visible", false, null, false)]
		bool GetMouseVisible();

		// Token: 0x0600063D RID: 1597
		[EngineMethod("set_mouse_visible", false, null, false)]
		void SetMouseVisible(bool value);

		// Token: 0x0600063E RID: 1598
		[EngineMethod("get_usable_area_percentages", false, null, false)]
		Vec2 GetUsableAreaPercentages();

		// Token: 0x0600063F RID: 1599
		[EngineMethod("is_enter_button_cross", false, null, false)]
		bool IsEnterButtonCross();
	}
}
