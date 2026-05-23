using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x0200021F RID: 543
	public sealed class MutexAccessRule : AccessRule
	{
		// Token: 0x06001F5F RID: 8031 RVA: 0x0006DC00 File Offset: 0x0006BE00
		public MutexAccessRule(IdentityReference identity, MutexRights eventRights, AccessControlType type)
			: this(identity, (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, type)
		{
		}

		// Token: 0x06001F60 RID: 8032 RVA: 0x0006DC0E File Offset: 0x0006BE0E
		public MutexAccessRule(string identity, MutexRights eventRights, AccessControlType type)
			: this(new NTAccount(identity), (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, type)
		{
		}

		// Token: 0x06001F61 RID: 8033 RVA: 0x0006DC21 File Offset: 0x0006BE21
		internal MutexAccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, type)
		{
		}

		// Token: 0x1700039F RID: 927
		// (get) Token: 0x06001F62 RID: 8034 RVA: 0x0006DC32 File Offset: 0x0006BE32
		public MutexRights MutexRights
		{
			get
			{
				return (MutexRights)base.AccessMask;
			}
		}
	}
}
