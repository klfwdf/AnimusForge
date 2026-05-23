using System;
using System.Text;

namespace TaleWorlds.Library
{
	// Token: 0x02000092 RID: 146
	public class StringWriter : IWriter
	{
		// Token: 0x17000090 RID: 144
		// (get) Token: 0x06000525 RID: 1317 RVA: 0x000127BF File Offset: 0x000109BF
		public string Data
		{
			get
			{
				return this._stringBuilder.ToString();
			}
		}

		// Token: 0x06000526 RID: 1318 RVA: 0x000127CC File Offset: 0x000109CC
		public StringWriter()
		{
			this._stringBuilder = new StringBuilder();
		}

		// Token: 0x06000527 RID: 1319 RVA: 0x000127DF File Offset: 0x000109DF
		private void AddToken(string token)
		{
			this._stringBuilder.Append(token);
			this._stringBuilder.Append(" ");
		}

		// Token: 0x06000528 RID: 1320 RVA: 0x000127FF File Offset: 0x000109FF
		public void WriteSerializableObject(ISerializableObject serializableObject)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000529 RID: 1321 RVA: 0x00012806 File Offset: 0x00010A06
		public void WriteByte(byte value)
		{
			this.AddToken(Convert.ToString(value));
		}

		// Token: 0x0600052A RID: 1322 RVA: 0x00012814 File Offset: 0x00010A14
		public void WriteBytes(byte[] bytes)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600052B RID: 1323 RVA: 0x0001281B File Offset: 0x00010A1B
		public void WriteInt(int value)
		{
			this.AddToken(Convert.ToString(value));
		}

		// Token: 0x0600052C RID: 1324 RVA: 0x00012829 File Offset: 0x00010A29
		public void WriteShort(short value)
		{
			this.AddToken(Convert.ToString(value));
		}

		// Token: 0x0600052D RID: 1325 RVA: 0x00012837 File Offset: 0x00010A37
		public void WriteString(string value)
		{
			this.WriteInt(value.Length);
			this.AddToken(value);
		}

		// Token: 0x0600052E RID: 1326 RVA: 0x0001284C File Offset: 0x00010A4C
		public void WriteColor(Color value)
		{
			this.WriteFloat(value.Red);
			this.WriteFloat(value.Green);
			this.WriteFloat(value.Blue);
			this.WriteFloat(value.Alpha);
		}

		// Token: 0x0600052F RID: 1327 RVA: 0x0001287E File Offset: 0x00010A7E
		public void WriteBool(bool value)
		{
			this.AddToken(value ? "1" : "0");
		}

		// Token: 0x06000530 RID: 1328 RVA: 0x00012895 File Offset: 0x00010A95
		public void WriteFloat(float value)
		{
			this.AddToken((value == 0f) ? "0" : Convert.ToString(value));
		}

		// Token: 0x06000531 RID: 1329 RVA: 0x000128B2 File Offset: 0x00010AB2
		public void WriteUInt(uint value)
		{
			this.AddToken(Convert.ToString(value));
		}

		// Token: 0x06000532 RID: 1330 RVA: 0x000128C0 File Offset: 0x00010AC0
		public void WriteULong(ulong value)
		{
			this.AddToken(Convert.ToString(value));
		}

		// Token: 0x06000533 RID: 1331 RVA: 0x000128CE File Offset: 0x00010ACE
		public void WriteLong(long value)
		{
			this.AddToken(Convert.ToString(value));
		}

		// Token: 0x06000534 RID: 1332 RVA: 0x000128DC File Offset: 0x00010ADC
		public void WriteVec2(Vec2 vec2)
		{
			this.WriteFloat(vec2.x);
			this.WriteFloat(vec2.y);
		}

		// Token: 0x06000535 RID: 1333 RVA: 0x000128F6 File Offset: 0x00010AF6
		public void WriteVec3(Vec3 vec3)
		{
			this.WriteFloat(vec3.x);
			this.WriteFloat(vec3.y);
			this.WriteFloat(vec3.z);
			this.WriteFloat(vec3.w);
		}

		// Token: 0x06000536 RID: 1334 RVA: 0x00012928 File Offset: 0x00010B28
		public void WriteVec3Int(Vec3i vec3)
		{
			this.WriteInt(vec3.X);
			this.WriteInt(vec3.Y);
			this.WriteInt(vec3.Z);
		}

		// Token: 0x06000537 RID: 1335 RVA: 0x0001294E File Offset: 0x00010B4E
		public void WriteSByte(sbyte value)
		{
			this.AddToken(Convert.ToString(value));
		}

		// Token: 0x06000538 RID: 1336 RVA: 0x0001295C File Offset: 0x00010B5C
		public void WriteUShort(ushort value)
		{
			this.AddToken(Convert.ToString(value));
		}

		// Token: 0x06000539 RID: 1337 RVA: 0x0001296A File Offset: 0x00010B6A
		public void WriteDouble(double value)
		{
			this.AddToken((value == 0.0) ? "0" : Convert.ToString(value));
		}

		// Token: 0x04000199 RID: 409
		private StringBuilder _stringBuilder;
	}
}
