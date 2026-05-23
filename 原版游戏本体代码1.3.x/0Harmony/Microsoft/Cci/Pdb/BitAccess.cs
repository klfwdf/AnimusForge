using System;
using System.IO;
using System.Text;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200036B RID: 875
	internal class BitAccess
	{
		// Token: 0x06001747 RID: 5959 RVA: 0x00047B67 File Offset: 0x00045D67
		internal BitAccess(int capacity)
		{
			this.buffer = new byte[capacity];
		}

		// Token: 0x06001748 RID: 5960 RVA: 0x00047B7B File Offset: 0x00045D7B
		internal BitAccess(byte[] buffer)
		{
			this.buffer = buffer;
			this.offset = 0;
		}

		// Token: 0x17000585 RID: 1413
		// (get) Token: 0x06001749 RID: 5961 RVA: 0x00047B91 File Offset: 0x00045D91
		internal byte[] Buffer
		{
			get
			{
				return this.buffer;
			}
		}

		// Token: 0x0600174A RID: 5962 RVA: 0x00047B99 File Offset: 0x00045D99
		internal void FillBuffer(Stream stream, int capacity)
		{
			this.MinCapacity(capacity);
			stream.Read(this.buffer, 0, capacity);
			this.offset = 0;
		}

		// Token: 0x0600174B RID: 5963 RVA: 0x00047BB8 File Offset: 0x00045DB8
		internal void Append(Stream stream, int count)
		{
			int newCapacity = this.offset + count;
			if (this.buffer.Length < newCapacity)
			{
				byte[] newBuffer = new byte[newCapacity];
				Array.Copy(this.buffer, newBuffer, this.buffer.Length);
				this.buffer = newBuffer;
			}
			stream.Read(this.buffer, this.offset, count);
			this.offset += count;
		}

		// Token: 0x17000586 RID: 1414
		// (get) Token: 0x0600174C RID: 5964 RVA: 0x00047C1D File Offset: 0x00045E1D
		// (set) Token: 0x0600174D RID: 5965 RVA: 0x00047C25 File Offset: 0x00045E25
		internal int Position
		{
			get
			{
				return this.offset;
			}
			set
			{
				this.offset = value;
			}
		}

		// Token: 0x0600174E RID: 5966 RVA: 0x00047C2E File Offset: 0x00045E2E
		internal void MinCapacity(int capacity)
		{
			if (this.buffer.Length < capacity)
			{
				this.buffer = new byte[capacity];
			}
			this.offset = 0;
		}

		// Token: 0x0600174F RID: 5967 RVA: 0x00047C4E File Offset: 0x00045E4E
		internal void Align(int alignment)
		{
			while (this.offset % alignment != 0)
			{
				this.offset++;
			}
		}

		// Token: 0x06001750 RID: 5968 RVA: 0x00047C6A File Offset: 0x00045E6A
		internal void ReadInt16(out short value)
		{
			value = (short)((int)(this.buffer[this.offset] & byte.MaxValue) | ((int)this.buffer[this.offset + 1] << 8));
			this.offset += 2;
		}

		// Token: 0x06001751 RID: 5969 RVA: 0x00047CA2 File Offset: 0x00045EA2
		internal void ReadInt8(out sbyte value)
		{
			value = (sbyte)this.buffer[this.offset];
			this.offset++;
		}

		// Token: 0x06001752 RID: 5970 RVA: 0x00047CC4 File Offset: 0x00045EC4
		internal void ReadInt32(out int value)
		{
			value = (int)(this.buffer[this.offset] & byte.MaxValue) | ((int)this.buffer[this.offset + 1] << 8) | ((int)this.buffer[this.offset + 2] << 16) | ((int)this.buffer[this.offset + 3] << 24);
			this.offset += 4;
		}

		// Token: 0x06001753 RID: 5971 RVA: 0x00047D2C File Offset: 0x00045F2C
		internal void ReadInt64(out long value)
		{
			value = (long)(((ulong)this.buffer[this.offset] & 255UL) | ((ulong)this.buffer[this.offset + 1] << 8) | ((ulong)this.buffer[this.offset + 2] << 16) | ((ulong)this.buffer[this.offset + 3] << 24) | ((ulong)this.buffer[this.offset + 4] << 32) | ((ulong)this.buffer[this.offset + 5] << 40) | ((ulong)this.buffer[this.offset + 6] << 48) | ((ulong)this.buffer[this.offset + 7] << 56));
			this.offset += 8;
		}

		// Token: 0x06001754 RID: 5972 RVA: 0x00047DE9 File Offset: 0x00045FE9
		internal void ReadUInt16(out ushort value)
		{
			value = (ushort)((int)(this.buffer[this.offset] & byte.MaxValue) | ((int)this.buffer[this.offset + 1] << 8));
			this.offset += 2;
		}

		// Token: 0x06001755 RID: 5973 RVA: 0x00047E21 File Offset: 0x00046021
		internal void ReadUInt8(out byte value)
		{
			value = this.buffer[this.offset] & byte.MaxValue;
			this.offset++;
		}

		// Token: 0x06001756 RID: 5974 RVA: 0x00047E48 File Offset: 0x00046048
		internal void ReadUInt32(out uint value)
		{
			value = (uint)((int)(this.buffer[this.offset] & byte.MaxValue) | ((int)this.buffer[this.offset + 1] << 8) | ((int)this.buffer[this.offset + 2] << 16) | ((int)this.buffer[this.offset + 3] << 24));
			this.offset += 4;
		}

		// Token: 0x06001757 RID: 5975 RVA: 0x00047EB0 File Offset: 0x000460B0
		internal void ReadUInt64(out ulong value)
		{
			value = ((ulong)this.buffer[this.offset] & 255UL) | ((ulong)this.buffer[this.offset + 1] << 8) | ((ulong)this.buffer[this.offset + 2] << 16) | ((ulong)this.buffer[this.offset + 3] << 24) | ((ulong)this.buffer[this.offset + 4] << 32) | ((ulong)this.buffer[this.offset + 5] << 40) | ((ulong)this.buffer[this.offset + 6] << 48) | ((ulong)this.buffer[this.offset + 7] << 56);
			this.offset += 8;
		}

		// Token: 0x06001758 RID: 5976 RVA: 0x00047F70 File Offset: 0x00046170
		internal void ReadInt32(int[] values)
		{
			for (int i = 0; i < values.Length; i++)
			{
				this.ReadInt32(out values[i]);
			}
		}

		// Token: 0x06001759 RID: 5977 RVA: 0x00047F98 File Offset: 0x00046198
		internal void ReadUInt32(uint[] values)
		{
			for (int i = 0; i < values.Length; i++)
			{
				this.ReadUInt32(out values[i]);
			}
		}

		// Token: 0x0600175A RID: 5978 RVA: 0x00047FC0 File Offset: 0x000461C0
		internal void ReadBytes(byte[] bytes)
		{
			for (int i = 0; i < bytes.Length; i++)
			{
				int num = i;
				byte[] array = this.buffer;
				int num2 = this.offset;
				this.offset = num2 + 1;
				bytes[num] = array[num2];
			}
		}

		// Token: 0x0600175B RID: 5979 RVA: 0x00047FF6 File Offset: 0x000461F6
		internal float ReadFloat()
		{
			float result = BitConverter.ToSingle(this.buffer, this.offset);
			this.offset += 4;
			return result;
		}

		// Token: 0x0600175C RID: 5980 RVA: 0x00048017 File Offset: 0x00046217
		internal double ReadDouble()
		{
			double result = BitConverter.ToDouble(this.buffer, this.offset);
			this.offset += 8;
			return result;
		}

		// Token: 0x0600175D RID: 5981 RVA: 0x00048038 File Offset: 0x00046238
		internal decimal ReadDecimal()
		{
			int[] bits = new int[4];
			this.ReadInt32(bits);
			return new decimal(bits[2], bits[3], bits[1], bits[0] < 0, (byte)((bits[0] & 16711680) >> 16));
		}

		// Token: 0x0600175E RID: 5982 RVA: 0x00048074 File Offset: 0x00046274
		internal void ReadBString(out string value)
		{
			ushort len;
			this.ReadUInt16(out len);
			value = Encoding.UTF8.GetString(this.buffer, this.offset, (int)len);
			this.offset += (int)len;
		}

		// Token: 0x0600175F RID: 5983 RVA: 0x000480B0 File Offset: 0x000462B0
		internal string ReadBString(int len)
		{
			string @string = Encoding.UTF8.GetString(this.buffer, this.offset, len);
			this.offset += len;
			return @string;
		}

		// Token: 0x06001760 RID: 5984 RVA: 0x000480D8 File Offset: 0x000462D8
		internal void ReadCString(out string value)
		{
			int len = 0;
			while (this.offset + len < this.buffer.Length && this.buffer[this.offset + len] != 0)
			{
				len++;
			}
			value = Encoding.UTF8.GetString(this.buffer, this.offset, len);
			this.offset += len + 1;
		}

		// Token: 0x06001761 RID: 5985 RVA: 0x0004813C File Offset: 0x0004633C
		internal void SkipCString(out string value)
		{
			int len = 0;
			while (this.offset + len < this.buffer.Length && this.buffer[this.offset + len] != 0)
			{
				len++;
			}
			this.offset += len + 1;
			value = null;
		}

		// Token: 0x06001762 RID: 5986 RVA: 0x00048188 File Offset: 0x00046388
		internal void ReadGuid(out Guid guid)
		{
			uint a;
			this.ReadUInt32(out a);
			ushort b;
			this.ReadUInt16(out b);
			ushort c;
			this.ReadUInt16(out c);
			byte d;
			this.ReadUInt8(out d);
			byte e;
			this.ReadUInt8(out e);
			byte f;
			this.ReadUInt8(out f);
			byte g;
			this.ReadUInt8(out g);
			byte h;
			this.ReadUInt8(out h);
			byte i;
			this.ReadUInt8(out i);
			byte j;
			this.ReadUInt8(out j);
			byte k;
			this.ReadUInt8(out k);
			guid = new Guid(a, b, c, d, e, f, g, h, i, j, k);
		}

		// Token: 0x06001763 RID: 5987 RVA: 0x0004820C File Offset: 0x0004640C
		internal string ReadString()
		{
			int len = 0;
			while (this.offset + len < this.buffer.Length && this.buffer[this.offset + len] != 0)
			{
				len += 2;
			}
			string @string = Encoding.Unicode.GetString(this.buffer, this.offset, len);
			this.offset += len + 2;
			return @string;
		}

		// Token: 0x04000B5B RID: 2907
		private byte[] buffer;

		// Token: 0x04000B5C RID: 2908
		private int offset;
	}
}
