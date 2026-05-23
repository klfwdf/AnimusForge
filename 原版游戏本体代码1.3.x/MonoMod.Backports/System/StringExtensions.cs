using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200001D RID: 29
	[NullableContext(1)]
	[Nullable(0)]
	public static class StringExtensions
	{
		// Token: 0x0600011F RID: 287 RVA: 0x00009148 File Offset: 0x00007348
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Replace(this string self, string oldValue, string newValue, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			ThrowHelper.ThrowIfArgumentNull(oldValue, ExceptionArgument.oldValue);
			ThrowHelper.ThrowIfArgumentNull(newValue, ExceptionArgument.newValue);
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(oldValue.Length, 0);
			ReadOnlySpan<char> readOnlySpan = self.AsSpan();
			ReadOnlySpan<char> value = oldValue.AsSpan();
			for (;;)
			{
				int num = readOnlySpan.IndexOf(value, comparison);
				if (num < 0)
				{
					break;
				}
				defaultInterpolatedStringHandler.AppendFormatted(readOnlySpan.Slice(0, num));
				defaultInterpolatedStringHandler.AppendLiteral(newValue);
				readOnlySpan = readOnlySpan.Slice(num + value.Length);
			}
			defaultInterpolatedStringHandler.AppendFormatted(readOnlySpan);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06000120 RID: 288 RVA: 0x000091D2 File Offset: 0x000073D2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this string self, string value, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			ThrowHelper.ThrowIfArgumentNull(value, ExceptionArgument.value);
			return self.IndexOf(value, comparison) >= 0;
		}

		// Token: 0x06000121 RID: 289 RVA: 0x000091F2 File Offset: 0x000073F2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this string self, char value, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			return self.IndexOf(value, comparison) >= 0;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000920A File Offset: 0x0000740A
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetHashCode(this string self, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			return StringComparerEx.FromComparison(comparison).GetHashCode(self);
		}

		// Token: 0x06000123 RID: 291 RVA: 0x00009220 File Offset: 0x00007420
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf(this string self, char value, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			return self.IndexOf(new string(value, 1), comparison);
		}
	}
}
