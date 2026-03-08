using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x02000372 RID: 882
	[ComVisible(true)]
	[Serializable]
	public sealed class ZoneMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable, IConstantMembershipCondition, IReportMatchMembershipCondition
	{
		// Token: 0x06002BAD RID: 11181 RVA: 0x000A2C3F File Offset: 0x000A0E3F
		internal ZoneMembershipCondition()
		{
			this.m_zone = SecurityZone.NoZone;
		}

		// Token: 0x06002BAE RID: 11182 RVA: 0x000A2C4E File Offset: 0x000A0E4E
		public ZoneMembershipCondition(SecurityZone zone)
		{
			ZoneMembershipCondition.VerifyZone(zone);
			this.SecurityZone = zone;
		}

		// Token: 0x170005D5 RID: 1493
		// (get) Token: 0x06002BB0 RID: 11184 RVA: 0x000A2C72 File Offset: 0x000A0E72
		// (set) Token: 0x06002BAF RID: 11183 RVA: 0x000A2C63 File Offset: 0x000A0E63
		public SecurityZone SecurityZone
		{
			get
			{
				if (this.m_zone == SecurityZone.NoZone && this.m_element != null)
				{
					this.ParseZone();
				}
				return this.m_zone;
			}
			set
			{
				ZoneMembershipCondition.VerifyZone(value);
				this.m_zone = value;
			}
		}

		// Token: 0x06002BB1 RID: 11185 RVA: 0x000A2C91 File Offset: 0x000A0E91
		private static void VerifyZone(SecurityZone zone)
		{
			if (zone < SecurityZone.MyComputer || zone > SecurityZone.Untrusted)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_IllegalZone"));
			}
		}

		// Token: 0x06002BB2 RID: 11186 RVA: 0x000A2CAC File Offset: 0x000A0EAC
		public bool Check(Evidence evidence)
		{
			object obj = null;
			return ((IReportMatchMembershipCondition)this).Check(evidence, out obj);
		}

		// Token: 0x06002BB3 RID: 11187 RVA: 0x000A2CC4 File Offset: 0x000A0EC4
		bool IReportMatchMembershipCondition.Check(Evidence evidence, out object usedEvidence)
		{
			usedEvidence = null;
			if (evidence == null)
			{
				return false;
			}
			Zone hostEvidence = evidence.GetHostEvidence<Zone>();
			if (hostEvidence != null)
			{
				if (this.m_zone == SecurityZone.NoZone && this.m_element != null)
				{
					this.ParseZone();
				}
				if (hostEvidence.SecurityZone == this.m_zone)
				{
					usedEvidence = hostEvidence;
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002BB4 RID: 11188 RVA: 0x000A2D0E File Offset: 0x000A0F0E
		public IMembershipCondition Copy()
		{
			if (this.m_zone == SecurityZone.NoZone && this.m_element != null)
			{
				this.ParseZone();
			}
			return new ZoneMembershipCondition(this.m_zone);
		}

		// Token: 0x06002BB5 RID: 11189 RVA: 0x000A2D32 File Offset: 0x000A0F32
		public SecurityElement ToXml()
		{
			return this.ToXml(null);
		}

		// Token: 0x06002BB6 RID: 11190 RVA: 0x000A2D3B File Offset: 0x000A0F3B
		public void FromXml(SecurityElement e)
		{
			this.FromXml(e, null);
		}

		// Token: 0x06002BB7 RID: 11191 RVA: 0x000A2D48 File Offset: 0x000A0F48
		public SecurityElement ToXml(PolicyLevel level)
		{
			if (this.m_zone == SecurityZone.NoZone && this.m_element != null)
			{
				this.ParseZone();
			}
			SecurityElement securityElement = new SecurityElement("IMembershipCondition");
			XMLUtil.AddClassAttribute(securityElement, base.GetType(), "System.Security.Policy.ZoneMembershipCondition");
			securityElement.AddAttribute("version", "1");
			if (this.m_zone != SecurityZone.NoZone)
			{
				securityElement.AddAttribute("Zone", Enum.GetName(typeof(SecurityZone), this.m_zone));
			}
			return securityElement;
		}

		// Token: 0x06002BB8 RID: 11192 RVA: 0x000A2DC8 File Offset: 0x000A0FC8
		public void FromXml(SecurityElement e, PolicyLevel level)
		{
			if (e == null)
			{
				throw new ArgumentNullException("e");
			}
			if (!e.Tag.Equals("IMembershipCondition"))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MembershipConditionElement"));
			}
			lock (this)
			{
				this.m_zone = SecurityZone.NoZone;
				this.m_element = e;
			}
		}

		// Token: 0x06002BB9 RID: 11193 RVA: 0x000A2E3C File Offset: 0x000A103C
		private void ParseZone()
		{
			lock (this)
			{
				if (this.m_element != null)
				{
					string text = this.m_element.Attribute("Zone");
					this.m_zone = SecurityZone.NoZone;
					if (text == null)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_ZoneCannotBeNull"));
					}
					this.m_zone = (SecurityZone)Enum.Parse(typeof(SecurityZone), text);
					ZoneMembershipCondition.VerifyZone(this.m_zone);
					this.m_element = null;
				}
			}
		}

		// Token: 0x06002BBA RID: 11194 RVA: 0x000A2ED8 File Offset: 0x000A10D8
		public override bool Equals(object o)
		{
			ZoneMembershipCondition zoneMembershipCondition = o as ZoneMembershipCondition;
			if (zoneMembershipCondition != null)
			{
				if (this.m_zone == SecurityZone.NoZone && this.m_element != null)
				{
					this.ParseZone();
				}
				if (zoneMembershipCondition.m_zone == SecurityZone.NoZone && zoneMembershipCondition.m_element != null)
				{
					zoneMembershipCondition.ParseZone();
				}
				if (this.m_zone == zoneMembershipCondition.m_zone)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002BBB RID: 11195 RVA: 0x000A2F2E File Offset: 0x000A112E
		public override int GetHashCode()
		{
			if (this.m_zone == SecurityZone.NoZone && this.m_element != null)
			{
				this.ParseZone();
			}
			return (int)this.m_zone;
		}

		// Token: 0x06002BBC RID: 11196 RVA: 0x000A2F4D File Offset: 0x000A114D
		public override string ToString()
		{
			if (this.m_zone == SecurityZone.NoZone && this.m_element != null)
			{
				this.ParseZone();
			}
			return string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Zone_ToString"), ZoneMembershipCondition.s_names[(int)this.m_zone]);
		}

		// Token: 0x040011AE RID: 4526
		private static readonly string[] s_names = new string[] { "MyComputer", "Intranet", "Trusted", "Internet", "Untrusted" };

		// Token: 0x040011AF RID: 4527
		private SecurityZone m_zone;

		// Token: 0x040011B0 RID: 4528
		private SecurityElement m_element;
	}
}
