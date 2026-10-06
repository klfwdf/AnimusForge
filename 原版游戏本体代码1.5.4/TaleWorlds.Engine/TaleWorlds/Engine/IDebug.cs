using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IDebug
{
	[EngineMethod("write_debug_line_on_screen", false, null, false)]
	void WriteDebugLineOnScreen(string line);

	[EngineMethod("abort_game", false, null, false)]
	void AbortGame(int ExitCode);

	[EngineMethod("assert_memory_usage", false, null, false)]
	void AssertMemoryUsage(int memoryMB);

	[EngineMethod("write_line", false, null, false)]
	void WriteLine(int logLevel, string line, int color, ulong filter);

	[EngineMethod("render_debug_direction_arrow", false, null, false)]
	void RenderDebugDirectionArrow(Vec3 position, Vec3 direction, uint color, bool depthCheck);

	[EngineMethod("render_debug_line", false, null, false)]
	void RenderDebugLine(Vec3 position, Vec3 direction, uint color, bool depthCheck, float time);

	[EngineMethod("render_debug_sphere", false, null, false)]
	void RenderDebugSphere(Vec3 position, float radius, uint color, bool depthCheck, float time);

	[EngineMethod("render_debug_capsule", false, null, false)]
	void RenderDebugCapsule(Vec3 p0, Vec3 p1, float radius, uint color, bool depthCheck, float time);

	[EngineMethod("render_debug_frame", false, null, false)]
	void RenderDebugFrame(ref MatrixFrame frame, float lineLength, float time);

	[EngineMethod("render_debug_text3d", false, null, false)]
	void RenderDebugText3d(Vec3 worldPosition, string str, uint color, int screenPosOffsetX, int screenPosOffsetY, float time);

	[EngineMethod("render_debug_text", false, null, false)]
	void RenderDebugText(float screenX, float screenY, string str, uint color, float time);

	[EngineMethod("render_debug_rect", false, null, false)]
	void RenderDebugRect(float left, float bottom, float right, float top);

	[EngineMethod("render_debug_rect_with_color", false, null, false)]
	void RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color);

	[EngineMethod("clear_all_debug_render_objects", false, null, false)]
	void ClearAllDebugRenderObjects();

	[EngineMethod("get_debug_vector", false, null, false)]
	Vec3 GetDebugVector();

	[EngineMethod("set_debug_vector", false, null, false)]
	void SetDebugVector(Vec3 debugVector);

	[EngineMethod("render_debug_box_object", false, null, false)]
	void RenderDebugBoxObject(Vec3 min, Vec3 max, uint color, bool depthCheck, float time);

	[EngineMethod("render_debug_box_object_with_frame", false, null, false)]
	void RenderDebugBoxObjectWithFrame(Vec3 min, Vec3 max, ref MatrixFrame frame, uint color, bool depthCheck, float time);

	[EngineMethod("post_warning_line", false, null, false)]
	void PostWarningLine(string line);

	[EngineMethod("is_error_report_mode_active", false, null, false)]
	bool IsErrorReportModeActive();

	[EngineMethod("is_error_report_mode_pause_mission", false, null, false)]
	bool IsErrorReportModePauseMission();

	[EngineMethod("set_error_report_scene", false, null, false)]
	void SetErrorReportScene(UIntPtr scenePointer);

	[EngineMethod("set_dump_generation_disabled", false, null, false)]
	void SetDumpGenerationDisabled(bool Disabled);

	[EngineMethod("message_box", false, null, false)]
	int MessageBox(string lpText, string lpCaption, uint uType);

	[EngineMethod("get_show_debug_info", false, null, false)]
	int GetShowDebugInfo();

	[EngineMethod("set_show_debug_info", false, null, false)]
	void SetShowDebugInfo(int value);

	[EngineMethod("error", false, null, false)]
	bool Error(string MessageString);

	[EngineMethod("warning", false, null, false)]
	bool Warning(string MessageString);

	[EngineMethod("content_warning", false, null, false)]
	bool ContentWarning(string MessageString);

	[EngineMethod("failed_assert", false, null, false)]
	bool FailedAssert(string messageString, string callerFile, string callerMethod, int callerLine);

	[EngineMethod("silent_assert", false, null, false)]
	bool SilentAssert(string messageString, string callerFile, string callerMethod, int callerLine, bool getDump);

	[EngineMethod("is_test_mode", false, null, false)]
	bool IsTestMode();

	[EngineMethod("echo_command_window", false, null, false)]
	void EchoCommandWindow(string content);
}
