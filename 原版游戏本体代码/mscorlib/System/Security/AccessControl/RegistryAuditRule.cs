using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x0200022E RID: 558
	public sealed class RegistryAuditRule : AuditRule
	{
		// Token: 0x0600201E RID: 8222 RVA: 0x00071056 File Offset: 0x0006F256
		public RegistryAuditRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
			: this(identity, (int)registryRights, false, inheritanceFlags, propagationFlags, flags)
		{
		}

		// Token: 0x0600201F RID: 8223 RVA: 0x00071066 File Offset: 0x0006F266
		public RegistryAuditRule(string identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
			: this(new NTAccount(identity), (int)registryRights, false, inheritanceFlags, propagationFlags, flags)
		{
		}

		// Token: 0x06002020 RID: 8224 RVA: 0x0007107B File Offset: 0x0006F27B
		internal RegistryAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
		{
		}

		// Token: 0x170003B9 RID: 953
		// (get) Token: 0x06002021 RID: 8225 RVA: 0x0007108C File Offset: 0x0006F28C
		public RegistryRights RegistryRights
		{
			get
			{
				return (RegistryRights)base.AccessMask;
			}
		}
	}
}
