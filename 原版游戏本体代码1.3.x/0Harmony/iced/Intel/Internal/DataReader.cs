using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.Internal
{
	// Token: 0x0200066C RID: 1644
	internal ref struct DataReader
	{
		// Token: 0x170007FD RID: 2045
		// (get) Token: 0x060023B2 RID: 9138 RVA: 0x00072E89 File Offset: 0x00071089
		// (set) Token: 0x060023B3 RID: 9139 RVA: 0x00072E91 File Offset: 0x00071091
		public int Index
		{
			readonly get
			{
				return this.index;
			}
			set
			{
				this.index = value;
			}
		}

		// Token: 0x170007FE RID: 2046
		// (get) Token: 0x060023B4 RID: 9140 RVA: 0x00072E9A File Offset: 0x0007109A
		public readonly bool CanRead
		{
			get
			{
				return this.index < this.data.Length;
			}
		}

		// Token: 0x060023B5 RID: 9141 RVA: 0x00072EAF File Offset: 0x000710AF
		public DataReader(ReadOnlySpan<byte> data)
		{
			this = new DataReader(data, 0);
		}

		// Token: 0x060023B6 RID: 9142 RVA: 0x00072EB9 File Offset: 0x000710B9
		public DataReader(ReadOnlySpan<byte> data, int maxStringLength)
		{
			this.data = data;
			this.stringData = ((maxStringLength == 0) ? Array2.Empty<char>() : new char[maxStringLength]);
			this.index = 0;
		}

		// Token: 0x060023B7 RID: 9143 RVA: 0x00072EE0 File Offset: 0x000710E0
		public unsafe byte ReadByte()
		{
			int num = this.index;
			this.index = num + 1;
			return *this.data[num];
		}

		// Token: 0x060023B8 RID: 9144 RVA: 0x00072F0C File Offset: 0x0007110C
		public uint ReadCompressedUInt32()
		{
			uint result = 0U;
			for (int shift = 0; shift < 32; shift += 7)
			{
				uint b = (uint)this.ReadByte();
				if ((b & 128U) == 0U)
				{
					return result | (b << shift);
				}
				result |= (b & 127U) << shift;
			}
			throw new InvalidOperationException();
		}

		// Token: 0x060023B9 RID: 9145 RVA: 0x00072F54 File Offset: 0x00071154
		[NullableContext(1)]
		public string ReadAsciiString()
		{
			int length = (int)this.ReadByte();
			char[] stringData = this.stringData;
			for (int i = 0; i < length; i++)
			{
				stringData[i] = (char)this.ReadByte();
			}
			return new string(stringData, 0, length);
		}

		// Token: 0x0400344F RID: 13391
		private readonly ReadOnlySpan<byte> data;

		// Token: 0x04003450 RID: 13392
		private readonly char[] stringData;

		// Token: 0x04003451 RID: 13393
		private int index;
	}
}
