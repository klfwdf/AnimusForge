using System;
using System.Runtime.InteropServices;
using System.Security.Util;
using System.Text;

namespace System
{
	// Token: 0x020000A5 RID: 165
	[ComVisible(true)]
	[Serializable]
	public sealed class ApplicationId
	{
		// Token: 0x0600098F RID: 2447 RVA: 0x0001F0BE File Offset: 0x0001D2BE
		internal ApplicationId()
		{
		}

		// Token: 0x06000990 RID: 2448 RVA: 0x0001F0C8 File Offset: 0x0001D2C8
		public ApplicationId(byte[] publicKeyToken, string name, Version version, string processorArchitecture, string culture)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyApplicationName"));
			}
			if (version == null)
			{
				throw new ArgumentNullException("version");
			}
			if (publicKeyToken == null)
			{
				throw new ArgumentNullException("publicKeyToken");
			}
			this.m_publicKeyToken = new byte[publicKeyToken.Length];
			Array.Copy(publicKeyToken, 0, this.m_publicKeyToken, 0, publicKeyToken.Length);
			this.m_name = name;
			this.m_version = version;
			this.m_processorArchitecture = processorArchitecture;
			this.m_culture = culture;
		}

		// Token: 0x1700013E RID: 318
		// (get) Token: 0x06000991 RID: 2449 RVA: 0x0001F160 File Offset: 0x0001D360
		public byte[] PublicKeyToken
		{
			get
			{
				byte[] array = new byte[this.m_publicKeyToken.Length];
				Array.Copy(this.m_publicKeyToken, 0, array, 0, this.m_publicKeyToken.Length);
				return array;
			}
		}

		// Token: 0x1700013F RID: 319
		// (get) Token: 0x06000992 RID: 2450 RVA: 0x0001F192 File Offset: 0x0001D392
		public string Name
		{
			get
			{
				return this.m_name;
			}
		}

		// Token: 0x17000140 RID: 320
		// (get) Token: 0x06000993 RID: 2451 RVA: 0x0001F19A File Offset: 0x0001D39A
		public Version Version
		{
			get
			{
				return this.m_version;
			}
		}

		// Token: 0x17000141 RID: 321
		// (get) Token: 0x06000994 RID: 2452 RVA: 0x0001F1A2 File Offset: 0x0001D3A2
		public string ProcessorArchitecture
		{
			get
			{
				return this.m_processorArchitecture;
			}
		}

		// Token: 0x17000142 RID: 322
		// (get) Token: 0x06000995 RID: 2453 RVA: 0x0001F1AA File Offset: 0x0001D3AA
		public string Culture
		{
			get
			{
				return this.m_culture;
			}
		}

		// Token: 0x06000996 RID: 2454 RVA: 0x0001F1B2 File Offset: 0x0001D3B2
		public ApplicationId Copy()
		{
			return new ApplicationId(this.m_publicKeyToken, this.m_name, this.m_version, this.m_processorArchitecture, this.m_culture);
		}

		// Token: 0x06000997 RID: 2455 RVA: 0x0001F1D8 File Offset: 0x0001D3D8
		public override string ToString()
		{
			StringBuilder stringBuilder = StringBuilderCache.Acquire(16);
			stringBuilder.Append(this.m_name);
			if (this.m_culture != null)
			{
				stringBuilder.Append(", culture=\"");
				stringBuilder.Append(this.m_culture);
				stringBuilder.Append("\"");
			}
			stringBuilder.Append(", version=\"");
			stringBuilder.Append(this.m_version.ToString());
			stringBuilder.Append("\"");
			if (this.m_publicKeyToken != null)
			{
				stringBuilder.Append(", publicKeyToken=\"");
				stringBuilder.Append(Hex.EncodeHexString(this.m_publicKeyToken));
				stringBuilder.Append("\"");
			}
			if (this.m_processorArchitecture != null)
			{
				stringBuilder.Append(", processorArchitecture =\"");
				stringBuilder.Append(this.m_processorArchitecture);
				stringBuilder.Append("\"");
			}
			return StringBuilderCache.GetStringAndRelease(stringBuilder);
		}

		// Token: 0x06000998 RID: 2456 RVA: 0x0001F2B8 File Offset: 0x0001D4B8
		public override bool Equals(object o)
		{
			ApplicationId applicationId = o as ApplicationId;
			if (applicationId == null)
			{
				return false;
			}
			if (!object.Equals(this.m_name, applicationId.m_name) || !object.Equals(this.m_version, applicationId.m_version) || !object.Equals(this.m_processorArchitecture, applicationId.m_processorArchitecture) || !object.Equals(this.m_culture, applicationId.m_culture))
			{
				return false;
			}
			if (this.m_publicKeyToken.Length != applicationId.m_publicKeyToken.Length)
			{
				return false;
			}
			for (int i = 0; i < this.m_publicKeyToken.Length; i++)
			{
				if (this.m_publicKeyToken[i] != applicationId.m_publicKeyToken[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000999 RID: 2457 RVA: 0x0001F35B File Offset: 0x0001D55B
		public override int GetHashCode()
		{
			return this.m_name.GetHashCode() ^ this.m_version.GetHashCode();
		}

		// Token: 0x040003C8 RID: 968
		private string m_name;

		// Token: 0x040003C9 RID: 969
		private Version m_version;

		// Token: 0x040003CA RID: 970
		private string m_processorArchitecture;

		// Token: 0x040003CB RID: 971
		private string m_culture;

		// Token: 0x040003CC RID: 972
		internal byte[] m_publicKeyToken;
	}
}
