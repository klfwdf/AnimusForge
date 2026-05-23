using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000631 RID: 1585
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ByteArrayCodeReader : CodeReader
	{
		// Token: 0x17000764 RID: 1892
		// (get) Token: 0x06002103 RID: 8451 RVA: 0x00068B95 File Offset: 0x00066D95
		// (set) Token: 0x06002104 RID: 8452 RVA: 0x00068BA4 File Offset: 0x00066DA4
		public int Position
		{
			get
			{
				return this.currentPosition - this.startPosition;
			}
			set
			{
				if (value > this.Count)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_value();
				}
				this.currentPosition = this.startPosition + value;
			}
		}

		// Token: 0x17000765 RID: 1893
		// (get) Token: 0x06002105 RID: 8453 RVA: 0x00068BC2 File Offset: 0x00066DC2
		public int Count
		{
			get
			{
				return this.endPosition - this.startPosition;
			}
		}

		// Token: 0x17000766 RID: 1894
		// (get) Token: 0x06002106 RID: 8454 RVA: 0x00068BD1 File Offset: 0x00066DD1
		public bool CanReadByte
		{
			get
			{
				return this.currentPosition < this.endPosition;
			}
		}

		// Token: 0x06002107 RID: 8455 RVA: 0x00068BE1 File Offset: 0x00066DE1
		public ByteArrayCodeReader(string hexData)
			: this(HexUtils.ToByteArray(hexData))
		{
		}

		// Token: 0x06002108 RID: 8456 RVA: 0x00068BEF File Offset: 0x00066DEF
		public ByteArrayCodeReader(byte[] data)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			this.data = data;
			this.currentPosition = 0;
			this.startPosition = 0;
			this.endPosition = data.Length;
		}

		// Token: 0x06002109 RID: 8457 RVA: 0x00068C20 File Offset: 0x00066E20
		public ByteArrayCodeReader(byte[] data, int index, int count)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			this.data = data;
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			if (index + count > data.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			this.currentPosition = index;
			this.startPosition = index;
			this.endPosition = index + count;
		}

		// Token: 0x0600210A RID: 8458 RVA: 0x00068C78 File Offset: 0x00066E78
		[NullableContext(0)]
		public ByteArrayCodeReader(ArraySegment<byte> data)
		{
			if (data.Array == null)
			{
				ThrowHelper.ThrowArgumentException();
			}
			this.data = data.Array;
			int offset = data.Offset;
			this.currentPosition = offset;
			this.startPosition = offset;
			this.endPosition = offset + data.Count;
		}

		// Token: 0x0600210B RID: 8459 RVA: 0x00068CCC File Offset: 0x00066ECC
		public override int ReadByte()
		{
			if (this.currentPosition >= this.endPosition)
			{
				return -1;
			}
			byte[] array = this.data;
			int num = this.currentPosition;
			this.currentPosition = num + 1;
			return array[num];
		}

		// Token: 0x04001695 RID: 5781
		private readonly byte[] data;

		// Token: 0x04001696 RID: 5782
		private int currentPosition;

		// Token: 0x04001697 RID: 5783
		private readonly int startPosition;

		// Token: 0x04001698 RID: 5784
		private readonly int endPosition;
	}
}
