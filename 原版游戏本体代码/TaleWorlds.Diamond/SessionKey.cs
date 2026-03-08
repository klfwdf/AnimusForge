using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000022 RID: 34
	[DataContract]
	[Serializable]
	public struct SessionKey
	{
		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060000B1 RID: 177 RVA: 0x000032EE File Offset: 0x000014EE
		public Guid Guid
		{
			get
			{
				return this._guid;
			}
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x000032F6 File Offset: 0x000014F6
		public SessionKey(Guid guid)
		{
			this._guid = guid;
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x000032FF File Offset: 0x000014FF
		public SessionKey(byte[] b)
		{
			this._guid = new Guid(b);
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x0000330D File Offset: 0x0000150D
		public static SessionKey NewGuid()
		{
			return new SessionKey(Guid.NewGuid());
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x0000331C File Offset: 0x0000151C
		public override string ToString()
		{
			return this._guid.ToString();
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00003340 File Offset: 0x00001540
		public byte[] ToByteArray()
		{
			return this._guid.ToByteArray();
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x0000335B File Offset: 0x0000155B
		public static bool operator ==(SessionKey a, SessionKey b)
		{
			return a._guid == b._guid;
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x0000336E File Offset: 0x0000156E
		public static bool operator !=(SessionKey a, SessionKey b)
		{
			return a._guid != b._guid;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00003384 File Offset: 0x00001584
		public override bool Equals(object o)
		{
			if (o != null && o is SessionKey)
			{
				SessionKey sessionKey = (SessionKey)o;
				return this._guid.Equals(sessionKey.Guid);
			}
			return false;
		}

		// Token: 0x060000BA RID: 186 RVA: 0x000033BC File Offset: 0x000015BC
		public override int GetHashCode()
		{
			return this._guid.GetHashCode();
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060000BB RID: 187 RVA: 0x000033DD File Offset: 0x000015DD
		public static SessionKey Empty
		{
			get
			{
				return new SessionKey(Guid.Empty);
			}
		}

		// Token: 0x0400003B RID: 59
		[DataMember]
		private readonly Guid _guid;
	}
}
