using System;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	// Token: 0x02000011 RID: 17
	internal class ByteBuffer
	{
		// Token: 0x0600003B RID: 59 RVA: 0x00002C3F File Offset: 0x00000E3F
		internal ByteBuffer(byte[] buffer)
		{
			this.buffer = buffer;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002C50 File Offset: 0x00000E50
		internal byte ReadByte()
		{
			this.CheckCanRead(1);
			byte[] array = this.buffer;
			int num = this.position;
			this.position = num + 1;
			return array[num];
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002C7C File Offset: 0x00000E7C
		internal byte[] ReadBytes(int length)
		{
			this.CheckCanRead(length);
			byte[] value = new byte[length];
			Buffer.BlockCopy(this.buffer, this.position, value, 0, length);
			this.position += length;
			return value;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002CBC File Offset: 0x00000EBC
		internal short ReadInt16()
		{
			this.CheckCanRead(2);
			short value = (short)((int)this.buffer[this.position] | ((int)this.buffer[this.position + 1] << 8));
			this.position += 2;
			return value;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002D00 File Offset: 0x00000F00
		internal int ReadInt32()
		{
			this.CheckCanRead(4);
			int value = (int)this.buffer[this.position] | ((int)this.buffer[this.position + 1] << 8) | ((int)this.buffer[this.position + 2] << 16) | ((int)this.buffer[this.position + 3] << 24);
			this.position += 4;
			return value;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002D6C File Offset: 0x00000F6C
		internal long ReadInt64()
		{
			this.CheckCanRead(8);
			uint low = (uint)((int)this.buffer[this.position] | ((int)this.buffer[this.position + 1] << 8) | ((int)this.buffer[this.position + 2] << 16) | ((int)this.buffer[this.position + 3] << 24));
			uint high = (uint)((int)this.buffer[this.position + 4] | ((int)this.buffer[this.position + 5] << 8) | ((int)this.buffer[this.position + 6] << 16) | ((int)this.buffer[this.position + 7] << 24));
			long value = (long)(((ulong)high << 32) | (ulong)low);
			this.position += 8;
			return value;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002E28 File Offset: 0x00001028
		internal float ReadSingle()
		{
			if (!BitConverter.IsLittleEndian)
			{
				byte[] bytes = this.ReadBytes(4);
				Array.Reverse(bytes);
				return BitConverter.ToSingle(bytes, 0);
			}
			this.CheckCanRead(4);
			float value = BitConverter.ToSingle(this.buffer, this.position);
			this.position += 4;
			return value;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002E7C File Offset: 0x0000107C
		internal double ReadDouble()
		{
			if (!BitConverter.IsLittleEndian)
			{
				byte[] bytes = this.ReadBytes(8);
				Array.Reverse(bytes);
				return BitConverter.ToDouble(bytes, 0);
			}
			this.CheckCanRead(8);
			double value = BitConverter.ToDouble(this.buffer, this.position);
			this.position += 8;
			return value;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00002ED0 File Offset: 0x000010D0
		private void CheckCanRead(int count)
		{
			if (this.position + count > this.buffer.Length)
			{
				string paramName = "count";
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 3);
				defaultInterpolatedStringHandler.AppendLiteral("position(");
				defaultInterpolatedStringHandler.AppendFormatted<int>(this.position);
				defaultInterpolatedStringHandler.AppendLiteral(") + count(");
				defaultInterpolatedStringHandler.AppendFormatted<int>(count);
				defaultInterpolatedStringHandler.AppendLiteral(") > buffer.Length(");
				defaultInterpolatedStringHandler.AppendFormatted<int>(this.buffer.Length);
				defaultInterpolatedStringHandler.AppendLiteral(")");
				throw new ArgumentOutOfRangeException(paramName, defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		// Token: 0x0400001C RID: 28
		internal byte[] buffer;

		// Token: 0x0400001D RID: 29
		internal int position;
	}
}
