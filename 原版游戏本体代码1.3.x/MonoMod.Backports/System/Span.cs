using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MonoMod;

namespace System
{
	// Token: 0x02000018 RID: 24
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(SpanDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public readonly ref struct Span<[Nullable(2)] T>
	{
		// Token: 0x17000012 RID: 18
		// (get) Token: 0x060000D7 RID: 215 RVA: 0x00005A24 File Offset: 0x00003C24
		public int Length
		{
			get
			{
				return this._length;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x060000D8 RID: 216 RVA: 0x00005A2C File Offset: 0x00003C2C
		public bool IsEmpty
		{
			get
			{
				return this._length == 0;
			}
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x00005A37 File Offset: 0x00003C37
		public static bool operator !=([Nullable(new byte[] { 0, 1 })] Span<T> left, [Nullable(new byte[] { 0, 1 })] Span<T> right)
		{
			return !(left == right);
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00005A43 File Offset: 0x00003C43
		[NullableContext(2)]
		[Obsolete("Equals() on Span will always throw an exception. Use == instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			throw new NotSupportedException("Cannot call Equals on Span");
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00005A4F File Offset: 0x00003C4F
		[Obsolete("GetHashCode() on Span will always throw an exception.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			throw new NotSupportedException("Cannot call GetHashCode on Span");
		}

		// Token: 0x060000DC RID: 220 RVA: 0x00005A5B File Offset: 0x00003C5B
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator Span<T>([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			return new Span<T>(array);
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00005A63 File Offset: 0x00003C63
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator Span<T>([Nullable(new byte[] { 0, 1 })] ArraySegment<T> segment)
		{
			return new Span<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x060000DE RID: 222 RVA: 0x00005A80 File Offset: 0x00003C80
		[Nullable(new byte[] { 0, 1 })]
		public static Span<T> Empty
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return default(Span<T>);
			}
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00005A96 File Offset: 0x00003C96
		[NullableContext(0)]
		public Span<T>.Enumerator GetEnumerator()
		{
			return new Span<T>.Enumerator(this);
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00005AA4 File Offset: 0x00003CA4
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

		// Token: 0x060000E1 RID: 225 RVA: 0x00005B04 File Offset: 0x00003D04
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

		// Token: 0x060000E2 RID: 226 RVA: 0x00005B7C File Offset: 0x00003D7C
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

		// Token: 0x060000E3 RID: 227 RVA: 0x00005BFF File Offset: 0x00003DFF
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

		// Token: 0x060000E4 RID: 228 RVA: 0x00005C3B File Offset: 0x00003E3B
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Span(object pinnable, IntPtr byteOffset, int length)
		{
			this._length = length;
			this._pinnable = pinnable;
			this._byteOffset = byteOffset;
		}

		// Token: 0x17000015 RID: 21
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

		// Token: 0x060000E6 RID: 230 RVA: 0x00005C6E File Offset: 0x00003E6E
		[EditorBrowsable(EditorBrowsableState.Never)]
		public ref T GetPinnableReference()
		{
			if (this._length != 0)
			{
				return this.DangerousGetPinnableReference();
			}
			return Unsafe.NullRef<T>();
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00005C84 File Offset: 0x00003E84
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
					UIntPtr pointerSizeLength = (UIntPtr)((ulong)((long)(length * Unsafe.SizeOf<T>() / sizeof(IntPtr))));
					SpanHelpers.ClearPointerSizedWithReferences(Unsafe.As<T, IntPtr>(this.DangerousGetPinnableReference()), pointerSizeLength);
					return;
				}
				SpanHelpers.ClearPointerSizedWithoutReferences(Unsafe.As<T, byte>(this.DangerousGetPinnableReference()), byteLength);
				return;
			}
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00005D30 File Offset: 0x00003F30
		public unsafe void Fill(T value)
		{
			int length = this._length;
			if (length == 0)
			{
				return;
			}
			if (Unsafe.SizeOf<T>() == 1)
			{
				byte value2 = *Unsafe.As<T, byte>(ref value);
				Unsafe.InitBlockUnaligned(Unsafe.As<T, byte>(this.DangerousGetPinnableReference()), value2, (uint)length);
				return;
			}
			ref T source = ref this.DangerousGetPinnableReference();
			int i;
			for (i = 0; i < (length & -8); i += 8)
			{
				*Unsafe.Add<T>(ref source, i) = value;
				*Unsafe.Add<T>(ref source, i + 1) = value;
				*Unsafe.Add<T>(ref source, i + 2) = value;
				*Unsafe.Add<T>(ref source, i + 3) = value;
				*Unsafe.Add<T>(ref source, i + 4) = value;
				*Unsafe.Add<T>(ref source, i + 5) = value;
				*Unsafe.Add<T>(ref source, i + 6) = value;
				*Unsafe.Add<T>(ref source, i + 7) = value;
			}
			if (i < (length & -4))
			{
				*Unsafe.Add<T>(ref source, i) = value;
				*Unsafe.Add<T>(ref source, i + 1) = value;
				*Unsafe.Add<T>(ref source, i + 2) = value;
				*Unsafe.Add<T>(ref source, i + 3) = value;
				i += 4;
			}
			while (i < length)
			{
				*Unsafe.Add<T>(ref source, i) = value;
				i++;
			}
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00005E57 File Offset: 0x00004057
		public void CopyTo([Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			if (!this.TryCopyTo(destination))
			{
				ThrowHelper.ThrowArgumentException_DestinationTooShort();
			}
		}

		// Token: 0x060000EA RID: 234 RVA: 0x00005E68 File Offset: 0x00004068
		public bool TryCopyTo([Nullable(new byte[] { 0, 1 })] Span<T> destination)
		{
			int length = this._length;
			int length2 = destination._length;
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

		// Token: 0x060000EB RID: 235 RVA: 0x00005EA5 File Offset: 0x000040A5
		public static bool operator ==([Nullable(new byte[] { 0, 1 })] Span<T> left, [Nullable(new byte[] { 0, 1 })] Span<T> right)
		{
			return left._length == right._length && Unsafe.AreSame<T>(left.DangerousGetPinnableReference(), right.DangerousGetPinnableReference());
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00005ECA File Offset: 0x000040CA
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator ReadOnlySpan<T>([Nullable(new byte[] { 0, 1 })] Span<T> span)
		{
			return new ReadOnlySpan<T>(span._pinnable, span._byteOffset, span._length);
		}

		// Token: 0x060000ED RID: 237 RVA: 0x00005EE4 File Offset: 0x000040E4
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

		// Token: 0x060000EE RID: 238 RVA: 0x00005F80 File Offset: 0x00004180
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public Span<T> Slice(int start)
		{
			if (start > this._length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr byteOffset = this._byteOffset.Add(start);
			int length = this._length - start;
			return new Span<T>(this._pinnable, byteOffset, length);
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00005FC0 File Offset: 0x000041C0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public Span<T> Slice(int start, int length)
		{
			if (start > this._length || length > this._length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr byteOffset = this._byteOffset.Add(start);
			return new Span<T>(this._pinnable, byteOffset, length);
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00006004 File Offset: 0x00004204
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

		// Token: 0x060000F1 RID: 241 RVA: 0x00006038 File Offset: 0x00004238
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ref T DangerousGetPinnableReference()
		{
			return Unsafe.AddByteOffset<T>(ILHelpers.ObjectAsRef<T>(this._pinnable), this._byteOffset);
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x060000F2 RID: 242 RVA: 0x00006050 File Offset: 0x00004250
		[Nullable(2)]
		internal object Pinnable
		{
			[NullableContext(2)]
			get
			{
				return this._pinnable;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x00006058 File Offset: 0x00004258
		internal IntPtr ByteOffset
		{
			get
			{
				return this._byteOffset;
			}
		}

		// Token: 0x04000031 RID: 49
		[Nullable(2)]
		private readonly object _pinnable;

		// Token: 0x04000032 RID: 50
		private readonly IntPtr _byteOffset;

		// Token: 0x04000033 RID: 51
		private readonly int _length;

		// Token: 0x02000059 RID: 89
		[Nullable(0)]
		public ref struct Enumerator
		{
			// Token: 0x060002BF RID: 703 RVA: 0x0000CD09 File Offset: 0x0000AF09
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal Enumerator([Nullable(new byte[] { 0, 1 })] Span<T> span)
			{
				this._span = span;
				this._index = -1;
			}

			// Token: 0x060002C0 RID: 704 RVA: 0x0000CD1C File Offset: 0x0000AF1C
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

			// Token: 0x17000043 RID: 67
			// (get) Token: 0x060002C1 RID: 705 RVA: 0x0000CD4A File Offset: 0x0000AF4A
			public ref T Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return this._span[this._index];
				}
			}

			// Token: 0x040000A3 RID: 163
			[Nullable(new byte[] { 0, 1 })]
			private readonly Span<T> _span;

			// Token: 0x040000A4 RID: 164
			private int _index;
		}
	}
}
