using System;
using System.Text;

namespace TaleWorlds.Library
{
	// Token: 0x0200001B RID: 27
	public class BinaryReader : IReader
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000050 RID: 80 RVA: 0x00002DD1 File Offset: 0x00000FD1
		// (set) Token: 0x06000051 RID: 81 RVA: 0x00002DD9 File Offset: 0x00000FD9
		public byte[] Data { get; private set; }

		// Token: 0x06000052 RID: 82 RVA: 0x00002DE2 File Offset: 0x00000FE2
		public BinaryReader(byte[] data)
		{
			this.Data = data;
			this._cursor = 0;
			this._buffer = new byte[4];
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000053 RID: 83 RVA: 0x00002E04 File Offset: 0x00001004
		public int UnreadByteCount
		{
			get
			{
				return this.Data.Length - this._cursor;
			}
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00002E15 File Offset: 0x00001015
		public ISerializableObject ReadSerializableObject()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00002E1C File Offset: 0x0000101C
		public int Read3ByteInt()
		{
			this._buffer[0] = this.ReadByte();
			this._buffer[1] = this.ReadByte();
			this._buffer[2] = this.ReadByte();
			if (this._buffer[0] == 255 && this._buffer[1] == 255 && this._buffer[2] == 255)
			{
				this._buffer[3] = byte.MaxValue;
			}
			else
			{
				this._buffer[3] = 0;
			}
			return BitConverter.ToInt32(this._buffer, 0);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00002EA4 File Offset: 0x000010A4
		public int ReadInt()
		{
			int result = BitConverter.ToInt32(this.Data, this._cursor);
			this._cursor += 4;
			return result;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00002EC5 File Offset: 0x000010C5
		public short ReadShort()
		{
			short result = BitConverter.ToInt16(this.Data, this._cursor);
			this._cursor += 2;
			return result;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00002EE8 File Offset: 0x000010E8
		public void ReadFloats(float[] output, int count)
		{
			int num = count * 4;
			Buffer.BlockCopy(this.Data, this._cursor, output, 0, num);
			this._cursor += num;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00002F1C File Offset: 0x0000111C
		public void ReadShorts(short[] output, int count)
		{
			int num = count * 2;
			Buffer.BlockCopy(this.Data, this._cursor, output, 0, num);
			this._cursor += num;
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00002F50 File Offset: 0x00001150
		public string ReadString()
		{
			int num = this.ReadInt();
			string result = null;
			if (num >= 0)
			{
				result = Encoding.UTF8.GetString(this.Data, this._cursor, num);
				this._cursor += num;
			}
			return result;
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00002F94 File Offset: 0x00001194
		public Color ReadColor()
		{
			float red = this.ReadFloat();
			float green = this.ReadFloat();
			float blue = this.ReadFloat();
			float alpha = this.ReadFloat();
			return new Color(red, green, blue, alpha);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002FC6 File Offset: 0x000011C6
		public bool ReadBool()
		{
			int num = (int)this.Data[this._cursor];
			this._cursor++;
			return num == 1;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00002FE6 File Offset: 0x000011E6
		public float ReadFloat()
		{
			float result = BitConverter.ToSingle(this.Data, this._cursor);
			this._cursor += 4;
			return result;
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00003007 File Offset: 0x00001207
		public uint ReadUInt()
		{
			uint result = BitConverter.ToUInt32(this.Data, this._cursor);
			this._cursor += 4;
			return result;
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00003028 File Offset: 0x00001228
		public ulong ReadULong()
		{
			ulong result = BitConverter.ToUInt64(this.Data, this._cursor);
			this._cursor += 8;
			return result;
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00003049 File Offset: 0x00001249
		public long ReadLong()
		{
			long result = BitConverter.ToInt64(this.Data, this._cursor);
			this._cursor += 8;
			return result;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x0000306A File Offset: 0x0000126A
		public byte ReadByte()
		{
			byte result = this.Data[this._cursor];
			this._cursor++;
			return result;
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00003088 File Offset: 0x00001288
		public byte[] ReadBytes(int length)
		{
			byte[] array = new byte[length];
			Array.Copy(this.Data, this._cursor, array, 0, length);
			this._cursor += length;
			return array;
		}

		// Token: 0x06000063 RID: 99 RVA: 0x000030C0 File Offset: 0x000012C0
		public Vec2 ReadVec2()
		{
			float a = this.ReadFloat();
			float b = this.ReadFloat();
			return new Vec2(a, b);
		}

		// Token: 0x06000064 RID: 100 RVA: 0x000030E0 File Offset: 0x000012E0
		public Vec3 ReadVec3()
		{
			float x = this.ReadFloat();
			float y = this.ReadFloat();
			float z = this.ReadFloat();
			float w = this.ReadFloat();
			return new Vec3(x, y, z, w);
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00003110 File Offset: 0x00001310
		public Vec3i ReadVec3Int()
		{
			int x = this.ReadInt();
			int y = this.ReadInt();
			int z = this.ReadInt();
			return new Vec3i(x, y, z);
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00003138 File Offset: 0x00001338
		public sbyte ReadSByte()
		{
			sbyte result = (sbyte)this.Data[this._cursor];
			this._cursor++;
			return result;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00003156 File Offset: 0x00001356
		public ushort ReadUShort()
		{
			ushort result = BitConverter.ToUInt16(this.Data, this._cursor);
			this._cursor += 2;
			return result;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00003177 File Offset: 0x00001377
		public double ReadDouble()
		{
			double result = BitConverter.ToDouble(this.Data, this._cursor);
			this._cursor += 8;
			return result;
		}

		// Token: 0x0400005D RID: 93
		private int _cursor;

		// Token: 0x0400005E RID: 94
		private byte[] _buffer;
	}
}
