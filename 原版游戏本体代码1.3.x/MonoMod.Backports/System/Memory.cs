using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000013 RID: 19
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(MemoryDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public readonly struct Memory<[Nullable(2)] T>
	{
		// Token: 0x0600003E RID: 62 RVA: 0x00003164 File Offset: 0x00001364
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

		// Token: 0x0600003F RID: 63 RVA: 0x000031C0 File Offset: 0x000013C0
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

		// Token: 0x06000040 RID: 64 RVA: 0x00003230 File Offset: 0x00001430
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

		// Token: 0x06000041 RID: 65 RVA: 0x000032A7 File Offset: 0x000014A7
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

		// Token: 0x06000042 RID: 66 RVA: 0x000032CB File Offset: 0x000014CB
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

		// Token: 0x06000043 RID: 67 RVA: 0x000032F5 File Offset: 0x000014F5
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Memory(object obj, int start, int length)
		{
			this._object = obj;
			this._index = start;
			this._length = length;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x0000330C File Offset: 0x0000150C
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator Memory<T>([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			return new Memory<T>(array);
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003314 File Offset: 0x00001514
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator Memory<T>([Nullable(new byte[] { 0, 1 })] ArraySegment<T> segment)
		{
			return new Memory<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003330 File Offset: 0x00001530
		[return: Nullable(new byte[] { 0, 1 })]
		public unsafe static implicit operator ReadOnlyMemory<T>([Nullable(new byte[] { 0, 1 })] Memory<T> memory)
		{
			return *Unsafe.As<Memory<T>, ReadOnlyMemory<T>>(ref memory);
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000047 RID: 71 RVA: 0x00003340 File Offset: 0x00001540
		[Nullable(new byte[] { 0, 1 })]
		public static Memory<T> Empty
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return default(Memory<T>);
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000048 RID: 72 RVA: 0x00003356 File Offset: 0x00001556
		public int Length
		{
			get
			{
				return this._length & int.MaxValue;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000049 RID: 73 RVA: 0x00003364 File Offset: 0x00001564
		public bool IsEmpty
		{
			get
			{
				return (this._length & int.MaxValue) == 0;
			}
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003378 File Offset: 0x00001578
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
			string text = this._object as string;
			if (text == null)
			{
				return this.Span.ToString();
			}
			return text.Substring(this._index, this._length & int.MaxValue);
		}

		// Token: 0x0600004B RID: 75 RVA: 0x0000343C File Offset: 0x0000163C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public Memory<T> Slice(int start)
		{
			int length = this._length;
			int num = length & int.MaxValue;
			if (start > num)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new Memory<T>(this._object, this._index + start, length - start);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003478 File Offset: 0x00001678
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public Memory<T> Slice(int start, int length)
		{
			int length2 = this._length;
			int num = length2 & int.MaxValue;
			if (start > num || length > num - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			return new Memory<T>(this._object, this._index + start, length | (length2 & int.MinValue));
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600004D RID: 77 RVA: 0x000034C0 File Offset: 0x000016C0
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
					string text = this._object as string;
					if (text != null)
					{
						return new Span<T>(text, (IntPtr)RuntimeHelpers.OffsetToStringData, text.Length).Slice(this._index, this._length);
					}
				}
				if (this._object != null)
				{
					return new Span<T>((T[])this._object, this._index, this._length & int.MaxValue);
				}
				return default(Span<T>);
			}
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003588 File Offset: 0x00001788
		public void CopyTo([Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			this.Span.CopyTo(destination.Span);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x000035AC File Offset: 0x000017AC
		public bool TryCopyTo([Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			return this.Span.TryCopyTo(destination.Span);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000035D0 File Offset: 0x000017D0
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

		// Token: 0x06000051 RID: 81 RVA: 0x000036CC File Offset: 0x000018CC
		public T[] ToArray()
		{
			return this.Span.ToArray();
		}

		// Token: 0x06000052 RID: 82 RVA: 0x000036E8 File Offset: 0x000018E8
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
				Memory<T> other = (Memory<T>)obj;
				return this.Equals(other);
			}
			return false;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x0000372F File Offset: 0x0000192F
		public bool Equals([Nullable(new byte[] { 0, 1 })] Memory<T> other)
		{
			return this._object == other._object && this._index == other._index && this._length == other._length;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x0000375D File Offset: 0x0000195D
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			if (this._object == null)
			{
				return 0;
			}
			return HashCode.Combine<object, int, int>(this._object, this._index, this._length);
		}

		// Token: 0x04000025 RID: 37
		[Nullable(2)]
		private readonly object _object;

		// Token: 0x04000026 RID: 38
		private readonly int _index;

		// Token: 0x04000027 RID: 39
		private readonly int _length;

		// Token: 0x04000028 RID: 40
		private const int RemoveFlagsBitMask = 2147483647;
	}
}
