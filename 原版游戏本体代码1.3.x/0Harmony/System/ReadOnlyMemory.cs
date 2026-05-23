using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000471 RID: 1137
	[NullableContext(2)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(MemoryDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	internal readonly struct ReadOnlyMemory<T>
	{
		// Token: 0x060018F1 RID: 6385 RVA: 0x000500FC File Offset: 0x0004E2FC
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

		// Token: 0x060018F2 RID: 6386 RVA: 0x00050120 File Offset: 0x0004E320
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

		// Token: 0x060018F3 RID: 6387 RVA: 0x00050160 File Offset: 0x0004E360
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ReadOnlyMemory(object obj, int start, int length)
		{
			this._object = obj;
			this._index = start;
			this._length = length;
		}

		// Token: 0x060018F4 RID: 6388 RVA: 0x00050177 File Offset: 0x0004E377
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator ReadOnlyMemory<T>([Nullable(new byte[] { 2, 1 })] T[] array)
		{
			return new ReadOnlyMemory<T>(array);
		}

		// Token: 0x060018F5 RID: 6389 RVA: 0x0005017F File Offset: 0x0004E37F
		[return: Nullable(new byte[] { 0, 1 })]
		public static implicit operator ReadOnlyMemory<T>([Nullable(new byte[] { 0, 1 })] ArraySegment<T> segment)
		{
			return new ReadOnlyMemory<T>(segment.Array, segment.Offset, segment.Count);
		}

		// Token: 0x170005A9 RID: 1449
		// (get) Token: 0x060018F6 RID: 6390 RVA: 0x0005019C File Offset: 0x0004E39C
		[Nullable(new byte[] { 0, 1 })]
		public static ReadOnlyMemory<T> Empty
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return default(ReadOnlyMemory<T>);
			}
		}

		// Token: 0x170005AA RID: 1450
		// (get) Token: 0x060018F7 RID: 6391 RVA: 0x000501B2 File Offset: 0x0004E3B2
		public int Length
		{
			get
			{
				return this._length & int.MaxValue;
			}
		}

		// Token: 0x170005AB RID: 1451
		// (get) Token: 0x060018F8 RID: 6392 RVA: 0x000501C0 File Offset: 0x0004E3C0
		public bool IsEmpty
		{
			get
			{
				return (this._length & int.MaxValue) == 0;
			}
		}

		// Token: 0x060018F9 RID: 6393 RVA: 0x000501D4 File Offset: 0x0004E3D4
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
			string str = this._object as string;
			if (str == null)
			{
				return this.Span.ToString();
			}
			return str.Substring(this._index, this._length & int.MaxValue);
		}

		// Token: 0x060018FA RID: 6394 RVA: 0x00050298 File Offset: 0x0004E498
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<T> Slice(int start)
		{
			int capturedLength = this._length;
			int actualLength = capturedLength & int.MaxValue;
			if (start > actualLength)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new ReadOnlyMemory<T>(this._object, this._index + start, capturedLength - start);
		}

		// Token: 0x060018FB RID: 6395 RVA: 0x000502D4 File Offset: 0x0004E4D4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<T> Slice(int start, int length)
		{
			int capturedLength = this._length;
			int actualLength = this._length & int.MaxValue;
			if (start > actualLength || length > actualLength - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new ReadOnlyMemory<T>(this._object, this._index + start, length | (capturedLength & int.MinValue));
		}

		// Token: 0x170005AC RID: 1452
		// (get) Token: 0x060018FC RID: 6396 RVA: 0x00050324 File Offset: 0x0004E524
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
					string s = this._object as string;
					if (s != null)
					{
						return new ReadOnlySpan<T>(s, (IntPtr)RuntimeHelpers.OffsetToStringData, s.Length).Slice(this._index, this._length);
					}
				}
				if (this._object != null)
				{
					return new ReadOnlySpan<T>((T[])this._object, this._index, this._length & int.MaxValue);
				}
				return default(ReadOnlySpan<T>);
			}
		}

		// Token: 0x060018FD RID: 6397 RVA: 0x000503F0 File Offset: 0x0004E5F0
		public void CopyTo([Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			this.Span.CopyTo(destination.Span);
		}

		// Token: 0x060018FE RID: 6398 RVA: 0x00050414 File Offset: 0x0004E614
		public bool TryCopyTo([Nullable(new byte[] { 0, 1 })] Memory<T> destination)
		{
			return this.Span.TryCopyTo(destination.Span);
		}

		// Token: 0x060018FF RID: 6399 RVA: 0x00050438 File Offset: 0x0004E638
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

		// Token: 0x06001900 RID: 6400 RVA: 0x00050534 File Offset: 0x0004E734
		[NullableContext(1)]
		public T[] ToArray()
		{
			return this.Span.ToArray();
		}

		// Token: 0x06001901 RID: 6401 RVA: 0x00050550 File Offset: 0x0004E750
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			if (obj is ReadOnlyMemory<T>)
			{
				ReadOnlyMemory<T> readOnlyMemory = (ReadOnlyMemory<T>)obj;
				return this.Equals(readOnlyMemory);
			}
			if (obj is Memory<T>)
			{
				Memory<T> memory = (Memory<T>)obj;
				return this.Equals(memory);
			}
			return false;
		}

		// Token: 0x06001902 RID: 6402 RVA: 0x00050591 File Offset: 0x0004E791
		public bool Equals([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> other)
		{
			return this._object == other._object && this._index == other._index && this._length == other._length;
		}

		// Token: 0x06001903 RID: 6403 RVA: 0x000505BF File Offset: 0x0004E7BF
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			if (this._object == null)
			{
				return 0;
			}
			return HashCode.Combine<object, int, int>(this._object, this._index, this._length);
		}

		// Token: 0x06001904 RID: 6404 RVA: 0x000505E2 File Offset: 0x0004E7E2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal object GetObjectStartLength(out int start, out int length)
		{
			start = this._index;
			length = this._length;
			return this._object;
		}

		// Token: 0x0400109A RID: 4250
		private readonly object _object;

		// Token: 0x0400109B RID: 4251
		private readonly int _index;

		// Token: 0x0400109C RID: 4252
		private readonly int _length;

		// Token: 0x0400109D RID: 4253
		internal const int RemoveFlagsBitMask = 2147483647;
	}
}
