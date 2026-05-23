using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000216 RID: 534
	public sealed class EventWaitHandleAuditRule : AuditRule
	{
		// Token: 0x06001F1B RID: 7963 RVA: 0x0006D3C6 File Offset: 0x0006B5C6
		public EventWaitHandleAuditRule(IdentityReference identity, EventWaitHandleRights eventRights, AuditFlags flags)
			: this(identity, (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, flags)
		{
		}

		// Token: 0x06001F1C RID: 7964 RVA: 0x0006D3D4 File Offset: 0x0006B5D4
		internal EventWaitHandleAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
		{
		}

		// Token: 0x17000396 RID: 918
		// (get) Token: 0x06001F1D RID: 7965 RVA: 0x0006D3E5 File Offset: 0x0006B5E5
		public EventWaitHandleRights EventWaitHandleRights
		{
			get
			{
				return (EventWaitHandleRights)base.AccessMask;
			}
		}
	}
}
