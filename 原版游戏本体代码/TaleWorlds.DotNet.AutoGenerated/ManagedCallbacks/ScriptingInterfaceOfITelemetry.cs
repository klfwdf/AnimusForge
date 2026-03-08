using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x0200000C RID: 12
	internal class ScriptingInterfaceOfITelemetry : ITelemetry
	{
		// Token: 0x06000062 RID: 98 RVA: 0x000030D8 File Offset: 0x000012D8
		public void BeginTelemetryScope(TelemetryLevelMask levelMask, string scopeName)
		{
			byte[] array = null;
			if (scopeName != null)
			{
				int byteCount = ScriptingInterfaceOfITelemetry._utf8.GetByteCount(scopeName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITelemetry._utf8.GetBytes(scopeName, 0, scopeName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfITelemetry.call_BeginTelemetryScopeDelegate(levelMask, array);
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00003133 File Offset: 0x00001333
		public void EndTelemetryScope()
		{
			ScriptingInterfaceOfITelemetry.call_EndTelemetryScopeDelegate();
		}

		// Token: 0x06000064 RID: 100 RVA: 0x0000313F File Offset: 0x0000133F
		public TelemetryLevelMask GetTelemetryLevelMask()
		{
			return ScriptingInterfaceOfITelemetry.call_GetTelemetryLevelMaskDelegate();
		}

		// Token: 0x06000065 RID: 101 RVA: 0x0000314B File Offset: 0x0000134B
		public bool HasTelemetryConnection()
		{
			return ScriptingInterfaceOfITelemetry.call_HasTelemetryConnectionDelegate();
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00003157 File Offset: 0x00001357
		public void StartTelemetryConnection(bool showErrors)
		{
			ScriptingInterfaceOfITelemetry.call_StartTelemetryConnectionDelegate(showErrors);
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00003164 File Offset: 0x00001364
		public void StopTelemetryConnection()
		{
			ScriptingInterfaceOfITelemetry.call_StopTelemetryConnectionDelegate();
		}

		// Token: 0x04000023 RID: 35
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000024 RID: 36
		public static ScriptingInterfaceOfITelemetry.BeginTelemetryScopeDelegate call_BeginTelemetryScopeDelegate;

		// Token: 0x04000025 RID: 37
		public static ScriptingInterfaceOfITelemetry.EndTelemetryScopeDelegate call_EndTelemetryScopeDelegate;

		// Token: 0x04000026 RID: 38
		public static ScriptingInterfaceOfITelemetry.GetTelemetryLevelMaskDelegate call_GetTelemetryLevelMaskDelegate;

		// Token: 0x04000027 RID: 39
		public static ScriptingInterfaceOfITelemetry.HasTelemetryConnectionDelegate call_HasTelemetryConnectionDelegate;

		// Token: 0x04000028 RID: 40
		public static ScriptingInterfaceOfITelemetry.StartTelemetryConnectionDelegate call_StartTelemetryConnectionDelegate;

		// Token: 0x04000029 RID: 41
		public static ScriptingInterfaceOfITelemetry.StopTelemetryConnectionDelegate call_StopTelemetryConnectionDelegate;

		// Token: 0x02000053 RID: 83
		// (Invoke) Token: 0x0600017F RID: 383
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void BeginTelemetryScopeDelegate(TelemetryLevelMask levelMask, byte[] scopeName);

		// Token: 0x02000054 RID: 84
		// (Invoke) Token: 0x06000183 RID: 387
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EndTelemetryScopeDelegate();

		// Token: 0x02000055 RID: 85
		// (Invoke) Token: 0x06000187 RID: 391
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate TelemetryLevelMask GetTelemetryLevelMaskDelegate();

		// Token: 0x02000056 RID: 86
		// (Invoke) Token: 0x0600018B RID: 395
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasTelemetryConnectionDelegate();

		// Token: 0x02000057 RID: 87
		// (Invoke) Token: 0x0600018F RID: 399
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StartTelemetryConnectionDelegate([MarshalAs(UnmanagedType.U1)] bool showErrors);

		// Token: 0x02000058 RID: 88
		// (Invoke) Token: 0x06000193 RID: 403
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StopTelemetryConnectionDelegate();
	}
}
