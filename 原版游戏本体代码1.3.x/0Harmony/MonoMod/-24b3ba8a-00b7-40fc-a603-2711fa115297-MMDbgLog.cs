using System;
using System.Runtime.CompilerServices;
using MonoMod.Logs;

namespace MonoMod
{
	// Token: 0x020004D1 RID: 1233
	[NullableContext(1)]
	[Nullable(0)]
	internal static class <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog
	{
		// Token: 0x170005ED RID: 1517
		// (get) Token: 0x06001B4C RID: 6988 RVA: 0x00058525 File Offset: 0x00056725
		public static bool IsWritingLog
		{
			get
			{
				return DebugLog.IsWritingLog;
			}
		}

		// Token: 0x06001B4D RID: 6989 RVA: 0x0005852C File Offset: 0x0005672C
		[ModuleInitializer]
		internal static void LogVersion()
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Info("Version 1.3.3");
		}

		// Token: 0x06001B4E RID: 6990 RVA: 0x00058538 File Offset: 0x00056738
		public static void Log(LogLevel level, string message)
		{
			DebugLog.Log("MonoMod.Core", level, message);
		}

		// Token: 0x06001B4F RID: 6991 RVA: 0x00058546 File Offset: 0x00056746
		public static void Log(LogLevel level, [InterpolatedStringHandlerArgument("level")] ref DebugLogInterpolatedStringHandler message)
		{
			DebugLog.Log("MonoMod.Core", level, ref message);
		}

		// Token: 0x06001B50 RID: 6992 RVA: 0x00058554 File Offset: 0x00056754
		public static void Spam(string message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Spam, message);
		}

		// Token: 0x06001B51 RID: 6993 RVA: 0x0005855D File Offset: 0x0005675D
		public static void Spam(ref <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogSpamStringHandler message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Spam, ref message.handler);
		}

		// Token: 0x06001B52 RID: 6994 RVA: 0x0005856B File Offset: 0x0005676B
		public static void Trace(string message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Trace, message);
		}

		// Token: 0x06001B53 RID: 6995 RVA: 0x00058574 File Offset: 0x00056774
		public static void Trace(ref <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Trace, ref message.handler);
		}

		// Token: 0x06001B54 RID: 6996 RVA: 0x00058582 File Offset: 0x00056782
		public static void Info(string message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Info, message);
		}

		// Token: 0x06001B55 RID: 6997 RVA: 0x0005858B File Offset: 0x0005678B
		public static void Info(ref <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogInfoStringHandler message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Info, ref message.handler);
		}

		// Token: 0x06001B56 RID: 6998 RVA: 0x00058599 File Offset: 0x00056799
		public static void Warning(string message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Warning, message);
		}

		// Token: 0x06001B57 RID: 6999 RVA: 0x000585A2 File Offset: 0x000567A2
		public static void Warning(ref <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Warning, ref message.handler);
		}

		// Token: 0x06001B58 RID: 7000 RVA: 0x000585B0 File Offset: 0x000567B0
		public static void Error(string message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Error, message);
		}

		// Token: 0x06001B59 RID: 7001 RVA: 0x000585B9 File Offset: 0x000567B9
		public static void Error(ref <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler message)
		{
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Log(LogLevel.Error, ref message.handler);
		}

		// Token: 0x020004D2 RID: 1234
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogSpamStringHandler
		{
			// Token: 0x06001B5A RID: 7002 RVA: 0x000585C7 File Offset: 0x000567C7
			public DebugLogSpamStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Spam, ref isEnabled);
			}

			// Token: 0x06001B5B RID: 7003 RVA: 0x000585D8 File Offset: 0x000567D8
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x06001B5C RID: 7004 RVA: 0x000585EB File Offset: 0x000567EB
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x06001B5D RID: 7005 RVA: 0x000585F8 File Offset: 0x000567F8
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x06001B5E RID: 7006 RVA: 0x00058606 File Offset: 0x00056806
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B5F RID: 7007 RVA: 0x00058614 File Offset: 0x00056814
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B60 RID: 7008 RVA: 0x00058624 File Offset: 0x00056824
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B61 RID: 7009 RVA: 0x00058632 File Offset: 0x00056832
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B62 RID: 7010 RVA: 0x00058642 File Offset: 0x00056842
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x06001B63 RID: 7011 RVA: 0x00058650 File Offset: 0x00056850
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x06001B64 RID: 7012 RVA: 0x0005865F File Offset: 0x0005685F
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x06001B65 RID: 7013 RVA: 0x0005866E File Offset: 0x0005686E
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x0400114D RID: 4429
			internal DebugLogInterpolatedStringHandler handler;
		}

		// Token: 0x020004D3 RID: 1235
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogTraceStringHandler
		{
			// Token: 0x06001B66 RID: 7014 RVA: 0x0005867E File Offset: 0x0005687E
			public DebugLogTraceStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Trace, ref isEnabled);
			}

			// Token: 0x06001B67 RID: 7015 RVA: 0x0005868F File Offset: 0x0005688F
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x06001B68 RID: 7016 RVA: 0x000586A2 File Offset: 0x000568A2
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x06001B69 RID: 7017 RVA: 0x000586AF File Offset: 0x000568AF
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x06001B6A RID: 7018 RVA: 0x000586BD File Offset: 0x000568BD
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B6B RID: 7019 RVA: 0x000586CB File Offset: 0x000568CB
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B6C RID: 7020 RVA: 0x000586DB File Offset: 0x000568DB
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B6D RID: 7021 RVA: 0x000586E9 File Offset: 0x000568E9
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B6E RID: 7022 RVA: 0x000586F9 File Offset: 0x000568F9
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x06001B6F RID: 7023 RVA: 0x00058707 File Offset: 0x00056907
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x06001B70 RID: 7024 RVA: 0x00058716 File Offset: 0x00056916
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x06001B71 RID: 7025 RVA: 0x00058725 File Offset: 0x00056925
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x0400114E RID: 4430
			internal DebugLogInterpolatedStringHandler handler;
		}

		// Token: 0x020004D4 RID: 1236
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogInfoStringHandler
		{
			// Token: 0x06001B72 RID: 7026 RVA: 0x00058735 File Offset: 0x00056935
			public DebugLogInfoStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Info, ref isEnabled);
			}

			// Token: 0x06001B73 RID: 7027 RVA: 0x00058746 File Offset: 0x00056946
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x06001B74 RID: 7028 RVA: 0x00058759 File Offset: 0x00056959
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x06001B75 RID: 7029 RVA: 0x00058766 File Offset: 0x00056966
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x06001B76 RID: 7030 RVA: 0x00058774 File Offset: 0x00056974
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B77 RID: 7031 RVA: 0x00058782 File Offset: 0x00056982
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B78 RID: 7032 RVA: 0x00058792 File Offset: 0x00056992
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B79 RID: 7033 RVA: 0x000587A0 File Offset: 0x000569A0
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B7A RID: 7034 RVA: 0x000587B0 File Offset: 0x000569B0
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x06001B7B RID: 7035 RVA: 0x000587BE File Offset: 0x000569BE
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x06001B7C RID: 7036 RVA: 0x000587CD File Offset: 0x000569CD
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x06001B7D RID: 7037 RVA: 0x000587DC File Offset: 0x000569DC
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x0400114F RID: 4431
			internal DebugLogInterpolatedStringHandler handler;
		}

		// Token: 0x020004D5 RID: 1237
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogWarningStringHandler
		{
			// Token: 0x06001B7E RID: 7038 RVA: 0x000587EC File Offset: 0x000569EC
			public DebugLogWarningStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Warning, ref isEnabled);
			}

			// Token: 0x06001B7F RID: 7039 RVA: 0x000587FD File Offset: 0x000569FD
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x06001B80 RID: 7040 RVA: 0x00058810 File Offset: 0x00056A10
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x06001B81 RID: 7041 RVA: 0x0005881D File Offset: 0x00056A1D
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x06001B82 RID: 7042 RVA: 0x0005882B File Offset: 0x00056A2B
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B83 RID: 7043 RVA: 0x00058839 File Offset: 0x00056A39
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B84 RID: 7044 RVA: 0x00058849 File Offset: 0x00056A49
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B85 RID: 7045 RVA: 0x00058857 File Offset: 0x00056A57
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B86 RID: 7046 RVA: 0x00058867 File Offset: 0x00056A67
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x06001B87 RID: 7047 RVA: 0x00058875 File Offset: 0x00056A75
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x06001B88 RID: 7048 RVA: 0x00058884 File Offset: 0x00056A84
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x06001B89 RID: 7049 RVA: 0x00058893 File Offset: 0x00056A93
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x04001150 RID: 4432
			internal DebugLogInterpolatedStringHandler handler;
		}

		// Token: 0x020004D6 RID: 1238
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogErrorStringHandler
		{
			// Token: 0x06001B8A RID: 7050 RVA: 0x000588A3 File Offset: 0x00056AA3
			public DebugLogErrorStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Error, ref isEnabled);
			}

			// Token: 0x06001B8B RID: 7051 RVA: 0x000588B4 File Offset: 0x00056AB4
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x06001B8C RID: 7052 RVA: 0x000588C7 File Offset: 0x00056AC7
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x06001B8D RID: 7053 RVA: 0x000588D4 File Offset: 0x00056AD4
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x06001B8E RID: 7054 RVA: 0x000588E2 File Offset: 0x00056AE2
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B8F RID: 7055 RVA: 0x000588F0 File Offset: 0x00056AF0
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B90 RID: 7056 RVA: 0x00058900 File Offset: 0x00056B00
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06001B91 RID: 7057 RVA: 0x0005890E File Offset: 0x00056B0E
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06001B92 RID: 7058 RVA: 0x0005891E File Offset: 0x00056B1E
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x06001B93 RID: 7059 RVA: 0x0005892C File Offset: 0x00056B2C
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x06001B94 RID: 7060 RVA: 0x0005893B File Offset: 0x00056B3B
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x06001B95 RID: 7061 RVA: 0x0005894A File Offset: 0x00056B4A
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x04001151 RID: 4433
			internal DebugLogInterpolatedStringHandler handler;
		}
	}
}
