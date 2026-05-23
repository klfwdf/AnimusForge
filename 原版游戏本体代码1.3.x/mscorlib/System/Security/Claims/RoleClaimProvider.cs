using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Security.Claims
{
	// Token: 0x02000321 RID: 801
	[ComVisible(false)]
	internal class RoleClaimProvider
	{
		// Token: 0x0600288B RID: 10379 RVA: 0x00094CA5 File Offset: 0x00092EA5
		public RoleClaimProvider(string issuer, string[] roles, ClaimsIdentity subject)
		{
			this.m_issuer = issuer;
			this.m_roles = roles;
			this.m_subject = subject;
		}

		// Token: 0x17000539 RID: 1337
		// (get) Token: 0x0600288C RID: 10380 RVA: 0x00094CC4 File Offset: 0x00092EC4
		public IEnumerable<Claim> Claims
		{
			get
			{
				int num;
				for (int i = 0; i < this.m_roles.Length; i = num + 1)
				{
					if (this.m_roles[i] != null)
					{
						yield return new Claim(this.m_subject.RoleClaimType, this.m_roles[i], "http://www.w3.org/2001/XMLSchema#string", this.m_issuer, this.m_issuer, this.m_subject);
					}
					num = i;
				}
				yield break;
			}
		}

		// Token: 0x04001008 RID: 4104
		private string m_issuer;

		// Token: 0x04001009 RID: 4105
		private string[] m_roles;

		// Token: 0x0400100A RID: 4106
		private ClaimsIdentity m_subject;
	}
}
