using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIDebug : IDebug
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AbortGameDelegate(int ExitCode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AssertMemoryUsageDelegate(int memoryMB);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearAllDebugRenderObjectsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ContentWarningDelegate(byte[] MessageString);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EchoCommandWindowDelegate(byte[] content);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ErrorDelegate(byte[] MessageString);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool FailedAssertDelegate(byte[] messageString, byte[] callerFile, byte[] callerMethod, int callerLine);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetDebugVectorDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetShowDebugInfoDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsErrorReportModeActiveDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsErrorReportModePauseMissionDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsTestModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int MessageBoxDelegate(byte[] lpText, byte[] lpCaption, uint uType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PostWarningLineDelegate(byte[] line);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugBoxObjectDelegate(Vec3 min, Vec3 max, uint color, [MarshalAs(UnmanagedType.U1)] bool depthCheck, float time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugBoxObjectWithFrameDelegate(Vec3 min, Vec3 max, ref MatrixFrame frame, uint color, [MarshalAs(UnmanagedType.U1)] bool depthCheck, float time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugCapsuleDelegate(Vec3 p0, Vec3 p1, float radius, uint color, [MarshalAs(UnmanagedType.U1)] bool depthCheck, float time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugDirectionArrowDelegate(Vec3 position, Vec3 direction, uint color, [MarshalAs(UnmanagedType.U1)] bool depthCheck);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugFrameDelegate(ref MatrixFrame frame, float lineLength, float time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugLineDelegate(Vec3 position, Vec3 direction, uint color, [MarshalAs(UnmanagedType.U1)] bool depthCheck, float time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugRectDelegate(float left, float bottom, float right, float top);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugRectWithColorDelegate(float left, float bottom, float right, float top, uint color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugSphereDelegate(Vec3 position, float radius, uint color, [MarshalAs(UnmanagedType.U1)] bool depthCheck, float time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugTextDelegate(float screenX, float screenY, byte[] str, uint color, float time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugText3dDelegate(Vec3 worldPosition, byte[] str, uint color, int screenPosOffsetX, int screenPosOffsetY, float time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDebugVectorDelegate(Vec3 debugVector);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDumpGenerationDisabledDelegate([MarshalAs(UnmanagedType.U1)] bool Disabled);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetErrorReportSceneDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetShowDebugInfoDelegate(int value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool SilentAssertDelegate(byte[] messageString, byte[] callerFile, byte[] callerMethod, int callerLine, [MarshalAs(UnmanagedType.U1)] bool getDump);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool WarningDelegate(byte[] MessageString);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WriteDebugLineOnScreenDelegate(byte[] line);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WriteLineDelegate(int logLevel, byte[] line, int color, ulong filter);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AbortGameDelegate call_AbortGameDelegate;

	public static AssertMemoryUsageDelegate call_AssertMemoryUsageDelegate;

	public static ClearAllDebugRenderObjectsDelegate call_ClearAllDebugRenderObjectsDelegate;

	public static ContentWarningDelegate call_ContentWarningDelegate;

	public static EchoCommandWindowDelegate call_EchoCommandWindowDelegate;

	public static ErrorDelegate call_ErrorDelegate;

	public static FailedAssertDelegate call_FailedAssertDelegate;

	public static GetDebugVectorDelegate call_GetDebugVectorDelegate;

	public static GetShowDebugInfoDelegate call_GetShowDebugInfoDelegate;

	public static IsErrorReportModeActiveDelegate call_IsErrorReportModeActiveDelegate;

	public static IsErrorReportModePauseMissionDelegate call_IsErrorReportModePauseMissionDelegate;

	public static IsTestModeDelegate call_IsTestModeDelegate;

	public static MessageBoxDelegate call_MessageBoxDelegate;

	public static PostWarningLineDelegate call_PostWarningLineDelegate;

	public static RenderDebugBoxObjectDelegate call_RenderDebugBoxObjectDelegate;

	public static RenderDebugBoxObjectWithFrameDelegate call_RenderDebugBoxObjectWithFrameDelegate;

	public static RenderDebugCapsuleDelegate call_RenderDebugCapsuleDelegate;

	public static RenderDebugDirectionArrowDelegate call_RenderDebugDirectionArrowDelegate;

	public static RenderDebugFrameDelegate call_RenderDebugFrameDelegate;

	public static RenderDebugLineDelegate call_RenderDebugLineDelegate;

	public static RenderDebugRectDelegate call_RenderDebugRectDelegate;

	public static RenderDebugRectWithColorDelegate call_RenderDebugRectWithColorDelegate;

	public static RenderDebugSphereDelegate call_RenderDebugSphereDelegate;

	public static RenderDebugTextDelegate call_RenderDebugTextDelegate;

	public static RenderDebugText3dDelegate call_RenderDebugText3dDelegate;

	public static SetDebugVectorDelegate call_SetDebugVectorDelegate;

	public static SetDumpGenerationDisabledDelegate call_SetDumpGenerationDisabledDelegate;

	public static SetErrorReportSceneDelegate call_SetErrorReportSceneDelegate;

	public static SetShowDebugInfoDelegate call_SetShowDebugInfoDelegate;

	public static SilentAssertDelegate call_SilentAssertDelegate;

	public static WarningDelegate call_WarningDelegate;

	public static WriteDebugLineOnScreenDelegate call_WriteDebugLineOnScreenDelegate;

	public static WriteLineDelegate call_WriteLineDelegate;

	public void AbortGame(int ExitCode)
	{
		call_AbortGameDelegate(ExitCode);
	}

	public void AssertMemoryUsage(int memoryMB)
	{
		call_AssertMemoryUsageDelegate(memoryMB);
	}

	public void ClearAllDebugRenderObjects()
	{
		call_ClearAllDebugRenderObjectsDelegate();
	}

	public bool ContentWarning(string MessageString)
	{
		byte[] array = null;
		if (MessageString != null)
		{
			int byteCount = _utf8.GetByteCount(MessageString);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(MessageString, 0, MessageString.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_ContentWarningDelegate(array);
	}

	public void EchoCommandWindow(string content)
	{
		byte[] array = null;
		if (content != null)
		{
			int byteCount = _utf8.GetByteCount(content);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(content, 0, content.Length, array, 0);
			array[byteCount] = 0;
		}
		call_EchoCommandWindowDelegate(array);
	}

	public bool Error(string MessageString)
	{
		byte[] array = null;
		if (MessageString != null)
		{
			int byteCount = _utf8.GetByteCount(MessageString);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(MessageString, 0, MessageString.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_ErrorDelegate(array);
	}

	public bool FailedAssert(string messageString, string callerFile, string callerMethod, int callerLine)
	{
		byte[] array = null;
		if (messageString != null)
		{
			int byteCount = _utf8.GetByteCount(messageString);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(messageString, 0, messageString.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (callerFile != null)
		{
			int byteCount2 = _utf8.GetByteCount(callerFile);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(callerFile, 0, callerFile.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		byte[] array3 = null;
		if (callerMethod != null)
		{
			int byteCount3 = _utf8.GetByteCount(callerMethod);
			array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
			_utf8.GetBytes(callerMethod, 0, callerMethod.Length, array3, 0);
			array3[byteCount3] = 0;
		}
		return call_FailedAssertDelegate(array, array2, array3, callerLine);
	}

	public Vec3 GetDebugVector()
	{
		return call_GetDebugVectorDelegate();
	}

	public int GetShowDebugInfo()
	{
		return call_GetShowDebugInfoDelegate();
	}

	public bool IsErrorReportModeActive()
	{
		return call_IsErrorReportModeActiveDelegate();
	}

	public bool IsErrorReportModePauseMission()
	{
		return call_IsErrorReportModePauseMissionDelegate();
	}

	public bool IsTestMode()
	{
		return call_IsTestModeDelegate();
	}

	public int MessageBox(string lpText, string lpCaption, uint uType)
	{
		byte[] array = null;
		if (lpText != null)
		{
			int byteCount = _utf8.GetByteCount(lpText);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(lpText, 0, lpText.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (lpCaption != null)
		{
			int byteCount2 = _utf8.GetByteCount(lpCaption);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(lpCaption, 0, lpCaption.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		return call_MessageBoxDelegate(array, array2, uType);
	}

	public void PostWarningLine(string line)
	{
		byte[] array = null;
		if (line != null)
		{
			int byteCount = _utf8.GetByteCount(line);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(line, 0, line.Length, array, 0);
			array[byteCount] = 0;
		}
		call_PostWarningLineDelegate(array);
	}

	public void RenderDebugBoxObject(Vec3 min, Vec3 max, uint color, bool depthCheck, float time)
	{
		call_RenderDebugBoxObjectDelegate(min, max, color, depthCheck, time);
	}

	public void RenderDebugBoxObjectWithFrame(Vec3 min, Vec3 max, ref MatrixFrame frame, uint color, bool depthCheck, float time)
	{
		call_RenderDebugBoxObjectWithFrameDelegate(min, max, ref frame, color, depthCheck, time);
	}

	public void RenderDebugCapsule(Vec3 p0, Vec3 p1, float radius, uint color, bool depthCheck, float time)
	{
		call_RenderDebugCapsuleDelegate(p0, p1, radius, color, depthCheck, time);
	}

	public void RenderDebugDirectionArrow(Vec3 position, Vec3 direction, uint color, bool depthCheck)
	{
		call_RenderDebugDirectionArrowDelegate(position, direction, color, depthCheck);
	}

	public void RenderDebugFrame(ref MatrixFrame frame, float lineLength, float time)
	{
		call_RenderDebugFrameDelegate(ref frame, lineLength, time);
	}

	public void RenderDebugLine(Vec3 position, Vec3 direction, uint color, bool depthCheck, float time)
	{
		call_RenderDebugLineDelegate(position, direction, color, depthCheck, time);
	}

	public void RenderDebugRect(float left, float bottom, float right, float top)
	{
		call_RenderDebugRectDelegate(left, bottom, right, top);
	}

	public void RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color)
	{
		call_RenderDebugRectWithColorDelegate(left, bottom, right, top, color);
	}

	public void RenderDebugSphere(Vec3 position, float radius, uint color, bool depthCheck, float time)
	{
		call_RenderDebugSphereDelegate(position, radius, color, depthCheck, time);
	}

	public void RenderDebugText(float screenX, float screenY, string str, uint color, float time)
	{
		byte[] array = null;
		if (str != null)
		{
			int byteCount = _utf8.GetByteCount(str);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(str, 0, str.Length, array, 0);
			array[byteCount] = 0;
		}
		call_RenderDebugTextDelegate(screenX, screenY, array, color, time);
	}

	public void RenderDebugText3d(Vec3 worldPosition, string str, uint color, int screenPosOffsetX, int screenPosOffsetY, float time)
	{
		byte[] array = null;
		if (str != null)
		{
			int byteCount = _utf8.GetByteCount(str);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(str, 0, str.Length, array, 0);
			array[byteCount] = 0;
		}
		call_RenderDebugText3dDelegate(worldPosition, array, color, screenPosOffsetX, screenPosOffsetY, time);
	}

	public void SetDebugVector(Vec3 debugVector)
	{
		call_SetDebugVectorDelegate(debugVector);
	}

	public void SetDumpGenerationDisabled(bool Disabled)
	{
		call_SetDumpGenerationDisabledDelegate(Disabled);
	}

	public void SetErrorReportScene(UIntPtr scenePointer)
	{
		call_SetErrorReportSceneDelegate(scenePointer);
	}

	public void SetShowDebugInfo(int value)
	{
		call_SetShowDebugInfoDelegate(value);
	}

	public bool SilentAssert(string messageString, string callerFile, string callerMethod, int callerLine, bool getDump)
	{
		byte[] array = null;
		if (messageString != null)
		{
			int byteCount = _utf8.GetByteCount(messageString);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(messageString, 0, messageString.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (callerFile != null)
		{
			int byteCount2 = _utf8.GetByteCount(callerFile);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(callerFile, 0, callerFile.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		byte[] array3 = null;
		if (callerMethod != null)
		{
			int byteCount3 = _utf8.GetByteCount(callerMethod);
			array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
			_utf8.GetBytes(callerMethod, 0, callerMethod.Length, array3, 0);
			array3[byteCount3] = 0;
		}
		return call_SilentAssertDelegate(array, array2, array3, callerLine, getDump);
	}

	public bool Warning(string MessageString)
	{
		byte[] array = null;
		if (MessageString != null)
		{
			int byteCount = _utf8.GetByteCount(MessageString);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(MessageString, 0, MessageString.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_WarningDelegate(array);
	}

	public void WriteDebugLineOnScreen(string line)
	{
		byte[] array = null;
		if (line != null)
		{
			int byteCount = _utf8.GetByteCount(line);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(line, 0, line.Length, array, 0);
			array[byteCount] = 0;
		}
		call_WriteDebugLineOnScreenDelegate(array);
	}

	public void WriteLine(int logLevel, string line, int color, ulong filter)
	{
		byte[] array = null;
		if (line != null)
		{
			int byteCount = _utf8.GetByteCount(line);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(line, 0, line.Length, array, 0);
			array[byteCount] = 0;
		}
		call_WriteLineDelegate(logLevel, array, color, filter);
	}
}
