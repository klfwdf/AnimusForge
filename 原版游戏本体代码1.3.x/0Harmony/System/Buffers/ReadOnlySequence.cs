using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
	// Token: 0x02000496 RID: 1174
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(ReadOnlySequenceDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	internal readonly struct ReadOnlySequence<[Nullable(2)] T>
	{
		// Token: 0x170005C4 RID: 1476
		// (get) Token: 0x06001A26 RID: 6694 RVA: 0x00055395 File Offset: 0x00053595
		public long Length
		{
			get
			{
				return this.GetLength();
			}
		}

		// Token: 0x170005C5 RID: 1477
		// (get) Token: 0x06001A27 RID: 6695 RVA: 0x0005539D File Offset: 0x0005359D
		public bool IsEmpty
		{
			get
			{
				return this.Length == 0L;
			}
		}

		// Token: 0x170005C6 RID: 1478
		// (get) Token: 0x06001A28 RID: 6696 RVA: 0x000553A9 File Offset: 0x000535A9
		public bool IsSingleSegment
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._sequenceStart.GetObject() == this._sequenceEnd.GetObject();
			}
		}

		// Token: 0x170005C7 RID: 1479
		// (get) Token: 0x06001A29 RID: 6697 RVA: 0x000553C3 File Offset: 0x000535C3
		[Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<T> First
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return this.GetFirstBuffer();
			}
		}

		// Token: 0x170005C8 RID: 1480
		// (get) Token: 0x06001A2A RID: 6698 RVA: 0x000553CB File Offset: 0x000535CB
		public SequencePosition Start
		{
			get
			{
				return this._sequenceStart;
			}
		}

		// Token: 0x170005C9 RID: 1481
		// (get) Token: 0x06001A2B RID: 6699 RVA: 0x000553D3 File Offset: 0x000535D3
		public SequencePosition End
		{
			get
			{
				return this._sequenceEnd;
			}
		}

		// Token: 0x06001A2C RID: 6700 RVA: 0x000553DB File Offset: 0x000535DB
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ReadOnlySequence(object startSegment, int startIndexAndFlags, object endSegment, int endIndexAndFlags)
		{
			this._sequenceStart = new SequencePosition(startSegment, startIndexAndFlags);
			this._sequenceEnd = new SequencePosition(endSegment, endIndexAndFlags);
		}

		// Token: 0x06001A2D RID: 6701 RVA: 0x000553F8 File Offset: 0x000535F8
		public ReadOnlySequence(ReadOnlySequenceSegment<T> startSegment, int startIndex, ReadOnlySequenceSegment<T> endSegment, int endIndex)
		{
			if (startSegment == null || endSegment == null || (startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex) || startSegment.Memory.Length < startIndex || endSegment.Memory.Length < endIndex || (startSegment == endSegment && endIndex < startIndex))
			{
				ThrowHelper.ThrowArgumentValidationException<T>(startSegment, startIndex, endSegment);
			}
			this._sequenceStart = new SequencePosition(startSegment, ReadOnlySequence.SegmentToSequenceStart(startIndex));
			this._sequenceEnd = new SequencePosition(endSegment, ReadOnlySequence.SegmentToSequenceEnd(endIndex));
		}

		// Token: 0x06001A2E RID: 6702 RVA: 0x00055476 File Offset: 0x00053676
		public ReadOnlySequence(T[] array)
		{
			ThrowHelper.ThrowIfArgumentNull(array, ExceptionArgument.array);
			this._sequenceStart = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceStart(0));
			this._sequenceEnd = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceEnd(array.Length));
		}

		// Token: 0x06001A2F RID: 6703 RVA: 0x000554A6 File Offset: 0x000536A6
		public ReadOnlySequence(T[] array, int start, int length)
		{
			if (array == null || start > array.Length || length > array.Length - start)
			{
				ThrowHelper.ThrowArgumentValidationException(array, start);
			}
			this._sequenceStart = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceStart(start));
			this._sequenceEnd = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceEnd(start + length));
		}

		// Token: 0x06001A30 RID: 6704 RVA: 0x000554E8 File Offset: 0x000536E8
		public unsafe ReadOnlySequence([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> memory)
		{
			MemoryManager<T> manager;
			int index;
			int length;
			if (MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out manager, out index, out length))
			{
				this._sequenceStart = new SequencePosition(manager, ReadOnlySequence.MemoryManagerToSequenceStart(index));
				this._sequenceEnd = new SequencePosition(manager, ReadOnlySequence.MemoryManagerToSequenceEnd(length));
				return;
			}
			ArraySegment<T> segment;
			if (MemoryMarshal.TryGetArray<T>(memory, out segment))
			{
				T[] array = segment.Array;
				int start = segment.Offset;
				this._sequenceStart = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceStart(start));
				this._sequenceEnd = new SequencePosition(array, ReadOnlySequence.ArrayToSequenceEnd(start + segment.Count));
				return;
			}
			if (typeof(T) == typeof(char))
			{
				string text;
				int start2;
				if (!MemoryMarshal.TryGetString(*Unsafe.As<ReadOnlyMemory<T>, ReadOnlyMemory<char>>(ref memory), out text, out start2, out length))
				{
					ThrowHelper.ThrowInvalidOperationException();
				}
				this._sequenceStart = new SequencePosition(text, ReadOnlySequence.StringToSequenceStart(start2));
				this._sequenceEnd = new SequencePosition(text, ReadOnlySequence.StringToSequenceEnd(start2 + length));
				return;
			}
			ThrowHelper.ThrowInvalidOperationException();
			this._sequenceStart = default(SequencePosition);
			this._sequenceEnd = default(SequencePosition);
		}

		// Token: 0x06001A31 RID: 6705 RVA: 0x000555F8 File Offset: 0x000537F8
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(long start, long length)
		{
			if (start < 0L || length < 0L)
			{
				ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
			}
			int startIndex = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			int endIndex = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object startObject = this._sequenceStart.GetObject();
			object endObject = this._sequenceEnd.GetObject();
			SequencePosition begin;
			SequencePosition end;
			if (startObject != endObject)
			{
				ReadOnlySequenceSegment<T> startSegment = (ReadOnlySequenceSegment<T>)startObject;
				int currentLength = startSegment.Memory.Length - startIndex;
				if ((long)currentLength > start)
				{
					startIndex += (int)start;
					begin = new SequencePosition(startObject, startIndex);
					end = ReadOnlySequence<T>.GetEndPosition(startSegment, startObject, startIndex, endObject, endIndex, length);
				}
				else
				{
					if (currentLength < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					begin = ReadOnlySequence<T>.SeekMultiSegment(startSegment.Next, endObject, endIndex, start - (long)currentLength, ExceptionArgument.start);
					int beginIndex = ReadOnlySequence<T>.GetIndex(begin);
					object beginObject = begin.GetObject();
					if (beginObject != endObject)
					{
						end = ReadOnlySequence<T>.GetEndPosition((ReadOnlySequenceSegment<T>)beginObject, beginObject, beginIndex, endObject, endIndex, length);
					}
					else
					{
						if ((long)(endIndex - beginIndex) < length)
						{
							ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
						}
						end = new SequencePosition(beginObject, beginIndex + (int)length);
					}
				}
			}
			else
			{
				if ((long)(endIndex - startIndex) < start)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(-1L);
				}
				startIndex += (int)start;
				begin = new SequencePosition(startObject, startIndex);
				if ((long)(endIndex - startIndex) < length)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
				end = new SequencePosition(startObject, startIndex + (int)length);
			}
			return this.SliceImpl(begin, end);
		}

		// Token: 0x06001A32 RID: 6706 RVA: 0x00055748 File Offset: 0x00053948
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(long start, SequencePosition end)
		{
			if (start < 0L)
			{
				ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
			}
			uint sliceEndIndex = (uint)ReadOnlySequence<T>.GetIndex(end);
			object sliceEndObject = end.GetObject();
			uint startIndex = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			object startObject = this._sequenceStart.GetObject();
			uint endIndex = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object endObject = this._sequenceEnd.GetObject();
			if (startObject == endObject)
			{
				if (!ReadOnlySequence<T>.InRange(sliceEndIndex, startIndex, endIndex))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if ((ulong)(sliceEndIndex - startIndex) < (ulong)start)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(-1L);
				}
			}
			else
			{
				ReadOnlySequenceSegment<T> startSegment = (ReadOnlySequenceSegment<T>)startObject;
				ulong startRange = (ulong)(startSegment.RunningIndex + (long)((ulong)startIndex));
				ulong sliceRange = (ulong)(((ReadOnlySequenceSegment<T>)sliceEndObject).RunningIndex + (long)((ulong)sliceEndIndex));
				if (!ReadOnlySequence<T>.InRange(sliceRange, startRange, (ulong)(((ReadOnlySequenceSegment<T>)endObject).RunningIndex + (long)((ulong)endIndex))))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (startRange + (ulong)start > sliceRange)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				int currentLength = startSegment.Memory.Length - (int)startIndex;
				if ((long)currentLength <= start)
				{
					if (currentLength < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					SequencePosition begin = ReadOnlySequence<T>.SeekMultiSegment(startSegment.Next, sliceEndObject, (int)sliceEndIndex, start - (long)currentLength, ExceptionArgument.start);
					return this.SliceImpl(begin, end);
				}
			}
			SequencePosition sequencePosition = new SequencePosition(startObject, (int)(startIndex + (uint)((int)start)));
			return this.SliceImpl(sequencePosition, end);
		}

		// Token: 0x06001A33 RID: 6707 RVA: 0x0005587C File Offset: 0x00053A7C
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(SequencePosition start, long length)
		{
			uint sliceStartIndex = (uint)ReadOnlySequence<T>.GetIndex(start);
			object sliceStartObject = start.GetObject();
			uint startIndex = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			object startObject = this._sequenceStart.GetObject();
			uint endIndex = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object endObject = this._sequenceEnd.GetObject();
			if (startObject == endObject)
			{
				if (!ReadOnlySequence<T>.InRange(sliceStartIndex, startIndex, endIndex))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (length < 0L)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
				if ((ulong)(endIndex - sliceStartIndex) < (ulong)length)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
			}
			else
			{
				ReadOnlySequenceSegment<T> sliceStartSegment = (ReadOnlySequenceSegment<T>)sliceStartObject;
				long num = sliceStartSegment.RunningIndex + (long)((ulong)sliceStartIndex);
				ulong startRange = (ulong)(((ReadOnlySequenceSegment<T>)startObject).RunningIndex + (long)((ulong)startIndex));
				ulong endRange = (ulong)(((ReadOnlySequenceSegment<T>)endObject).RunningIndex + (long)((ulong)endIndex));
				if (!ReadOnlySequence<T>.InRange((ulong)num, startRange, endRange))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (length < 0L)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
				if (num + length > (long)endRange)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
				}
				int currentLength = sliceStartSegment.Memory.Length - (int)sliceStartIndex;
				if ((long)currentLength < length)
				{
					if (currentLength < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					SequencePosition end = ReadOnlySequence<T>.SeekMultiSegment(sliceStartSegment.Next, endObject, (int)endIndex, length - (long)currentLength, ExceptionArgument.length);
					return this.SliceImpl(start, end);
				}
			}
			SequencePosition sequencePosition = new SequencePosition(sliceStartObject, (int)(sliceStartIndex + (uint)((int)length)));
			return this.SliceImpl(start, sequencePosition);
		}

		// Token: 0x06001A34 RID: 6708 RVA: 0x000559BD File Offset: 0x00053BBD
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(int start, int length)
		{
			return this.Slice((long)start, (long)length);
		}

		// Token: 0x06001A35 RID: 6709 RVA: 0x000559C9 File Offset: 0x00053BC9
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(int start, SequencePosition end)
		{
			return this.Slice((long)start, end);
		}

		// Token: 0x06001A36 RID: 6710 RVA: 0x000559D4 File Offset: 0x00053BD4
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(SequencePosition start, int length)
		{
			return this.Slice(start, (long)length);
		}

		// Token: 0x06001A37 RID: 6711 RVA: 0x000559DF File Offset: 0x00053BDF
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(SequencePosition start, SequencePosition end)
		{
			this.BoundsCheck((uint)ReadOnlySequence<T>.GetIndex(start), start.GetObject(), (uint)ReadOnlySequence<T>.GetIndex(end), end.GetObject());
			return this.SliceImpl(start, end);
		}

		// Token: 0x06001A38 RID: 6712 RVA: 0x00055A0D File Offset: 0x00053C0D
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		public ReadOnlySequence<T> Slice(SequencePosition start)
		{
			this.BoundsCheck(start);
			return this.SliceImpl(start, this._sequenceEnd);
		}

		// Token: 0x06001A39 RID: 6713 RVA: 0x00055A28 File Offset: 0x00053C28
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
			SequencePosition begin = ReadOnlySequence<T>.Seek(this._sequenceStart, this._sequenceEnd, start, ExceptionArgument.start);
			return this.SliceImpl(begin, this._sequenceEnd);
		}

		// Token: 0x06001A3A RID: 6714 RVA: 0x00055A6C File Offset: 0x00053C6C
		public unsafe override string ToString()
		{
			if (typeof(T) == typeof(char))
			{
				ReadOnlySequence<T> localThis = this;
				ReadOnlySequence<char> charSequence = *Unsafe.As<ReadOnlySequence<T>, ReadOnlySequence<char>>(ref localThis);
				string text;
				int start;
				int length;
				if (SequenceMarshal.TryGetString(charSequence, out text, out start, out length))
				{
					return text.Substring(start, length);
				}
				if (this.Length < 2147483647L)
				{
					return new string((charSequence).ToArray<char>());
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

		// Token: 0x06001A3B RID: 6715 RVA: 0x00055B33 File Offset: 0x00053D33
		[NullableContext(0)]
		public ReadOnlySequence<T>.Enumerator GetEnumerator()
		{
			return new ReadOnlySequence<T>.Enumerator(ref this);
		}

		// Token: 0x06001A3C RID: 6716 RVA: 0x00055B3B File Offset: 0x00053D3B
		public SequencePosition GetPosition(long offset)
		{
			return this.GetPosition(offset, this._sequenceStart);
		}

		// Token: 0x06001A3D RID: 6717 RVA: 0x00055B4A File Offset: 0x00053D4A
		public SequencePosition GetPosition(long offset, SequencePosition origin)
		{
			if (offset < 0L)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();
			}
			return ReadOnlySequence<T>.Seek(origin, this._sequenceEnd, offset, ExceptionArgument.offset);
		}

		// Token: 0x06001A3E RID: 6718 RVA: 0x00055B68 File Offset: 0x00053D68
		public bool TryGet(ref SequencePosition position, [Nullable(new byte[] { 0, 1 })] out ReadOnlyMemory<T> memory, bool advance = true)
		{
			SequencePosition next;
			bool result = this.TryGetBuffer(position, out memory, out next);
			if (advance)
			{
				position = next;
			}
			return result;
		}

		// Token: 0x06001A3F RID: 6719 RVA: 0x00055B8C File Offset: 0x00053D8C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetBuffer(in SequencePosition position, [Nullable(new byte[] { 0, 1 })] out ReadOnlyMemory<T> memory, out SequencePosition next)
		{
			object positionObject = position.GetObject();
			next = default(SequencePosition);
			if (positionObject == null)
			{
				memory = default(ReadOnlyMemory<T>);
				return false;
			}
			ReadOnlySequence<T>.SequenceType type = this.GetSequenceType();
			object endObject = this._sequenceEnd.GetObject();
			int startIndex = ReadOnlySequence<T>.GetIndex(position);
			int endIndex = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			if (type == ReadOnlySequence<T>.SequenceType.MultiSegment)
			{
				ReadOnlySequenceSegment<T> startSegment = (ReadOnlySequenceSegment<T>)positionObject;
				if (startSegment != endObject)
				{
					ReadOnlySequenceSegment<T> nextSegment = startSegment.Next;
					if (nextSegment == null)
					{
						ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
					}
					next = new SequencePosition(nextSegment, 0);
					memory = startSegment.Memory.Slice(startIndex);
				}
				else
				{
					memory = startSegment.Memory.Slice(startIndex, endIndex - startIndex);
				}
			}
			else
			{
				if (positionObject != endObject)
				{
					ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
				}
				if (type == ReadOnlySequence<T>.SequenceType.Array)
				{
					memory = new ReadOnlyMemory<T>((T[])positionObject, startIndex, endIndex - startIndex);
				}
				else if (typeof(T) == typeof(char) && type == ReadOnlySequence<T>.SequenceType.String)
				{
					memory = (ReadOnlyMemory<T>)((string)positionObject).AsMemory(startIndex, endIndex - startIndex);
				}
				else
				{
					memory = ((MemoryManager<T>)positionObject).Memory.Slice(startIndex, endIndex - startIndex);
				}
			}
			return true;
		}

		// Token: 0x06001A40 RID: 6720 RVA: 0x00055CD4 File Offset: 0x00053ED4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		private ReadOnlyMemory<T> GetFirstBuffer()
		{
			object startObject = this._sequenceStart.GetObject();
			if (startObject == null)
			{
				return default(ReadOnlyMemory<T>);
			}
			int startIndex = this._sequenceStart.GetInteger();
			int endIndex = this._sequenceEnd.GetInteger();
			bool isMultiSegment = startObject != this._sequenceEnd.GetObject();
			if (startIndex >= 0)
			{
				if (endIndex < 0)
				{
					if (isMultiSegment)
					{
						ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
					}
					return new ReadOnlyMemory<T>((T[])startObject, startIndex, (endIndex & int.MaxValue) - startIndex);
				}
				ReadOnlyMemory<T> memory = ((ReadOnlySequenceSegment<T>)startObject).Memory;
				if (isMultiSegment)
				{
					return memory.Slice(startIndex);
				}
				return memory.Slice(startIndex, endIndex - startIndex);
			}
			else
			{
				if (isMultiSegment)
				{
					ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
				}
				if (typeof(T) == typeof(char) && endIndex < 0)
				{
					return (ReadOnlyMemory<T>)((string)startObject).AsMemory(startIndex & int.MaxValue, endIndex - startIndex);
				}
				startIndex &= int.MaxValue;
				return ((MemoryManager<T>)startObject).Memory.Slice(startIndex, endIndex - startIndex);
			}
		}

		// Token: 0x06001A41 RID: 6721 RVA: 0x00055DDC File Offset: 0x00053FDC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static SequencePosition Seek(in SequencePosition start, in SequencePosition end, long offset, ExceptionArgument argument)
		{
			int startIndex = ReadOnlySequence<T>.GetIndex(start);
			int endIndex = ReadOnlySequence<T>.GetIndex(end);
			object startObject = start.GetObject();
			object endObject = end.GetObject();
			if (startObject != endObject)
			{
				ReadOnlySequenceSegment<T> startSegment = (ReadOnlySequenceSegment<T>)startObject;
				int currentLength = startSegment.Memory.Length - startIndex;
				if ((long)currentLength <= offset)
				{
					if (currentLength < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					return ReadOnlySequence<T>.SeekMultiSegment(startSegment.Next, endObject, endIndex, offset - (long)currentLength, argument);
				}
			}
			else if ((long)(endIndex - startIndex) < offset)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(argument);
			}
			return new SequencePosition(startObject, startIndex + (int)offset);
		}

		// Token: 0x06001A42 RID: 6722 RVA: 0x00055E64 File Offset: 0x00054064
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static SequencePosition SeekMultiSegment([Nullable(new byte[] { 2, 1 })] ReadOnlySequenceSegment<T> currentSegment, object endObject, int endIndex, long offset, ExceptionArgument argument)
		{
			while (currentSegment != null && currentSegment != endObject)
			{
				int memoryLength = currentSegment.Memory.Length;
				if ((long)memoryLength > offset)
				{
					IL_3A:
					return new SequencePosition(currentSegment, (int)offset);
				}
				offset -= (long)memoryLength;
				currentSegment = currentSegment.Next;
			}
			if (currentSegment == null || (long)endIndex < offset)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(argument);
				goto IL_3A;
			}
			goto IL_3A;
		}

		// Token: 0x06001A43 RID: 6723 RVA: 0x00055EB4 File Offset: 0x000540B4
		private void BoundsCheck(in SequencePosition position)
		{
			uint sliceStartIndex = (uint)ReadOnlySequence<T>.GetIndex(position);
			uint startIndex = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			uint endIndex = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object startObject = this._sequenceStart.GetObject();
			object endObject = this._sequenceEnd.GetObject();
			if (startObject == endObject)
			{
				if (!ReadOnlySequence<T>.InRange(sliceStartIndex, startIndex, endIndex))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					return;
				}
			}
			else
			{
				ulong startRange = (ulong)(((ReadOnlySequenceSegment<T>)startObject).RunningIndex + (long)((ulong)startIndex));
				if (!ReadOnlySequence<T>.InRange((ulong)(((ReadOnlySequenceSegment<T>)position.GetObject()).RunningIndex + (long)((ulong)sliceStartIndex)), startRange, (ulong)(((ReadOnlySequenceSegment<T>)endObject).RunningIndex + (long)((ulong)endIndex))))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
			}
		}

		// Token: 0x06001A44 RID: 6724 RVA: 0x00055F50 File Offset: 0x00054150
		[NullableContext(2)]
		private void BoundsCheck(uint sliceStartIndex, object sliceStartObject, uint sliceEndIndex, object sliceEndObject)
		{
			uint startIndex = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			uint endIndex = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object startObject = this._sequenceStart.GetObject();
			object endObject = this._sequenceEnd.GetObject();
			if (startObject == endObject)
			{
				if (sliceStartObject != sliceEndObject || sliceStartObject != startObject || sliceStartIndex > sliceEndIndex || sliceStartIndex < startIndex || sliceEndIndex > endIndex)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					return;
				}
			}
			else
			{
				long num = ((ReadOnlySequenceSegment<T>)sliceStartObject).RunningIndex + (long)((ulong)sliceStartIndex);
				ulong sliceEndRange = (ulong)(((ReadOnlySequenceSegment<T>)sliceEndObject).RunningIndex + (long)((ulong)sliceEndIndex));
				if (num > (long)sliceEndRange)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (num < ((ReadOnlySequenceSegment<T>)startObject).RunningIndex + (long)((ulong)startIndex) || sliceEndRange > (ulong)(((ReadOnlySequenceSegment<T>)endObject).RunningIndex + (long)((ulong)endIndex)))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
			}
		}

		// Token: 0x06001A45 RID: 6725 RVA: 0x00055FFC File Offset: 0x000541FC
		private static SequencePosition GetEndPosition(ReadOnlySequenceSegment<T> startSegment, object startObject, int startIndex, object endObject, int endIndex, long length)
		{
			int currentLength = startSegment.Memory.Length - startIndex;
			if ((long)currentLength > length)
			{
				return new SequencePosition(startObject, startIndex + (int)length);
			}
			if (currentLength < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
			}
			return ReadOnlySequence<T>.SeekMultiSegment(startSegment.Next, endObject, endIndex, length - (long)currentLength, ExceptionArgument.length);
		}

		// Token: 0x06001A46 RID: 6726 RVA: 0x00056049 File Offset: 0x00054249
		[NullableContext(0)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ReadOnlySequence<T>.SequenceType GetSequenceType()
		{
			return (ReadOnlySequence<T>.SequenceType)(-(ReadOnlySequence<T>.SequenceType)(2 * (this._sequenceStart.GetInteger() >> 31) + (this._sequenceEnd.GetInteger() >> 31)));
		}

		// Token: 0x06001A47 RID: 6727 RVA: 0x0005606B File Offset: 0x0005426B
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int GetIndex(in SequencePosition position)
		{
			return position.GetInteger() & int.MaxValue;
		}

		// Token: 0x06001A48 RID: 6728 RVA: 0x0005607C File Offset: 0x0005427C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		private ReadOnlySequence<T> SliceImpl(in SequencePosition start, in SequencePosition end)
		{
			return new ReadOnlySequence<T>(start.GetObject(), ReadOnlySequence<T>.GetIndex(start) | (this._sequenceStart.GetInteger() & int.MinValue), end.GetObject(), ReadOnlySequence<T>.GetIndex(end) | (this._sequenceEnd.GetInteger() & int.MinValue));
		}

		// Token: 0x06001A49 RID: 6729 RVA: 0x000560CC File Offset: 0x000542CC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long GetLength()
		{
			int startIndex = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			int endIndex = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object startObject = this._sequenceStart.GetObject();
			object endObject = this._sequenceEnd.GetObject();
			if (startObject != endObject)
			{
				ReadOnlySequenceSegment<T> startSegment = (ReadOnlySequenceSegment<T>)startObject;
				return ((ReadOnlySequenceSegment<T>)endObject).RunningIndex + (long)endIndex - (startSegment.RunningIndex + (long)startIndex);
			}
			return (long)(endIndex - startIndex);
		}

		// Token: 0x06001A4A RID: 6730 RVA: 0x00056134 File Offset: 0x00054334
		internal bool TryGetReadOnlySequenceSegment([Nullable(new byte[] { 2, 1 })] out ReadOnlySequenceSegment<T> startSegment, out int startIndex, [Nullable(new byte[] { 2, 1 })] out ReadOnlySequenceSegment<T> endSegment, out int endIndex)
		{
			object startObject = this._sequenceStart.GetObject();
			if (startObject == null || this.GetSequenceType() != ReadOnlySequence<T>.SequenceType.MultiSegment)
			{
				startSegment = null;
				startIndex = 0;
				endSegment = null;
				endIndex = 0;
				return false;
			}
			startSegment = (ReadOnlySequenceSegment<T>)startObject;
			startIndex = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			endSegment = (ReadOnlySequenceSegment<T>)this._sequenceEnd.GetObject();
			endIndex = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			return true;
		}

		// Token: 0x06001A4B RID: 6731 RVA: 0x000561A0 File Offset: 0x000543A0
		internal bool TryGetArray([Nullable(new byte[] { 0, 1 })] out ArraySegment<T> segment)
		{
			if (this.GetSequenceType() != ReadOnlySequence<T>.SequenceType.Array)
			{
				segment = default(ArraySegment<T>);
				return false;
			}
			int startIndex = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			segment = new ArraySegment<T>((T[])this._sequenceStart.GetObject(), startIndex, ReadOnlySequence<T>.GetIndex(this._sequenceEnd) - startIndex);
			return true;
		}

		// Token: 0x06001A4C RID: 6732 RVA: 0x000561F8 File Offset: 0x000543F8
		internal bool TryGetString([<1c2fb156-e9ba-45cc-af54-d5335bdb59af>MaybeNullWhen(false)] out string text, out int start, out int length)
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

		// Token: 0x06001A4D RID: 6733 RVA: 0x00056264 File Offset: 0x00054464
		private static bool InRange(uint value, uint start, uint end)
		{
			return value - start <= end - start;
		}

		// Token: 0x06001A4E RID: 6734 RVA: 0x00056264 File Offset: 0x00054464
		private static bool InRange(ulong value, ulong start, ulong end)
		{
			return value - start <= end - start;
		}

		// Token: 0x040010DD RID: 4317
		private readonly SequencePosition _sequenceStart;

		// Token: 0x040010DE RID: 4318
		private readonly SequencePosition _sequenceEnd;

		// Token: 0x040010DF RID: 4319
		[Nullable(new byte[] { 0, 1 })]
		public static readonly ReadOnlySequence<T> Empty = new ReadOnlySequence<T>(SpanHelpers.PerTypeValues<T>.EmptyArray);

		// Token: 0x02000497 RID: 1175
		[NullableContext(0)]
		public struct Enumerator
		{
			// Token: 0x06001A50 RID: 6736 RVA: 0x00056282 File Offset: 0x00054482
			public Enumerator([Nullable(new byte[] { 0, 1 })] in ReadOnlySequence<T> sequence)
			{
				this._currentMemory = default(ReadOnlyMemory<T>);
				this._next = sequence.Start;
				this._sequence = sequence;
			}

			// Token: 0x170005CA RID: 1482
			// (get) Token: 0x06001A51 RID: 6737 RVA: 0x000562A8 File Offset: 0x000544A8
			[Nullable(new byte[] { 0, 1 })]
			public ReadOnlyMemory<T> Current
			{
				[return: Nullable(new byte[] { 0, 1 })]
				get
				{
					return this._currentMemory;
				}
			}

			// Token: 0x06001A52 RID: 6738 RVA: 0x000562B0 File Offset: 0x000544B0
			public bool MoveNext()
			{
				return this._next.GetObject() != null && this._sequence.TryGet(ref this._next, out this._currentMemory, true);
			}

			// Token: 0x040010E0 RID: 4320
			[Nullable(new byte[] { 0, 1 })]
			private readonly ReadOnlySequence<T> _sequence;

			// Token: 0x040010E1 RID: 4321
			private SequencePosition _next;

			// Token: 0x040010E2 RID: 4322
			[Nullable(new byte[] { 0, 1 })]
			private ReadOnlyMemory<T> _currentMemory;
		}

		// Token: 0x02000498 RID: 1176
		[NullableContext(0)]
		private enum SequenceType
		{
			// Token: 0x040010E4 RID: 4324
			MultiSegment,
			// Token: 0x040010E5 RID: 4325
			Array,
			// Token: 0x040010E6 RID: 4326
			MemoryManager,
			// Token: 0x040010E7 RID: 4327
			String,
			// Token: 0x040010E8 RID: 4328
			Empty
		}
	}
}
