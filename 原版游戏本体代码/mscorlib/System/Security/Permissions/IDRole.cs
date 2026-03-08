using System;
using System.Security.Principal;

namespace System.Security.Permissions
{
	// Token: 0x02000304 RID: 772
	[Serializable]
	internal class IDRole
	{
		// Token: 0x17000506 RID: 1286
		// (get) Token: 0x0600271E RID: 10014 RVA: 0x0008D58C File Offset: 0x0008B78C
		internal SecurityIdentifier Sid
		{
			[SecurityCritical]
			get
			{
				if (string.IsNullOrEmpty(this.m_role))
				{
					return null;
				}
				if (this.m_sid == null)
				{
					NTAccount identity = new NTAccount(this.m_role);
					IdentityReferenceCollection identityReferenceCollection = NTAccount.Translate(new IdentityReferenceCollection(1) { identity }, typeof(SecurityIdentifier), false);
					this.m_sid = identityReferenceCollection[0] as SecurityIdentifier;
				}
				return this.m_sid;
			}
		}

		// Token: 0x0600271F RID: 10015 RVA: 0x0008D5FC File Offset: 0x0008B7FC
		internal SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("Identity");
			if (this.m_authenticated)
			{
				securityElement.AddAttribute("Authenticated", "true");
			}
			if (this.m_id != null)
			{
				securityElement.AddAttribute("ID", SecurityElement.Escape(this.m_id));
			}
			if (this.m_role != null)
			{
				securityElement.AddAttribute("Role", SecurityElement.Escape(this.m_role));
			}
			return securityElement;
		}

		// Token: 0x06002720 RID: 10016 RVA: 0x0008D66C File Offset: 0x0008B86C
		internal void FromXml(SecurityElement e)
		{
			string text = e.Attribute("Authenticated");
			if (text != null)
			{
				this.m_authenticated = string.Compare(text, "true", StringComparison.OrdinalIgnoreCase) == 0;
			}
			else
			{
				this.m_authenticated = false;
			}
			string text2 = e.Attribute("ID");
			if (text2 != null)
			{
				this.m_id = text2;
			}
			else
			{
				this.m_id = null;
			}
			string text3 = e.Attribute("Role");
			if (text3 != null)
			{
				this.m_role = text3;
				return;
			}
			this.m_role = null;
		}

		// Token: 0x06002721 RID: 10017 RVA: 0x0008D6E3 File Offset: 0x0008B8E3
		public override int GetHashCode()
		{
			return (this.m_authenticated ? 0 : 101) + ((this.m_id == null) ? 0 : this.m_id.GetHashCode()) + ((this.m_role == null) ? 0 : this.m_role.GetHashCode());
		}

		// Token: 0x04000F1F RID: 3871
		internal bool m_authenticated;

		// Token: 0x04000F20 RID: 3872
		internal string m_id;

		// Token: 0x04000F21 RID: 3873
		internal string m_role;

		// Token: 0x04000F22 RID: 3874
		[NonSerialized]
		private SecurityIdentifier m_sid;
	}
}
