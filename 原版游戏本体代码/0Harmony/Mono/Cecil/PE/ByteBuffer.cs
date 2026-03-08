using System;

namespace Mono.Cecil.PE
{
	// Token: 0x020002BF RID: 703
	internal class ByteBuffer
	{
		// Token: 0x060011FF RID: 4607 RVA: 0x000376F5 File Offset: 0x000358F5
		public ByteBuffer()
		{
			this.buffer = Empty<byte>.Array;
		}

		// Token: 0x06001200 RID: 4608 RVA: 0x00037708 File Offset: 0x00035908
		public ByteBuffer(int length)
		{
			this.buffer = new byte[length];
		}

		// Token: 0x06001201 RID: 4609 RVA: 0x0003771C File Offset: 0x0003591C
		public ByteBuffer(byte[] buffer)
		{
			this.buffer = buffer ?? Empty<byte>.Array;
			this.length = this.buffer.Length;
		}

		// Token: 0x06001202 RID: 4610 RVA: 0x00037742 File Offset: 0x00035942
		public void Advance(int length)
		{
			this.position += length;
		}

		// Token: 0x06001203 RID: 4611 RVA: 0x00037754 File Offset: 0x00035954
		public byte ReadByte()
		{
			byte[] array = this.buffer;
			int num = this.position;
			this.position = num + 1;
			return array[num];
		}

		// Token: 0x06001204 RID: 4612 RVA: 0x00037779 File Offset: 0x00035979
		public sbyte ReadSByte()
		{
			return (sbyte)this.ReadByte();
		}

		// Token: 0x06001205 RID: 4613 RVA: 0x00037784 File Offset: 0x00035984
		public byte[] ReadBytes(int length)
		{
			byte[] bytes = new byte[length];
			Buffer.BlockCopy(this.buffer, this.position, bytes, 0, length);
			this.position += length;
			return bytes;
		}

		// Token: 0x06001206 RID: 4614 RVA: 0x000377BB File Offset: 0x000359BB
		public ushort ReadUInt16()
		{
			ushort result = (ushort)((int)this.buffer[this.position] | ((int)this.buffer[this.position + 1] << 8));
			this.position += 2;
			return result;
		}

		// Token: 0x06001207 RID: 4615 RVA: 0x000377EB File Offset: 0x000359EB
		public short ReadInt16()
		{
			return (short)this.ReadUInt16();
		}

		// Token: 0x06001208 RID: 4616 RVA: 0x000377F4 File Offset: 0x000359F4
		public uint ReadUInt32()
		{
			uint result = (uint)((int)this.buffer[this.position] | ((int)this.buffer[this.position + 1] << 8) | ((int)this.buffer[this.position + 2] << 16) | ((int)this.buffer[this.position + 3] << 24));
			this.position += 4;
			return result;
		}

		// Token: 0x06001209 RID: 4617 RVA: 0x00037854 File Offset: 0x00035A54
		public int ReadInt32()
		{
			return (int)this.ReadUInt32();
		}

		// Token: 0x0600120A RID: 4618 RVA: 0x0003785C File Offset: 0x00035A5C
		public ulong ReadUInt64()
		{
			uint low = this.ReadUInt32();
			return ((ulong)this.ReadUInt32() << 32) | (ulong)low;
		}

		// Token: 0x0600120B RID: 4619 RVA: 0x0003787D File Offset: 0x00035A7D
		public long ReadInt64()
		{
			return (long)this.ReadUInt64();
		}

		// Token: 0x0600120C RID: 4620 RVA: 0x00037888 File Offset: 0x00035A88
		public uint ReadCompressedUInt32()
		{
			byte first = this.ReadByte();
			if ((first & 128) == 0)
			{
				return (uint)first;
			}
			if ((first & 64) == 0)
			{
				return (((uint)first & 4294967167U) << 8) | (uint)this.ReadByte();
			}
			return (uint)((((int)first & -193) << 24) | ((int)this.ReadByte() << 16) | ((int)this.ReadByte() << 8) | (int)this.ReadByte());
		}

		// Token: 0x0600120D RID: 4621 RVA: 0x000378E4 File Offset: 0x00035AE4
		public int ReadCompressedInt32()
		{
			byte b = this.buffer[this.position];
			uint num = this.ReadCompressedUInt32();
			int v = (int)num >> 1;
			if ((num & 1U) == 0U)
			{
				return v;
			}
			int num2 = (int)(b & 192);
			if (num2 == 0 || num2 == 64)
			{
				return v - 64;
			}
			if (num2 != 128)
			{
				return v - 268435456;
			}
			return v - 8192;
		}

		// Token: 0x0600120E RID: 4622 RVA: 0x0003793D File Offset: 0x00035B3D
		public float ReadSingle()
		{
			if (!BitConverter.IsLittleEndian)
			{
				byte[] array = this.ReadBytes(4);
				Array.Reverse(array);
				return BitConverter.ToSingle(array, 0);
			}
			float result = BitConverter.ToSingle(this.buffer, this.position);
			this.position += 4;
			return result;
		}

		// Token: 0x0600120F RID: 4623 RVA: 0x00037979 File Offset: 0x00035B79
		public double ReadDouble()
		{
			if (!BitConverter.IsLittleEndian)
			{
				byte[] array = this.ReadBytes(8);
				Array.Reverse(array);
				return BitConverter.ToDouble(array, 0);
			}
			double result = BitConverter.ToDouble(this.buffer, this.position);
			this.position += 8;
			return result;
		}

		// Token: 0x06001210 RID: 4624 RVA: 0x000379B8 File Offset: 0x00035BB8
		public void WriteByte(byte value)
		{
			if (this.position == this.buffer.Length)
			{
				this.Grow(1);
			}
			byte[] array = this.buffer;
			int num = this.position;
			this.position = num + 1;
			array[num] = value;
			if (this.position > this.length)
			{
				this.length = this.position;
			}
		}

		// Token: 0x06001211 RID: 4625 RVA: 0x0002977D File Offset: 0x0002797D
		public void WriteSByte(sbyte value)
		{
			this.WriteByte((byte)value);
		}

		// Token: 0x06001212 RID: 4626 RVA: 0x00037A10 File Offset: 0x00035C10
		public void WriteUInt16(ushort value)
		{
			if (this.position + 2 > this.buffer.Length)
			{
				this.Grow(2);
			}
			byte[] array = this.buffer;
			int num = this.position;
			this.position = num + 1;
			array[num] = (byte)value;
			byte[] array2 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array2[num] = (byte)(value >> 8);
			if (this.position > this.length)
			{
				this.length = this.position;
			}
		}

		// Token: 0x06001213 RID: 4627 RVA: 0x00037A86 File Offset: 0x00035C86
		public void WriteInt16(short value)
		{
			this.WriteUInt16((ushort)value);
		}

		// Token: 0x06001214 RID: 4628 RVA: 0x00037A90 File Offset: 0x00035C90
		public void WriteUInt32(uint value)
		{
			if (this.position + 4 > this.buffer.Length)
			{
				this.Grow(4);
			}
			byte[] array = this.buffer;
			int num = this.position;
			this.position = num + 1;
			array[num] = (byte)value;
			byte[] array2 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array2[num] = (byte)(value >> 8);
			byte[] array3 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array3[num] = (byte)(value >> 16);
			byte[] array4 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array4[num] = (byte)(value >> 24);
			if (this.position > this.length)
			{
				this.length = this.position;
			}
		}

		// Token: 0x06001215 RID: 4629 RVA: 0x00037B40 File Offset: 0x00035D40
		public void WriteInt32(int value)
		{
			this.WriteUInt32((uint)value);
		}

		// Token: 0x06001216 RID: 4630 RVA: 0x00037B4C File Offset: 0x00035D4C
		public void WriteUInt64(ulong value)
		{
			if (this.position + 8 > this.buffer.Length)
			{
				this.Grow(8);
			}
			byte[] array = this.buffer;
			int num = this.position;
			this.position = num + 1;
			array[num] = (byte)value;
			byte[] array2 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array2[num] = (byte)(value >> 8);
			byte[] array3 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array3[num] = (byte)(value >> 16);
			byte[] array4 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array4[num] = (byte)(value >> 24);
			byte[] array5 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array5[num] = (byte)(value >> 32);
			byte[] array6 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array6[num] = (byte)(value >> 40);
			byte[] array7 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array7[num] = (byte)(value >> 48);
			byte[] array8 = this.buffer;
			num = this.position;
			this.position = num + 1;
			array8[num] = (byte)(value >> 56);
			if (this.position > this.length)
			{
				this.length = this.position;
			}
		}

		// Token: 0x06001217 RID: 4631 RVA: 0x00037C70 File Offset: 0x00035E70
		public void WriteInt64(long value)
		{
			this.WriteUInt64((ulong)value);
		}

		// Token: 0x06001218 RID: 4632 RVA: 0x00037C7C File Offset: 0x00035E7C
		public void WriteCompressedUInt32(uint value)
		{
			if (value < 128U)
			{
				this.WriteByte((byte)value);
				return;
			}
			if (value < 16384U)
			{
				this.WriteByte((byte)(128U | (value >> 8)));
				this.WriteByte((byte)(value & 255U));
				return;
			}
			this.WriteByte((byte)((value >> 24) | 192U));
			this.WriteByte((byte)((value >> 16) & 255U));
			this.WriteByte((byte)((value >> 8) & 255U));
			this.WriteByte((byte)(value & 255U));
		}

		// Token: 0x06001219 RID: 4633 RVA: 0x00037D04 File Offset: 0x00035F04
		public void WriteCompressedInt32(int value)
		{
			int signMask = value >> 31;
			if ((value & -64) == (signMask & -64))
			{
				int i = ((value & 63) << 1) | (signMask & 1);
				this.WriteByte((byte)i);
				return;
			}
			if ((value & -8192) == (signMask & -8192))
			{
				int j = ((value & 8191) << 1) | (signMask & 1);
				ushort val = (ushort)(32768 | j);
				this.WriteUInt16(BitConverter.IsLittleEndian ? ByteBuffer.ReverseEndianness(val) : val);
				return;
			}
			if ((value & -268435456) == (signMask & -268435456))
			{
				int k = ((value & 268435455) << 1) | (signMask & 1);
				uint val2 = (uint)(-1073741824 | k);
				this.WriteUInt32(BitConverter.IsLittleEndian ? ByteBuffer.ReverseEndianness(val2) : val2);
				return;
			}
			throw new ArgumentOutOfRangeException("value", "valid range is -2^28 to 2^28 -1");
		}

		// Token: 0x0600121A RID: 4634 RVA: 0x00037DC3 File Offset: 0x00035FC3
		private static uint ReverseEndianness(uint value)
		{
			return ByteBuffer.RotateRight(value & 16711935U, 8) + ByteBuffer.RotateLeft(value & 4278255360U, 8);
		}

		// Token: 0x0600121B RID: 4635 RVA: 0x00037DE0 File Offset: 0x00035FE0
		private static uint RotateRight(uint value, int offset)
		{
			return (value >> offset) | (value << 32 - offset);
		}

		// Token: 0x0600121C RID: 4636 RVA: 0x00037DF2 File Offset: 0x00035FF2
		private static uint RotateLeft(uint value, int offset)
		{
			return (value << offset) | (value >> 32 - offset);
		}

		// Token: 0x0600121D RID: 4637 RVA: 0x00037E04 File Offset: 0x00036004
		private static ushort ReverseEndianness(ushort value)
		{
			return (ushort)((value >> 8) + ((int)value << 8));
		}

		// Token: 0x0600121E RID: 4638 RVA: 0x00037E10 File Offset: 0x00036010
		public void WriteBytes(byte[] bytes)
		{
			int length = bytes.Length;
			if (this.position + length > this.buffer.Length)
			{
				this.Grow(length);
			}
			Buffer.BlockCopy(bytes, 0, this.buffer, this.position, length);
			this.position += length;
			if (this.position > this.length)
			{
				this.length = this.position;
			}
		}

		// Token: 0x0600121F RID: 4639 RVA: 0x00037E78 File Offset: 0x00036078
		public void WriteBytes(int length)
		{
			if (this.position + length > this.buffer.Length)
			{
				this.Grow(length);
			}
			this.position += length;
			if (this.position > this.length)
			{
				this.length = this.position;
			}
		}

		// Token: 0x06001220 RID: 4640 RVA: 0x00037EC8 File Offset: 0x000360C8
		public void WriteBytes(ByteBuffer buffer)
		{
			if (this.position + buffer.length > this.buffer.Length)
			{
				this.Grow(buffer.length);
			}
			Buffer.BlockCopy(buffer.buffer, 0, this.buffer, this.position, buffer.length);
			this.position += buffer.length;
			if (this.position > this.length)
			{
				this.length = this.position;
			}
		}

		// Token: 0x06001221 RID: 4641 RVA: 0x00037F44 File Offset: 0x00036144
		public void WriteSingle(float value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			this.WriteBytes(bytes);
		}

		// Token: 0x06001222 RID: 4642 RVA: 0x00037F6C File Offset: 0x0003616C
		public void WriteDouble(double value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			this.WriteBytes(bytes);
		}

		// Token: 0x06001223 RID: 4643 RVA: 0x00037F94 File Offset: 0x00036194
		private void Grow(int desired)
		{
			byte[] array = this.buffer;
			int current_length = array.Length;
			byte[] buffer = new byte[Math.Max(current_length + desired, current_length * 2)];
			Buffer.BlockCopy(array, 0, buffer, 0, current_length);
			this.buffer = buffer;
		}

		// Token: 0x040006A0 RID: 1696
		internal byte[] buffer;

		// Token: 0x040006A1 RID: 1697
		internal int length;

		// Token: 0x040006A2 RID: 1698
		internal int position;
	}
}
