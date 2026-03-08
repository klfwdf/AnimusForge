using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000016 RID: 22
	[NullableContext(2)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(MemoryDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public readonly struct ReadOnlyMemory<T>
	{
		// Token: 0x060000AA RID: 170 RVA: 0x00005168 File Offset: 0x00003368
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlyMemory([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			if (array == null)
			{
				this = default(ReadOnlyMemory<T>);
				return;
			}
			this._object = array;
			this._index = 0;
			this._length = array.Length;
		}

		// Token: 0x060000AB RID: 171 RVA: 0x0000518C File Offset: 0x0000338C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlyMemory([Nullable(new byte[] { 2, 1 })] T[] array, int start, int length)
		{
			if (array == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException();
				}
				this = default(ReadOnlyMemory<T>);
				return;
			}
			if (start > array.Length || length > array.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this._object = array;
			this._index = start;
			this._length = length;
		}

		// Token: 0x060000AC RID: 172 RVA: 0x000051CC File Offset: 0x000033CC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ReadOnlyMemory(object obj, int start, int length)
		{
			this._object = obj;
			this._index = start;
			this._length = length;
		}

		// Token: 0x060000AD RID: 173 RVA: 0x000051E3 File Offset: 0x000033E3
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator ReadOnlyMemory<T>([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			return new ReadOnlyMemory<T>(array);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x000051EB File Offset: 0x000033EB
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator ReadOnlyMemory<T>([Nullable(new byte[] { 0, 1 })] ArraySegment<T> segment)
		{
			return new ReadOnlyMemory<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x060000AF RID: 175 RVA: 0x00005208 File Offset: 0x00003408
		[Nullable(new byte[] { 0, 1 })]
		public static ReadOnlyMemory<T> Empty
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return default(ReadOnlyMemory<T>);
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x060000B0 RID: 176 RVA: 0x0000521E File Offset: 0x0000341E
		public int Length
		{
			get
			{
				return this._length & int.MaxValue;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x060000B1 RID: 177 RVA: 0x0000522C File Offset: 0x0000342C
		public bool IsEmpty
		{
			get
			{
				return (this._length & int.MaxValue) == 0;
			}
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00005240 File Offset: 0x00003440
		[NullableContext(1)]
		public override string ToString()
		{
			if (!(typeof(T) == typeof(char)))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 2);
				defaultInterpolatedStringHandler.AppendLiteral("System.ReadOnlyMemory<");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(T).Name);
				defaultInterpolatedStringHandler.AppendLiteral(">[");
				defaultInterpolatedStringHandler.AppendFormatted<int>(this._length & int.MaxValue);
				defaultInterpolatedStringHandler.AppendLiteral("]");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			string text = this._object as string;
			if (text == null)
			{
				return this.Span.ToString();
			}
			return text.Substring(this._index, this._length & int.MaxValue);
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00005304 File Offset: 0x00003504
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<T> Slice(int start)
		{
			int length = this._length;
			int num = length & int.MaxValue;
			if (start > num)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new ReadOnlyMemory<T>(this._object, this._index + start, length - start);
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00005340 File Offset: 0x00003540
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<T> Slice(int start, int length)
		{
			int length2 = this._length;
			int num = this._length & int.MaxValue;
			if (start > num || length > num - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new ReadOnlyMemory<T>(this._object, this._index + start, length | (length2 & int.MinValue));
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x060000B5 RID: 181 RVA: 0x00005390 File Offset: 0x00003590
		[Nullable(new byte[] { 0, 1 })]
		public ReadOnlySpan<T> Span
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				if (this._index < 0)
				{
					return ((MemoryManager<T>)this._object).GetSpan().Slice(this._index & int.MaxValue, this._length);
				}
				if (typeof(T) == typeof(char))
				{
					string text = this._object as string;
					if (text != null)
					{
						return new ReadOnlySpan<T>(text, (IntPtr)RuntimeHelpers.OffsetToStringData, text.Length).Slice(this._index, this._length);
					}
				}
				if (this._object != null)
				{
					return new ReadOnlySpan<T>((T[])this._object, this._index, this._length & int.MaxValue);
				}
				return default(ReadOnlySpan<T>);
			}
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x0000545C File Offset: 0x0000365C
		public void CopyTo([Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			this.Span.CopyTo(destination.Span);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00005480 File Offset: 0x00003680
		public bool TryCopyTo([Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			return this.Span.TryCopyTo(destination.Span);
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x000054A4 File Offset: 0x000036A4
		public unsafe MemoryHandle Pin()
		{
			if (this._index < 0)
			{
				return ((MemoryManager<T>)this._object).Pin(this._index & int.MaxValue);
			}
			if (typeof(T) == typeof(char))
			{
				string text = this._object as string;
				if (text != null)
				{
					GCHandle handle = GCHandle.Alloc(text, GCHandleType.Pinned);
					return new MemoryHandle(Unsafe.Add<T>((void*)handle.AddrOfPinnedObject(), this._index), handle, null);
				}
			}
			T[] array = this._object as T[];
			if (array == null)
			{
				return default(MemoryHandle);
			}
			if (this._length < 0)
			{
				return new MemoryHandle(Unsafe.Add<T>(Unsafe.AsPointer<T>(MemoryMarshal.GetReference<T>(array)), this._index), default(GCHandle), null);
			}
			GCHandle handle2 = GCHandle.Alloc(array, GCHandleType.Pinned);
			return new MemoryHandle(Unsafe.Add<T>((void*)handle2.AddrOfPinnedObject(), this._index), handle2, null);
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x000055A0 File Offset: 0x000037A0
		[NullableContext(1)]
		public T[] ToArray()
		{
			return this.Span.ToArray();
		}

		// Token: 0x060000BA RID: 186 RVA: 0x000055BC File Offset: 0x000037BC
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			if (obj is ReadOnlyMemory<T>)
			{
				ReadOnlyMemory<T> other = (ReadOnlyMemory<T>)obj;
				return this.Equals(other);
			}
			if (obj is Memory<T>)
			{
				Memory<T> memory = (Memory<T>)obj;
				return this.Equals(memory);
			}
			return false;
		}

		// Token: 0x060000BB RID: 187 RVA: 0x000055FD File Offset: 0x000037FD
		public bool Equals([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> other)
		{
			return this._object == other._object && this._index == other._index && this._length == other._length;
		}

		// Token: 0x060000BC RID: 188 RVA: 0x0000562B File Offset: 0x0000382B
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			if (this._object == null)
			{
				return 0;
			}
			return HashCode.Combine<object, int, int>(this._object, this._index, this._length);
		}

		// Token: 0x060000BD RID: 189 RVA: 0x0000564E File Offset: 0x0000384E
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal object GetObjectStartLength(out int start, out int length)
		{
			start = this._index;
			length = this._length;
			return this._object;
		}

		// Token: 0x0400002A RID: 42
		private readonly object _object;

		// Token: 0x0400002B RID: 43
		private readonly int _index;

		// Token: 0x0400002C RID: 44
		private readonly int _length;

		// Token: 0x0400002D RID: 45
		internal const int RemoveFlagsBitMask = 2147483647;
	}
}
