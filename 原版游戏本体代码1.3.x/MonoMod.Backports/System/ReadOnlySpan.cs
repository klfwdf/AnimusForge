using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MonoMod;

namespace System
{
	// Token: 0x02000017 RID: 23
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(SpanDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public readonly ref struct ReadOnlySpan<[Nullable(2)] T>
	{
		// Token: 0x1700000C RID: 12
		// (get) Token: 0x060000BE RID: 190 RVA: 0x00005666 File Offset: 0x00003866
		public int Length
		{
			get
			{
				return this._length;
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x060000BF RID: 191 RVA: 0x0000566E File Offset: 0x0000386E
		public bool IsEmpty
		{
			get
			{
				return this._length == 0;
			}
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00005679 File Offset: 0x00003879
		public static bool operator !=([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> left, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> right)
		{
			return !(left == right);
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x00005685 File Offset: 0x00003885
		[NullableContext(2)]
		[Obsolete("Equals() on ReadOnlySpan will always throw an exception. Use == instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			throw new NotSupportedException("Cannot call Equals on Span");
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00005691 File Offset: 0x00003891
		[Obsolete("GetHashCode() on ReadOnlySpan will always throw an exception.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			throw new NotSupportedException("Cannot call GetHashCodee on Span");
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x0000569D File Offset: 0x0000389D
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator ReadOnlySpan<T>([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			return new ReadOnlySpan<T>(array);
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x000056A5 File Offset: 0x000038A5
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator ReadOnlySpan<T>([Nullable(new byte[] { 0, 1 })] ArraySegment<T> segment)
		{
			T[] array = segment.Array;
			if (array == null)
			{
				throw new ArgumentNullException("segment");
			}
			return new ReadOnlySpan<T>(array, segment.Offset, segment.Count);
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x060000C5 RID: 197 RVA: 0x000056D0 File Offset: 0x000038D0
		[Nullable(new byte[] { 0, 1 })]
		public static ReadOnlySpan<T> Empty
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return default(ReadOnlySpan<T>);
			}
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x000056E6 File Offset: 0x000038E6
		[NullableContext(0)]
		public ReadOnlySpan<T>.Enumerator GetEnumerator()
		{
			return new ReadOnlySpan<T>.Enumerator(this);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x000056F3 File Offset: 0x000038F3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlySpan([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			if (array == null)
			{
				this = default(ReadOnlySpan<T>);
				return;
			}
			this._length = array.Length;
			this._pinnable = array;
			this._byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment;
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x0000571C File Offset: 0x0000391C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlySpan([Nullable(new byte[] { 2, 1 })] T[] array, int start, int length)
		{
			if (array == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				this = default(ReadOnlySpan<T>);
				return;
			}
			if (start > array.Length || length > array.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			this._length = length;
			this._pinnable = array;
			this._byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment.Add(start);
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00005773 File Offset: 0x00003973
		[NullableContext(0)]
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe ReadOnlySpan(void* pointer, int length)
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			if (length < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			this._length = length;
			this._pinnable = null;
			this._byteOffset = new IntPtr(pointer);
		}

		// Token: 0x060000CA RID: 202 RVA: 0x000057AF File Offset: 0x000039AF
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ReadOnlySpan(object pinnable, IntPtr byteOffset, int length)
		{
			this._length = length;
			this._pinnable = pinnable;
			this._byteOffset = byteOffset;
		}

		// Token: 0x1700000F RID: 15
		public T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (index >= this._length)
				{
					ThrowHelper.ThrowIndexOutOfRangeException();
				}
				return Unsafe.Add<T>(this.DangerousGetPinnableReference(), index);
			}
		}

		// Token: 0x060000CC RID: 204 RVA: 0x000057E2 File Offset: 0x000039E2
		[EditorBrowsable(EditorBrowsableState.Never)]
		public ref readonly T GetPinnableReference()
		{
			if (this._length != 0)
			{
				return this.DangerousGetPinnableReference();
			}
			return Unsafe.AsRef<T>(null);
		}

		// Token: 0x060000CD RID: 205 RVA: 0x000057FA File Offset: 0x000039FA
		public void CopyTo([Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			if (!this.TryCopyTo(destination))
			{
				ThrowHelper.ThrowArgumentException_DestinationTooShort();
			}
		}

		// Token: 0x060000CE RID: 206 RVA: 0x0000580C File Offset: 0x00003A0C
		public bool TryCopyTo([Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			int length = this._length;
			int length2 = destination.Length;
			if (length == 0)
			{
				return true;
			}
			if (length > length2)
			{
				return false;
			}
			ref T src = ref this.DangerousGetPinnableReference();
			SpanHelpers.CopyTo<T>(destination.DangerousGetPinnableReference(), length2, ref src, length);
			return true;
		}

		// Token: 0x060000CF RID: 207 RVA: 0x0000584A File Offset: 0x00003A4A
		public static bool operator ==([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> left, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> right)
		{
			return left._length == right._length && Unsafe.AreSame<T>(left.DangerousGetPinnableReference(), right.DangerousGetPinnableReference());
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x00005870 File Offset: 0x00003A70
		public unsafe override string ToString()
		{
			if (typeof(T) == typeof(char))
			{
				if (this._byteOffset == (IntPtr)RuntimeHelpers.OffsetToStringData)
				{
					string text = Unsafe.As<object>(this._pinnable) as string;
					if (text != null && this._length == text.Length)
					{
						return text;
					}
				}
				fixed (char* ptr = Unsafe.As<T, char>(this.DangerousGetPinnableReference()))
				{
					return new string(ptr, 0, this._length);
				}
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 2);
			defaultInterpolatedStringHandler.AppendLiteral("System.ReadOnlySpan<");
			defaultInterpolatedStringHandler.AppendFormatted(typeof(T).Name);
			defaultInterpolatedStringHandler.AppendLiteral(">[");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this._length);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x00005944 File Offset: 0x00003B44
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySpan<T> Slice(int start)
		{
			if (start > this._length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr byteOffset = this._byteOffset.Add(start);
			int length = this._length - start;
			return new ReadOnlySpan<T>(this._pinnable, byteOffset, length);
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00005984 File Offset: 0x00003B84
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySpan<T> Slice(int start, int length)
		{
			if (start > this._length || length > this._length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr byteOffset = this._byteOffset.Add(start);
			return new ReadOnlySpan<T>(this._pinnable, byteOffset, length);
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x000059C8 File Offset: 0x00003BC8
		public T[] ToArray()
		{
			if (this._length == 0)
			{
				return SpanHelpers.PerTypeValues<T>.EmptyArray;
			}
			T[] array = new T[this._length];
			this.CopyTo(array);
			return array;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x000059FC File Offset: 0x00003BFC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ref T DangerousGetPinnableReference()
		{
			return Unsafe.AddByteOffset<T>(ILHelpers.ObjectAsRef<T>(this._pinnable), this._byteOffset);
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x060000D5 RID: 213 RVA: 0x00005A14 File Offset: 0x00003C14
		[Nullable(2)]
		internal object Pinnable
		{
			[NullableContext(2)]
			get
			{
				return this._pinnable;
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x060000D6 RID: 214 RVA: 0x00005A1C File Offset: 0x00003C1C
		internal IntPtr ByteOffset
		{
			get
			{
				return this._byteOffset;
			}
		}

		// Token: 0x0400002E RID: 46
		[Nullable(2)]
		private readonly object _pinnable;

		// Token: 0x0400002F RID: 47
		private readonly IntPtr _byteOffset;

		// Token: 0x04000030 RID: 48
		private readonly int _length;

		// Token: 0x02000058 RID: 88
		[Nullable(0)]
		public ref struct Enumerator
		{
			// Token: 0x060002BC RID: 700 RVA: 0x0000CCB6 File Offset: 0x0000AEB6
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal Enumerator([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> span)
			{
				this._span = span;
				this._index = -1;
			}

			// Token: 0x060002BD RID: 701 RVA: 0x0000CCC8 File Offset: 0x0000AEC8
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				int num = this._index + 1;
				if (num < this._span.Length)
				{
					this._index = num;
					return true;
				}
				return false;
			}

			// Token: 0x17000042 RID: 66
			// (get) Token: 0x060002BE RID: 702 RVA: 0x0000CCF6 File Offset: 0x0000AEF6
			public ref readonly T Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return this._span[this._index];
				}
			}

			// Token: 0x040000A1 RID: 161
			[Nullable(new byte[] { 0, 1 })]
			private readonly ReadOnlySpan<T> _span;

			// Token: 0x040000A2 RID: 162
			private int _index;
		}
	}
}
