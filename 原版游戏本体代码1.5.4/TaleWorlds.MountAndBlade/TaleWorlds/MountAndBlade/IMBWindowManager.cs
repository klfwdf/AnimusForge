using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBWindowManager
{
	[EngineMethod("erase_message_lines", false, null, false)]
	void EraseMessageLines();

	[EngineMethod("world_to_screen", false, null, true)]
	float WorldToScreen(UIntPtr cameraPointer, Vec3 worldSpacePosition, ref float screenX, ref float screenY, ref float w);

	[EngineMethod("world_to_screen_with_fixed_z", false, null, false)]
	float WorldToScreenWithFixedZ(UIntPtr cameraPointer, Vec3 cameraPosition, Vec3 worldSpacePosition, ref float screenX, ref float screenY, ref float w);

	[EngineMethod("dont_change_cursor_pos", false, null, false)]
	void DontChangeCursorPos();

	[EngineMethod("pre_display", false, null, false)]
	void PreDisplay();

	[EngineMethod("screen_to_world", false, null, false)]
	void ScreenToWorld(UIntPtr pointer, float screenX, float screenY, float z, ref Vec3 worldSpacePosition);

	[EngineMethod("get_screen_resolution", false, null, false)]
	Vec2 GetScreenResolution();
}
