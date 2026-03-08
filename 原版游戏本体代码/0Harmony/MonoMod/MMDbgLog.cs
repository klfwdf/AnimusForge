using System;
using System.Runtime.CompilerServices;
using MonoMod.Logs;

namespace MonoMod
{
	// Token: 0x02000809 RID: 2057
	[NullableContext(1)]
	[Nullable(0)]
	internal static class MMDbgLog
	{
		// Token: 0x17000804 RID: 2052
		// (get) Token: 0x0600273C RID: 10044 RVA: 0x00058525 File Offset: 0x00056725
		public static bool IsWritingLog
		{
			get
			{
				return DebugLog.IsWritingLog;
			}
		}

		// Token: 0x0600273D RID: 10045 RVA: 0x00087A04 File Offset: 0x00085C04
		[ModuleInitializer]
		internal static void LogVersion()
		{
			MMDbgLog.Info("Version 25.0.11");
		}

		// Token: 0x0600273E RID: 10046 RVA: 0x00087A10 File Offset: 0x00085C10
		public static void Log(LogLevel level, string message)
		{
			DebugLog.Log("MonoMod.Utils", level, message);
		}

		// Token: 0x0600273F RID: 10047 RVA: 0x00087A1E File Offset: 0x00085C1E
		public static void Log(LogLevel level, [InterpolatedStringHandlerArgument("level")] ref DebugLogInterpolatedStringHandler message)
		{
			DebugLog.Log("MonoMod.Utils", level, ref message);
		}

		// Token: 0x06002740 RID: 10048 RVA: 0x00087A2C File Offset: 0x00085C2C
		public static void Spam(string message)
		{
			MMDbgLog.Log(LogLevel.Spam, message);
		}

		// Token: 0x06002741 RID: 10049 RVA: 0x00087A35 File Offset: 0x00085C35
		public static void Spam(ref MMDbgLog.DebugLogSpamStringHandler message)
		{
			MMDbgLog.Log(LogLevel.Spam, ref message.handler);
		}

		// Token: 0x06002742 RID: 10050 RVA: 0x00087A43 File Offset: 0x00085C43
		public static void Trace(string message)
		{
			MMDbgLog.Log(LogLevel.Trace, message);
		}

		// Token: 0x06002743 RID: 10051 RVA: 0x00087A4C File Offset: 0x00085C4C
		public static void Trace(ref MMDbgLog.DebugLogTraceStringHandler message)
		{
			MMDbgLog.Log(LogLevel.Trace, ref message.handler);
		}

		// Token: 0x06002744 RID: 10052 RVA: 0x00087A5A File Offset: 0x00085C5A
		public static void Info(string message)
		{
			MMDbgLog.Log(LogLevel.Info, message);
		}

		// Token: 0x06002745 RID: 10053 RVA: 0x00087A63 File Offset: 0x00085C63
		public static void Info(ref MMDbgLog.DebugLogInfoStringHandler message)
		{
			MMDbgLog.Log(LogLevel.Info, ref message.handler);
		}

		// Token: 0x06002746 RID: 10054 RVA: 0x00087A71 File Offset: 0x00085C71
		public static void Warning(string message)
		{
			MMDbgLog.Log(LogLevel.Warning, message);
		}

		// Token: 0x06002747 RID: 10055 RVA: 0x00087A7A File Offset: 0x00085C7A
		public static void Warning(ref MMDbgLog.DebugLogWarningStringHandler message)
		{
			MMDbgLog.Log(LogLevel.Warning, ref message.handler);
		}

		// Token: 0x06002748 RID: 10056 RVA: 0x00087A88 File Offset: 0x00085C88
		public static void Error(string message)
		{
			MMDbgLog.Log(LogLevel.Error, message);
		}

		// Token: 0x06002749 RID: 10057 RVA: 0x00087A91 File Offset: 0x00085C91
		public static void Error(ref MMDbgLog.DebugLogErrorStringHandler message)
		{
			MMDbgLog.Log(LogLevel.Error, ref message.handler);
		}

		// Token: 0x0200080A RID: 2058
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogSpamStringHandler
		{
			// Token: 0x0600274A RID: 10058 RVA: 0x00087A9F File Offset: 0x00085C9F
			public DebugLogSpamStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Spam, ref isEnabled);
			}

			// Token: 0x0600274B RID: 10059 RVA: 0x00087AB0 File Offset: 0x00085CB0
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x0600274C RID: 10060 RVA: 0x00087AC3 File Offset: 0x00085CC3
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x0600274D RID: 10061 RVA: 0x00087AD0 File Offset: 0x00085CD0
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x0600274E RID: 10062 RVA: 0x00087ADE File Offset: 0x00085CDE
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x0600274F RID: 10063 RVA: 0x00087AEC File Offset: 0x00085CEC
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06002750 RID: 10064 RVA: 0x00087AFC File Offset: 0x00085CFC
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06002751 RID: 10065 RVA: 0x00087B0A File Offset: 0x00085D0A
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06002752 RID: 10066 RVA: 0x00087B1A File Offset: 0x00085D1A
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x06002753 RID: 10067 RVA: 0x00087B28 File Offset: 0x00085D28
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x06002754 RID: 10068 RVA: 0x00087B37 File Offset: 0x00085D37
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x06002755 RID: 10069 RVA: 0x00087B46 File Offset: 0x00085D46
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x040039DA RID: 14810
			internal DebugLogInterpolatedStringHandler handler;
		}

		// Token: 0x0200080B RID: 2059
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogTraceStringHandler
		{
			// Token: 0x06002756 RID: 10070 RVA: 0x00087B56 File Offset: 0x00085D56
			public DebugLogTraceStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Trace, ref isEnabled);
			}

			// Token: 0x06002757 RID: 10071 RVA: 0x00087B67 File Offset: 0x00085D67
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x06002758 RID: 10072 RVA: 0x00087B7A File Offset: 0x00085D7A
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x06002759 RID: 10073 RVA: 0x00087B87 File Offset: 0x00085D87
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x0600275A RID: 10074 RVA: 0x00087B95 File Offset: 0x00085D95
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x0600275B RID: 10075 RVA: 0x00087BA3 File Offset: 0x00085DA3
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x0600275C RID: 10076 RVA: 0x00087BB3 File Offset: 0x00085DB3
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x0600275D RID: 10077 RVA: 0x00087BC1 File Offset: 0x00085DC1
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x0600275E RID: 10078 RVA: 0x00087BD1 File Offset: 0x00085DD1
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x0600275F RID: 10079 RVA: 0x00087BDF File Offset: 0x00085DDF
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x06002760 RID: 10080 RVA: 0x00087BEE File Offset: 0x00085DEE
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x06002761 RID: 10081 RVA: 0x00087BFD File Offset: 0x00085DFD
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x040039DB RID: 14811
			internal DebugLogInterpolatedStringHandler handler;
		}

		// Token: 0x0200080C RID: 2060
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogInfoStringHandler
		{
			// Token: 0x06002762 RID: 10082 RVA: 0x00087C0D File Offset: 0x00085E0D
			public DebugLogInfoStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Info, ref isEnabled);
			}

			// Token: 0x06002763 RID: 10083 RVA: 0x00087C1E File Offset: 0x00085E1E
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x06002764 RID: 10084 RVA: 0x00087C31 File Offset: 0x00085E31
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x06002765 RID: 10085 RVA: 0x00087C3E File Offset: 0x00085E3E
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x06002766 RID: 10086 RVA: 0x00087C4C File Offset: 0x00085E4C
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06002767 RID: 10087 RVA: 0x00087C5A File Offset: 0x00085E5A
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06002768 RID: 10088 RVA: 0x00087C6A File Offset: 0x00085E6A
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06002769 RID: 10089 RVA: 0x00087C78 File Offset: 0x00085E78
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x0600276A RID: 10090 RVA: 0x00087C88 File Offset: 0x00085E88
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x0600276B RID: 10091 RVA: 0x00087C96 File Offset: 0x00085E96
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x0600276C RID: 10092 RVA: 0x00087CA5 File Offset: 0x00085EA5
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x0600276D RID: 10093 RVA: 0x00087CB4 File Offset: 0x00085EB4
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x040039DC RID: 14812
			internal DebugLogInterpolatedStringHandler handler;
		}

		// Token: 0x0200080D RID: 2061
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogWarningStringHandler
		{
			// Token: 0x0600276E RID: 10094 RVA: 0x00087CC4 File Offset: 0x00085EC4
			public DebugLogWarningStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Warning, ref isEnabled);
			}

			// Token: 0x0600276F RID: 10095 RVA: 0x00087CD5 File Offset: 0x00085ED5
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x06002770 RID: 10096 RVA: 0x00087CE8 File Offset: 0x00085EE8
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x06002771 RID: 10097 RVA: 0x00087CF5 File Offset: 0x00085EF5
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x06002772 RID: 10098 RVA: 0x00087D03 File Offset: 0x00085F03
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06002773 RID: 10099 RVA: 0x00087D11 File Offset: 0x00085F11
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06002774 RID: 10100 RVA: 0x00087D21 File Offset: 0x00085F21
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06002775 RID: 10101 RVA: 0x00087D2F File Offset: 0x00085F2F
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06002776 RID: 10102 RVA: 0x00087D3F File Offset: 0x00085F3F
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x06002777 RID: 10103 RVA: 0x00087D4D File Offset: 0x00085F4D
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x06002778 RID: 10104 RVA: 0x00087D5C File Offset: 0x00085F5C
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x06002779 RID: 10105 RVA: 0x00087D6B File Offset: 0x00085F6B
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x040039DD RID: 14813
			internal DebugLogInterpolatedStringHandler handler;
		}

		// Token: 0x0200080E RID: 2062
		[Nullable(0)]
		[InterpolatedStringHandler]
		internal ref struct DebugLogErrorStringHandler
		{
			// Token: 0x0600277A RID: 10106 RVA: 0x00087D7B File Offset: 0x00085F7B
			public DebugLogErrorStringHandler(int literalLen, int formattedCount, out bool isEnabled)
			{
				this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, LogLevel.Error, ref isEnabled);
			}

			// Token: 0x0600277B RID: 10107 RVA: 0x00087D8C File Offset: 0x00085F8C
			public override string ToString()
			{
				return this.handler.ToString();
			}

			// Token: 0x0600277C RID: 10108 RVA: 0x00087D9F File Offset: 0x00085F9F
			public string ToStringAndClear()
			{
				return this.handler.ToStringAndClear();
			}

			// Token: 0x0600277D RID: 10109 RVA: 0x00087DAC File Offset: 0x00085FAC
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendLiteral(string s)
			{
				this.handler.AppendLiteral(s);
			}

			// Token: 0x0600277E RID: 10110 RVA: 0x00087DBA File Offset: 0x00085FBA
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x0600277F RID: 10111 RVA: 0x00087DC8 File Offset: 0x00085FC8
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(string s, int alignment = 0, string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06002780 RID: 10112 RVA: 0x00087DD8 File Offset: 0x00085FD8
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s)
			{
				this.handler.AppendFormatted(s);
			}

			// Token: 0x06002781 RID: 10113 RVA: 0x00087DE6 File Offset: 0x00085FE6
			[NullableContext(0)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
			{
				this.handler.AppendFormatted(s, alignment, format);
			}

			// Token: 0x06002782 RID: 10114 RVA: 0x00087DF6 File Offset: 0x00085FF6
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value)
			{
				this.handler.AppendFormatted<T>(value);
			}

			// Token: 0x06002783 RID: 10115 RVA: 0x00087E04 File Offset: 0x00086004
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
			{
				this.handler.AppendFormatted<T>(value, alignment);
			}

			// Token: 0x06002784 RID: 10116 RVA: 0x00087E13 File Offset: 0x00086013
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, string format)
			{
				this.handler.AppendFormatted<T>(value, format);
			}

			// Token: 0x06002785 RID: 10117 RVA: 0x00087E22 File Offset: 0x00086022
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
			{
				this.handler.AppendFormatted<T>(value, alignment, format);
			}

			// Token: 0x040039DE RID: 14814
			internal DebugLogInterpolatedStringHandler handler;
		}
	}
}
