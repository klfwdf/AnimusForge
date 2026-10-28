using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IScreen
{
	[EngineMethod("get_real_screen_resolution_width", false, null, false)]
	float GetRealScreenResolutionWidth();

	[EngineMethod("get_real_screen_resolution_height", false, null, false)]
	float GetRealScreenResolutionHeight();

	[EngineMethod("get_desktop_width", false, null, false)]
	float GetDesktopWidth();

	[EngineMethod("get_desktop_height", false, null, false)]
	float GetDesktopHeight();

	[EngineMethod("get_aspect_ratio", false, null, false)]
	float GetAspectRatio();

	[EngineMethod("get_mouse_visible", false, null, false)]
	bool GetMouseVisible();

	[EngineMethod("set_mouse_visible", false, null, false)]
	void SetMouseVisible(bool value);

	[EngineMethod("get_usable_area_percentages", false, null, false)]
	Vec2 GetUsableAreaPercentages();

	[EngineMethod("is_enter_button_cross", false, null, false)]
	bool IsEnterButtonCross();
}
