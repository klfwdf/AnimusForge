using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A58 RID: 2648
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class StringBuilder : ISerializable
	{
		// Token: 0x060066D7 RID: 26327 RVA: 0x0015A2EC File Offset: 0x001584EC
		[__DynamicallyInvokable]
		public StringBuilder()
			: this(16)
		{
		}

		// Token: 0x060066D8 RID: 26328 RVA: 0x0015A2F6 File Offset: 0x001584F6
		[__DynamicallyInvokable]
		public StringBuilder(int capacity)
			: this(string.Empty, capacity)
		{
		}

		// Token: 0x060066D9 RID: 26329 RVA: 0x0015A304 File Offset: 0x00158504
		[__DynamicallyInvokable]
		public StringBuilder(string value)
			: this(value, 16)
		{
		}

		// Token: 0x060066DA RID: 26330 RVA: 0x0015A30F File Offset: 0x0015850F
		[__DynamicallyInvokable]
		public StringBuilder(string value, int capacity)
			: this(value, 0, (value != null) ? value.Length : 0, capacity)
		{
		}

		// Token: 0x060066DB RID: 26331 RVA: 0x0015A328 File Offset: 0x00158528
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe StringBuilder(string value, int startIndex, int length, int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", new object[] { "capacity" }));
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", new object[] { "length" }));
			}
			if (startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
			}
			if (value == null)
			{
				value = string.Empty;
			}
			if (startIndex > value.Length - length)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
			}
			this.m_MaxCapacity = int.MaxValue;
			if (capacity == 0)
			{
				capacity = 16;
			}
			if (capacity < length)
			{
				capacity = length;
			}
			this.m_ChunkChars = new char[capacity];
			this.m_ChunkLength = length;
			fixed (string text = value)
			{
				char* ptr = text;
				if (ptr != null)
				{
					ptr += RuntimeHelpers.OffsetToStringData / 2;
				}
				StringBuilder.ThreadSafeCopy(ptr + startIndex, this.m_ChunkChars, 0, length);
			}
		}

		// Token: 0x060066DC RID: 26332 RVA: 0x0015A424 File Offset: 0x00158624
		[__DynamicallyInvokable]
		public StringBuilder(int capacity, int maxCapacity)
		{
			if (capacity > maxCapacity)
			{
				throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
			}
			if (maxCapacity < 1)
			{
				throw new ArgumentOutOfRangeException("maxCapacity", Environment.GetResourceString("ArgumentOutOfRange_SmallMaxCapacity"));
			}
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", new object[] { "capacity" }));
			}
			if (capacity == 0)
			{
				capacity = Math.Min(16, maxCapacity);
			}
			this.m_MaxCapacity = maxCapacity;
			this.m_ChunkChars = new char[capacity];
		}

		// Token: 0x060066DD RID: 26333 RVA: 0x0015A4B0 File Offset: 0x001586B0
		[SecurityCritical]
		private StringBuilder(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			int num = 0;
			string text = null;
			int num2 = int.MaxValue;
			bool flag = false;
			SerializationInfoEnumerator enumerator = info.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string name = enumerator.Name;
				if (!(name == "m_MaxCapacity"))
				{
					if (!(name == "m_StringValue"))
					{
						if (name == "Capacity")
						{
							num = info.GetInt32("Capacity");
							flag = true;
						}
					}
					else
					{
						text = info.GetString("m_StringValue");
					}
				}
				else
				{
					num2 = info.GetInt32("m_MaxCapacity");
				}
			}
			if (text == null)
			{
				text = string.Empty;
			}
			if (num2 < 1 || text.Length > num2)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_StringBuilderMaxCapacity"));
			}
			if (!flag)
			{
				num = 16;
				if (num < text.Length)
				{
					num = text.Length;
				}
				if (num > num2)
				{
					num = num2;
				}
			}
			if (num < 0 || num < text.Length || num > num2)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_StringBuilderCapacity"));
			}
			this.m_MaxCapacity = num2;
			this.m_ChunkChars = new char[num];
			text.CopyTo(0, this.m_ChunkChars, 0, text.Length);
			this.m_ChunkLength = text.Length;
			this.m_ChunkPrevious = null;
		}

		// Token: 0x060066DE RID: 26334 RVA: 0x0015A5EC File Offset: 0x001587EC
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.AddValue("m_MaxCapacity", this.m_MaxCapacity);
			info.AddValue("Capacity", this.Capacity);
			info.AddValue("m_StringValue", this.ToString());
			info.AddValue("m_currentThread", 0);
		}

		// Token: 0x060066DF RID: 26335 RVA: 0x0015A648 File Offset: 0x00158848
		[Conditional("_DEBUG")]
		private void VerifyClassInvariant()
		{
			StringBuilder stringBuilder = this;
			int maxCapacity = this.m_MaxCapacity;
			for (;;)
			{
				StringBuilder chunkPrevious = stringBuilder.m_ChunkPrevious;
				if (chunkPrevious == null)
				{
					break;
				}
				stringBuilder = chunkPrevious;
			}
		}

		// Token: 0x17001194 RID: 4500
		// (get) Token: 0x060066E0 RID: 26336 RVA: 0x0015A66C File Offset: 0x0015886C
		// (set) Token: 0x060066E1 RID: 26337 RVA: 0x0015A680 File Offset: 0x00158880
		[__DynamicallyInvokable]
		public int Capacity
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_ChunkChars.Length + this.m_ChunkOffset;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
				}
				if (value > this.MaxCapacity)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
				}
				if (value < this.Length)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
				}
				if (this.Capacity != value)
				{
					int num = value - this.m_ChunkOffset;
					char[] array = new char[num];
					Array.Copy(this.m_ChunkChars, array, this.m_ChunkLength);
					this.m_ChunkChars = array;
				}
			}
		}

		// Token: 0x17001195 RID: 4501
		// (get) Token: 0x060066E2 RID: 26338 RVA: 0x0015A714 File Offset: 0x00158914
		[__DynamicallyInvokable]
		public int MaxCapacity
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_MaxCapacity;
			}
		}

		// Token: 0x060066E3 RID: 26339 RVA: 0x0015A71C File Offset: 0x0015891C
		[__DynamicallyInvokable]
		public int EnsureCapacity(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
			}
			if (this.Capacity < capacity)
			{
				this.Capacity = capacity;
			}
			return this.Capacity;
		}

		// Token: 0x060066E4 RID: 26340 RVA: 0x0015A750 File Offset: 0x00158950
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe override string ToString()
		{
			if (this.Length == 0)
			{
				return string.Empty;
			}
			string text = string.FastAllocateString(this.Length);
			StringBuilder stringBuilder = this;
			fixed (string text2 = text)
			{
				char* ptr = text2;
				if (ptr != null)
				{
					ptr += RuntimeHelpers.OffsetToStringData / 2;
				}
				for (;;)
				{
					if (stringBuilder.m_ChunkLength > 0)
					{
						char[] chunkChars = stringBuilder.m_ChunkChars;
						int chunkOffset = stringBuilder.m_ChunkOffset;
						int chunkLength = stringBuilder.m_ChunkLength;
						if ((ulong)(chunkLength + chunkOffset) > (ulong)((long)text.Length) || chunkLength > chunkChars.Length)
						{
							break;
						}
						char[] array;
						char* smem;
						if ((array = chunkChars) == null || array.Length == 0)
						{
							smem = null;
						}
						else
						{
							smem = &array[0];
						}
						string.wstrcpy(ptr + chunkOffset, smem, chunkLength);
						array = null;
					}
					stringBuilder = stringBuilder.m_ChunkPrevious;
					if (stringBuilder == null)
					{
						goto Block_7;
					}
				}
				throw new ArgumentOutOfRangeException("chunkLength", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				Block_7:;
			}
			return text;
		}

		// Token: 0x060066E5 RID: 26341 RVA: 0x0015A81C File Offset: 0x00158A1C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe string ToString(int startIndex, int length)
		{
			int length2 = this.Length;
			if (startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
			}
			if (startIndex > length2)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndexLargerThanLength"));
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
			}
			if (startIndex > length2 - length)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
			}
			StringBuilder stringBuilder = this;
			int num = startIndex + length;
			string text = string.FastAllocateString(length);
			int i = length;
			fixed (string text2 = text)
			{
				char* ptr = text2;
				if (ptr != null)
				{
					ptr += RuntimeHelpers.OffsetToStringData / 2;
				}
				while (i > 0)
				{
					int num2 = num - stringBuilder.m_ChunkOffset;
					if (num2 >= 0)
					{
						if (num2 > stringBuilder.m_ChunkLength)
						{
							num2 = stringBuilder.m_ChunkLength;
						}
						int num3 = i;
						int num4 = num3;
						int num5 = num2 - num3;
						if (num5 < 0)
						{
							num4 += num5;
							num5 = 0;
						}
						i -= num4;
						if (num4 > 0)
						{
							char[] chunkChars = stringBuilder.m_ChunkChars;
							if ((ulong)(num4 + i) > (ulong)((long)length) || num4 + num5 > chunkChars.Length)
							{
								throw new ArgumentOutOfRangeException("chunkCount", Environment.GetResourceString("ArgumentOutOfRange_Index"));
							}
							fixed (char* ptr2 = &chunkChars[num5])
							{
								char* smem = ptr2;
								string.wstrcpy(ptr + i, smem, num4);
							}
						}
					}
					stringBuilder = stringBuilder.m_ChunkPrevious;
				}
			}
			return text;
		}

		// Token: 0x060066E6 RID: 26342 RVA: 0x0015A97E File Offset: 0x00158B7E
		[__DynamicallyInvokable]
		public StringBuilder Clear()
		{
			this.Length = 0;
			return this;
		}

		// Token: 0x17001196 RID: 4502
		// (get) Token: 0x060066E7 RID: 26343 RVA: 0x0015A988 File Offset: 0x00158B88
		// (set) Token: 0x060066E8 RID: 26344 RVA: 0x0015A998 File Offset: 0x00158B98
		[__DynamicallyInvokable]
		public int Length
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_ChunkOffset + this.m_ChunkLength;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
				}
				if (value > this.MaxCapacity)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
				}
				int capacity = this.Capacity;
				if (value == 0 && this.m_ChunkPrevious == null)
				{
					this.m_ChunkLength = 0;
					this.m_ChunkOffset = 0;
					return;
				}
				int num = value - this.Length;
				if (num > 0)
				{
					this.Append('\0', num);
					return;
				}
				StringBuilder stringBuilder = this.FindChunkForIndex(value);
				if (stringBuilder != this)
				{
					int num2 = capacity - stringBuilder.m_ChunkOffset;
					char[] array = new char[num2];
					Array.Copy(stringBuilder.m_ChunkChars, array, stringBuilder.m_ChunkLength);
					this.m_ChunkChars = array;
					this.m_ChunkPrevious = stringBuilder.m_ChunkPrevious;
					this.m_ChunkOffset = stringBuilder.m_ChunkOffset;
				}
				this.m_ChunkLength = value - stringBuilder.m_ChunkOffset;
			}
		}

		// Token: 0x17001197 RID: 4503
		[__DynamicallyInvokable]
		[IndexerName("Chars")]
		public char this[int index]
		{
			[__DynamicallyInvokable]
			get
			{
				StringBuilder stringBuilder = this;
				int num;
				for (;;)
				{
					num = index - stringBuilder.m_ChunkOffset;
					if (num >= 0)
					{
						break;
					}
					stringBuilder = stringBuilder.m_ChunkPrevious;
					if (stringBuilder == null)
					{
						goto Block_3;
					}
				}
				if (num >= stringBuilder.m_ChunkLength)
				{
					throw new IndexOutOfRangeException();
				}
				return stringBuilder.m_ChunkChars[num];
				Block_3:
				throw new IndexOutOfRangeException();
			}
			[__DynamicallyInvokable]
			set
			{
				StringBuilder stringBuilder = this;
				int num;
				for (;;)
				{
					num = index - stringBuilder.m_ChunkOffset;
					if (num >= 0)
					{
						break;
					}
					stringBuilder = stringBuilder.m_ChunkPrevious;
					if (stringBuilder == null)
					{
						goto Block_3;
					}
				}
				if (num >= stringBuilder.m_ChunkLength)
				{
					throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				stringBuilder.m_ChunkChars[num] = value;
				return;
				Block_3:
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
		}

		// Token: 0x060066EB RID: 26347 RVA: 0x0015AB1C File Offset: 0x00158D1C
		[__DynamicallyInvokable]
		public StringBuilder Append(char value, int repeatCount)
		{
			if (repeatCount < 0)
			{
				throw new ArgumentOutOfRangeException("repeatCount", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
			}
			if (repeatCount == 0)
			{
				return this;
			}
			int num = this.m_ChunkLength;
			while (repeatCount > 0)
			{
				if (num < this.m_ChunkChars.Length)
				{
					this.m_ChunkChars[num++] = value;
					repeatCount--;
				}
				else
				{
					this.m_ChunkLength = num;
					this.ExpandByABlock(repeatCount);
					num = 0;
				}
			}
			this.m_ChunkLength = num;
			return this;
		}

		// Token: 0x060066EC RID: 26348 RVA: 0x0015AB8C File Offset: 0x00158D8C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe StringBuilder Append(char[] value, int startIndex, int charCount)
		{
			if (startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (charCount < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && charCount == 0)
			{
				return this;
			}
			if (value == null)
			{
				if (startIndex == 0 && charCount == 0)
				{
					return this;
				}
				throw new ArgumentNullException("value");
			}
			else
			{
				if (charCount > value.Length - startIndex)
				{
					throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (charCount == 0)
				{
					return this;
				}
				fixed (char* ptr = &value[startIndex])
				{
					char* value2 = ptr;
					this.Append(value2, charCount);
				}
				return this;
			}
		}

		// Token: 0x060066ED RID: 26349 RVA: 0x0015AC28 File Offset: 0x00158E28
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe StringBuilder Append(string value)
		{
			if (value != null)
			{
				char[] chunkChars = this.m_ChunkChars;
				int chunkLength = this.m_ChunkLength;
				int length = value.Length;
				int num = chunkLength + length;
				if (num < chunkChars.Length)
				{
					if (length <= 2)
					{
						if (length > 0)
						{
							chunkChars[chunkLength] = value[0];
						}
						if (length > 1)
						{
							chunkChars[chunkLength + 1] = value[1];
						}
					}
					else
					{
						fixed (string text = value)
						{
							char* ptr = text;
							if (ptr != null)
							{
								ptr += RuntimeHelpers.OffsetToStringData / 2;
							}
							fixed (char* ptr2 = &chunkChars[chunkLength])
							{
								char* dmem = ptr2;
								string.wstrcpy(dmem, ptr, length);
							}
						}
					}
					this.m_ChunkLength = num;
				}
				else
				{
					this.AppendHelper(value);
				}
			}
			return this;
		}

		// Token: 0x060066EE RID: 26350 RVA: 0x0015ACC4 File Offset: 0x00158EC4
		[SecuritySafeCritical]
		private unsafe void AppendHelper(string value)
		{
			fixed (string text = value)
			{
				char* ptr = text;
				if (ptr != null)
				{
					ptr += RuntimeHelpers.OffsetToStringData / 2;
				}
				this.Append(ptr, value.Length);
			}
		}

		// Token: 0x060066EF RID: 26351
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe extern void ReplaceBufferInternal(char* newBuffer, int newLength);

		// Token: 0x060066F0 RID: 26352
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe extern void ReplaceBufferAnsiInternal(sbyte* newBuffer, int newLength);

		// Token: 0x060066F1 RID: 26353 RVA: 0x0015ACF4 File Offset: 0x00158EF4
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe StringBuilder Append(string value, int startIndex, int count)
		{
			if (startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && count == 0)
			{
				return this;
			}
			if (value == null)
			{
				if (startIndex == 0 && count == 0)
				{
					return this;
				}
				throw new ArgumentNullException("value");
			}
			else
			{
				if (count == 0)
				{
					return this;
				}
				if (startIndex > value.Length - count)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				fixed (string text = value)
				{
					char* ptr = text;
					if (ptr != null)
					{
						ptr += RuntimeHelpers.OffsetToStringData / 2;
					}
					this.Append(ptr + startIndex, count);
				}
				return this;
			}
		}

		// Token: 0x060066F2 RID: 26354 RVA: 0x0015AD9B File Offset: 0x00158F9B
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public StringBuilder AppendLine()
		{
			return this.Append(Environment.NewLine);
		}

		// Token: 0x060066F3 RID: 26355 RVA: 0x0015ADA8 File Offset: 0x00158FA8
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public StringBuilder AppendLine(string value)
		{
			this.Append(value);
			return this.Append(Environment.NewLine);
		}

		// Token: 0x060066F4 RID: 26356 RVA: 0x0015ADC0 File Offset: 0x00158FC0
		[ComVisible(false)]
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
		{
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("Arg_NegativeArgCount"));
			}
			if (destinationIndex < 0)
			{
				throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", new object[] { "destinationIndex" }));
			}
			if (destinationIndex > destination.Length - count)
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_OffsetOut"));
			}
			if (sourceIndex > this.Length)
			{
				throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (sourceIndex > this.Length - count)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_LongerThanSrcString"));
			}
			StringBuilder stringBuilder = this;
			int num = sourceIndex + count;
			int num2 = destinationIndex + count;
			while (count > 0)
			{
				int num3 = num - stringBuilder.m_ChunkOffset;
				if (num3 >= 0)
				{
					if (num3 > stringBuilder.m_ChunkLength)
					{
						num3 = stringBuilder.m_ChunkLength;
					}
					int num4 = count;
					int num5 = num3 - count;
					if (num5 < 0)
					{
						num4 += num5;
						num5 = 0;
					}
					num2 -= num4;
					count -= num4;
					StringBuilder.ThreadSafeCopy(stringBuilder.m_ChunkChars, num5, destination, num2, num4);
				}
				stringBuilder = stringBuilder.m_ChunkPrevious;
			}
		}

		// Token: 0x060066F5 RID: 26357 RVA: 0x0015AEDC File Offset: 0x001590DC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe StringBuilder Insert(int index, string value, int count)
		{
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			int length = this.Length;
			if (index > length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (value == null || value.Length == 0 || count == 0)
			{
				return this;
			}
			long num = (long)value.Length * (long)count;
			if (num > (long)(this.MaxCapacity - this.Length))
			{
				throw new OutOfMemoryException();
			}
			StringBuilder stringBuilder;
			int num2;
			this.MakeRoom(index, (int)num, out stringBuilder, out num2, false);
			fixed (string text = value)
			{
				char* ptr = text;
				if (ptr != null)
				{
					ptr += RuntimeHelpers.OffsetToStringData / 2;
				}
				while (count > 0)
				{
					this.ReplaceInPlaceAtChunk(ref stringBuilder, ref num2, ptr, value.Length);
					count--;
				}
			}
			return this;
		}

		// Token: 0x060066F6 RID: 26358 RVA: 0x0015AF9C File Offset: 0x0015919C
		[__DynamicallyInvokable]
		public StringBuilder Remove(int startIndex, int length)
		{
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
			}
			if (startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
			}
			if (length > this.Length - startIndex)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (this.Length == length && startIndex == 0)
			{
				this.Length = 0;
				return this;
			}
			if (length > 0)
			{
				StringBuilder stringBuilder;
				int num;
				this.Remove(startIndex, length, out stringBuilder, out num);
			}
			return this;
		}

		// Token: 0x060066F7 RID: 26359 RVA: 0x0015B021 File Offset: 0x00159221
		[__DynamicallyInvokable]
		public StringBuilder Append(bool value)
		{
			return this.Append(value.ToString());
		}

		// Token: 0x060066F8 RID: 26360 RVA: 0x0015B030 File Offset: 0x00159230
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public StringBuilder Append(sbyte value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x060066F9 RID: 26361 RVA: 0x0015B044 File Offset: 0x00159244
		[__DynamicallyInvokable]
		public StringBuilder Append(byte value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x060066FA RID: 26362 RVA: 0x0015B058 File Offset: 0x00159258
		[__DynamicallyInvokable]
		public StringBuilder Append(char value)
		{
			if (this.m_ChunkLength < this.m_ChunkChars.Length)
			{
				char[] chunkChars = this.m_ChunkChars;
				int chunkLength = this.m_ChunkLength;
				this.m_ChunkLength = chunkLength + 1;
				chunkChars[chunkLength] = value;
			}
			else
			{
				this.Append(value, 1);
			}
			return this;
		}

		// Token: 0x060066FB RID: 26363 RVA: 0x0015B09A File Offset: 0x0015929A
		[__DynamicallyInvokable]
		public StringBuilder Append(short value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x060066FC RID: 26364 RVA: 0x0015B0AE File Offset: 0x001592AE
		[__DynamicallyInvokable]
		public StringBuilder Append(int value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x060066FD RID: 26365 RVA: 0x0015B0C2 File Offset: 0x001592C2
		[__DynamicallyInvokable]
		public StringBuilder Append(long value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x060066FE RID: 26366 RVA: 0x0015B0D6 File Offset: 0x001592D6
		[__DynamicallyInvokable]
		public StringBuilder Append(float value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x060066FF RID: 26367 RVA: 0x0015B0EA File Offset: 0x001592EA
		[__DynamicallyInvokable]
		public StringBuilder Append(double value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x06006700 RID: 26368 RVA: 0x0015B0FE File Offset: 0x001592FE
		[__DynamicallyInvokable]
		public StringBuilder Append(decimal value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x06006701 RID: 26369 RVA: 0x0015B112 File Offset: 0x00159312
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public StringBuilder Append(ushort value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x06006702 RID: 26370 RVA: 0x0015B126 File Offset: 0x00159326
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public StringBuilder Append(uint value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x06006703 RID: 26371 RVA: 0x0015B13A File Offset: 0x0015933A
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public StringBuilder Append(ulong value)
		{
			return this.Append(value.ToString(CultureInfo.CurrentCulture));
		}

		// Token: 0x06006704 RID: 26372 RVA: 0x0015B14E File Offset: 0x0015934E
		[__DynamicallyInvokable]
		public StringBuilder Append(object value)
		{
			if (value == null)
			{
				return this;
			}
			return this.Append(value.ToString());
		}

		// Token: 0x06006705 RID: 26373 RVA: 0x0015B164 File Offset: 0x00159364
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe StringBuilder Append(char[] value)
		{
			if (value != null && value.Length != 0)
			{
				fixed (char* ptr = &value[0])
				{
					char* value2 = ptr;
					this.Append(value2, value.Length);
				}
			}
			return this;
		}

		// Token: 0x06006706 RID: 26374 RVA: 0x0015B194 File Offset: 0x00159394
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe StringBuilder Insert(int index, string value)
		{
			if (index > this.Length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (value != null)
			{
				fixed (string text = value)
				{
					char* ptr = text;
					if (ptr != null)
					{
						ptr += RuntimeHelpers.OffsetToStringData / 2;
					}
					this.Insert(index, ptr, value.Length);
				}
			}
			return this;
		}

		// Token: 0x06006707 RID: 26375 RVA: 0x0015B1E3 File Offset: 0x001593E3
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, bool value)
		{
			return this.Insert(index, value.ToString(), 1);
		}

		// Token: 0x06006708 RID: 26376 RVA: 0x0015B1F4 File Offset: 0x001593F4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, sbyte value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x06006709 RID: 26377 RVA: 0x0015B20A File Offset: 0x0015940A
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, byte value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x0600670A RID: 26378 RVA: 0x0015B220 File Offset: 0x00159420
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, short value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x0600670B RID: 26379 RVA: 0x0015B236 File Offset: 0x00159436
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe StringBuilder Insert(int index, char value)
		{
			this.Insert(index, &value, 1);
			return this;
		}

		// Token: 0x0600670C RID: 26380 RVA: 0x0015B244 File Offset: 0x00159444
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, char[] value)
		{
			if (index > this.Length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (value != null)
			{
				this.Insert(index, value, 0, value.Length);
			}
			return this;
		}

		// Token: 0x0600670D RID: 26381 RVA: 0x0015B278 File Offset: 0x00159478
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe StringBuilder Insert(int index, char[] value, int startIndex, int charCount)
		{
			int length = this.Length;
			if (index > length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (value == null)
			{
				if (startIndex == 0 && charCount == 0)
				{
					return this;
				}
				throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_String"));
			}
			else
			{
				if (startIndex < 0)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
				}
				if (charCount < 0)
				{
					throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
				}
				if (startIndex > value.Length - charCount)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (charCount > 0)
				{
					fixed (char* ptr = &value[startIndex])
					{
						char* value2 = ptr;
						this.Insert(index, value2, charCount);
					}
				}
				return this;
			}
		}

		// Token: 0x0600670E RID: 26382 RVA: 0x0015B330 File Offset: 0x00159530
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, int value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x0600670F RID: 26383 RVA: 0x0015B346 File Offset: 0x00159546
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, long value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x06006710 RID: 26384 RVA: 0x0015B35C File Offset: 0x0015955C
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, float value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x06006711 RID: 26385 RVA: 0x0015B372 File Offset: 0x00159572
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, double value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x06006712 RID: 26386 RVA: 0x0015B388 File Offset: 0x00159588
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, decimal value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x06006713 RID: 26387 RVA: 0x0015B39E File Offset: 0x0015959E
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, ushort value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x06006714 RID: 26388 RVA: 0x0015B3B4 File Offset: 0x001595B4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, uint value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x06006715 RID: 26389 RVA: 0x0015B3CA File Offset: 0x001595CA
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, ulong value)
		{
			return this.Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
		}

		// Token: 0x06006716 RID: 26390 RVA: 0x0015B3E0 File Offset: 0x001595E0
		[__DynamicallyInvokable]
		public StringBuilder Insert(int index, object value)
		{
			if (value == null)
			{
				return this;
			}
			return this.Insert(index, value.ToString(), 1);
		}

		// Token: 0x06006717 RID: 26391 RVA: 0x0015B3F5 File Offset: 0x001595F5
		[__DynamicallyInvokable]
		public StringBuilder AppendFormat(string format, object arg0)
		{
			return this.AppendFormatHelper(null, format, new ParamsArray(arg0));
		}

		// Token: 0x06006718 RID: 26392 RVA: 0x0015B405 File Offset: 0x00159605
		[__DynamicallyInvokable]
		public StringBuilder AppendFormat(string format, object arg0, object arg1)
		{
			return this.AppendFormatHelper(null, format, new ParamsArray(arg0, arg1));
		}

		// Token: 0x06006719 RID: 26393 RVA: 0x0015B416 File Offset: 0x00159616
		[__DynamicallyInvokable]
		public StringBuilder AppendFormat(string format, object arg0, object arg1, object arg2)
		{
			return this.AppendFormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));
		}

		// Token: 0x0600671A RID: 26394 RVA: 0x0015B429 File Offset: 0x00159629
		[__DynamicallyInvokable]
		public StringBuilder AppendFormat(string format, params object[] args)
		{
			if (args == null)
			{
				throw new ArgumentNullException((format == null) ? "format" : "args");
			}
			return this.AppendFormatHelper(null, format, new ParamsArray(args));
		}

		// Token: 0x0600671B RID: 26395 RVA: 0x0015B451 File Offset: 0x00159651
		[__DynamicallyInvokable]
		public StringBuilder AppendFormat(IFormatProvider provider, string format, object arg0)
		{
			return this.AppendFormatHelper(provider, format, new ParamsArray(arg0));
		}

		// Token: 0x0600671C RID: 26396 RVA: 0x0015B461 File Offset: 0x00159661
		[__DynamicallyInvokable]
		public StringBuilder AppendFormat(IFormatProvider provider, string format, object arg0, object arg1)
		{
			return this.AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1));
		}

		// Token: 0x0600671D RID: 26397 RVA: 0x0015B473 File Offset: 0x00159673
		[__DynamicallyInvokable]
		public StringBuilder AppendFormat(IFormatProvider provider, string format, object arg0, object arg1, object arg2)
		{
			return this.AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));
		}

		// Token: 0x0600671E RID: 26398 RVA: 0x0015B487 File Offset: 0x00159687
		[__DynamicallyInvokable]
		public StringBuilder AppendFormat(IFormatProvider provider, string format, params object[] args)
		{
			if (args == null)
			{
				throw new ArgumentNullException((format == null) ? "format" : "args");
			}
			return this.AppendFormatHelper(provider, format, new ParamsArray(args));
		}

		// Token: 0x0600671F RID: 26399 RVA: 0x0015B4AF File Offset: 0x001596AF
		private static void FormatError()
		{
			throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
		}

		// Token: 0x06006720 RID: 26400 RVA: 0x0015B4C0 File Offset: 0x001596C0
		internal StringBuilder AppendFormatHelper(IFormatProvider provider, string format, ParamsArray args)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			int i = 0;
			int length = format.Length;
			char c = '\0';
			ICustomFormatter customFormatter = null;
			if (provider != null)
			{
				customFormatter = (ICustomFormatter)provider.GetFormat(typeof(ICustomFormatter));
			}
			for (;;)
			{
				while (i < length)
				{
					c = format[i];
					i++;
					if (c == '}')
					{
						if (i < length && format[i] == '}')
						{
							i++;
						}
						else
						{
							StringBuilder.FormatError();
						}
					}
					if (c == '{')
					{
						if (i >= length || format[i] != '{')
						{
							i--;
							break;
						}
						i++;
					}
					this.Append(c);
				}
				if (i == length)
				{
					return this;
				}
				i++;
				if (i == length || (c = format[i]) < '0' || c > '9')
				{
					StringBuilder.FormatError();
				}
				int num = 0;
				do
				{
					num = num * 10 + (int)c - 48;
					i++;
					if (i == length)
					{
						StringBuilder.FormatError();
					}
					c = format[i];
				}
				while (c >= '0' && c <= '9' && num < 1000000);
				if (num >= args.Length)
				{
					break;
				}
				while (i < length && (c = format[i]) == ' ')
				{
					i++;
				}
				bool flag = false;
				int num2 = 0;
				if (c == ',')
				{
					i++;
					while (i < length && format[i] == ' ')
					{
						i++;
					}
					if (i == length)
					{
						StringBuilder.FormatError();
					}
					c = format[i];
					if (c == '-')
					{
						flag = true;
						i++;
						if (i == length)
						{
							StringBuilder.FormatError();
						}
						c = format[i];
					}
					if (c < '0' || c > '9')
					{
						StringBuilder.FormatError();
					}
					do
					{
						num2 = num2 * 10 + (int)c - 48;
						i++;
						if (i == length)
						{
							StringBuilder.FormatError();
						}
						c = format[i];
						if (c < '0' || c > '9')
						{
							break;
						}
					}
					while (num2 < 1000000);
				}
				while (i < length && (c = format[i]) == ' ')
				{
					i++;
				}
				object obj = args[num];
				StringBuilder stringBuilder = null;
				if (c == ':')
				{
					i++;
					for (;;)
					{
						if (i == length)
						{
							StringBuilder.FormatError();
						}
						c = format[i];
						i++;
						if (c == '{')
						{
							if (i < length && format[i] == '{')
							{
								i++;
							}
							else
							{
								StringBuilder.FormatError();
							}
						}
						else if (c == '}')
						{
							if (i >= length || format[i] != '}')
							{
								break;
							}
							i++;
						}
						if (stringBuilder == null)
						{
							stringBuilder = new StringBuilder();
						}
						stringBuilder.Append(c);
					}
					i--;
				}
				if (c != '}')
				{
					StringBuilder.FormatError();
				}
				i++;
				string text = null;
				string text2 = null;
				if (customFormatter != null)
				{
					if (stringBuilder != null)
					{
						text = stringBuilder.ToString();
					}
					text2 = customFormatter.Format(text, obj, provider);
				}
				if (text2 == null)
				{
					IFormattable formattable = obj as IFormattable;
					if (formattable != null)
					{
						if (text == null && stringBuilder != null)
						{
							text = stringBuilder.ToString();
						}
						text2 = formattable.ToString(text, provider);
					}
					else if (obj != null)
					{
						text2 = obj.ToString();
					}
				}
				if (text2 == null)
				{
					text2 = string.Empty;
				}
				int num3 = num2 - text2.Length;
				if (!flag && num3 > 0)
				{
					this.Append(' ', num3);
				}
				this.Append(text2);
				if (flag && num3 > 0)
				{
					this.Append(' ', num3);
				}
			}
			throw new FormatException(Environment.GetResourceString("Format_IndexOutOfRange"));
		}

		// Token: 0x06006721 RID: 26401 RVA: 0x0015B7E7 File Offset: 0x001599E7
		[__DynamicallyInvokable]
		public StringBuilder Replace(string oldValue, string newValue)
		{
			return this.Replace(oldValue, newValue, 0, this.Length);
		}

		// Token: 0x06006722 RID: 26402 RVA: 0x0015B7F8 File Offset: 0x001599F8
		[__DynamicallyInvokable]
		public bool Equals(StringBuilder sb)
		{
			if (sb == null)
			{
				return false;
			}
			if (this.Capacity != sb.Capacity || this.MaxCapacity != sb.MaxCapacity || this.Length != sb.Length)
			{
				return false;
			}
			if (sb == this)
			{
				return true;
			}
			StringBuilder stringBuilder = this;
			int i = stringBuilder.m_ChunkLength;
			StringBuilder stringBuilder2 = sb;
			int j = stringBuilder2.m_ChunkLength;
			for (;;)
			{
				IL_49:
				i--;
				j--;
				while (i < 0)
				{
					stringBuilder = stringBuilder.m_ChunkPrevious;
					if (stringBuilder != null)
					{
						i = stringBuilder.m_ChunkLength + i;
					}
					else
					{
						IL_7F:
						while (j < 0)
						{
							stringBuilder2 = stringBuilder2.m_ChunkPrevious;
							if (stringBuilder2 == null)
							{
								break;
							}
							j = stringBuilder2.m_ChunkLength + j;
						}
						if (i < 0)
						{
							goto Block_8;
						}
						if (j < 0)
						{
							return false;
						}
						if (stringBuilder.m_ChunkChars[i] != stringBuilder2.m_ChunkChars[j])
						{
							return false;
						}
						goto IL_49;
					}
				}
				goto IL_7F;
			}
			Block_8:
			return j < 0;
		}

		// Token: 0x06006723 RID: 26403 RVA: 0x0015B8AC File Offset: 0x00159AAC
		[__DynamicallyInvokable]
		public StringBuilder Replace(string oldValue, string newValue, int startIndex, int count)
		{
			int length = this.Length;
			if (startIndex > length)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (count < 0 || startIndex > length - count)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (oldValue == null)
			{
				throw new ArgumentNullException("oldValue");
			}
			if (oldValue.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "oldValue");
			}
			if (newValue == null)
			{
				newValue = "";
			}
			int num = newValue.Length - oldValue.Length;
			int[] array = null;
			int num2 = 0;
			StringBuilder stringBuilder = this.FindChunkForIndex(startIndex);
			int num3 = startIndex - stringBuilder.m_ChunkOffset;
			while (count > 0)
			{
				if (this.StartsWith(stringBuilder, num3, count, oldValue))
				{
					if (array == null)
					{
						array = new int[5];
					}
					else if (num2 >= array.Length)
					{
						int[] array2 = new int[array.Length * 3 / 2 + 4];
						Array.Copy(array, array2, array.Length);
						array = array2;
					}
					array[num2++] = num3;
					num3 += oldValue.Length;
					count -= oldValue.Length;
				}
				else
				{
					num3++;
					count--;
				}
				if (num3 >= stringBuilder.m_ChunkLength || count == 0)
				{
					int num4 = num3 + stringBuilder.m_ChunkOffset;
					this.ReplaceAllInChunk(array, num2, stringBuilder, oldValue.Length, newValue);
					num4 += (newValue.Length - oldValue.Length) * num2;
					num2 = 0;
					stringBuilder = this.FindChunkForIndex(num4);
					num3 = num4 - stringBuilder.m_ChunkOffset;
				}
			}
			return this;
		}

		// Token: 0x06006724 RID: 26404 RVA: 0x0015BA2B File Offset: 0x00159C2B
		[__DynamicallyInvokable]
		public StringBuilder Replace(char oldChar, char newChar)
		{
			return this.Replace(oldChar, newChar, 0, this.Length);
		}

		// Token: 0x06006725 RID: 26405 RVA: 0x0015BA3C File Offset: 0x00159C3C
		[__DynamicallyInvokable]
		public StringBuilder Replace(char oldChar, char newChar, int startIndex, int count)
		{
			int length = this.Length;
			if (startIndex > length)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (count < 0 || startIndex > length - count)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			int num = startIndex + count;
			StringBuilder stringBuilder = this;
			for (;;)
			{
				int num2 = num - stringBuilder.m_ChunkOffset;
				int num3 = startIndex - stringBuilder.m_ChunkOffset;
				if (num2 >= 0)
				{
					int i = Math.Max(num3, 0);
					int num4 = Math.Min(stringBuilder.m_ChunkLength, num2);
					while (i < num4)
					{
						if (stringBuilder.m_ChunkChars[i] == oldChar)
						{
							stringBuilder.m_ChunkChars[i] = newChar;
						}
						i++;
					}
				}
				if (num3 >= 0)
				{
					break;
				}
				stringBuilder = stringBuilder.m_ChunkPrevious;
			}
			return this;
		}

		// Token: 0x06006726 RID: 26406 RVA: 0x0015BAF4 File Offset: 0x00159CF4
		[SecurityCritical]
		[CLSCompliant(false)]
		public unsafe StringBuilder Append(char* value, int valueCount)
		{
			if (valueCount < 0)
			{
				throw new ArgumentOutOfRangeException("valueCount", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
			}
			int num = valueCount + this.m_ChunkLength;
			if (num <= this.m_ChunkChars.Length)
			{
				StringBuilder.ThreadSafeCopy(value, this.m_ChunkChars, this.m_ChunkLength, valueCount);
				this.m_ChunkLength = num;
			}
			else
			{
				int num2 = this.m_ChunkChars.Length - this.m_ChunkLength;
				if (num2 > 0)
				{
					StringBuilder.ThreadSafeCopy(value, this.m_ChunkChars, this.m_ChunkLength, num2);
					this.m_ChunkLength = this.m_ChunkChars.Length;
				}
				int num3 = valueCount - num2;
				this.ExpandByABlock(num3);
				StringBuilder.ThreadSafeCopy(value + num2, this.m_ChunkChars, 0, num3);
				this.m_ChunkLength = num3;
			}
			return this;
		}

		// Token: 0x06006727 RID: 26407 RVA: 0x0015BBA8 File Offset: 0x00159DA8
		[SecurityCritical]
		private unsafe void Insert(int index, char* value, int valueCount)
		{
			if (index > this.Length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (valueCount > 0)
			{
				StringBuilder stringBuilder;
				int num;
				this.MakeRoom(index, valueCount, out stringBuilder, out num, false);
				this.ReplaceInPlaceAtChunk(ref stringBuilder, ref num, value, valueCount);
			}
		}

		// Token: 0x06006728 RID: 26408 RVA: 0x0015BBF0 File Offset: 0x00159DF0
		[SecuritySafeCritical]
		private unsafe void ReplaceAllInChunk(int[] replacements, int replacementsCount, StringBuilder sourceChunk, int removeCount, string value)
		{
			if (replacementsCount <= 0)
			{
				return;
			}
			fixed (string text = value)
			{
				char* ptr = text;
				if (ptr != null)
				{
					ptr += RuntimeHelpers.OffsetToStringData / 2;
				}
				int num = (value.Length - removeCount) * replacementsCount;
				StringBuilder stringBuilder = sourceChunk;
				int num2 = replacements[0];
				if (num > 0)
				{
					this.MakeRoom(stringBuilder.m_ChunkOffset + num2, num, out stringBuilder, out num2, true);
				}
				int num3 = 0;
				for (;;)
				{
					this.ReplaceInPlaceAtChunk(ref stringBuilder, ref num2, ptr, value.Length);
					int num4 = replacements[num3] + removeCount;
					num3++;
					if (num3 >= replacementsCount)
					{
						break;
					}
					int num5 = replacements[num3];
					if (num != 0)
					{
						fixed (char* ptr2 = &sourceChunk.m_ChunkChars[num4])
						{
							char* value2 = ptr2;
							this.ReplaceInPlaceAtChunk(ref stringBuilder, ref num2, value2, num5 - num4);
						}
					}
					else
					{
						num2 += num5 - num4;
					}
				}
				if (num < 0)
				{
					this.Remove(stringBuilder.m_ChunkOffset + num2, -num, out stringBuilder, out num2);
				}
			}
		}

		// Token: 0x06006729 RID: 26409 RVA: 0x0015BCC4 File Offset: 0x00159EC4
		private bool StartsWith(StringBuilder chunk, int indexInChunk, int count, string value)
		{
			for (int i = 0; i < value.Length; i++)
			{
				if (count == 0)
				{
					return false;
				}
				if (indexInChunk >= chunk.m_ChunkLength)
				{
					chunk = this.Next(chunk);
					if (chunk == null)
					{
						return false;
					}
					indexInChunk = 0;
				}
				if (value[i] != chunk.m_ChunkChars[indexInChunk])
				{
					return false;
				}
				indexInChunk++;
				count--;
			}
			return true;
		}

		// Token: 0x0600672A RID: 26410 RVA: 0x0015BD24 File Offset: 0x00159F24
		[SecurityCritical]
		private unsafe void ReplaceInPlaceAtChunk(ref StringBuilder chunk, ref int indexInChunk, char* value, int count)
		{
			if (count != 0)
			{
				for (;;)
				{
					int val = chunk.m_ChunkLength - indexInChunk;
					int num = Math.Min(val, count);
					StringBuilder.ThreadSafeCopy(value, chunk.m_ChunkChars, indexInChunk, num);
					indexInChunk += num;
					if (indexInChunk >= chunk.m_ChunkLength)
					{
						chunk = this.Next(chunk);
						indexInChunk = 0;
					}
					count -= num;
					if (count == 0)
					{
						break;
					}
					value += num;
				}
			}
		}

		// Token: 0x0600672B RID: 26411 RVA: 0x0015BD8C File Offset: 0x00159F8C
		[SecurityCritical]
		private unsafe static void ThreadSafeCopy(char* sourcePtr, char[] destination, int destinationIndex, int count)
		{
			if (count <= 0)
			{
				return;
			}
			if (destinationIndex <= destination.Length && destinationIndex + count <= destination.Length)
			{
				fixed (char* ptr = &destination[destinationIndex])
				{
					char* dmem = ptr;
					string.wstrcpy(dmem, sourcePtr, count);
				}
				return;
			}
			throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
		}

		// Token: 0x0600672C RID: 26412 RVA: 0x0015BDD8 File Offset: 0x00159FD8
		[SecurityCritical]
		private unsafe static void ThreadSafeCopy(char[] source, int sourceIndex, char[] destination, int destinationIndex, int count)
		{
			if (count <= 0)
			{
				return;
			}
			if (sourceIndex <= source.Length && sourceIndex + count <= source.Length)
			{
				fixed (char* ptr = &source[sourceIndex])
				{
					char* sourcePtr = ptr;
					StringBuilder.ThreadSafeCopy(sourcePtr, destination, destinationIndex, count);
				}
				return;
			}
			throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
		}

		// Token: 0x0600672D RID: 26413 RVA: 0x0015BE28 File Offset: 0x0015A028
		[SecurityCritical]
		internal unsafe void InternalCopy(IntPtr dest, int len)
		{
			if (len == 0)
			{
				return;
			}
			bool flag = true;
			byte* ptr = (byte*)dest.ToPointer();
			StringBuilder stringBuilder = this.FindChunkForByte(len);
			do
			{
				int num = stringBuilder.m_ChunkOffset * 2;
				int len2 = stringBuilder.m_ChunkLength * 2;
				fixed (char* ptr2 = &stringBuilder.m_ChunkChars[0])
				{
					char* ptr3 = ptr2;
					byte* src = (byte*)ptr3;
					if (flag)
					{
						flag = false;
						Buffer.Memcpy(ptr + num, src, len - num);
					}
					else
					{
						Buffer.Memcpy(ptr + num, src, len2);
					}
				}
				stringBuilder = stringBuilder.m_ChunkPrevious;
			}
			while (stringBuilder != null);
		}

		// Token: 0x0600672E RID: 26414 RVA: 0x0015BEA4 File Offset: 0x0015A0A4
		private StringBuilder FindChunkForIndex(int index)
		{
			StringBuilder stringBuilder = this;
			while (stringBuilder.m_ChunkOffset > index)
			{
				stringBuilder = stringBuilder.m_ChunkPrevious;
			}
			return stringBuilder;
		}

		// Token: 0x0600672F RID: 26415 RVA: 0x0015BEC8 File Offset: 0x0015A0C8
		private StringBuilder FindChunkForByte(int byteIndex)
		{
			StringBuilder stringBuilder = this;
			while (stringBuilder.m_ChunkOffset * 2 > byteIndex)
			{
				stringBuilder = stringBuilder.m_ChunkPrevious;
			}
			return stringBuilder;
		}

		// Token: 0x06006730 RID: 26416 RVA: 0x0015BEEC File Offset: 0x0015A0EC
		private StringBuilder Next(StringBuilder chunk)
		{
			if (chunk == this)
			{
				return null;
			}
			return this.FindChunkForIndex(chunk.m_ChunkOffset + chunk.m_ChunkLength);
		}

		// Token: 0x06006731 RID: 26417 RVA: 0x0015BF08 File Offset: 0x0015A108
		private void ExpandByABlock(int minBlockCharCount)
		{
			if (minBlockCharCount + this.Length < minBlockCharCount || minBlockCharCount + this.Length > this.m_MaxCapacity)
			{
				throw new ArgumentOutOfRangeException("requiredLength", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
			}
			int num = Math.Max(minBlockCharCount, Math.Min(this.Length, 8000));
			this.m_ChunkPrevious = new StringBuilder(this);
			this.m_ChunkOffset += this.m_ChunkLength;
			this.m_ChunkLength = 0;
			if (this.m_ChunkOffset + num < num)
			{
				this.m_ChunkChars = null;
				throw new OutOfMemoryException();
			}
			this.m_ChunkChars = new char[num];
		}

		// Token: 0x06006732 RID: 26418 RVA: 0x0015BFA8 File Offset: 0x0015A1A8
		private StringBuilder(StringBuilder from)
		{
			this.m_ChunkLength = from.m_ChunkLength;
			this.m_ChunkOffset = from.m_ChunkOffset;
			this.m_ChunkChars = from.m_ChunkChars;
			this.m_ChunkPrevious = from.m_ChunkPrevious;
			this.m_MaxCapacity = from.m_MaxCapacity;
		}

		// Token: 0x06006733 RID: 26419 RVA: 0x0015BFF8 File Offset: 0x0015A1F8
		[SecuritySafeCritical]
		private unsafe void MakeRoom(int index, int count, out StringBuilder chunk, out int indexInChunk, bool doneMoveFollowingChars)
		{
			if (count + this.Length < count || count + this.Length > this.m_MaxCapacity)
			{
				throw new ArgumentOutOfRangeException("requiredLength", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
			}
			chunk = this;
			while (chunk.m_ChunkOffset > index)
			{
				chunk.m_ChunkOffset += count;
				chunk = chunk.m_ChunkPrevious;
			}
			indexInChunk = index - chunk.m_ChunkOffset;
			if (!doneMoveFollowingChars && chunk.m_ChunkLength <= 32 && chunk.m_ChunkChars.Length - chunk.m_ChunkLength >= count)
			{
				int i = chunk.m_ChunkLength;
				while (i > indexInChunk)
				{
					i--;
					chunk.m_ChunkChars[i + count] = chunk.m_ChunkChars[i];
				}
				chunk.m_ChunkLength += count;
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(Math.Max(count, 16), chunk.m_MaxCapacity, chunk.m_ChunkPrevious);
			stringBuilder.m_ChunkLength = count;
			int num = Math.Min(count, indexInChunk);
			if (num > 0)
			{
				char[] array;
				char* ptr;
				if ((array = chunk.m_ChunkChars) == null || array.Length == 0)
				{
					ptr = null;
				}
				else
				{
					ptr = &array[0];
				}
				StringBuilder.ThreadSafeCopy(ptr, stringBuilder.m_ChunkChars, 0, num);
				int num2 = indexInChunk - num;
				if (num2 >= 0)
				{
					StringBuilder.ThreadSafeCopy(ptr + num, chunk.m_ChunkChars, 0, num2);
					indexInChunk = num2;
				}
				array = null;
			}
			chunk.m_ChunkPrevious = stringBuilder;
			chunk.m_ChunkOffset += count;
			if (num < count)
			{
				chunk = stringBuilder;
				indexInChunk = num;
			}
		}

		// Token: 0x06006734 RID: 26420 RVA: 0x0015C16E File Offset: 0x0015A36E
		private StringBuilder(int size, int maxCapacity, StringBuilder previousBlock)
		{
			this.m_ChunkChars = new char[size];
			this.m_MaxCapacity = maxCapacity;
			this.m_ChunkPrevious = previousBlock;
			if (previousBlock != null)
			{
				this.m_ChunkOffset = previousBlock.m_ChunkOffset + previousBlock.m_ChunkLength;
			}
		}

		// Token: 0x06006735 RID: 26421 RVA: 0x0015C1A8 File Offset: 0x0015A3A8
		[SecuritySafeCritical]
		private void Remove(int startIndex, int count, out StringBuilder chunk, out int indexInChunk)
		{
			int num = startIndex + count;
			chunk = this;
			StringBuilder stringBuilder = null;
			int num2 = 0;
			for (;;)
			{
				if (num - chunk.m_ChunkOffset >= 0)
				{
					if (stringBuilder == null)
					{
						stringBuilder = chunk;
						num2 = num - stringBuilder.m_ChunkOffset;
					}
					if (startIndex - chunk.m_ChunkOffset >= 0)
					{
						break;
					}
				}
				else
				{
					chunk.m_ChunkOffset -= count;
				}
				chunk = chunk.m_ChunkPrevious;
			}
			indexInChunk = startIndex - chunk.m_ChunkOffset;
			int num3 = indexInChunk;
			int count2 = stringBuilder.m_ChunkLength - num2;
			if (stringBuilder != chunk)
			{
				num3 = 0;
				chunk.m_ChunkLength = indexInChunk;
				stringBuilder.m_ChunkPrevious = chunk;
				stringBuilder.m_ChunkOffset = chunk.m_ChunkOffset + chunk.m_ChunkLength;
				if (indexInChunk == 0)
				{
					stringBuilder.m_ChunkPrevious = chunk.m_ChunkPrevious;
					chunk = stringBuilder;
				}
			}
			stringBuilder.m_ChunkLength -= num2 - num3;
			if (num3 != num2)
			{
				StringBuilder.ThreadSafeCopy(stringBuilder.m_ChunkChars, num2, stringBuilder.m_ChunkChars, num3, count2);
			}
		}

		// Token: 0x04002E1D RID: 11805
		internal char[] m_ChunkChars;

		// Token: 0x04002E1E RID: 11806
		internal StringBuilder m_ChunkPrevious;

		// Token: 0x04002E1F RID: 11807
		internal int m_ChunkLength;

		// Token: 0x04002E20 RID: 11808
		internal int m_ChunkOffset;

		// Token: 0x04002E21 RID: 11809
		internal int m_MaxCapacity;

		// Token: 0x04002E22 RID: 11810
		internal const int DefaultCapacity = 16;

		// Token: 0x04002E23 RID: 11811
		private const string CapacityField = "Capacity";

		// Token: 0x04002E24 RID: 11812
		private const string MaxCapacityField = "m_MaxCapacity";

		// Token: 0x04002E25 RID: 11813
		private const string StringValueField = "m_StringValue";

		// Token: 0x04002E26 RID: 11814
		private const string ThreadIDField = "m_currentThread";

		// Token: 0x04002E27 RID: 11815
		internal const int MaxChunkSize = 8000;
	}
}
