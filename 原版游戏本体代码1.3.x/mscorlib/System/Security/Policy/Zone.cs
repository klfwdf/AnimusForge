using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Security.Policy
{
	// Token: 0x02000371 RID: 881
	[ComVisible(true)]
	[Serializable]
	public sealed class Zone : EvidenceBase, IIdentityPermissionFactory
	{
		// Token: 0x06002B9E RID: 11166 RVA: 0x000A2A95 File Offset: 0x000A0C95
		public Zone(SecurityZone zone)
		{
			if (zone < SecurityZone.NoZone || zone > SecurityZone.Untrusted)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_IllegalZone"));
			}
			this.m_zone = zone;
		}

		// Token: 0x06002B9F RID: 11167 RVA: 0x000A2ABC File Offset: 0x000A0CBC
		private Zone(Zone zone)
		{
			this.m_url = zone.m_url;
			this.m_zone = zone.m_zone;
		}

		// Token: 0x06002BA0 RID: 11168 RVA: 0x000A2ADC File Offset: 0x000A0CDC
		private Zone(string url)
		{
			this.m_url = url;
			this.m_zone = SecurityZone.NoZone;
		}

		// Token: 0x06002BA1 RID: 11169 RVA: 0x000A2AF2 File Offset: 0x000A0CF2
		public static Zone CreateFromUrl(string url)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			return new Zone(url);
		}

		// Token: 0x06002BA2 RID: 11170
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern SecurityZone _CreateFromUrl(string url);

		// Token: 0x06002BA3 RID: 11171 RVA: 0x000A2B08 File Offset: 0x000A0D08
		public IPermission CreateIdentityPermission(Evidence evidence)
		{
			return new ZoneIdentityPermission(this.SecurityZone);
		}

		// Token: 0x170005D4 RID: 1492
		// (get) Token: 0x06002BA4 RID: 11172 RVA: 0x000A2B15 File Offset: 0x000A0D15
		public SecurityZone SecurityZone
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_url != null)
				{
					this.m_zone = Zone._CreateFromUrl(this.m_url);
				}
				return this.m_zone;
			}
		}

		// Token: 0x06002BA5 RID: 11173 RVA: 0x000A2B38 File Offset: 0x000A0D38
		public override bool Equals(object o)
		{
			Zone zone = o as Zone;
			return zone != null && this.SecurityZone == zone.SecurityZone;
		}

		// Token: 0x06002BA6 RID: 11174 RVA: 0x000A2B5F File Offset: 0x000A0D5F
		public override int GetHashCode()
		{
			return (int)this.SecurityZone;
		}

		// Token: 0x06002BA7 RID: 11175 RVA: 0x000A2B67 File Offset: 0x000A0D67
		public override EvidenceBase Clone()
		{
			return new Zone(this);
		}

		// Token: 0x06002BA8 RID: 11176 RVA: 0x000A2B6F File Offset: 0x000A0D6F
		public object Copy()
		{
			return this.Clone();
		}

		// Token: 0x06002BA9 RID: 11177 RVA: 0x000A2B78 File Offset: 0x000A0D78
		internal SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("System.Security.Policy.Zone");
			securityElement.AddAttribute("version", "1");
			if (this.SecurityZone != SecurityZone.NoZone)
			{
				securityElement.AddChild(new SecurityElement("Zone", Zone.s_names[(int)this.SecurityZone]));
			}
			else
			{
				securityElement.AddChild(new SecurityElement("Zone", Zone.s_names[Zone.s_names.Length - 1]));
			}
			return securityElement;
		}

		// Token: 0x06002BAA RID: 11178 RVA: 0x000A2BE7 File Offset: 0x000A0DE7
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x06002BAB RID: 11179 RVA: 0x000A2BF4 File Offset: 0x000A0DF4
		internal object Normalize()
		{
			return Zone.s_names[(int)this.SecurityZone];
		}

		// Token: 0x040011AB RID: 4523
		[OptionalField(VersionAdded = 2)]
		private string m_url;

		// Token: 0x040011AC RID: 4524
		private SecurityZone m_zone;

		// Token: 0x040011AD RID: 4525
		private static readonly string[] s_names = new string[] { "MyComputer", "Intranet", "Trusted", "Internet", "Untrusted", "NoZone" };
	}
}
