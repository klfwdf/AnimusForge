using System;
using System.Buffers;
using System.Globalization;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020004B6 RID: 1206
	[NullableContext(2)]
	[Nullable(0)]
	[InterpolatedStringHandler]
	internal ref struct DefaultInterpolatedStringHandler
	{
		// Token: 0x06001B06 RID: 6918 RVA: 0x00057ABC File Offset: 0x00055CBC
		public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
		{
			this._provider = null;
			this._chars = (this._arrayToReturnToPool = ArrayPool<char>.Shared.Rent(DefaultInterpolatedStringHandler.GetDefaultLength(literalLength, formattedCount)));
			this._pos = 0;
			this._hasCustomFormatter = false;
		}

		// Token: 0x06001B07 RID: 6919 RVA: 0x00057B04 File Offset: 0x00055D04
		public DefaultInterpolatedStringHandler(int literalLength, int formattedCount, IFormatProvider provider)
		{
			this._provider = provider;
			this._chars = (this._arrayToReturnToPool = ArrayPool<char>.Shared.Rent(DefaultInterpolatedStringHandler.GetDefaultLength(literalLength, formattedCount)));
			this._pos = 0;
			this._hasCustomFormatter = provider != null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
		}

		// Token: 0x06001B08 RID: 6920 RVA: 0x00057B56 File Offset: 0x00055D56
		[NullableContext(0)]
		public DefaultInterpolatedStringHandler(int literalLength, int formattedCount, [Nullable(2)] IFormatProvider provider, Span<char> initialBuffer)
		{
			this._provider = provider;
			this._chars = initialBuffer;
			this._arrayToReturnToPool = null;
			this._pos = 0;
			this._hasCustomFormatter = provider != null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
		}

		// Token: 0x06001B09 RID: 6921 RVA: 0x00057B87 File Offset: 0x00055D87
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int GetDefaultLength(int literalLength, int formattedCount)
		{
			return Math.Max(256, literalLength + formattedCount * 11);
		}

		// Token: 0x06001B0A RID: 6922 RVA: 0x00057B9C File Offset: 0x00055D9C
		[NullableContext(1)]
		public override string ToString()
		{
			return this.Text.ToString();
		}

		// Token: 0x06001B0B RID: 6923 RVA: 0x00057BC0 File Offset: 0x00055DC0
		[NullableContext(1)]
		public string ToStringAndClear()
		{
			string result = this.Text.ToString();
			this.Clear();
			return result;
		}

		// Token: 0x06001B0C RID: 6924 RVA: 0x00057BE8 File Offset: 0x00055DE8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Clear()
		{
			char[] toReturn = this._arrayToReturnToPool;
			this = default(DefaultInterpolatedStringHandler);
			if (toReturn != null)
			{
				ArrayPool<char>.Shared.Return(toReturn, false);
			}
		}

		// Token: 0x170005E1 RID: 1505
		// (get) Token: 0x06001B0D RID: 6925 RVA: 0x00057C12 File Offset: 0x00055E12
		[Nullable(0)]
		internal ReadOnlySpan<char> Text
		{
			[NullableContext(0)]
			get
			{
				return this._chars.Slice(0, this._pos);
			}
		}

		// Token: 0x06001B0E RID: 6926 RVA: 0x00057C2C File Offset: 0x00055E2C
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

		// Token: 0x06001B0F RID: 6927 RVA: 0x00057CD4 File Offset: 0x00055ED4
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

		// Token: 0x06001B10 RID: 6928 RVA: 0x00057D20 File Offset: 0x00055F20
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
			string s;
			if (value is IFormattable)
			{
				s = ((IFormattable)((object)value)).ToString(null, this._provider);
			}
			else
			{
				ref T ptr = ref value;
				T t = default(T);
				string text;
				if (t == null)
				{
					t = value;
					ptr = ref t;
					if (t == null)
					{
						text = null;
						goto IL_BD;
					}
				}
				text = ptr.ToString();
				IL_BD:
				s = text;
			}
			if (s != null)
			{
				this.AppendStringDirect(s);
			}
		}

		// Token: 0x06001B11 RID: 6929 RVA: 0x00057DF8 File Offset: 0x00055FF8
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
			string s;
			if (value is IFormattable)
			{
				s = ((IFormattable)((object)value)).ToString(format, this._provider);
			}
			else
			{
				ref T ptr = ref value;
				T t = default(T);
				string text;
				if (t == null)
				{
					t = value;
					ptr = ref t;
					if (t == null)
					{
						text = null;
						goto IL_BF;
					}
				}
				text = ptr.ToString();
				IL_BF:
				s = text;
			}
			if (s != null)
			{
				this.AppendStringDirect(s);
			}
		}

		// Token: 0x06001B12 RID: 6930 RVA: 0x00057ED0 File Offset: 0x000560D0
		[NullableContext(1)]
		public void AppendFormatted<[Nullable(2)] T>(T value, int alignment)
		{
			int startingPos = this._pos;
			this.AppendFormatted<T>(value);
			if (alignment != 0)
			{
				this.AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
			}
		}

		// Token: 0x06001B13 RID: 6931 RVA: 0x00057EF8 File Offset: 0x000560F8
		public void AppendFormatted<T>([Nullable(1)] T value, int alignment, string format)
		{
			int startingPos = this._pos;
			this.AppendFormatted<T>(value, format);
			if (alignment != 0)
			{
				this.AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
			}
		}

		// Token: 0x06001B14 RID: 6932 RVA: 0x00057F1F File Offset: 0x0005611F
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

		// Token: 0x06001B15 RID: 6933 RVA: 0x00057F42 File Offset: 0x00056142
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

		// Token: 0x06001B16 RID: 6934 RVA: 0x00057F67 File Offset: 0x00056167
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

		// Token: 0x06001B17 RID: 6935 RVA: 0x00057F8A File Offset: 0x0005618A
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

		// Token: 0x06001B18 RID: 6936 RVA: 0x00057FAF File Offset: 0x000561AF
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

		// Token: 0x06001B19 RID: 6937 RVA: 0x00057FE8 File Offset: 0x000561E8
		[NullableContext(0)]
		public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, [Nullable(2)] string format = null)
		{
			bool leftAlign = false;
			if (alignment < 0)
			{
				leftAlign = true;
				alignment = -alignment;
			}
			int paddingRequired = alignment - value.Length;
			if (paddingRequired <= 0)
			{
				this.AppendFormatted(value);
				return;
			}
			this.EnsureCapacityForAdditionalChars(value.Length + paddingRequired);
			if (leftAlign)
			{
				value.CopyTo(this._chars.Slice(this._pos));
				this._pos += value.Length;
				this._chars.Slice(this._pos, paddingRequired).Fill(' ');
				this._pos += paddingRequired;
				return;
			}
			this._chars.Slice(this._pos, paddingRequired).Fill(' ');
			this._pos += paddingRequired;
			value.CopyTo(this._chars.Slice(this._pos));
			this._pos += value.Length;
		}

		// Token: 0x06001B1A RID: 6938 RVA: 0x000580D8 File Offset: 0x000562D8
		public void AppendFormatted(string value)
		{
			if (!this._hasCustomFormatter && value != null && value.AsSpan().TryCopyTo(this._chars.Slice(this._pos)))
			{
				this._pos += value.Length;
				return;
			}
			this.AppendFormattedSlow(value);
		}

		// Token: 0x06001B1B RID: 6939 RVA: 0x0005812C File Offset: 0x0005632C
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

		// Token: 0x06001B1C RID: 6940 RVA: 0x0005818B File Offset: 0x0005638B
		public void AppendFormatted(string value, int alignment = 0, string format = null)
		{
			this.AppendFormatted<string>(value, alignment, format);
		}

		// Token: 0x06001B1D RID: 6941 RVA: 0x00058196 File Offset: 0x00056396
		public void AppendFormatted(object value, int alignment = 0, string format = null)
		{
			this.AppendFormatted<object>(value, alignment, format);
		}

		// Token: 0x06001B1E RID: 6942 RVA: 0x000581A1 File Offset: 0x000563A1
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool HasCustomFormatter(IFormatProvider provider)
		{
			return provider.GetType() != typeof(CultureInfo) && provider.GetFormat(typeof(ICustomFormatter)) != null;
		}

		// Token: 0x06001B1F RID: 6943 RVA: 0x000581D0 File Offset: 0x000563D0
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void AppendCustomFormatter<T>([Nullable(1)] T value, string format)
		{
			ICustomFormatter formatter = (ICustomFormatter)this._provider.GetFormat(typeof(ICustomFormatter));
			if (formatter != null)
			{
				string customFormatted = formatter.Format(format, value, this._provider);
				if (customFormatted != null)
				{
					this.AppendStringDirect(customFormatted);
				}
			}
		}

		// Token: 0x06001B20 RID: 6944 RVA: 0x0005821C File Offset: 0x0005641C
		private void AppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
		{
			int charsWritten = this._pos - startingPos;
			bool leftAlign = false;
			if (alignment < 0)
			{
				leftAlign = true;
				alignment = -alignment;
			}
			int paddingNeeded = alignment - charsWritten;
			if (paddingNeeded > 0)
			{
				this.EnsureCapacityForAdditionalChars(paddingNeeded);
				if (leftAlign)
				{
					this._chars.Slice(this._pos, paddingNeeded).Fill(' ');
				}
				else
				{
					this._chars.Slice(startingPos, charsWritten).CopyTo(this._chars.Slice(startingPos + paddingNeeded));
					this._chars.Slice(startingPos, paddingNeeded).Fill(' ');
				}
				this._pos += paddingNeeded;
			}
		}

		// Token: 0x06001B21 RID: 6945 RVA: 0x000582B6 File Offset: 0x000564B6
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EnsureCapacityForAdditionalChars(int additionalChars)
		{
			if (this._chars.Length - this._pos < additionalChars)
			{
				this.Grow(additionalChars);
			}
		}

		// Token: 0x06001B22 RID: 6946 RVA: 0x000582D4 File Offset: 0x000564D4
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void GrowThenCopyString(string value)
		{
			this.Grow(value.Length);
			value.AsSpan().CopyTo(this._chars.Slice(this._pos));
			this._pos += value.Length;
		}

		// Token: 0x06001B23 RID: 6947 RVA: 0x0005831F File Offset: 0x0005651F
		[NullableContext(0)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void GrowThenCopySpan(ReadOnlySpan<char> value)
		{
			this.Grow(value.Length);
			value.CopyTo(this._chars.Slice(this._pos));
			this._pos += value.Length;
		}

		// Token: 0x06001B24 RID: 6948 RVA: 0x0005835A File Offset: 0x0005655A
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Grow(int additionalChars)
		{
			this.GrowCore((uint)(this._pos + additionalChars));
		}

		// Token: 0x06001B25 RID: 6949 RVA: 0x0005836A File Offset: 0x0005656A
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Grow()
		{
			this.GrowCore((uint)(this._chars.Length + 1));
		}

		// Token: 0x06001B26 RID: 6950 RVA: 0x00058380 File Offset: 0x00056580
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void GrowCore(uint requiredMinCapacity)
		{
			int arraySize = (int)MathEx.Clamp(Math.Max(requiredMinCapacity, Math.Min((uint)(this._chars.Length * 2), uint.MaxValue)), 256U, 2147483647U);
			char[] newArray = ArrayPool<char>.Shared.Rent(arraySize);
			this._chars.Slice(0, this._pos).CopyTo(newArray);
			char[] toReturn = this._arrayToReturnToPool;
			this._chars = (this._arrayToReturnToPool = newArray);
			if (toReturn != null)
			{
				ArrayPool<char>.Shared.Return(toReturn, false);
			}
		}

		// Token: 0x04001129 RID: 4393
		private const int GuessedLengthPerHole = 11;

		// Token: 0x0400112A RID: 4394
		private const int MinimumArrayPoolLength = 256;

		// Token: 0x0400112B RID: 4395
		private readonly IFormatProvider _provider;

		// Token: 0x0400112C RID: 4396
		private char[] _arrayToReturnToPool;

		// Token: 0x0400112D RID: 4397
		[Nullable(0)]
		private Span<char> _chars;

		// Token: 0x0400112E RID: 4398
		private int _pos;

		// Token: 0x0400112F RID: 4399
		private readonly bool _hasCustomFormatter;
	}
}
