using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MonoMod;

namespace System
{
	// Token: 0x02000474 RID: 1140
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(SpanDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	internal readonly ref struct Span<[Nullable(2)] T>
	{
		// Token: 0x170005B4 RID: 1460
		// (get) Token: 0x06001921 RID: 6433 RVA: 0x00050A09 File Offset: 0x0004EC09
		public int Length
		{
			get
			{
				return this._length;
			}
		}

		// Token: 0x170005B5 RID: 1461
		// (get) Token: 0x06001922 RID: 6434 RVA: 0x00050A11 File Offset: 0x0004EC11
		public bool IsEmpty
		{
			get
			{
				return this._length == 0;
			}
		}

		// Token: 0x06001923 RID: 6435 RVA: 0x00050A1C File Offset: 0x0004EC1C
		public static bool operator !=([Nullable(new byte[] { 0, 1 })] Span<T> left, [Nullable(new byte[] { 0, 1 })] Span<T> right)
		{
			return !(left == right);
		}

		// Token: 0x06001924 RID: 6436 RVA: 0x00050619 File Offset: 0x0004E819
		[NullableContext(2)]
		[Obsolete("Equals() on Span will always throw an exception. Use == instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			throw new NotSupportedException("Cannot call Equals on Span");
		}

		// Token: 0x06001925 RID: 6437 RVA: 0x00050A28 File Offset: 0x0004EC28
		[Obsolete("GetHashCode() on Span will always throw an exception.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			throw new NotSupportedException("Cannot call GetHashCode on Span");
		}

		// Token: 0x06001926 RID: 6438 RVA: 0x00050A34 File Offset: 0x0004EC34
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator Span<T>([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			return new Span<T>(array);
		}

		// Token: 0x06001927 RID: 6439 RVA: 0x00050A3C File Offset: 0x0004EC3C
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator Span<T>([Nullable(new byte[] { 0, 1 })] ArraySegment<T> segment)
		{
			return new Span<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x170005B6 RID: 1462
		// (get) Token: 0x06001928 RID: 6440 RVA: 0x00050A58 File Offset: 0x0004EC58
		[Nullable(new byte[] { 0, 1 })]
		public static Span<T> Empty
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return default(Span<T>);
			}
		}

		// Token: 0x06001929 RID: 6441 RVA: 0x00050A6E File Offset: 0x0004EC6E
		[NullableContext(0)]
		public Span<T>.Enumerator GetEnumerator()
		{
			return new Span<T>.Enumerator(this);
		}

		// Token: 0x0600192A RID: 6442 RVA: 0x00050A7C File Offset: 0x0004EC7C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			if (array == null)
			{
				this = default(Span<T>);
				return;
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			this._length = array.Length;
			this._pinnable = array;
			this._byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment;
		}

		// Token: 0x0600192B RID: 6443 RVA: 0x00050ADC File Offset: 0x0004ECDC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		internal static Span<T> Create([Nullable(new byte[] { 2, 1 })] T[] array, int start)
		{
			if (array == null)
			{
				if (start != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(Span<T>);
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			if (start > array.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment.Add(start);
			int length = array.Length - start;
			return new Span<T>(array, byteOffset, length);
		}

		// Token: 0x0600192C RID: 6444 RVA: 0x00050B54 File Offset: 0x0004ED54
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span([Nullable(new byte[] { 2, 1 })] T[] array, int start, int length)
		{
			if (array == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				this = default(Span<T>);
				return;
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			if (start > array.Length || length > array.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			this._length = length;
			this._pinnable = array;
			this._byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment.Add(start);
		}

		// Token: 0x0600192D RID: 6445 RVA: 0x00050BD7 File Offset: 0x0004EDD7
		[NullableContext(0)]
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Span(void* pointer, int length)
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

		// Token: 0x0600192E RID: 6446 RVA: 0x00050C13 File Offset: 0x0004EE13
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Span(object pinnable, IntPtr byteOffset, int length)
		{
			this._length = length;
			this._pinnable = pinnable;
			this._byteOffset = byteOffset;
		}

		// Token: 0x170005B7 RID: 1463
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

		// Token: 0x06001930 RID: 6448 RVA: 0x00050C46 File Offset: 0x0004EE46
		[EditorBrowsable(EditorBrowsableState.Never)]
		public ref T GetPinnableReference()
		{
			if (this._length != 0)
			{
				return this.DangerousGetPinnableReference();
			}
			return Unsafe.NullRef<T>();
		}

		// Token: 0x06001931 RID: 6449 RVA: 0x00050C5C File Offset: 0x0004EE5C
		public unsafe void Clear()
		{
			int length = this._length;
			if (length == 0)
			{
				return;
			}
			UIntPtr byteLength = (UIntPtr)((ulong)length * (ulong)((long)Unsafe.SizeOf<T>()));
			if ((Unsafe.SizeOf<T>() & (sizeof(IntPtr) - 1)) != 0)
			{
				if (this._pinnable == null)
				{
					byte* ptr = (byte*)this._byteOffset.ToPointer();
					SpanHelpers.ClearLessThanPointerSized(ptr, byteLength);
					return;
				}
				SpanHelpers.ClearLessThanPointerSized(Unsafe.As<T, byte>(this.DangerousGetPinnableReference()), byteLength);
				return;
			}
			else
			{
				if (SpanHelpers.IsReferenceOrContainsReferences<T>())
				{
					UIntPtr pointerSizedLength = (UIntPtr)((ulong)((long)(length * Unsafe.SizeOf<T>() / sizeof(IntPtr))));
					SpanHelpers.ClearPointerSizedWithReferences(Unsafe.As<T, IntPtr>(this.DangerousGetPinnableReference()), pointerSizedLength);
					return;
				}
				SpanHelpers.ClearPointerSizedWithoutReferences(Unsafe.As<T, byte>(this.DangerousGetPinnableReference()), byteLength);
				return;
			}
		}

		// Token: 0x06001932 RID: 6450 RVA: 0x00050D08 File Offset: 0x0004EF08
		public unsafe void Fill(T value)
		{
			int length = this._length;
			if (length == 0)
			{
				return;
			}
			if (Unsafe.SizeOf<T>() == 1)
			{
				byte fill = *Unsafe.As<T, byte>(ref value);
				Unsafe.InitBlockUnaligned(Unsafe.As<T, byte>(this.DangerousGetPinnableReference()), fill, (uint)length);
				return;
			}
			ref T r = ref this.DangerousGetPinnableReference();
			int i;
			for (i = 0; i < (length & -8); i += 8)
			{
				*Unsafe.Add<T>(ref r, i) = value;
				*Unsafe.Add<T>(ref r, i + 1) = value;
				*Unsafe.Add<T>(ref r, i + 2) = value;
				*Unsafe.Add<T>(ref r, i + 3) = value;
				*Unsafe.Add<T>(ref r, i + 4) = value;
				*Unsafe.Add<T>(ref r, i + 5) = value;
				*Unsafe.Add<T>(ref r, i + 6) = value;
				*Unsafe.Add<T>(ref r, i + 7) = value;
			}
			if (i < (length & -4))
			{
				*Unsafe.Add<T>(ref r, i) = value;
				*Unsafe.Add<T>(ref r, i + 1) = value;
				*Unsafe.Add<T>(ref r, i + 2) = value;
				*Unsafe.Add<T>(ref r, i + 3) = value;
				i += 4;
			}
			while (i < length)
			{
				*Unsafe.Add<T>(ref r, i) = value;
				i++;
			}
		}

		// Token: 0x06001933 RID: 6451 RVA: 0x00050E2F File Offset: 0x0004F02F
		public void CopyTo([Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			if (!this.TryCopyTo(destination))
			{
				ThrowHelper.ThrowArgumentException_DestinationTooShort();
			}
		}

		// Token: 0x06001934 RID: 6452 RVA: 0x00050E40 File Offset: 0x0004F040
		public bool TryCopyTo([Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			int length = this._length;
			int destLength = destination._length;
			if (length == 0)
			{
				return true;
			}
			if (length > destLength)
			{
				return false;
			}
			ref T src = ref this.DangerousGetPinnableReference();
			SpanHelpers.CopyTo<T>(destination.DangerousGetPinnableReference(), destLength, ref src, length);
			return true;
		}

		// Token: 0x06001935 RID: 6453 RVA: 0x00050E7D File Offset: 0x0004F07D
		public static bool operator ==([Nullable(new byte[] { 0, 1 })] Span<T> left, [Nullable(new byte[] { 0, 1 })] Span<T> right)
		{
			return left._length == right._length && Unsafe.AreSame<T>(left.DangerousGetPinnableReference(), right.DangerousGetPinnableReference());
		}

		// Token: 0x06001936 RID: 6454 RVA: 0x00050EA2 File Offset: 0x0004F0A2
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator ReadOnlySpan<T>([Nullable(new byte[] { 0, 1 })] Span<T> span)
		{
			return new ReadOnlySpan<T>(span._pinnable, span._byteOffset, span._length);
		}

		// Token: 0x06001937 RID: 6455 RVA: 0x00050EBC File Offset: 0x0004F0BC
		public unsafe override string ToString()
		{
			if (typeof(T) == typeof(char))
			{
				fixed (char* ptr = Unsafe.As<T, char>(this.DangerousGetPinnableReference()))
				{
					return new string(ptr, 0, this._length);
				}
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 2);
			defaultInterpolatedStringHandler.AppendLiteral("System.Span<");
			defaultInterpolatedStringHandler.AppendFormatted(typeof(T).Name);
			defaultInterpolatedStringHandler.AppendLiteral(">[");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this._length);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06001938 RID: 6456 RVA: 0x00050F58 File Offset: 0x0004F158
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public Span<T> Slice(int start)
		{
			if (start > this._length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr newOffset = this._byteOffset.Add(start);
			int length = this._length - start;
			return new Span<T>(this._pinnable, newOffset, length);
		}

		// Token: 0x06001939 RID: 6457 RVA: 0x00050F98 File Offset: 0x0004F198
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public Span<T> Slice(int start, int length)
		{
			if (start > this._length || length > this._length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr newOffset = this._byteOffset.Add(start);
			return new Span<T>(this._pinnable, newOffset, length);
		}

		// Token: 0x0600193A RID: 6458 RVA: 0x00050FDC File Offset: 0x0004F1DC
		public T[] ToArray()
		{
			if (this._length == 0)
			{
				return SpanHelpers.PerTypeValues<T>.EmptyArray;
			}
			T[] result = new T[this._length];
			this.CopyTo(result);
			return result;
		}

		// Token: 0x0600193B RID: 6459 RVA: 0x00051010 File Offset: 0x0004F210
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ref T DangerousGetPinnableReference()
		{
			return Unsafe.AddByteOffset<T>(ILHelpers.ObjectAsRef<T>(this._pinnable), this._byteOffset);
		}

		// Token: 0x170005B8 RID: 1464
		// (get) Token: 0x0600193C RID: 6460 RVA: 0x00051028 File Offset: 0x0004F228
		[Nullable(2)]
		internal object Pinnable
		{
			[NullableContext(2)]
			get
			{
				return this._pinnable;
			}
		}

		// Token: 0x170005B9 RID: 1465
		// (get) Token: 0x0600193D RID: 6461 RVA: 0x00051030 File Offset: 0x0004F230
		internal IntPtr ByteOffset
		{
			get
			{
				return this._byteOffset;
			}
		}

		// Token: 0x040010A3 RID: 4259
		[Nullable(2)]
		private readonly object _pinnable;

		// Token: 0x040010A4 RID: 4260
		private readonly IntPtr _byteOffset;

		// Token: 0x040010A5 RID: 4261
		private readonly int _length;

		// Token: 0x02000475 RID: 1141
		[Nullable(0)]
		public ref struct Enumerator
		{
			// Token: 0x0600193E RID: 6462 RVA: 0x00051038 File Offset: 0x0004F238
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal Enumerator([Nullable(new byte[] { 0, 1 })] Span<T> span)
			{
				this._span = span;
				this._index = -1;
			}

			// Token: 0x0600193F RID: 6463 RVA: 0x00051048 File Offset: 0x0004F248
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				int index = this._index + 1;
				if (index < this._span.Length)
				{
					this._index = index;
					return true;
				}
				return false;
			}

			// Token: 0x170005BA RID: 1466
			// (get) Token: 0x06001940 RID: 6464 RVA: 0x00051076 File Offset: 0x0004F276
			public ref T Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return this._span[this._index];
				}
			}

			// Token: 0x040010A6 RID: 4262
			[Nullable(new byte[] { 0, 1 })]
			private readonly Span<T> _span;

			// Token: 0x040010A7 RID: 4263
			private int _index;
		}
	}
}
