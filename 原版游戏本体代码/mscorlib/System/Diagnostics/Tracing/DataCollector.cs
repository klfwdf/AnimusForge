using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200043D RID: 1085
	[SecurityCritical]
	internal struct DataCollector
	{
		// Token: 0x060035E3 RID: 13795 RVA: 0x000D1E78 File Offset: 0x000D0078
		internal unsafe void Enable(byte* scratch, int scratchSize, EventSource.EventData* datas, int dataCount, GCHandle* pins, int pinCount)
		{
			this.datasStart = datas;
			this.scratchEnd = scratch + scratchSize;
			this.datasEnd = datas + dataCount;
			this.pinsEnd = pins + pinCount;
			this.scratch = scratch;
			this.datas = datas;
			this.pins = pins;
			this.writingScalars = false;
		}

		// Token: 0x060035E4 RID: 13796 RVA: 0x000D1ED7 File Offset: 0x000D00D7
		internal void Disable()
		{
			this = default(DataCollector);
		}

		// Token: 0x060035E5 RID: 13797 RVA: 0x000D1EE0 File Offset: 0x000D00E0
		internal unsafe EventSource.EventData* Finish()
		{
			this.ScalarsEnd();
			return this.datas;
		}

		// Token: 0x060035E6 RID: 13798 RVA: 0x000D1EF0 File Offset: 0x000D00F0
		internal unsafe void AddScalar(void* value, int size)
		{
			if (this.bufferNesting != 0)
			{
				int num = this.bufferPos;
				int num2;
				checked
				{
					this.bufferPos += size;
					this.EnsureBuffer();
					num2 = 0;
				}
				while (num2 != size)
				{
					this.buffer[num] = ((byte*)value)[num2];
					num2++;
					num++;
				}
				return;
			}
			byte* ptr = this.scratch;
			byte* ptr2 = ptr + size;
			if (this.scratchEnd < ptr2)
			{
				throw new IndexOutOfRangeException(Environment.GetResourceString("EventSource_AddScalarOutOfRange"));
			}
			this.ScalarsBegin();
			this.scratch = ptr2;
			for (int num3 = 0; num3 != size; num3++)
			{
				ptr[num3] = ((byte*)value)[num3];
			}
		}

		// Token: 0x060035E7 RID: 13799 RVA: 0x000D1F90 File Offset: 0x000D0190
		internal unsafe void AddBinary(string value, int size)
		{
			if (size > 65535)
			{
				size = 65534;
			}
			if (this.bufferNesting != 0)
			{
				this.EnsureBuffer(size + 2);
			}
			this.AddScalar((void*)(&size), 2);
			if (size != 0)
			{
				if (this.bufferNesting == 0)
				{
					this.ScalarsEnd();
					this.PinArray(value, size);
					return;
				}
				int startIndex = this.bufferPos;
				checked
				{
					this.bufferPos += size;
					this.EnsureBuffer();
				}
				fixed (string text = value)
				{
					void* ptr = text;
					if (ptr != null)
					{
						ptr = (void*)((byte*)ptr + RuntimeHelpers.OffsetToStringData);
					}
					Marshal.Copy((IntPtr)ptr, this.buffer, startIndex, size);
				}
			}
		}

		// Token: 0x060035E8 RID: 13800 RVA: 0x000D2021 File Offset: 0x000D0221
		internal void AddBinary(Array value, int size)
		{
			this.AddArray(value, size, 1);
		}

		// Token: 0x060035E9 RID: 13801 RVA: 0x000D202C File Offset: 0x000D022C
		internal unsafe void AddArray(Array value, int length, int itemSize)
		{
			if (length > 65535)
			{
				length = 65535;
			}
			int num = length * itemSize;
			if (this.bufferNesting != 0)
			{
				this.EnsureBuffer(num + 2);
			}
			this.AddScalar((void*)(&length), 2);
			checked
			{
				if (length != 0)
				{
					if (this.bufferNesting == 0)
					{
						this.ScalarsEnd();
						this.PinArray(value, num);
						return;
					}
					int dstOffset = this.bufferPos;
					this.bufferPos += num;
					this.EnsureBuffer();
					Buffer.BlockCopy(value, 0, this.buffer, dstOffset, num);
				}
			}
		}

		// Token: 0x060035EA RID: 13802 RVA: 0x000D20AB File Offset: 0x000D02AB
		internal int BeginBufferedArray()
		{
			this.BeginBuffered();
			this.bufferPos += 2;
			return this.bufferPos;
		}

		// Token: 0x060035EB RID: 13803 RVA: 0x000D20C7 File Offset: 0x000D02C7
		internal void EndBufferedArray(int bookmark, int count)
		{
			this.EnsureBuffer();
			this.buffer[bookmark - 2] = (byte)count;
			this.buffer[bookmark - 1] = (byte)(count >> 8);
			this.EndBuffered();
		}

		// Token: 0x060035EC RID: 13804 RVA: 0x000D20EF File Offset: 0x000D02EF
		internal void BeginBuffered()
		{
			this.ScalarsEnd();
			this.bufferNesting++;
		}

		// Token: 0x060035ED RID: 13805 RVA: 0x000D2105 File Offset: 0x000D0305
		internal void EndBuffered()
		{
			this.bufferNesting--;
			if (this.bufferNesting == 0)
			{
				this.EnsureBuffer();
				this.PinArray(this.buffer, this.bufferPos);
				this.buffer = null;
				this.bufferPos = 0;
			}
		}

		// Token: 0x060035EE RID: 13806 RVA: 0x000D2144 File Offset: 0x000D0344
		private void EnsureBuffer()
		{
			int num = this.bufferPos;
			if (this.buffer == null || this.buffer.Length < num)
			{
				this.GrowBuffer(num);
			}
		}

		// Token: 0x060035EF RID: 13807 RVA: 0x000D2174 File Offset: 0x000D0374
		private void EnsureBuffer(int additionalSize)
		{
			int num = this.bufferPos + additionalSize;
			if (this.buffer == null || this.buffer.Length < num)
			{
				this.GrowBuffer(num);
			}
		}

		// Token: 0x060035F0 RID: 13808 RVA: 0x000D21A4 File Offset: 0x000D03A4
		private void GrowBuffer(int required)
		{
			int num = ((this.buffer == null) ? 64 : this.buffer.Length);
			do
			{
				num *= 2;
			}
			while (num < required);
			Array.Resize<byte>(ref this.buffer, num);
		}

		// Token: 0x060035F1 RID: 13809 RVA: 0x000D21DC File Offset: 0x000D03DC
		private unsafe void PinArray(object value, int size)
		{
			GCHandle* ptr = this.pins;
			if (this.pinsEnd == ptr)
			{
				throw new IndexOutOfRangeException(Environment.GetResourceString("EventSource_PinArrayOutOfRange"));
			}
			EventSource.EventData* ptr2 = this.datas;
			if (this.datasEnd == ptr2)
			{
				throw new IndexOutOfRangeException(Environment.GetResourceString("EventSource_DataDescriptorsOutOfRange"));
			}
			this.pins = ptr + 1;
			this.datas = ptr2 + 1;
			*ptr = GCHandle.Alloc(value, GCHandleType.Pinned);
			ptr2->DataPointer = ptr->AddrOfPinnedObject();
			ptr2->m_Size = size;
		}

		// Token: 0x060035F2 RID: 13810 RVA: 0x000D2268 File Offset: 0x000D0468
		private unsafe void ScalarsBegin()
		{
			if (!this.writingScalars)
			{
				EventSource.EventData* ptr = this.datas;
				if (this.datasEnd == ptr)
				{
					throw new IndexOutOfRangeException(Environment.GetResourceString("EventSource_DataDescriptorsOutOfRange"));
				}
				ptr->DataPointer = (IntPtr)((void*)this.scratch);
				this.writingScalars = true;
			}
		}

		// Token: 0x060035F3 RID: 13811 RVA: 0x000D22B8 File Offset: 0x000D04B8
		private unsafe void ScalarsEnd()
		{
			if (this.writingScalars)
			{
				EventSource.EventData* ptr = this.datas;
				ptr->m_Size = (this.scratch - checked((UIntPtr)ptr->m_Ptr)) / 1;
				this.datas = ptr + 1;
				this.writingScalars = false;
			}
		}

		// Token: 0x04001810 RID: 6160
		[ThreadStatic]
		internal static DataCollector ThreadInstance;

		// Token: 0x04001811 RID: 6161
		private unsafe byte* scratchEnd;

		// Token: 0x04001812 RID: 6162
		private unsafe EventSource.EventData* datasEnd;

		// Token: 0x04001813 RID: 6163
		private unsafe GCHandle* pinsEnd;

		// Token: 0x04001814 RID: 6164
		private unsafe EventSource.EventData* datasStart;

		// Token: 0x04001815 RID: 6165
		private unsafe byte* scratch;

		// Token: 0x04001816 RID: 6166
		private unsafe EventSource.EventData* datas;

		// Token: 0x04001817 RID: 6167
		private unsafe GCHandle* pins;

		// Token: 0x04001818 RID: 6168
		private byte[] buffer;

		// Token: 0x04001819 RID: 6169
		private int bufferPos;

		// Token: 0x0400181A RID: 6170
		private int bufferNesting;

		// Token: 0x0400181B RID: 6171
		private bool writingScalars;
	}
}
