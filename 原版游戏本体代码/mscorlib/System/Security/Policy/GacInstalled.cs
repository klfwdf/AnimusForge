using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Security.Policy
{
	// Token: 0x02000373 RID: 883
	[ComVisible(true)]
	[Serializable]
	public sealed class GacInstalled : EvidenceBase, IIdentityPermissionFactory
	{
		// Token: 0x06002BBF RID: 11199 RVA: 0x000A2FC3 File Offset: 0x000A11C3
		public IPermission CreateIdentityPermission(Evidence evidence)
		{
			return new GacIdentityPermission();
		}

		// Token: 0x06002BC0 RID: 11200 RVA: 0x000A2FCA File Offset: 0x000A11CA
		public override bool Equals(object o)
		{
			return o is GacInstalled;
		}

		// Token: 0x06002BC1 RID: 11201 RVA: 0x000A2FD5 File Offset: 0x000A11D5
		public override int GetHashCode()
		{
			return 0;
		}

		// Token: 0x06002BC2 RID: 11202 RVA: 0x000A2FD8 File Offset: 0x000A11D8
		public override EvidenceBase Clone()
		{
			return new GacInstalled();
		}

		// Token: 0x06002BC3 RID: 11203 RVA: 0x000A2FDF File Offset: 0x000A11DF
		public object Copy()
		{
			return this.Clone();
		}

		// Token: 0x06002BC4 RID: 11204 RVA: 0x000A2FE8 File Offset: 0x000A11E8
		internal SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement(base.GetType().FullName);
			securityElement.AddAttribute("version", "1");
			return securityElement;
		}

		// Token: 0x06002BC5 RID: 11205 RVA: 0x000A3017 File Offset: 0x000A1217
		public override string ToString()
		{
			return this.ToXml().ToString();
		}
	}
}
