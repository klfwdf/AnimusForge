using System;

namespace System.Globalization
{
	// Token: 0x020003E0 RID: 992
	[Serializable]
	public sealed class SortVersion : IEquatable<SortVersion>
	{
		// Token: 0x1700076E RID: 1902
		// (get) Token: 0x060032E5 RID: 13029 RVA: 0x000C4996 File Offset: 0x000C2B96
		public int FullVersion
		{
			get
			{
				return this.m_NlsVersion;
			}
		}

		// Token: 0x1700076F RID: 1903
		// (get) Token: 0x060032E6 RID: 13030 RVA: 0x000C499E File Offset: 0x000C2B9E
		public Guid SortId
		{
			get
			{
				return this.m_SortId;
			}
		}

		// Token: 0x060032E7 RID: 13031 RVA: 0x000C49A6 File Offset: 0x000C2BA6
		public SortVersion(int fullVersion, Guid sortId)
		{
			this.m_SortId = sortId;
			this.m_NlsVersion = fullVersion;
		}

		// Token: 0x060032E8 RID: 13032 RVA: 0x000C49BC File Offset: 0x000C2BBC
		internal SortVersion(int nlsVersion, int effectiveId, Guid customVersion)
		{
			this.m_NlsVersion = nlsVersion;
			if (customVersion == Guid.Empty)
			{
				byte[] bytes = BitConverter.GetBytes(effectiveId);
				byte h = (byte)((uint)effectiveId >> 24);
				byte i = (byte)((effectiveId & 16711680) >> 16);
				byte j = (byte)((effectiveId & 65280) >> 8);
				byte k = (byte)(effectiveId & 255);
				customVersion = new Guid(0, 0, 0, 0, 0, 0, 0, h, i, j, k);
			}
			this.m_SortId = customVersion;
		}

		// Token: 0x060032E9 RID: 13033 RVA: 0x000C4A2C File Offset: 0x000C2C2C
		public override bool Equals(object obj)
		{
			SortVersion sortVersion = obj as SortVersion;
			return sortVersion != null && this.Equals(sortVersion);
		}

		// Token: 0x060032EA RID: 13034 RVA: 0x000C4A52 File Offset: 0x000C2C52
		public bool Equals(SortVersion other)
		{
			return !(other == null) && this.m_NlsVersion == other.m_NlsVersion && this.m_SortId == other.m_SortId;
		}

		// Token: 0x060032EB RID: 13035 RVA: 0x000C4A80 File Offset: 0x000C2C80
		public override int GetHashCode()
		{
			return (this.m_NlsVersion * 7) | this.m_SortId.GetHashCode();
		}

		// Token: 0x060032EC RID: 13036 RVA: 0x000C4A9C File Offset: 0x000C2C9C
		public static bool operator ==(SortVersion left, SortVersion right)
		{
			if (left != null)
			{
				return left.Equals(right);
			}
			return right == null || right.Equals(left);
		}

		// Token: 0x060032ED RID: 13037 RVA: 0x000C4AB5 File Offset: 0x000C2CB5
		public static bool operator !=(SortVersion left, SortVersion right)
		{
			return !(left == right);
		}

		// Token: 0x04001697 RID: 5783
		private int m_NlsVersion;

		// Token: 0x04001698 RID: 5784
		private Guid m_SortId;
	}
}
