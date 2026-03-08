using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x0200036D RID: 877
	[ComVisible(true)]
	[Serializable]
	public sealed class StrongNameMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable, IConstantMembershipCondition, IReportMatchMembershipCondition
	{
		// Token: 0x06002B64 RID: 11108 RVA: 0x000A1C39 File Offset: 0x0009FE39
		internal StrongNameMembershipCondition()
		{
		}

		// Token: 0x06002B65 RID: 11109 RVA: 0x000A1C44 File Offset: 0x0009FE44
		public StrongNameMembershipCondition(StrongNamePublicKeyBlob blob, string name, Version version)
		{
			if (blob == null)
			{
				throw new ArgumentNullException("blob");
			}
			if (name != null && name.Equals(""))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyStrongName"));
			}
			this.m_publicKeyBlob = blob;
			this.m_name = name;
			this.m_version = version;
		}

		// Token: 0x170005CE RID: 1486
		// (get) Token: 0x06002B67 RID: 11111 RVA: 0x000A1CB1 File Offset: 0x0009FEB1
		// (set) Token: 0x06002B66 RID: 11110 RVA: 0x000A1C9A File Offset: 0x0009FE9A
		public StrongNamePublicKeyBlob PublicKey
		{
			get
			{
				if (this.m_publicKeyBlob == null && this.m_element != null)
				{
					this.ParseKeyBlob();
				}
				return this.m_publicKeyBlob;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("PublicKey");
				}
				this.m_publicKeyBlob = value;
			}
		}

		// Token: 0x170005CF RID: 1487
		// (get) Token: 0x06002B69 RID: 11113 RVA: 0x000A1D34 File Offset: 0x0009FF34
		// (set) Token: 0x06002B68 RID: 11112 RVA: 0x000A1CD0 File Offset: 0x0009FED0
		public string Name
		{
			get
			{
				if (this.m_name == null && this.m_element != null)
				{
					this.ParseName();
				}
				return this.m_name;
			}
			set
			{
				if (value == null)
				{
					if (this.m_publicKeyBlob == null && this.m_element != null)
					{
						this.ParseKeyBlob();
					}
					if (this.m_version == null && this.m_element != null)
					{
						this.ParseVersion();
					}
					this.m_element = null;
				}
				else if (value.Length == 0)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"));
				}
				this.m_name = value;
			}
		}

		// Token: 0x170005D0 RID: 1488
		// (get) Token: 0x06002B6B RID: 11115 RVA: 0x000A1DA4 File Offset: 0x0009FFA4
		// (set) Token: 0x06002B6A RID: 11114 RVA: 0x000A1D54 File Offset: 0x0009FF54
		public Version Version
		{
			get
			{
				if (this.m_version == null && this.m_element != null)
				{
					this.ParseVersion();
				}
				return this.m_version;
			}
			set
			{
				if (value == null)
				{
					if (this.m_name == null && this.m_element != null)
					{
						this.ParseName();
					}
					if (this.m_publicKeyBlob == null && this.m_element != null)
					{
						this.ParseKeyBlob();
					}
					this.m_element = null;
				}
				this.m_version = value;
			}
		}

		// Token: 0x06002B6C RID: 11116 RVA: 0x000A1DC4 File Offset: 0x0009FFC4
		public bool Check(Evidence evidence)
		{
			object obj = null;
			return ((IReportMatchMembershipCondition)this).Check(evidence, out obj);
		}

		// Token: 0x06002B6D RID: 11117 RVA: 0x000A1DDC File Offset: 0x0009FFDC
		bool IReportMatchMembershipCondition.Check(Evidence evidence, out object usedEvidence)
		{
			usedEvidence = null;
			if (evidence == null)
			{
				return false;
			}
			StrongName delayEvaluatedHostEvidence = evidence.GetDelayEvaluatedHostEvidence<StrongName>();
			if (delayEvaluatedHostEvidence != null)
			{
				bool flag = this.PublicKey != null && this.PublicKey.Equals(delayEvaluatedHostEvidence.PublicKey);
				bool flag2 = this.Name == null || (delayEvaluatedHostEvidence.Name != null && StrongName.CompareNames(delayEvaluatedHostEvidence.Name, this.Name));
				bool flag3 = this.Version == null || (delayEvaluatedHostEvidence.Version != null && delayEvaluatedHostEvidence.Version.CompareTo(this.Version) == 0);
				if (flag && flag2 && flag3)
				{
					usedEvidence = delayEvaluatedHostEvidence;
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002B6E RID: 11118 RVA: 0x000A1E78 File Offset: 0x000A0078
		public IMembershipCondition Copy()
		{
			return new StrongNameMembershipCondition(this.PublicKey, this.Name, this.Version);
		}

		// Token: 0x06002B6F RID: 11119 RVA: 0x000A1E91 File Offset: 0x000A0091
		public SecurityElement ToXml()
		{
			return this.ToXml(null);
		}

		// Token: 0x06002B70 RID: 11120 RVA: 0x000A1E9A File Offset: 0x000A009A
		public void FromXml(SecurityElement e)
		{
			this.FromXml(e, null);
		}

		// Token: 0x06002B71 RID: 11121 RVA: 0x000A1EA4 File Offset: 0x000A00A4
		public SecurityElement ToXml(PolicyLevel level)
		{
			SecurityElement securityElement = new SecurityElement("IMembershipCondition");
			XMLUtil.AddClassAttribute(securityElement, base.GetType(), "System.Security.Policy.StrongNameMembershipCondition");
			securityElement.AddAttribute("version", "1");
			if (this.PublicKey != null)
			{
				securityElement.AddAttribute("PublicKeyBlob", Hex.EncodeHexString(this.PublicKey.PublicKey));
			}
			if (this.Name != null)
			{
				securityElement.AddAttribute("Name", this.Name);
			}
			if (this.Version != null)
			{
				securityElement.AddAttribute("AssemblyVersion", this.Version.ToString());
			}
			return securityElement;
		}

		// Token: 0x06002B72 RID: 11122 RVA: 0x000A1F38 File Offset: 0x000A0138
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
				this.m_name = null;
				this.m_publicKeyBlob = null;
				this.m_version = null;
				this.m_element = e;
			}
		}

		// Token: 0x06002B73 RID: 11123 RVA: 0x000A1FBC File Offset: 0x000A01BC
		private void ParseName()
		{
			lock (this)
			{
				if (this.m_element != null)
				{
					string text = this.m_element.Attribute("Name");
					this.m_name = ((text == null) ? null : text);
					if (this.m_version != null && this.m_name != null && this.m_publicKeyBlob != null)
					{
						this.m_element = null;
					}
				}
			}
		}

		// Token: 0x06002B74 RID: 11124 RVA: 0x000A2038 File Offset: 0x000A0238
		private void ParseKeyBlob()
		{
			lock (this)
			{
				if (this.m_element != null)
				{
					string text = this.m_element.Attribute("PublicKeyBlob");
					StrongNamePublicKeyBlob strongNamePublicKeyBlob = new StrongNamePublicKeyBlob();
					if (text == null)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_BlobCannotBeNull"));
					}
					strongNamePublicKeyBlob.PublicKey = Hex.DecodeHexString(text);
					this.m_publicKeyBlob = strongNamePublicKeyBlob;
					if (this.m_version != null && this.m_name != null && this.m_publicKeyBlob != null)
					{
						this.m_element = null;
					}
				}
			}
		}

		// Token: 0x06002B75 RID: 11125 RVA: 0x000A20D8 File Offset: 0x000A02D8
		private void ParseVersion()
		{
			lock (this)
			{
				if (this.m_element != null)
				{
					string text = this.m_element.Attribute("AssemblyVersion");
					this.m_version = ((text == null) ? null : new Version(text));
					if (this.m_version != null && this.m_name != null && this.m_publicKeyBlob != null)
					{
						this.m_element = null;
					}
				}
			}
		}

		// Token: 0x06002B76 RID: 11126 RVA: 0x000A215C File Offset: 0x000A035C
		public override string ToString()
		{
			string arg = "";
			string arg2 = "";
			if (this.Name != null)
			{
				arg = " " + string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("StrongName_Name"), this.Name);
			}
			if (this.Version != null)
			{
				arg2 = " " + string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("StrongName_Version"), this.Version);
			}
			return string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("StrongName_ToString"), Hex.EncodeHexString(this.PublicKey.PublicKey), arg, arg2);
		}

		// Token: 0x06002B77 RID: 11127 RVA: 0x000A21F8 File Offset: 0x000A03F8
		public override bool Equals(object o)
		{
			StrongNameMembershipCondition strongNameMembershipCondition = o as StrongNameMembershipCondition;
			if (strongNameMembershipCondition != null)
			{
				if (this.m_publicKeyBlob == null && this.m_element != null)
				{
					this.ParseKeyBlob();
				}
				if (strongNameMembershipCondition.m_publicKeyBlob == null && strongNameMembershipCondition.m_element != null)
				{
					strongNameMembershipCondition.ParseKeyBlob();
				}
				if (object.Equals(this.m_publicKeyBlob, strongNameMembershipCondition.m_publicKeyBlob))
				{
					if (this.m_name == null && this.m_element != null)
					{
						this.ParseName();
					}
					if (strongNameMembershipCondition.m_name == null && strongNameMembershipCondition.m_element != null)
					{
						strongNameMembershipCondition.ParseName();
					}
					if (object.Equals(this.m_name, strongNameMembershipCondition.m_name))
					{
						if (this.m_version == null && this.m_element != null)
						{
							this.ParseVersion();
						}
						if (strongNameMembershipCondition.m_version == null && strongNameMembershipCondition.m_element != null)
						{
							strongNameMembershipCondition.ParseVersion();
						}
						if (object.Equals(this.m_version, strongNameMembershipCondition.m_version))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x06002B78 RID: 11128 RVA: 0x000A22E4 File Offset: 0x000A04E4
		public override int GetHashCode()
		{
			if (this.m_publicKeyBlob == null && this.m_element != null)
			{
				this.ParseKeyBlob();
			}
			if (this.m_publicKeyBlob != null)
			{
				return this.m_publicKeyBlob.GetHashCode();
			}
			if (this.m_name == null && this.m_element != null)
			{
				this.ParseName();
			}
			if (this.m_version == null && this.m_element != null)
			{
				this.ParseVersion();
			}
			if (this.m_name != null || this.m_version != null)
			{
				return ((this.m_name == null) ? 0 : this.m_name.GetHashCode()) + ((this.m_version == null) ? 0 : this.m_version.GetHashCode());
			}
			return typeof(StrongNameMembershipCondition).GetHashCode();
		}

		// Token: 0x040011A1 RID: 4513
		private StrongNamePublicKeyBlob m_publicKeyBlob;

		// Token: 0x040011A2 RID: 4514
		private string m_name;

		// Token: 0x040011A3 RID: 4515
		private Version m_version;

		// Token: 0x040011A4 RID: 4516
		private SecurityElement m_element;

		// Token: 0x040011A5 RID: 4517
		private const string s_tagName = "Name";

		// Token: 0x040011A6 RID: 4518
		private const string s_tagVersion = "AssemblyVersion";

		// Token: 0x040011A7 RID: 4519
		private const string s_tagPublicKeyBlob = "PublicKeyBlob";
	}
}
