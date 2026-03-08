using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200047F RID: 1151
	[NullableContext(1)]
	[Nullable(0)]
	internal static class StringExtensions
	{
		// Token: 0x06001970 RID: 6512 RVA: 0x000541F0 File Offset: 0x000523F0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Replace(this string self, string oldValue, string newValue, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			ThrowHelper.ThrowIfArgumentNull(oldValue, ExceptionArgument.oldValue);
			ThrowHelper.ThrowIfArgumentNull(newValue, ExceptionArgument.newValue);
			DefaultInterpolatedStringHandler ish = new DefaultInterpolatedStringHandler(oldValue.Length, 0);
			ReadOnlySpan<char> from = self.AsSpan();
			ReadOnlySpan<char> old = oldValue.AsSpan();
			for (;;)
			{
				int idx = from.IndexOf(old, comparison);
				if (idx < 0)
				{
					break;
				}
				ish.AppendFormatted(from.Slice(0, idx));
				ish.AppendLiteral(newValue);
				from = from.Slice(idx + old.Length);
			}
			ish.AppendFormatted(from);
			return ish.ToStringAndClear();
		}

		// Token: 0x06001971 RID: 6513 RVA: 0x0005427A File Offset: 0x0005247A
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this string self, string value, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			ThrowHelper.ThrowIfArgumentNull(value, ExceptionArgument.value);
			return self.IndexOf(value, comparison) >= 0;
		}

		// Token: 0x06001972 RID: 6514 RVA: 0x0005429A File Offset: 0x0005249A
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this string self, char value, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			return self.IndexOf(value, comparison) >= 0;
		}

		// Token: 0x06001973 RID: 6515 RVA: 0x000542B2 File Offset: 0x000524B2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetHashCode(this string self, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			return StringComparerEx.FromComparison(comparison).GetHashCode(self);
		}

		// Token: 0x06001974 RID: 6516 RVA: 0x000542C8 File Offset: 0x000524C8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf(this string self, char value, StringComparison comparison)
		{
			ThrowHelper.ThrowIfArgumentNull(self, ExceptionArgument.self);
			return self.IndexOf(new string(value, 1), comparison);
		}
	}
}
