using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000220 RID: 544
	public sealed class MutexAuditRule : AuditRule
	{
		// Token: 0x06001F63 RID: 8035 RVA: 0x0006DC3A File Offset: 0x0006BE3A
		public MutexAuditRule(IdentityReference identity, MutexRights eventRights, AuditFlags flags)
			: this(identity, (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, flags)
		{
		}

		// Token: 0x06001F64 RID: 8036 RVA: 0x0006DC48 File Offset: 0x0006BE48
		internal MutexAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
		{
		}

		// Token: 0x170003A0 RID: 928
		// (get) Token: 0x06001F65 RID: 8037 RVA: 0x0006DC59 File Offset: 0x0006BE59
		public MutexRights MutexRights
		{
			get
			{
				return (MutexRights)base.AccessMask;
			}
		}
	}
}
