using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x02000378 RID: 888
	[ComVisible(true)]
	[Serializable]
	public sealed class PublisherMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable, IConstantMembershipCondition, IReportMatchMembershipCondition
	{
		// Token: 0x06002C0C RID: 11276 RVA: 0x000A413F File Offset: 0x000A233F
		internal PublisherMembershipCondition()
		{
			this.m_element = null;
			this.m_certificate = null;
		}

		// Token: 0x06002C0D RID: 11277 RVA: 0x000A4155 File Offset: 0x000A2355
		public PublisherMembershipCondition(X509Certificate certificate)
		{
			PublisherMembershipCondition.CheckCertificate(certificate);
			this.m_certificate = new X509Certificate(certificate);
		}

		// Token: 0x06002C0E RID: 11278 RVA: 0x000A416F File Offset: 0x000A236F
		private static void CheckCertificate(X509Certificate certificate)
		{
			if (certificate == null)
			{
				throw new ArgumentNullException("certificate");
			}
		}

		// Token: 0x170005DD RID: 1501
		// (get) Token: 0x06002C10 RID: 11280 RVA: 0x000A4193 File Offset: 0x000A2393
		// (set) Token: 0x06002C0F RID: 11279 RVA: 0x000A417F File Offset: 0x000A237F
		public X509Certificate Certificate
		{
			get
			{
				if (this.m_certificate == null && this.m_element != null)
				{
					this.ParseCertificate();
				}
				if (this.m_certificate != null)
				{
					return new X509Certificate(this.m_certificate);
				}
				return null;
			}
			set
			{
				PublisherMembershipCondition.CheckCertificate(value);
				this.m_certificate = new X509Certificate(value);
			}
		}

		// Token: 0x06002C11 RID: 11281 RVA: 0x000A41C0 File Offset: 0x000A23C0
		public override string ToString()
		{
			if (this.m_certificate == null && this.m_element != null)
			{
				this.ParseCertificate();
			}
			if (this.m_certificate == null)
			{
				return Environment.GetResourceString("Publisher_ToString");
			}
			string subject = this.m_certificate.Subject;
			if (subject != null)
			{
				return string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Publisher_ToStringArg"), Hex.EncodeHexString(this.m_certificate.GetPublicKey()));
			}
			return Environment.GetResourceString("Publisher_ToString");
		}

		// Token: 0x06002C12 RID: 11282 RVA: 0x000A4234 File Offset: 0x000A2434
		public bool Check(Evidence evidence)
		{
			object obj = null;
			return ((IReportMatchMembershipCondition)this).Check(evidence, out obj);
		}

		// Token: 0x06002C13 RID: 11283 RVA: 0x000A424C File Offset: 0x000A244C
		bool IReportMatchMembershipCondition.Check(Evidence evidence, out object usedEvidence)
		{
			usedEvidence = null;
			if (evidence == null)
			{
				return false;
			}
			Publisher hostEvidence = evidence.GetHostEvidence<Publisher>();
			if (hostEvidence != null)
			{
				if (this.m_certificate == null && this.m_element != null)
				{
					this.ParseCertificate();
				}
				if (hostEvidence.Equals(new Publisher(this.m_certificate)))
				{
					usedEvidence = hostEvidence;
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002C14 RID: 11284 RVA: 0x000A429A File Offset: 0x000A249A
		public IMembershipCondition Copy()
		{
			if (this.m_certificate == null && this.m_element != null)
			{
				this.ParseCertificate();
			}
			return new PublisherMembershipCondition(this.m_certificate);
		}

		// Token: 0x06002C15 RID: 11285 RVA: 0x000A42BD File Offset: 0x000A24BD
		public SecurityElement ToXml()
		{
			return this.ToXml(null);
		}

		// Token: 0x06002C16 RID: 11286 RVA: 0x000A42C6 File Offset: 0x000A24C6
		public void FromXml(SecurityElement e)
		{
			this.FromXml(e, null);
		}

		// Token: 0x06002C17 RID: 11287 RVA: 0x000A42D0 File Offset: 0x000A24D0
		public SecurityElement ToXml(PolicyLevel level)
		{
			if (this.m_certificate == null && this.m_element != null)
			{
				this.ParseCertificate();
			}
			SecurityElement securityElement = new SecurityElement("IMembershipCondition");
			XMLUtil.AddClassAttribute(securityElement, base.GetType(), "System.Security.Policy.PublisherMembershipCondition");
			securityElement.AddAttribute("version", "1");
			if (this.m_certificate != null)
			{
				securityElement.AddAttribute("X509Certificate", this.m_certificate.GetRawCertDataString());
			}
			return securityElement;
		}

		// Token: 0x06002C18 RID: 11288 RVA: 0x000A4340 File Offset: 0x000A2540
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
				this.m_element = e;
				this.m_certificate = null;
			}
		}

		// Token: 0x06002C19 RID: 11289 RVA: 0x000A43B4 File Offset: 0x000A25B4
		private void ParseCertificate()
		{
			lock (this)
			{
				if (this.m_element != null)
				{
					string text = this.m_element.Attribute("X509Certificate");
					this.m_certificate = ((text == null) ? null : new X509Certificate(Hex.DecodeHexString(text)));
					PublisherMembershipCondition.CheckCertificate(this.m_certificate);
					this.m_element = null;
				}
			}
		}

		// Token: 0x06002C1A RID: 11290 RVA: 0x000A4430 File Offset: 0x000A2630
		public override bool Equals(object o)
		{
			PublisherMembershipCondition publisherMembershipCondition = o as PublisherMembershipCondition;
			if (publisherMembershipCondition != null)
			{
				if (this.m_certificate == null && this.m_element != null)
				{
					this.ParseCertificate();
				}
				if (publisherMembershipCondition.m_certificate == null && publisherMembershipCondition.m_element != null)
				{
					publisherMembershipCondition.ParseCertificate();
				}
				if (Publisher.PublicKeyEquals(this.m_certificate, publisherMembershipCondition.m_certificate))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002C1B RID: 11291 RVA: 0x000A4489 File Offset: 0x000A2689
		public override int GetHashCode()
		{
			if (this.m_certificate == null && this.m_element != null)
			{
				this.ParseCertificate();
			}
			if (this.m_certificate != null)
			{
				return this.m_certificate.GetHashCode();
			}
			return typeof(PublisherMembershipCondition).GetHashCode();
		}

		// Token: 0x040011BB RID: 4539
		private X509Certificate m_certificate;

		// Token: 0x040011BC RID: 4540
		private SecurityElement m_element;
	}
}
