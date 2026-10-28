using System.Runtime.CompilerServices;

namespace TaleWorlds.Library;

public interface IDebugManager
{
	void ShowWarning(string message);

	void Assert(bool condition, string message, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0);

	void SilentAssert(bool condition, string message = "", bool getDump = false, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0);

	void Print(string message, int logLevel = 0, Debug.DebugColor color = Debug.DebugColor.White, ulong debugFilter = 17592186044416uL);

	void PrintError(string error, string stackTrace, ulong debugFilter = 17592186044416uL);

	void PrintWarning(string warning, ulong debugFilter = 17592186044416uL);

	void ShowError(string message);

	void ShowMessageBox(string lpText, string lpCaption, uint uType);

	void DisplayDebugMessage(string message);

	void WatchVariable(string name, object value);

	void WriteDebugLineOnScreen(string message);

	void RenderDebugLine(Vec3 position, Vec3 direction, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f);

	void RenderDebugSphere(Vec3 position, float radius, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f);

	void RenderDebugText3D(Vec3 position, string text, uint color = uint.MaxValue, int screenPosOffsetX = 0, int screenPosOffsetY = 0, float time = 0f);

	void RenderDebugFrame(MatrixFrame frame, float lineLength, float time = 0f);

	void RenderDebugText(float screenX, float screenY, string text, uint color = uint.MaxValue, float time = 0f);

	void RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color = uint.MaxValue);

	Vec3 GetDebugVector();

	void SetDebugVector(Vec3 value);

	void SetCrashReportCustomString(string customString);

	void SetCrashReportCustomStack(string customStack);

	void SetTestModeEnabled(bool testModeEnabled);

	void AbortGame();

	void DoDelayedexit(int returnCode);

	void ReportMemoryBookmark(string message);
}
