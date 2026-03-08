using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000225 RID: 549
	public class AccessRule<T> : AccessRule where T : struct
	{
		// Token: 0x06001FBC RID: 8124 RVA: 0x0006ED66 File Offset: 0x0006CF66
		public AccessRule(IdentityReference identity, T rights, AccessControlType type)
			: this(identity, (int)((object)rights), false, InheritanceFlags.None, PropagationFlags.None, type)
		{
		}

		// Token: 0x06001FBD RID: 8125 RVA: 0x0006ED7E File Offset: 0x0006CF7E
		public AccessRule(string identity, T rights, AccessControlType type)
			: this(new NTAccount(identity), (int)((object)rights), false, InheritanceFlags.None, PropagationFlags.None, type)
		{
		}

		// Token: 0x06001FBE RID: 8126 RVA: 0x0006ED9B File Offset: 0x0006CF9B
		public AccessRule(IdentityReference identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
			: this(identity, (int)((object)rights), false, inheritanceFlags, propagationFlags, type)
		{
		}

		// Token: 0x06001FBF RID: 8127 RVA: 0x0006EDB5 File Offset: 0x0006CFB5
		public AccessRule(string identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
			: this(new NTAccount(identity), (int)((object)rights), false, inheritanceFlags, propagationFlags, type)
		{
		}

		// Token: 0x06001FC0 RID: 8128 RVA: 0x0006EDD4 File Offset: 0x0006CFD4
		internal AccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, type)
		{
		}

		// Token: 0x170003B1 RID: 945
		// (get) Token: 0x06001FC1 RID: 8129 RVA: 0x0006EDE5 File Offset: 0x0006CFE5
		public T Rights
		{
			get
			{
				return (T)((object)base.AccessMask);
			}
		}
	}
}
