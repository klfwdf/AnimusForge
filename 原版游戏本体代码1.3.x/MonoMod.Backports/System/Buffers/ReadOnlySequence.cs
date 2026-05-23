using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
	// Token: 0x02000030 RID: 48
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(ReadOnlySequenceDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public readonly struct ReadOnlySequence<[Nullable(2)] T>
	{
		// Token: 0x17000020 RID: 32
		// (get) Token: 0x060001C9 RID: 457 RVA: 0x0000A1A9 File Offset: 0x000083A9
		public long Length
		{
			get
			{
				return this.GetLength();
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060001CA RID: 458 RVA: 0x0000A1B1 File Offset: 0x000083B1
		public bool IsEmpty
		{
			get
			{
				return this.Length == 0L;
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060001CB RID: 459 RVA: 0x0000A1BD File Offset: 0x000083BD
		public bool IsSingleSegment
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._sequenceStart.GetObject() == this._sequenceEnd.GetObject();
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060001CC RID: 460 RVA: 0x0000A1D7 File Offset: 0x000083D7
		[Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<T> First
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return this.GetFirstBuffer();
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060001CD RID: 461 RVA: 0x0000A1DF File Offset: 0x000083DF
		public SequencePosition Start
		{
			get
			{
				return this._sequenceStart;
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060001CE RID: 462 RVA: 0x0000A1E7 File Offset: 0x000083E7
		public SequencePosition End
		{
			get
			{
				return this._sequenceEnd;
			}
		}

		// Token: 0x060001CF RID: 463 RVA: 0x0000A1EF File Offset: 0x000083EF
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ReadOnlySequence(object startSegment, int startIndexAndFlags, object endSegment, int endIndexAndFlags)
		{
			this._sequenceStart = new SequencePosition(startSegment, startIndexAndFlags);
			this._sequenceEnd = new SequencePosition(endSegment, endIndexAndFlags);
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x0000A20C File Offset: 0x0000840C
		public ReadOnlySequence(ReadOnlySequenceSegment<T> startSegment, int startIndex, ReadOnlySequenceSegment<T> endSegment, int endIndex)
		{
			if (startSegment == null || endSegment == null || (startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex) || startSegment.Memory.Length < startIndex || endSegment.Memory.Length < endIndex || (startSegment == endSegment && endIndex < startIndex))
			{
				ThrowHelper.ThrowArgumentValidationException<T>(startSegment, startIndex, endSegment);
			}
			this._sequenceStart = new SequencePosition(startSegment, ReadOnlySequence.SegmentToSequenceStart(startIndex));
			this._sequenceEnd = new SequencePosition(endSegment, ReadOnlySequence.SegmentToSequenceEnd(endIndex));
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x0000A28A File Offset: 0x0000848A
		public ReadOnlySequence(T[] array)
		{
			ThrowHelper.ThrowIfArgumentNull(array, ExceptionArgument.array);
			this._sequenceStart = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceStart(0));
			this._sequenceEnd = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceEnd(array.Length));
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x0000A2BA File Offset: 0x000084BA
		public ReadOnlySequence(T[] array, int start, int length)
		{
			if (array == null || start > array.Length || length > array.Length - start)
			{
				ThrowHelper.ThrowArgumentValidationException(array, start);
			}
			this._sequenceStart = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceStart(start));
			this._sequenceEnd = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceEnd(start + length));
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x0000A2FC File Offset: 0x000084FC
		public unsafe ReadOnlySequence([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory)
		{
			MemoryManager<T> @object;
			int startIndex;
			int num;
			if (MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out @object, out startIndex, out num))
			{
				this._sequenceStart = new SequencePosition(@object, ReadOnlySequence.MemoryManagerToSequenceStart(startIndex));
				this._sequenceEnd = new SequencePosition(@object, ReadOnlySequence.MemoryManagerToSequenceEnd(num));
				return;
			}
			ArraySegment<T> arraySegment;
			if (MemoryMarshal.TryGetArray<T>(memory, out arraySegment))
			{
				T[] array = arraySegment.Array;
				int offset = arraySegment.Offset;
				this._sequenceStart = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceStart(offset));
				this._sequenceEnd = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceEnd(offset + arraySegment.Count));
				return;
			}
			if (typeof(T) == typeof(char))
			{
				string object2;
				int num2;
				if (!MemoryMarshal.TryGetString(*Unsafe.As<ReadOnlyMemory<T>, ReadOnlyMemory<char>>(ref memory), out object2, out num2, out num))
				{
					ThrowHelper.ThrowInvalidOperationException();
				}
				this._sequenceStart = new SequencePosition(object2, ReadOnlySequence.StringToSequenceStart(num2));
				this._sequenceEnd = new SequencePosition(object2, ReadOnlySequence.StringToSequenceEnd(num2 + num));
				return;
			}
			ThrowHelper.ThrowInvalidOperationException();
			this._sequenceStart = default(SequencePosition);
			this._sequenceEnd = default(SequencePosition);
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x0000A40C File Offset: 0x0000860C
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(long start, long length)
		{
			if (start < 0L || length < 0L)
			{
				ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
			}
			int num = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			int index = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object @object = this._sequenceStart.GetObject();
			object object2 = this._sequenceEnd.GetObject();
			SequencePosition sequencePosition;
			SequencePosition endPosition;
			if (@object != object2)
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				int num2 = readOnlySequenceSegment.Memory.Length - num;
				if ((long)num2 > start)
				{
					num += (int)start;
					sequencePosition = new SequencePosition(@object, num);
					endPosition = ReadOnlySequence<T>.GetEndPosition(readOnlySequenceSegment, @object, num, object2, index, length);
				}
				else
				{
					if (num2 < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					sequencePosition = ReadOnlySequence<T>.SeekMultiSegment(readOnlySequenceSegment.Next, object2, index, start - (long)num2, ExceptionArgument.start);
					int index2 = ReadOnlySequence<T>.GetIndex(sequencePosition);
					object object3 = sequencePosition.GetObject();
					if (object3 != object2)
					{
						endPosition = ReadOnlySequence<T>.GetEndPosition((ReadOnlySequenceSegment<T>)object3, object3, index2, object2, index, length);
					}
					else
					{
						if ((long)(index - index2) < length)
						{
							ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
						}
						endPosition = new SequencePosition(object3, index2 + (int)length);
					}
				}
			}
			else
			{
				if ((long)(index - num) < start)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(-1L);
				}
				num += (int)start;
				sequencePosition = new SequencePosition(@object, num);
				if ((long)(index - num) < length)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
				endPosition = new SequencePosition(@object, num + (int)length);
			}
			return this.SliceImpl(sequencePosition, endPosition);
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x0000A55C File Offset: 0x0000875C
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(long start, SequencePosition end)
		{
			if (start < 0L)
			{
				ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
			}
			uint index = (uint)ReadOnlySequence<T>.GetIndex(end);
			object @object = end.GetObject();
			uint index2 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			object object2 = this._sequenceStart.GetObject();
			uint index3 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object object3 = this._sequenceEnd.GetObject();
			if (object2 == object3)
			{
				if (!ReadOnlySequence<T>.InRange(index, index2, index3))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if ((ulong)(index - index2) < (ulong)start)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(-1L);
				}
			}
			else
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)object2;
				ulong num = (ulong)(readOnlySequenceSegment.RunningIndex + (long)((ulong)index2));
				ulong num2 = (ulong)(((ReadOnlySequenceSegment<T>)@object).RunningIndex + (long)((ulong)index));
				if (!ReadOnlySequence<T>.InRange(num2, num, (ulong)(((ReadOnlySequenceSegment<T>)object3).RunningIndex + (long)((ulong)index3))))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (num + (ulong)start > num2)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				int num3 = readOnlySequenceSegment.Memory.Length - (int)index2;
				if ((long)num3 <= start)
				{
					if (num3 < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					SequencePosition sequencePosition = ReadOnlySequence<T>.SeekMultiSegment(readOnlySequenceSegment.Next, @object, (int)index, start - (long)num3, ExceptionArgument.start);
					return this.SliceImpl(sequencePosition, end);
				}
			}
			SequencePosition sequencePosition2 = new SequencePosition(object2, (int)(index2 + (uint)((int)start)));
			return this.SliceImpl(sequencePosition2, end);
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x0000A690 File Offset: 0x00008890
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(SequencePosition start, long length)
		{
			uint index = (uint)ReadOnlySequence<T>.GetIndex(start);
			object @object = start.GetObject();
			uint index2 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			object object2 = this._sequenceStart.GetObject();
			uint index3 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object object3 = this._sequenceEnd.GetObject();
			if (object2 == object3)
			{
				if (!ReadOnlySequence<T>.InRange(index, index2, index3))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (length < 0L)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
				if ((ulong)(index3 - index) < (ulong)length)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
			}
			else
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				long num = readOnlySequenceSegment.RunningIndex + (long)((ulong)index);
				ulong start2 = (ulong)(((ReadOnlySequenceSegment<T>)object2).RunningIndex + (long)((ulong)index2));
				ulong num2 = (ulong)(((ReadOnlySequenceSegment<T>)object3).RunningIndex + (long)((ulong)index3));
				if (!ReadOnlySequence<T>.InRange((ulong)num, start2, num2))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (length < 0L)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
				if (num + length > (long)num2)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
				}
				int num3 = readOnlySequenceSegment.Memory.Length - (int)index;
				if ((long)num3 < length)
				{
					if (num3 < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					SequencePosition sequencePosition = ReadOnlySequence<T>.SeekMultiSegment(readOnlySequenceSegment.Next, object3, (int)index3, length - (long)num3, ExceptionArgument.length);
					return this.SliceImpl(start, sequencePosition);
				}
			}
			SequencePosition sequencePosition2 = new SequencePosition(@object, (int)(index + (uint)((int)length)));
			return this.SliceImpl(start, sequencePosition2);
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x0000A7D1 File Offset: 0x000089D1
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(int start, int length)
		{
			return this.Slice((long)start, (long)length);
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x0000A7DD File Offset: 0x000089DD
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(int start, SequencePosition end)
		{
			return this.Slice((long)start, end);
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x0000A7E8 File Offset: 0x000089E8
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(SequencePosition start, int length)
		{
			return this.Slice(start, (long)length);
		}

		// Token: 0x060001DA RID: 474 RVA: 0x0000A7F3 File Offset: 0x000089F3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(SequencePosition start, SequencePosition end)
		{
			this.BoundsCheck((uint)ReadOnlySequence<T>.GetIndex(start), start.GetObject(), (uint)ReadOnlySequence<T>.GetIndex(end), end.GetObject());
			return this.SliceImpl(start, end);
		}

		// Token: 0x060001DB RID: 475 RVA: 0x0000A821 File Offset: 0x00008A21
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(SequencePosition start)
		{
			this.BoundsCheck(start);
			return this.SliceImpl(start, this._sequenceEnd);
		}

		// Token: 0x060001DC RID: 476 RVA: 0x0000A83C File Offset: 0x00008A3C
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(long start)
		{
			if (start < 0L)
			{
				ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
			}
			if (start == 0L)
			{
				return this;
			}
			SequencePosition sequencePosition = ReadOnlySequence<T>.Seek(this._sequenceStart, this._sequenceEnd, start, ExceptionArgument.start);
			return this.SliceImpl(sequencePosition, this._sequenceEnd);
		}

		// Token: 0x060001DD RID: 477 RVA: 0x0000A880 File Offset: 0x00008A80
		public unsafe override string ToString()
		{
			if (typeof(T) == typeof(char))
			{
				ReadOnlySequence<T> readOnlySequence = this;
				ReadOnlySequence<char> sequence = *Unsafe.As<ReadOnlySequence<T>, ReadOnlySequence<char>>(ref readOnlySequence);
				string text;
				int startIndex;
				int length;
				if (SequenceMarshal.TryGetString(sequence, out text, out startIndex, out length))
				{
					return text.Substring(startIndex, length);
				}
				if (this.Length < 2147483647L)
				{
					return new string((sequence).ToArray<char>());
				}
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
			defaultInterpolatedStringHandler.AppendLiteral("System.Buffers.ReadOnlySequence<");
			defaultInterpolatedStringHandler.AppendFormatted(typeof(T).Name);
			defaultInterpolatedStringHandler.AppendLiteral(">[");
			defaultInterpolatedStringHandler.AppendFormatted<long>(this.Length);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060001DE RID: 478 RVA: 0x0000A947 File Offset: 0x00008B47
		[NullableContext(0)]
		public ReadOnlySequence<T>.Enumerator GetEnumerator()
		{
			return new ReadOnlySequence<T>.Enumerator(ref this);
		}

		// Token: 0x060001DF RID: 479 RVA: 0x0000A94F File Offset: 0x00008B4F
		public SequencePosition GetPosition(long offset)
		{
			return this.GetPosition(offset, this._sequenceStart);
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x0000A95E File Offset: 0x00008B5E
		public SequencePosition GetPosition(long offset, SequencePosition origin)
		{
			if (offset < 0L)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();
			}
			return ReadOnlySequence<T>.Seek(origin, this._sequenceEnd, offset, ExceptionArgument.offset);
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x0000A97C File Offset: 0x00008B7C
		public bool TryGet(ref SequencePosition position, [Nullable(new byte[] { 0, 1 })] out ReadOnlyMemory<T> memory, bool advance = true)
		{
			SequencePosition sequencePosition;
			bool result = this.TryGetBuffer(position, out memory, out sequencePosition);
			if (advance)
			{
				position = sequencePosition;
			}
			return result;
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x0000A9A0 File Offset: 0x00008BA0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetBuffer(in SequencePosition position, [Nullable(new byte[] { 0, 1 })] out ReadOnlyMemory<T> memory, out SequencePosition next)
		{
			object @object = position.GetObject();
			next = default(SequencePosition);
			if (@object == null)
			{
				memory = default(ReadOnlyMemory<T>);
				return false;
			}
			ReadOnlySequence<T>.SequenceType sequenceType = this.GetSequenceType();
			object object2 = this._sequenceEnd.GetObject();
			int index = ReadOnlySequence<T>.GetIndex(position);
			int index2 = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			if (sequenceType == ReadOnlySequence<T>.SequenceType.MultiSegment)
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				if (readOnlySequenceSegment != object2)
				{
					ReadOnlySequenceSegment<T> next2 = readOnlySequenceSegment.Next;
					if (next2 == null)
					{
						ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
					}
					next = new SequencePosition(next2, 0);
					memory = readOnlySequenceSegment.Memory.Slice(index);
				}
				else
				{
					memory = readOnlySequenceSegment.Memory.Slice(index, index2 - index);
				}
			}
			else
			{
				if (@object != object2)
				{
					ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
				}
				if (sequenceType == ReadOnlySequence<T>.SequenceType.Array)
				{
					memory = new ReadOnlyMemory<T>((T[])@object, index, index2 - index);
				}
				else if (typeof(T) == typeof(char) && sequenceType == ReadOnlySequence<T>.SequenceType.String)
				{
					memory = (ReadOnlyMemory<T>)((string)@object).AsMemory(index, index2 - index);
				}
				else
				{
					memory = ((MemoryManager<T>)@object).Memory.Slice(index, index2 - index);
				}
			}
			return true;
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x0000AAE8 File Offset: 0x00008CE8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		private ReadOnlyMemory<T> GetFirstBuffer()
		{
			object @object = this._sequenceStart.GetObject();
			if (@object == null)
			{
				return default(ReadOnlyMemory<T>);
			}
			int num = this._sequenceStart.GetInteger();
			int integer = this._sequenceEnd.GetInteger();
			bool flag = @object != this._sequenceEnd.GetObject();
			if (num >= 0)
			{
				if (integer < 0)
				{
					if (flag)
					{
						ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
					}
					return new ReadOnlyMemory<T>((T[])@object, num, (integer & int.MaxValue) - num);
				}
				ReadOnlyMemory<T> memory = ((ReadOnlySequenceSegment<T>)@object).Memory;
				if (flag)
				{
					return memory.Slice(num);
				}
				return memory.Slice(num, integer - num);
			}
			else
			{
				if (flag)
				{
					ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
				}
				if (typeof(T) == typeof(char) && integer < 0)
				{
					return (ReadOnlyMemory<T>)((string)@object).AsMemory(num & int.MaxValue, integer - num);
				}
				num &= int.MaxValue;
				return ((MemoryManager<T>)@object).Memory.Slice(num, integer - num);
			}
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x0000ABF0 File Offset: 0x00008DF0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static SequencePosition Seek(in SequencePosition start, in SequencePosition end, long offset, ExceptionArgument argument)
		{
			int index = ReadOnlySequence<T>.GetIndex(start);
			int index2 = ReadOnlySequence<T>.GetIndex(end);
			object @object = start.GetObject();
			object object2 = end.GetObject();
			if (@object != object2)
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				int num = readOnlySequenceSegment.Memory.Length - index;
				if ((long)num <= offset)
				{
					if (num < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					return ReadOnlySequence<T>.SeekMultiSegment(readOnlySequenceSegment.Next, object2, index2, offset - (long)num, argument);
				}
			}
			else if ((long)(index2 - index) < offset)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(argument);
			}
			return new SequencePosition(@object, index + (int)offset);
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x0000AC78 File Offset: 0x00008E78
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static SequencePosition SeekMultiSegment([Nullable(new byte[] { 2, 1 })] ReadOnlySequenceSegment<T> currentSegment, object endObject, int endIndex, long offset, ExceptionArgument argument)
		{
			while (currentSegment != null && currentSegment != endObject)
			{
				int length = currentSegment.Memory.Length;
				if ((long)length > offset)
				{
					IL_3A:
					return new SequencePosition(currentSegment, (int)offset);
				}
				offset -= (long)length;
				currentSegment = currentSegment.Next;
			}
			if (currentSegment == null || (long)endIndex < offset)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(argument);
				goto IL_3A;
			}
			goto IL_3A;
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x0000ACC8 File Offset: 0x00008EC8
		private void BoundsCheck(in SequencePosition position)
		{
			uint index = (uint)ReadOnlySequence<T>.GetIndex(position);
			uint index2 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			uint index3 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object @object = this._sequenceStart.GetObject();
			object object2 = this._sequenceEnd.GetObject();
			if (@object == object2)
			{
				if (!ReadOnlySequence<T>.InRange(index, index2, index3))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					return;
				}
			}
			else
			{
				ulong start = (ulong)(((ReadOnlySequenceSegment<T>)@object).RunningIndex + (long)((ulong)index2));
				if (!ReadOnlySequence<T>.InRange((ulong)(((ReadOnlySequenceSegment<T>)position.GetObject()).RunningIndex + (long)((ulong)index)), start, (ulong)(((ReadOnlySequenceSegment<T>)object2).RunningIndex + (long)((ulong)index3))))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
			}
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x0000AD64 File Offset: 0x00008F64
		[NullableContext(2)]
		private void BoundsCheck(uint sliceStartIndex, object sliceStartObject, uint sliceEndIndex, object sliceEndObject)
		{
			uint index = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			uint index2 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object @object = this._sequenceStart.GetObject();
			object object2 = this._sequenceEnd.GetObject();
			if (@object == object2)
			{
				if (sliceStartObject != sliceEndObject || sliceStartObject != @object || sliceStartIndex > sliceEndIndex || sliceStartIndex < index || sliceEndIndex > index2)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					return;
				}
			}
			else
			{
				long num = ((ReadOnlySequenceSegment<T>)sliceStartObject).RunningIndex + (long)((ulong)sliceStartIndex);
				ulong num2 = (ulong)(((ReadOnlySequenceSegment<T>)sliceEndObject).RunningIndex + (long)((ulong)sliceEndIndex));
				if (num > (long)num2)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (num < ((ReadOnlySequenceSegment<T>)@object).RunningIndex + (long)((ulong)index) || num2 > (ulong)(((ReadOnlySequenceSegment<T>)object2).RunningIndex + (long)((ulong)index2)))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
			}
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x0000AE10 File Offset: 0x00009010
		private static SequencePosition GetEndPosition(ReadOnlySequenceSegment<T> startSegment, object startObject, int startIndex, object endObject, int endIndex, long length)
		{
			int num = startSegment.Memory.Length - startIndex;
			if ((long)num > length)
			{
				return new SequencePosition(startObject, startIndex + (int)length);
			}
			if (num < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
			}
			return ReadOnlySequence<T>.SeekMultiSegment(startSegment.Next, endObject, endIndex, length - (long)num, ExceptionArgument.length);
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x0000AE5D File Offset: 0x0000905D
		[NullableContext(0)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ReadOnlySequence<T>.SequenceType GetSequenceType()
		{
			return (ReadOnlySequence<T>.SequenceType)(-(ReadOnlySequence<T>.SequenceType)(2 * (this._sequenceStart.GetInteger() >> 31) + (this._sequenceEnd.GetInteger() >> 31)));
		}

		// Token: 0x060001EA RID: 490 RVA: 0x0000AE7F File Offset: 0x0000907F
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int GetIndex(in SequencePosition position)
		{
			return position.GetInteger() & int.MaxValue;
		}

		// Token: 0x060001EB RID: 491 RVA: 0x0000AE90 File Offset: 0x00009090
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		private ReadOnlySequence<T> SliceImpl(in SequencePosition start, in SequencePosition end)
		{
			return new ReadOnlySequence<T>(start.GetObject(), ReadOnlySequence<T>.GetIndex(start) | (this._sequenceStart.GetInteger() & int.MinValue), end.GetObject(), ReadOnlySequence<T>.GetIndex(end) | (this._sequenceEnd.GetInteger() & int.MinValue));
		}

		// Token: 0x060001EC RID: 492 RVA: 0x0000AEE0 File Offset: 0x000090E0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long GetLength()
		{
			int index = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			int index2 = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object @object = this._sequenceStart.GetObject();
			object object2 = this._sequenceEnd.GetObject();
			if (@object != object2)
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				return ((ReadOnlySequenceSegment<T>)object2).RunningIndex + (long)index2 - (readOnlySequenceSegment.RunningIndex + (long)index);
			}
			return (long)(index2 - index);
		}

		// Token: 0x060001ED RID: 493 RVA: 0x0000AF48 File Offset: 0x00009148
		internal bool TryGetReadOnlySequenceSegment([Nullable(new byte[] { 2, 1 })] out ReadOnlySequenceSegment<T> startSegment, out int startIndex, [Nullable(new byte[] { 2, 1 })] out ReadOnlySequenceSegment<T> endSegment, out int endIndex)
		{
			object @object = this._sequenceStart.GetObject();
			if (@object == null || this.GetSequenceType() != ReadOnlySequence<T>.SequenceType.MultiSegment)
			{
				startSegment = null;
				startIndex = 0;
				endSegment = null;
				endIndex = 0;
				return false;
			}
			startSegment = (ReadOnlySequenceSegment<T>)@object;
			startIndex = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			endSegment = (ReadOnlySequenceSegment<T>)this._sequenceEnd.GetObject();
			endIndex = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			return true;
		}

		// Token: 0x060001EE RID: 494 RVA: 0x0000AFB4 File Offset: 0x000091B4
		internal bool TryGetArray([Nullable(new byte[] { 0, 1 })] out ArraySegment<T> segment)
		{
			if (this.GetSequenceType() != ReadOnlySequence<T>.SequenceType.Array)
			{
				segment = default(ArraySegment<T>);
				return false;
			}
			int index = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			segment = new ArraySegment<T>((T[])this._sequenceStart.GetObject(), index, ReadOnlySequence<T>.GetIndex(this._sequenceEnd) - index);
			return true;
		}

		// Token: 0x060001EF RID: 495 RVA: 0x0000B00C File Offset: 0x0000920C
		internal bool TryGetString([MaybeNullWhen(false)] out string text, out int start, out int length)
		{
			if (typeof(T) != typeof(char) || this.GetSequenceType() != ReadOnlySequence<T>.SequenceType.String)
			{
				start = 0;
				length = 0;
				text = null;
				return false;
			}
			start = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			length = ReadOnlySequence<T>.GetIndex(this._sequenceEnd) - start;
			text = (string)this._sequenceStart.GetObject();
			return true;
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x0000B078 File Offset: 0x00009278
		private static bool InRange(uint value, uint start, uint end)
		{
			return value - start <= end - start;
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x0000B085 File Offset: 0x00009285
		private static bool InRange(ulong value, ulong start, ulong end)
		{
			return value - start <= end - start;
		}

		// Token: 0x04000059 RID: 89
		private readonly SequencePosition _sequenceStart;

		// Token: 0x0400005A RID: 90
		private readonly SequencePosition _sequenceEnd;

		// Token: 0x0400005B RID: 91
		[Nullable(new byte[] { 0, 1 })]
		public static readonly ReadOnlySequence<T> Empty = new ReadOnlySequence<T>(SpanHelpers.PerTypeValues<T>.EmptyArray);

		// Token: 0x02000063 RID: 99
		[NullableContext(0)]
		public struct Enumerator
		{
			// Token: 0x060002D2 RID: 722 RVA: 0x0000CF98 File Offset: 0x0000B198
			public Enumerator([Nullable(new byte[] { 0, 1 })] in ReadOnlySequence<T> sequence)
			{
				this._currentMemory = default(ReadOnlyMemory<T>);
				this._next = sequence.Start;
				this._sequence = sequence;
			}

			// Token: 0x17000045 RID: 69
			// (get) Token: 0x060002D3 RID: 723 RVA: 0x0000CFBE File Offset: 0x0000B1BE
			[Nullable(new byte[] { 0, 1 })]
			public ReadOnlyMemory<T> Current
			{
				[return: Nullable(new byte[] { 0, 1 })]
				get
				{
					return this._currentMemory;
				}
			}

			// Token: 0x060002D4 RID: 724 RVA: 0x0000CFC6 File Offset: 0x0000B1C6
			public bool MoveNext()
			{
				return this._next.GetObject() != null && this._sequence.TryGet(ref this._next, out this._currentMemory, true);
			}

			// Token: 0x040000B5 RID: 181
			[Nullable(new byte[] { 0, 1 })]
			private readonly ReadOnlySequence<T> _sequence;

			// Token: 0x040000B6 RID: 182
			private SequencePosition _next;

			// Token: 0x040000B7 RID: 183
			[Nullable(new byte[] { 0, 1 })]
			private ReadOnlyMemory<T> _currentMemory;
		}

		// Token: 0x02000064 RID: 100
		[NullableContext(0)]
		private enum SequenceType
		{
			// Token: 0x040000B9 RID: 185
			MultiSegment,
			// Token: 0x040000BA RID: 186
			Array,
			// Token: 0x040000BB RID: 187
			MemoryManager,
			// Token: 0x040000BC RID: 188
			String,
			// Token: 0x040000BD RID: 189
			Empty
		}
	}
}
