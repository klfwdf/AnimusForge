using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000035 RID: 53
	[ApplicationInterfaceBase]
	internal interface IDebug
	{
		// Token: 0x06000548 RID: 1352
		[EngineMethod("write_debug_line_on_screen", false, null, false)]
		void WriteDebugLineOnScreen(string line);

		// Token: 0x06000549 RID: 1353
		[EngineMethod("abort_game", false, null, false)]
		void AbortGame(int ExitCode);

		// Token: 0x0600054A RID: 1354
		[EngineMethod("assert_memory_usage", false, null, false)]
		void AssertMemoryUsage(int memoryMB);

		// Token: 0x0600054B RID: 1355
		[EngineMethod("write_line", false, null, false)]
		void WriteLine(int logLevel, string line, int color, ulong filter);

		// Token: 0x0600054C RID: 1356
		[EngineMethod("render_debug_direction_arrow", false, null, false)]
		void RenderDebugDirectionArrow(Vec3 position, Vec3 direction, uint color, bool depthCheck);

		// Token: 0x0600054D RID: 1357
		[EngineMethod("render_debug_line", false, null, false)]
		void RenderDebugLine(Vec3 position, Vec3 direction, uint color, bool depthCheck, float time);

		// Token: 0x0600054E RID: 1358
		[EngineMethod("render_debug_sphere", false, null, false)]
		void RenderDebugSphere(Vec3 position, float radius, uint color, bool depthCheck, float time);

		// Token: 0x0600054F RID: 1359
		[EngineMethod("render_debug_capsule", false, null, false)]
		void RenderDebugCapsule(Vec3 p0, Vec3 p1, float radius, uint color, bool depthCheck, float time);

		// Token: 0x06000550 RID: 1360
		[EngineMethod("render_debug_frame", false, null, false)]
		void RenderDebugFrame(ref MatrixFrame frame, float lineLength, float time);

		// Token: 0x06000551 RID: 1361
		[EngineMethod("render_debug_text3d", false, null, false)]
		void RenderDebugText3d(Vec3 worldPosition, string str, uint color, int screenPosOffsetX, int screenPosOffsetY, float time);

		// Token: 0x06000552 RID: 1362
		[EngineMethod("render_debug_text", false, null, false)]
		void RenderDebugText(float screenX, float screenY, string str, uint color, float time);

		// Token: 0x06000553 RID: 1363
		[EngineMethod("render_debug_rect", false, null, false)]
		void RenderDebugRect(float left, float bottom, float right, float top);

		// Token: 0x06000554 RID: 1364
		[EngineMethod("render_debug_rect_with_color", false, null, false)]
		void RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color);

		// Token: 0x06000555 RID: 1365
		[EngineMethod("clear_all_debug_render_objects", false, null, false)]
		void ClearAllDebugRenderObjects();

		// Token: 0x06000556 RID: 1366
		[EngineMethod("get_debug_vector", false, null, false)]
		Vec3 GetDebugVector();

		// Token: 0x06000557 RID: 1367
		[EngineMethod("set_debug_vector", false, null, false)]
		void SetDebugVector(Vec3 debugVector);

		// Token: 0x06000558 RID: 1368
		[EngineMethod("render_debug_box_object", false, null, false)]
		void RenderDebugBoxObject(Vec3 min, Vec3 max, uint color, bool depthCheck, float time);

		// Token: 0x06000559 RID: 1369
		[EngineMethod("render_debug_box_object_with_frame", false, null, false)]
		void RenderDebugBoxObjectWithFrame(Vec3 min, Vec3 max, ref MatrixFrame frame, uint color, bool depthCheck, float time);

		// Token: 0x0600055A RID: 1370
		[EngineMethod("post_warning_line", false, null, false)]
		void PostWarningLine(string line);

		// Token: 0x0600055B RID: 1371
		[EngineMethod("is_error_report_mode_active", false, null, false)]
		bool IsErrorReportModeActive();

		// Token: 0x0600055C RID: 1372
		[EngineMethod("is_error_report_mode_pause_mission", false, null, false)]
		bool IsErrorReportModePauseMission();

		// Token: 0x0600055D RID: 1373
		[EngineMethod("set_error_report_scene", false, null, false)]
		void SetErrorReportScene(UIntPtr scenePointer);

		// Token: 0x0600055E RID: 1374
		[EngineMethod("set_dump_generation_disabled", false, null, false)]
		void SetDumpGenerationDisabled(bool Disabled);

		// Token: 0x0600055F RID: 1375
		[EngineMethod("message_box", false, null, false)]
		int MessageBox(string lpText, string lpCaption, uint uType);

		// Token: 0x06000560 RID: 1376
		[EngineMethod("get_show_debug_info", false, null, false)]
		int GetShowDebugInfo();

		// Token: 0x06000561 RID: 1377
		[EngineMethod("set_show_debug_info", false, null, false)]
		void SetShowDebugInfo(int value);

		// Token: 0x06000562 RID: 1378
		[EngineMethod("error", false, null, false)]
		bool Error(string MessageString);

		// Token: 0x06000563 RID: 1379
		[EngineMethod("warning", false, null, false)]
		bool Warning(string MessageString);

		// Token: 0x06000564 RID: 1380
		[EngineMethod("content_warning", false, null, false)]
		bool ContentWarning(string MessageString);

		// Token: 0x06000565 RID: 1381
		[EngineMethod("failed_assert", false, null, false)]
		bool FailedAssert(string messageString, string callerFile, string callerMethod, int callerLine);

		// Token: 0x06000566 RID: 1382
		[EngineMethod("silent_assert", false, null, false)]
		bool SilentAssert(string messageString, string callerFile, string callerMethod, int callerLine, bool getDump);

		// Token: 0x06000567 RID: 1383
		[EngineMethod("is_test_mode", false, null, false)]
		bool IsTestMode();

		// Token: 0x06000568 RID: 1384
		[EngineMethod("echo_command_window", false, null, false)]
		void EchoCommandWindow(string content);
	}
}
