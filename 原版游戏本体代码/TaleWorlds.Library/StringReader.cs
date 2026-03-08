using System;
using System.Globalization;

namespace TaleWorlds.Library
{
	// Token: 0x02000091 RID: 145
	public class StringReader : IReader
	{
		// Token: 0x1700008F RID: 143
		// (get) Token: 0x0600050F RID: 1295 RVA: 0x0001258D File Offset: 0x0001078D
		// (set) Token: 0x06000510 RID: 1296 RVA: 0x00012595 File Offset: 0x00010795
		public string Data { get; private set; }

		// Token: 0x06000511 RID: 1297 RVA: 0x0001259E File Offset: 0x0001079E
		private string GetNextToken()
		{
			string result = this._tokens[this._currentIndex];
			this._currentIndex++;
			return result;
		}

		// Token: 0x06000512 RID: 1298 RVA: 0x000125BB File Offset: 0x000107BB
		public StringReader(string data)
		{
			this.Data = data;
			this._tokens = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		}

		// Token: 0x06000513 RID: 1299 RVA: 0x000125E2 File Offset: 0x000107E2
		public ISerializableObject ReadSerializableObject()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000514 RID: 1300 RVA: 0x000125E9 File Offset: 0x000107E9
		public int ReadInt()
		{
			return Convert.ToInt32(this.GetNextToken());
		}

		// Token: 0x06000515 RID: 1301 RVA: 0x000125F6 File Offset: 0x000107F6
		public short ReadShort()
		{
			return Convert.ToInt16(this.GetNextToken());
		}

		// Token: 0x06000516 RID: 1302 RVA: 0x00012604 File Offset: 0x00010804
		public string ReadString()
		{
			int num = this.ReadInt();
			int i = 0;
			string text = "";
			while (i < num)
			{
				string nextToken = this.GetNextToken();
				text += nextToken;
				i = text.Length;
				if (i < num)
				{
					text += " ";
				}
			}
			if (text.Length != num)
			{
				throw new Exception("invalid string format, length does not match");
			}
			return text;
		}

		// Token: 0x06000517 RID: 1303 RVA: 0x00012664 File Offset: 0x00010864
		public Color ReadColor()
		{
			float red = this.ReadFloat();
			float green = this.ReadFloat();
			float blue = this.ReadFloat();
			float alpha = this.ReadFloat();
			return new Color(red, green, blue, alpha);
		}

		// Token: 0x06000518 RID: 1304 RVA: 0x00012698 File Offset: 0x00010898
		public bool ReadBool()
		{
			string nextToken = this.GetNextToken();
			return nextToken == "1" || (!(nextToken == "0") && Convert.ToBoolean(nextToken));
		}

		// Token: 0x06000519 RID: 1305 RVA: 0x000126D0 File Offset: 0x000108D0
		public float ReadFloat()
		{
			return Convert.ToSingle(this.GetNextToken(), CultureInfo.InvariantCulture);
		}

		// Token: 0x0600051A RID: 1306 RVA: 0x000126E2 File Offset: 0x000108E2
		public uint ReadUInt()
		{
			return Convert.ToUInt32(this.GetNextToken());
		}

		// Token: 0x0600051B RID: 1307 RVA: 0x000126EF File Offset: 0x000108EF
		public ulong ReadULong()
		{
			return Convert.ToUInt64(this.GetNextToken());
		}

		// Token: 0x0600051C RID: 1308 RVA: 0x000126FC File Offset: 0x000108FC
		public long ReadLong()
		{
			return Convert.ToInt64(this.GetNextToken());
		}

		// Token: 0x0600051D RID: 1309 RVA: 0x00012709 File Offset: 0x00010909
		public byte ReadByte()
		{
			return Convert.ToByte(this.GetNextToken());
		}

		// Token: 0x0600051E RID: 1310 RVA: 0x00012716 File Offset: 0x00010916
		public byte[] ReadBytes(int length)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600051F RID: 1311 RVA: 0x00012720 File Offset: 0x00010920
		public Vec2 ReadVec2()
		{
			float a = this.ReadFloat();
			float b = this.ReadFloat();
			return new Vec2(a, b);
		}

		// Token: 0x06000520 RID: 1312 RVA: 0x00012740 File Offset: 0x00010940
		public Vec3 ReadVec3()
		{
			float x = this.ReadFloat();
			float y = this.ReadFloat();
			float z = this.ReadFloat();
			float w = this.ReadFloat();
			return new Vec3(x, y, z, w);
		}

		// Token: 0x06000521 RID: 1313 RVA: 0x00012770 File Offset: 0x00010970
		public Vec3i ReadVec3Int()
		{
			int x = this.ReadInt();
			int y = this.ReadInt();
			int z = this.ReadInt();
			return new Vec3i(x, y, z);
		}

		// Token: 0x06000522 RID: 1314 RVA: 0x00012798 File Offset: 0x00010998
		public sbyte ReadSByte()
		{
			return Convert.ToSByte(this.GetNextToken());
		}

		// Token: 0x06000523 RID: 1315 RVA: 0x000127A5 File Offset: 0x000109A5
		public ushort ReadUShort()
		{
			return Convert.ToUInt16(this.GetNextToken());
		}

		// Token: 0x06000524 RID: 1316 RVA: 0x000127B2 File Offset: 0x000109B2
		public double ReadDouble()
		{
			return Convert.ToDouble(this.GetNextToken());
		}

		// Token: 0x04000197 RID: 407
		private string[] _tokens;

		// Token: 0x04000198 RID: 408
		private int _currentIndex;
	}
}
