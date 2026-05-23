using System;
using System.Runtime.CompilerServices;
using MonoMod.Logs;

namespace MonoMod.Utils
{
	// Token: 0x020008CB RID: 2251
	[NullableContext(1)]
	[Nullable(0)]
	[InterpolatedStringHandler]
	internal ref struct AssertionInterpolatedStringHandler
	{
		// Token: 0x06002EBC RID: 11964 RVA: 0x000A0F73 File Offset: 0x0009F173
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public AssertionInterpolatedStringHandler(int literalLen, int formattedCount, bool assertValue, out bool isEnabled)
		{
			this.handler = new DebugLogInterpolatedStringHandler(literalLen, formattedCount, !assertValue, false, ref isEnabled);
		}

		// Token: 0x06002EBD RID: 11965 RVA: 0x000A0F89 File Offset: 0x0009F189
		public override string ToString()
		{
			return this.handler.ToString();
		}

		// Token: 0x06002EBE RID: 11966 RVA: 0x000A0F9C File Offset: 0x0009F19C
		public string ToStringAndClear()
		{
			return this.handler.ToStringAndClear();
		}

		// Token: 0x06002EBF RID: 11967 RVA: 0x000A0FA9 File Offset: 0x0009F1A9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AppendLiteral(string s)
		{
			this.handler.AppendLiteral(s);
		}

		// Token: 0x06002EC0 RID: 11968 RVA: 0x000A0FB7 File Offset: 0x0009F1B7
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AppendFormatted(string s)
		{
			this.handler.AppendFormatted(s);
		}

		// Token: 0x06002EC1 RID: 11969 RVA: 0x000A0FC5 File Offset: 0x0009F1C5
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AppendFormatted(string s, int alignment = 0, string format = null)
		{
			this.handler.AppendFormatted(s, alignment, format);
		}

		// Token: 0x06002EC2 RID: 11970 RVA: 0x000A0FD5 File Offset: 0x0009F1D5
		[NullableContext(0)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AppendFormatted(ReadOnlySpan<char> s)
		{
			this.handler.AppendFormatted(s);
		}

		// Token: 0x06002EC3 RID: 11971 RVA: 0x000A0FE3 File Offset: 0x0009F1E3
		[NullableContext(0)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AppendFormatted(ReadOnlySpan<char> s, int alignment = 0, [Nullable(2)] string format = null)
		{
			this.handler.AppendFormatted(s, alignment, format);
		}

		// Token: 0x06002EC4 RID: 11972 RVA: 0x000A0FF3 File Offset: 0x0009F1F3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AppendFormatted<[Nullable(2)] T>(T value)
		{
			this.handler.AppendFormatted<T>(value);
		}

		// Token: 0x06002EC5 RID: 11973 RVA: 0x000A1001 File Offset: 0x0009F201
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
		{
			this.handler.AppendFormatted<T>(value, alignment);
		}

		// Token: 0x06002EC6 RID: 11974 RVA: 0x000A1010 File Offset: 0x0009F210
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AppendFormatted<T>([Nullable(1)] T value, string format)
		{
			this.handler.AppendFormatted<T>(value, format);
		}

		// Token: 0x06002EC7 RID: 11975 RVA: 0x000A101F File Offset: 0x0009F21F
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
		{
			this.handler.AppendFormatted<T>(value, alignment, format);
		}

		// Token: 0x04003B41 RID: 15169
		private DebugLogInterpolatedStringHandler handler;
	}
}
