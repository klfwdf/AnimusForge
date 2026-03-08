using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MonoMod;

namespace System
{
	// Token: 0x02000472 RID: 1138
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(SpanDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	internal readonly ref struct ReadOnlySpan<[Nullable(2)] T>
	{
		// Token: 0x170005AD RID: 1453
		// (get) Token: 0x06001905 RID: 6405 RVA: 0x000505FA File Offset: 0x0004E7FA
		public int Length
		{
			get
			{
				return this._length;
			}
		}

		// Token: 0x170005AE RID: 1454
		// (get) Token: 0x06001906 RID: 6406 RVA: 0x00050602 File Offset: 0x0004E802
		public bool IsEmpty
		{
			get
			{
				return this._length == 0;
			}
		}

		// Token: 0x06001907 RID: 6407 RVA: 0x0005060D File Offset: 0x0004E80D
		public static bool operator !=([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> left, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> right)
		{
			return !(left == right);
		}

		// Token: 0x06001908 RID: 6408 RVA: 0x00050619 File Offset: 0x0004E819
		[NullableContext(2)]
		[Obsolete("Equals() on ReadOnlySpan will always throw an exception. Use == instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			throw new NotSupportedException("Cannot call Equals on Span");
		}

		// Token: 0x06001909 RID: 6409 RVA: 0x00050625 File Offset: 0x0004E825
		[Obsolete("GetHashCode() on ReadOnlySpan will always throw an exception.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			throw new NotSupportedException("Cannot call GetHashCodee on Span");
		}

		// Token: 0x0600190A RID: 6410 RVA: 0x00050631 File Offset: 0x0004E831
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator ReadOnlySpan<T>([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			return new ReadOnlySpan<T>(array);
		}

		// Token: 0x0600190B RID: 6411 RVA: 0x00050639 File Offset: 0x0004E839
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

		// Token: 0x170005AF RID: 1455
		// (get) Token: 0x0600190C RID: 6412 RVA: 0x00050664 File Offset: 0x0004E864
		[Nullable(new byte[] { 0, 1 })]
		public static ReadOnlySpan<T> Empty
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return default(ReadOnlySpan<T>);
			}
		}

		// Token: 0x0600190D RID: 6413 RVA: 0x0005067A File Offset: 0x0004E87A
		[NullableContext(0)]
		public ReadOnlySpan<T>.Enumerator GetEnumerator()
		{
			return new ReadOnlySpan<T>.Enumerator(this);
		}

		// Token: 0x0600190E RID: 6414 RVA: 0x00050687 File Offset: 0x0004E887
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

		// Token: 0x0600190F RID: 6415 RVA: 0x000506B0 File Offset: 0x0004E8B0
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

		// Token: 0x06001910 RID: 6416 RVA: 0x00050707 File Offset: 0x0004E907
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

		// Token: 0x06001911 RID: 6417 RVA: 0x00050743 File Offset: 0x0004E943
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ReadOnlySpan(object pinnable, IntPtr byteOffset, int length)
		{
			this._length = length;
			this._pinnable = pinnable;
			this._byteOffset = byteOffset;
		}

		// Token: 0x170005B0 RID: 1456
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

		// Token: 0x06001913 RID: 6419 RVA: 0x00050776 File Offset: 0x0004E976
		[EditorBrowsable(EditorBrowsableState.Never)]
		public ref readonly T GetPinnableReference()
		{
			if (this._length != 0)
			{
				return this.DangerousGetPinnableReference();
			}
			return Unsafe.AsRef<T>(null);
		}

		// Token: 0x06001914 RID: 6420 RVA: 0x0005078E File Offset: 0x0004E98E
		public void CopyTo([Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			if (!this.TryCopyTo(destination))
			{
				ThrowHelper.ThrowArgumentException_DestinationTooShort();
			}
		}

		// Token: 0x06001915 RID: 6421 RVA: 0x000507A0 File Offset: 0x0004E9A0
		public bool TryCopyTo([Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			int length = this._length;
			int destLength = destination.Length;
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

		// Token: 0x06001916 RID: 6422 RVA: 0x000507DE File Offset: 0x0004E9DE
		public static bool operator ==([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> left, [Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> right)
		{
			return left._length == right._length && Unsafe.AreSame<T>(left.DangerousGetPinnableReference(), right.DangerousGetPinnableReference());
		}

		// Token: 0x06001917 RID: 6423 RVA: 0x00050804 File Offset: 0x0004EA04
		public unsafe override string ToString()
		{
			if (typeof(T) == typeof(char))
			{
				if (this._byteOffset == (IntPtr)RuntimeHelpers.OffsetToStringData)
				{
					string str = Unsafe.As<object>(this._pinnable) as string;
					if (str != null && this._length == str.Length)
					{
						return str;
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

		// Token: 0x06001918 RID: 6424 RVA: 0x000508D8 File Offset: 0x0004EAD8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySpan<T> Slice(int start)
		{
			if (start > this._length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr newOffset = this._byteOffset.Add(start);
			int length = this._length - start;
			return new ReadOnlySpan<T>(this._pinnable, newOffset, length);
		}

		// Token: 0x06001919 RID: 6425 RVA: 0x00050918 File Offset: 0x0004EB18
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySpan<T> Slice(int start, int length)
		{
			if (start > this._length || length > this._length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr newOffset = this._byteOffset.Add(start);
			return new ReadOnlySpan<T>(this._pinnable, newOffset, length);
		}

		// Token: 0x0600191A RID: 6426 RVA: 0x0005095C File Offset: 0x0004EB5C
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

		// Token: 0x0600191B RID: 6427 RVA: 0x00050990 File Offset: 0x0004EB90
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ref T DangerousGetPinnableReference()
		{
			return Unsafe.AddByteOffset<T>(ILHelpers.ObjectAsRef<T>(this._pinnable), this._byteOffset);
		}

		// Token: 0x170005B1 RID: 1457
		// (get) Token: 0x0600191C RID: 6428 RVA: 0x000509A8 File Offset: 0x0004EBA8
		[Nullable(2)]
		internal object Pinnable
		{
			[NullableContext(2)]
			get
			{
				return this._pinnable;
			}
		}

		// Token: 0x170005B2 RID: 1458
		// (get) Token: 0x0600191D RID: 6429 RVA: 0x000509B0 File Offset: 0x0004EBB0
		internal IntPtr ByteOffset
		{
			get
			{
				return this._byteOffset;
			}
		}

		// Token: 0x0400109E RID: 4254
		[Nullable(2)]
		private readonly object _pinnable;

		// Token: 0x0400109F RID: 4255
		private readonly IntPtr _byteOffset;

		// Token: 0x040010A0 RID: 4256
		private readonly int _length;

		// Token: 0x02000473 RID: 1139
		[Nullable(0)]
		public ref struct Enumerator
		{
			// Token: 0x0600191E RID: 6430 RVA: 0x000509B8 File Offset: 0x0004EBB8
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal Enumerator([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> span)
			{
				this._span = span;
				this._index = -1;
			}

			// Token: 0x0600191F RID: 6431 RVA: 0x000509C8 File Offset: 0x0004EBC8
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

			// Token: 0x170005B3 RID: 1459
			// (get) Token: 0x06001920 RID: 6432 RVA: 0x000509F6 File Offset: 0x0004EBF6
			public ref readonly T Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return this._span[this._index];
				}
			}

			// Token: 0x040010A1 RID: 4257
			[Nullable(new byte[] { 0, 1 })]
			private readonly ReadOnlySpan<T> _span;

			// Token: 0x040010A2 RID: 4258
			private int _index;
		}
	}
}
