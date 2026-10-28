using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfITelemetry : ITelemetry
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BeginTelemetryScopeDelegate(TelemetryLevelMask levelMask, byte[] scopeName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EndTelemetryScopeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate TelemetryLevelMask GetTelemetryLevelMaskDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasTelemetryConnectionDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StartTelemetryConnectionDelegate([MarshalAs(UnmanagedType.U1)] bool showErrors);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StopTelemetryConnectionDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static BeginTelemetryScopeDelegate call_BeginTelemetryScopeDelegate;

	public static EndTelemetryScopeDelegate call_EndTelemetryScopeDelegate;

	public static GetTelemetryLevelMaskDelegate call_GetTelemetryLevelMaskDelegate;

	public static HasTelemetryConnectionDelegate call_HasTelemetryConnectionDelegate;

	public static StartTelemetryConnectionDelegate call_StartTelemetryConnectionDelegate;

	public static StopTelemetryConnectionDelegate call_StopTelemetryConnectionDelegate;

	public void BeginTelemetryScope(TelemetryLevelMask levelMask, string scopeName)
	{
		byte[] array = null;
		if (scopeName != null)
		{
			int byteCount = _utf8.GetByteCount(scopeName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(scopeName, 0, scopeName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_BeginTelemetryScopeDelegate(levelMask, array);
	}

	public void EndTelemetryScope()
	{
		call_EndTelemetryScopeDelegate();
	}

	public TelemetryLevelMask GetTelemetryLevelMask()
	{
		return call_GetTelemetryLevelMaskDelegate();
	}

	public bool HasTelemetryConnection()
	{
		return call_HasTelemetryConnectionDelegate();
	}

	public void StartTelemetryConnection(bool showErrors)
	{
		call_StartTelemetryConnectionDelegate(showErrors);
	}

	public void StopTelemetryConnection()
	{
		call_StopTelemetryConnectionDelegate();
	}
}
