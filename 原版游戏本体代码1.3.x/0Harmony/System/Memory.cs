using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x0200046E RID: 1134
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(MemoryDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	internal readonly struct Memory<[Nullable(2)] T>
	{
		// Token: 0x06001885 RID: 6277 RVA: 0x0004E0F8 File Offset: 0x0004C2F8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Memory([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			if (array == null)
			{
				this = default(Memory<T>);
				return;
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			this._object = array;
			this._index = 0;
			this._length = array.Length;
		}

		// Token: 0x06001886 RID: 6278 RVA: 0x0004E154 File Offset: 0x0004C354
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Memory([Nullable(new byte[] { 2, 1 })] T[] array, int start)
		{
			if (array == null)
			{
				if (start != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException();
				}
				this = default(Memory<T>);
				return;
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			if (start > array.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this._object = array;
			this._index = start;
			this._length = array.Length - start;
		}

		// Token: 0x06001887 RID: 6279 RVA: 0x0004E1C4 File Offset: 0x0004C3C4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Memory([Nullable(new byte[] { 2, 1 })] T[] array, int start, int length)
		{
			if (array == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException();
				}
				this = default(Memory<T>);
				return;
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			if (start > array.Length || length > array.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this._object = array;
			this._index = start;
			this._length = length;
		}

		// Token: 0x06001888 RID: 6280 RVA: 0x0004E23B File Offset: 0x0004C43B
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Memory(MemoryManager<T> manager, int length)
		{
			if (length < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this._object = manager;
			this._index = int.MinValue;
			this._length = length;
		}

		// Token: 0x06001889 RID: 6281 RVA: 0x0004E25F File Offset: 0x0004C45F
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Memory(MemoryManager<T> manager, int start, int length)
		{
			if (length < 0 || start < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this._object = manager;
			this._index = start | int.MinValue;
			this._length = length;
		}

		// Token: 0x0600188A RID: 6282 RVA: 0x0004E289 File Offset: 0x0004C489
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Memory(object obj, int start, int length)
		{
			this._object = obj;
			this._index = start;
			this._length = length;
		}

		// Token: 0x0600188B RID: 6283 RVA: 0x0004E2A0 File Offset: 0x0004C4A0
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator Memory<T>([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			return new Memory<T>(array);
		}

		// Token: 0x0600188C RID: 6284 RVA: 0x0004E2A8 File Offset: 0x0004C4A8
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator Memory<T>([Nullable(new byte[] { 0, 1 })] ArraySegment<T> segment)
		{
			return new Memory<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x0600188D RID: 6285 RVA: 0x0004E2C4 File Offset: 0x0004C4C4
		[return: Nullable(new byte[] { 0, 1 })]
		public unsafe static implicit operator ReadOnlyMemory<T>([Nullable(new byte[] { 0, 1 })] Memory<T> memory)
		{
			return *Unsafe.As<Memory<T>, ReadOnlyMemory<T>>(ref memory);
		}

		// Token: 0x170005A4 RID: 1444
		// (get) Token: 0x0600188E RID: 6286 RVA: 0x0004E2D4 File Offset: 0x0004C4D4
		[Nullable(new byte[] { 0, 1 })]
		public static Memory<T> Empty
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return default(Memory<T>);
			}
		}

		// Token: 0x170005A5 RID: 1445
		// (get) Token: 0x0600188F RID: 6287 RVA: 0x0004E2EA File Offset: 0x0004C4EA
		public int Length
		{
			get
			{
				return this._length & int.MaxValue;
			}
		}

		// Token: 0x170005A6 RID: 1446
		// (get) Token: 0x06001890 RID: 6288 RVA: 0x0004E2F8 File Offset: 0x0004C4F8
		public bool IsEmpty
		{
			get
			{
				return (this._length & int.MaxValue) == 0;
			}
		}

		// Token: 0x06001891 RID: 6289 RVA: 0x0004E30C File Offset: 0x0004C50C
		public override string ToString()
		{
			if (!(typeof(T) == typeof(char)))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 2);
				defaultInterpolatedStringHandler.AppendLiteral("System.Memory<");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(T).Name);
				defaultInterpolatedStringHandler.AppendLiteral(">[");
				defaultInterpolatedStringHandler.AppendFormatted<int>(this._length & int.MaxValue);
				defaultInterpolatedStringHandler.AppendLiteral("]");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			string str = this._object as string;
			if (str == null)
			{
				return this.Span.ToString();
			}
			return str.Substring(this._index, this._length & int.MaxValue);
		}

		// Token: 0x06001892 RID: 6290 RVA: 0x0004E3D0 File Offset: 0x0004C5D0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public Memory<T> Slice(int start)
		{
			int capturedLength = this._length;
			int actualLength = capturedLength & int.MaxValue;
			if (start > actualLength)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new Memory<T>(this._object, this._index + start, capturedLength - start);
		}

		// Token: 0x06001893 RID: 6291 RVA: 0x0004E40C File Offset: 0x0004C60C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public Memory<T> Slice(int start, int length)
		{
			int capturedLength = this._length;
			int actualLength = capturedLength & int.MaxValue;
			if (start > actualLength || length > actualLength - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			return new Memory<T>(this._object, this._index + start, length | (capturedLength & int.MinValue));
		}

		// Token: 0x170005A7 RID: 1447
		// (get) Token: 0x06001894 RID: 6292 RVA: 0x0004E454 File Offset: 0x0004C654
		[Nullable(new byte[] { 0, 1 })]
		public Span<T> Span
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
					string s = this._object as string;
					if (s != null)
					{
						return new Span<T>(s, (IntPtr)RuntimeHelpers.OffsetToStringData, s.Length).Slice(this._index, this._length);
					}
				}
				if (this._object != null)
				{
					return new Span<T>((T[])this._object, this._index, this._length & int.MaxValue);
				}
				return default(Span<T>);
			}
		}

		// Token: 0x06001895 RID: 6293 RVA: 0x0004E51C File Offset: 0x0004C71C
		public void CopyTo([Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			this.Span.CopyTo(destination.Span);
		}

		// Token: 0x06001896 RID: 6294 RVA: 0x0004E540 File Offset: 0x0004C740
		public bool TryCopyTo([Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			return this.Span.TryCopyTo(destination.Span);
		}

		// Token: 0x06001897 RID: 6295 RVA: 0x0004E564 File Offset: 0x0004C764
		public unsafe MemoryHandle Pin()
		{
			if (this._index < 0)
			{
				return ((MemoryManager<T>)this._object).Pin(this._index & int.MaxValue);
			}
			if (typeof(T) == typeof(char))
			{
				string s = this._object as string;
				if (s != null)
				{
					GCHandle handle = GCHandle.Alloc(s, GCHandleType.Pinned);
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

		// Token: 0x06001898 RID: 6296 RVA: 0x0004E660 File Offset: 0x0004C860
		public T[] ToArray()
		{
			return this.Span.ToArray();
		}

		// Token: 0x06001899 RID: 6297 RVA: 0x0004E67C File Offset: 0x0004C87C
		[NullableContext(2)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			if (obj is ReadOnlyMemory<T>)
			{
				return ((ReadOnlyMemory<T>)obj).Equals(this);
			}
			if (obj is Memory<T>)
			{
				Memory<T> memory = (Memory<T>)obj;
				return this.Equals(memory);
			}
			return false;
		}

		// Token: 0x0600189A RID: 6298 RVA: 0x0004E6C3 File Offset: 0x0004C8C3
		public bool Equals([Nullable(new byte[] { 0, 1 })] Memory<T> other)
		{
			return this._object == other._object && this._index == other._index && this._length == other._length;
		}

		// Token: 0x0600189B RID: 6299 RVA: 0x0004E6F1 File Offset: 0x0004C8F1
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			if (this._object == null)
			{
				return 0;
			}
			return HashCode.Combine<object, int, int>(this._object, this._index, this._length);
		}

		// Token: 0x04001095 RID: 4245
		[Nullable(2)]
		private readonly object _object;

		// Token: 0x04001096 RID: 4246
		private readonly int _index;

		// Token: 0x04001097 RID: 4247
		private readonly int _length;

		// Token: 0x04001098 RID: 4248
		private const int RemoveFlagsBitMask = 2147483647;
	}
}
