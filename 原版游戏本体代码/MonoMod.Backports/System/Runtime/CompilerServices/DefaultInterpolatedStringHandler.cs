using System;
using System.Buffers;
using System.Globalization;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000041 RID: 65
	[NullableContext(2)]
	[Nullable(0)]
	[InterpolatedStringHandler]
	public ref struct DefaultInterpolatedStringHandler
	{
		// Token: 0x0600027B RID: 635 RVA: 0x0000C1F8 File Offset: 0x0000A3F8
		public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
		{
			this._provider = null;
			this._chars = (this._arrayToReturnToPool = ArrayPool<char>.Shared.Rent(DefaultInterpolatedStringHandler.GetDefaultLength(literalLength, formattedCount)));
			this._pos = 0;
			this._hasCustomFormatter = false;
		}

		// Token: 0x0600027C RID: 636 RVA: 0x0000C240 File Offset: 0x0000A440
		public DefaultInterpolatedStringHandler(int literalLength, int formattedCount, IFormatProvider provider)
		{
			this._provider = provider;
			this._chars = (this._arrayToReturnToPool = ArrayPool<char>.Shared.Rent(DefaultInterpolatedStringHandler.GetDefaultLength(literalLength, formattedCount)));
			this._pos = 0;
			this._hasCustomFormatter = provider != null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
		}

		// Token: 0x0600027D RID: 637 RVA: 0x0000C292 File Offset: 0x0000A492
		[NullableContext(0)]
		public DefaultInterpolatedStringHandler(int literalLength, int formattedCount, [Nullable(2)] IFormatProvider provider, Span<char> initialBuffer)
		{
			this._provider = provider;
			this._chars = initialBuffer;
			this._arrayToReturnToPool = null;
			this._pos = 0;
			this._hasCustomFormatter = provider != null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
		}

		// Token: 0x0600027E RID: 638 RVA: 0x0000C2C3 File Offset: 0x0000A4C3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int GetDefaultLength(int literalLength, int formattedCount)
		{
			return Math.Max(256, literalLength + formattedCount * 11);
		}

		// Token: 0x0600027F RID: 639 RVA: 0x0000C2D8 File Offset: 0x0000A4D8
		[NullableContext(1)]
		public override string ToString()
		{
			return this.Text.ToString();
		}

		// Token: 0x06000280 RID: 640 RVA: 0x0000C2FC File Offset: 0x0000A4FC
		[NullableContext(1)]
		public string ToStringAndClear()
		{
			string result = this.Text.ToString();
			this.Clear();
			return result;
		}

		// Token: 0x06000281 RID: 641 RVA: 0x0000C324 File Offset: 0x0000A524
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Clear()
		{
			char[] arrayToReturnToPool = this._arrayToReturnToPool;
			this = default(DefaultInterpolatedStringHandler);
			if (arrayToReturnToPool != null)
			{
				ArrayPool<char>.Shared.Return(arrayToReturnToPool, false);
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x06000282 RID: 642 RVA: 0x0000C34E File Offset: 0x0000A54E
		[Nullable(0)]
		internal ReadOnlySpan<char> Text
		{
			[NullableContext(0)]
			get
			{
				return this._chars.Slice(0, this._pos);
			}
		}

		// Token: 0x06000283 RID: 643 RVA: 0x0000C368 File Offset: 0x0000A568
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void AppendLiteral(string value)
		{
			if (value.Length == 1)
			{
				Span<char> chars = this._chars;
				int pos = this._pos;
				if (pos < chars.Length)
				{
					*chars[pos] = value[0];
					this._pos = pos + 1;
					return;
				}
				this.GrowThenCopyString(value);
				return;
			}
			else
			{
				if (value.Length != 2)
				{
					this.AppendStringDirect(value);
					return;
				}
				Span<char> chars2 = this._chars;
				int pos2 = this._pos;
				if ((ulong)pos2 < (ulong)((long)(chars2.Length - 1)))
				{
					value.AsSpan().CopyTo(chars2.Slice(pos2));
					this._pos = pos2 + 2;
					return;
				}
				this.GrowThenCopyString(value);
				return;
			}
		}

		// Token: 0x06000284 RID: 644 RVA: 0x0000C410 File Offset: 0x0000A610
		[NullableContext(1)]
		private void AppendStringDirect(string value)
		{
			if (value.AsSpan().TryCopyTo(this._chars.Slice(this._pos)))
			{
				this._pos += value.Length;
				return;
			}
			this.GrowThenCopyString(value);
		}

		// Token: 0x06000285 RID: 645 RVA: 0x0000C45C File Offset: 0x0000A65C
		[NullableContext(1)]
		public unsafe void AppendFormatted<[Nullable(2)] T>(T value)
		{
			if (this._hasCustomFormatter)
			{
				this.AppendCustomFormatter<T>(value, null);
				return;
			}
			if (typeof(T) == typeof(IntPtr))
			{
				this.AppendFormatted(*Unsafe.As<T, IntPtr>(ref value));
				return;
			}
			if (typeof(T) == typeof(UIntPtr))
			{
				this.AppendFormatted(*Unsafe.As<T, UIntPtr>(ref value));
				return;
			}
			string text;
			if (value is IFormattable)
			{
				text = ((IFormattable)((object)value)).ToString(null, this._provider);
			}
			else
			{
				ref T ptr = ref value;
				T t = default(T);
				string text2;
				if (t == null)
				{
					t = value;
					ptr = ref t;
					if (t == null)
					{
						text2 = null;
						goto IL_BD;
					}
				}
				text2 = ptr.ToString();
				IL_BD:
				text = text2;
			}
			if (text != null)
			{
				this.AppendStringDirect(text);
			}
		}

		// Token: 0x06000286 RID: 646 RVA: 0x0000C534 File Offset: 0x0000A734
		public unsafe void AppendFormatted<T>([Nullable(1)] T value, string format)
		{
			if (this._hasCustomFormatter)
			{
				this.AppendCustomFormatter<T>(value, format);
				return;
			}
			if (typeof(T) == typeof(IntPtr))
			{
				this.AppendFormatted(*Unsafe.As<T, IntPtr>(ref value), format);
				return;
			}
			if (typeof(T) == typeof(UIntPtr))
			{
				this.AppendFormatted(*Unsafe.As<T, UIntPtr>(ref value), format);
				return;
			}
			string text;
			if (value is IFormattable)
			{
				text = ((IFormattable)((object)value)).ToString(format, this._provider);
			}
			else
			{
				ref T ptr = ref value;
				T t = default(T);
				string text2;
				if (t == null)
				{
					t = value;
					ptr = ref t;
					if (t == null)
					{
						text2 = null;
						goto IL_BF;
					}
				}
				text2 = ptr.ToString();
				IL_BF:
				text = text2;
			}
			if (text != null)
			{
				this.AppendStringDirect(text);
			}
		}

		// Token: 0x06000287 RID: 647 RVA: 0x0000C60C File Offset: 0x0000A80C
		[NullableContext(1)]
		public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
		{
			int pos = this._pos;
			this.AppendFormatted<T>(value);
			if (alignment != 0)
			{
				this.AppendOrInsertAlignmentIfNeeded(pos, alignment);
			}
		}

		// Token: 0x06000288 RID: 648 RVA: 0x0000C634 File Offset: 0x0000A834
		public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
		{
			int pos = this._pos;
			this.AppendFormatted<T>(value, format);
			if (alignment != 0)
			{
				this.AppendOrInsertAlignmentIfNeeded(pos, alignment);
			}
		}

		// Token: 0x06000289 RID: 649 RVA: 0x0000C65B File Offset: 0x0000A85B
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AppendFormatted(IntPtr value)
		{
			if (IntPtr.Size == 4)
			{
				this.AppendFormatted<int>((int)value);
				return;
			}
			this.AppendFormatted<long>((long)value);
		}

		// Token: 0x0600028A RID: 650 RVA: 0x0000C67E File Offset: 0x0000A87E
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AppendFormatted(IntPtr value, string format)
		{
			if (IntPtr.Size == 4)
			{
				this.AppendFormatted<int>((int)value, format);
				return;
			}
			this.AppendFormatted<long>((long)value, format);
		}

		// Token: 0x0600028B RID: 651 RVA: 0x0000C6A3 File Offset: 0x0000A8A3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AppendFormatted(UIntPtr value)
		{
			if (UIntPtr.Size == 4)
			{
				this.AppendFormatted<uint>((uint)value);
				return;
			}
			this.AppendFormatted<ulong>((ulong)value);
		}

		// Token: 0x0600028C RID: 652 RVA: 0x0000C6C6 File Offset: 0x0000A8C6
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AppendFormatted(UIntPtr value, string format)
		{
			if (UIntPtr.Size == 4)
			{
				this.AppendFormatted<uint>((uint)value, format);
				return;
			}
			this.AppendFormatted<ulong>((ulong)value, format);
		}

		// Token: 0x0600028D RID: 653 RVA: 0x0000C6EB File Offset: 0x0000A8EB
		[NullableContext(0)]
		public void AppendFormatted(ReadOnlySpan<char> value)
		{
			if (value.TryCopyTo(this._chars.Slice(this._pos)))
			{
				this._pos += value.Length;
				return;
			}
			this.GrowThenCopySpan(value);
		}

		// Token: 0x0600028E RID: 654 RVA: 0x0000C724 File Offset: 0x0000A924
		[NullableContext(0)]
		public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, [Nullable(2)] string format = null)
		{
			bool flag = false;
			if (alignment < 0)
			{
				flag = true;
				alignment = -alignment;
			}
			int num = alignment - value.Length;
			if (num <= 0)
			{
				this.AppendFormatted(value);
				return;
			}
			this.EnsureCapacityForAdditionalChars(value.Length + num);
			if (flag)
			{
				value.CopyTo(this._chars.Slice(this._pos));
				this._pos += value.Length;
				this._chars.Slice(this._pos, num).Fill(' ');
				this._pos += num;
				return;
			}
			this._chars.Slice(this._pos, num).Fill(' ');
			this._pos += num;
			value.CopyTo(this._chars.Slice(this._pos));
			this._pos += value.Length;
		}

		// Token: 0x0600028F RID: 655 RVA: 0x0000C814 File Offset: 0x0000AA14
		public void AppendFormatted(string value)
		{
			if (!this._hasCustomFormatter && value != null && value.AsSpan().TryCopyTo(this._chars.Slice(this._pos)))
			{
				this._pos += value.Length;
				return;
			}
			this.AppendFormattedSlow(value);
		}

		// Token: 0x06000290 RID: 656 RVA: 0x0000C868 File Offset: 0x0000AA68
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void AppendFormattedSlow(string value)
		{
			if (this._hasCustomFormatter)
			{
				this.AppendCustomFormatter<string>(value, null);
				return;
			}
			if (value != null)
			{
				this.EnsureCapacityForAdditionalChars(value.Length);
				value.AsSpan().CopyTo(this._chars.Slice(this._pos));
				this._pos += value.Length;
			}
		}

		// Token: 0x06000291 RID: 657 RVA: 0x0000C8C7 File Offset: 0x0000AAC7
		public void AppendFormatted(string value, int alignment = 0, string format = null)
		{
			this.AppendFormatted<string>(value, alignment, format);
		}

		// Token: 0x06000292 RID: 658 RVA: 0x0000C8D2 File Offset: 0x0000AAD2
		public void AppendFormatted(object value, int alignment = 0, string format = null)
		{
			this.AppendFormatted<object>(value, alignment, format);
		}

		// Token: 0x06000293 RID: 659 RVA: 0x0000C8DD File Offset: 0x0000AADD
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool HasCustomFormatter(IFormatProvider provider)
		{
			return provider.GetType() != typeof(CultureInfo) && provider.GetFormat(typeof(ICustomFormatter)) != null;
		}

		// Token: 0x06000294 RID: 660 RVA: 0x0000C90C File Offset: 0x0000AB0C
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void AppendCustomFormatter<T>([Nullable(1)] T value, string format)
		{
			ICustomFormatter customFormatter = (ICustomFormatter)this._provider.GetFormat(typeof(ICustomFormatter));
			if (customFormatter != null)
			{
				string text = customFormatter.Format(format, value, this._provider);
				if (text != null)
				{
					this.AppendStringDirect(text);
				}
			}
		}

		// Token: 0x06000295 RID: 661 RVA: 0x0000C958 File Offset: 0x0000AB58
		private void AppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
		{
			int num = this._pos - startingPos;
			bool flag = false;
			if (alignment < 0)
			{
				flag = true;
				alignment = -alignment;
			}
			int num2 = alignment - num;
			if (num2 > 0)
			{
				this.EnsureCapacityForAdditionalChars(num2);
				if (flag)
				{
					this._chars.Slice(this._pos, num2).Fill(' ');
				}
				else
				{
					this._chars.Slice(startingPos, num).CopyTo(this._chars.Slice(startingPos + num2));
					this._chars.Slice(startingPos, num2).Fill(' ');
				}
				this._pos += num2;
			}
		}

		// Token: 0x06000296 RID: 662 RVA: 0x0000C9F2 File Offset: 0x0000ABF2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EnsureCapacityForAdditionalChars(int additionalChars)
		{
			if (this._chars.Length - this._pos < additionalChars)
			{
				this.Grow(additionalChars);
			}
		}

		// Token: 0x06000297 RID: 663 RVA: 0x0000CA10 File Offset: 0x0000AC10
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void GrowThenCopyString(string value)
		{
			this.Grow(value.Length);
			value.AsSpan().CopyTo(this._chars.Slice(this._pos));
			this._pos += value.Length;
		}

		// Token: 0x06000298 RID: 664 RVA: 0x0000CA5B File Offset: 0x0000AC5B
		[NullableContext(0)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void GrowThenCopySpan(ReadOnlySpan<char> value)
		{
			this.Grow(value.Length);
			value.CopyTo(this._chars.Slice(this._pos));
			this._pos += value.Length;
		}

		// Token: 0x06000299 RID: 665 RVA: 0x0000CA96 File Offset: 0x0000AC96
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Grow(int additionalChars)
		{
			this.GrowCore((uint)(this._pos + additionalChars));
		}

		// Token: 0x0600029A RID: 666 RVA: 0x0000CAA6 File Offset: 0x0000ACA6
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Grow()
		{
			this.GrowCore((uint)(this._chars.Length + 1));
		}

		// Token: 0x0600029B RID: 667 RVA: 0x0000CABC File Offset: 0x0000ACBC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void GrowCore(uint requiredMinCapacity)
		{
			int minimumLength = (int)MathEx.Clamp(Math.Max(requiredMinCapacity, Math.Min((uint)(this._chars.Length * 2), uint.MaxValue)), 256U, 2147483647U);
			char[] array = ArrayPool<char>.Shared.Rent(minimumLength);
			this._chars.Slice(0, this._pos).CopyTo(array);
			char[] arrayToReturnToPool = this._arrayToReturnToPool;
			this._chars = (this._arrayToReturnToPool = array);
			if (arrayToReturnToPool != null)
			{
				ArrayPool<char>.Shared.Return(arrayToReturnToPool, false);
			}
		}

		// Token: 0x0400007C RID: 124
		private const int GuessedLengthPerHole = 11;

		// Token: 0x0400007D RID: 125
		private const int MinimumArrayPoolLength = 256;

		// Token: 0x0400007E RID: 126
		private readonly IFormatProvider _provider;

		// Token: 0x0400007F RID: 127
		private char[] _arrayToReturnToPool;

		// Token: 0x04000080 RID: 128
		[Nullable(0)]
		private Span<char> _chars;

		// Token: 0x04000081 RID: 129
		private int _pos;

		// Token: 0x04000082 RID: 130
		private readonly bool _hasCustomFormatter;
	}
}
