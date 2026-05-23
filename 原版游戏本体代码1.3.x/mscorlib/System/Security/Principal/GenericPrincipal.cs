using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Claims;

namespace System.Security.Principal
{
	// Token: 0x02000323 RID: 803
	[ComVisible(true)]
	[Serializable]
	public class GenericPrincipal : ClaimsPrincipal
	{
		// Token: 0x06002898 RID: 10392 RVA: 0x00094E34 File Offset: 0x00093034
		public GenericPrincipal(IIdentity identity, string[] roles)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}
			this.m_identity = identity;
			if (roles != null)
			{
				this.m_roles = new string[roles.Length];
				for (int i = 0; i < roles.Length; i++)
				{
					this.m_roles[i] = roles[i];
				}
			}
			else
			{
				this.m_roles = null;
			}
			this.AddIdentityWithRoles(this.m_identity, this.m_roles);
		}

		// Token: 0x06002899 RID: 10393 RVA: 0x00094EA4 File Offset: 0x000930A4
		[OnDeserialized]
		private void OnDeserializedMethod(StreamingContext context)
		{
			ClaimsIdentity claimsIdentity = null;
			foreach (ClaimsIdentity claimsIdentity2 in base.Identities)
			{
				if (claimsIdentity2 != null)
				{
					claimsIdentity = claimsIdentity2;
					break;
				}
			}
			if (this.m_roles != null && this.m_roles.Length != 0 && claimsIdentity != null)
			{
				claimsIdentity.ExternalClaims.Add(new RoleClaimProvider("LOCAL AUTHORITY", this.m_roles, claimsIdentity).Claims);
				return;
			}
			if (claimsIdentity == null)
			{
				this.AddIdentityWithRoles(this.m_identity, this.m_roles);
			}
		}

		// Token: 0x0600289A RID: 10394 RVA: 0x00094F40 File Offset: 0x00093140
		[SecuritySafeCritical]
		private void AddIdentityWithRoles(IIdentity identity, string[] roles)
		{
			ClaimsIdentity claimsIdentity = identity as ClaimsIdentity;
			if (claimsIdentity != null)
			{
				claimsIdentity = claimsIdentity.Clone();
			}
			else
			{
				claimsIdentity = new ClaimsIdentity(identity);
			}
			if (roles != null && roles.Length != 0)
			{
				claimsIdentity.ExternalClaims.Add(new RoleClaimProvider("LOCAL AUTHORITY", roles, claimsIdentity).Claims);
			}
			base.AddIdentity(claimsIdentity);
		}

		// Token: 0x1700053E RID: 1342
		// (get) Token: 0x0600289B RID: 10395 RVA: 0x00094F91 File Offset: 0x00093191
		public override IIdentity Identity
		{
			get
			{
				return this.m_identity;
			}
		}

		// Token: 0x0600289C RID: 10396 RVA: 0x00094F9C File Offset: 0x0009319C
		public override bool IsInRole(string role)
		{
			if (role == null || this.m_roles == null)
			{
				return false;
			}
			for (int i = 0; i < this.m_roles.Length; i++)
			{
				if (this.m_roles[i] != null && string.Compare(this.m_roles[i], role, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return true;
				}
			}
			return base.IsInRole(role);
		}

		// Token: 0x0400100D RID: 4109
		private IIdentity m_identity;

		// Token: 0x0400100E RID: 4110
		private string[] m_roles;
	}
}
