using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000234 RID: 564
	public abstract class AuditRule : AuthorizationRule
	{
		// Token: 0x06002043 RID: 8259 RVA: 0x00071518 File Offset: 0x0006F718
		protected AuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags auditFlags)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags)
		{
			if (auditFlags == AuditFlags.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumAtLeastOneFlag"), "auditFlags");
			}
			if ((auditFlags & ~(AuditFlags.Success | AuditFlags.Failure)) != AuditFlags.None)
			{
				throw new ArgumentOutOfRangeException("auditFlags", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
			}
			this._flags = auditFlags;
		}

		// Token: 0x170003C6 RID: 966
		// (get) Token: 0x06002044 RID: 8260 RVA: 0x0007156F File Offset: 0x0006F76F
		public AuditFlags AuditFlags
		{
			get
			{
				return this._flags;
			}
		}

		// Token: 0x04000BAC RID: 2988
		private readonly AuditFlags _flags;
	}
}
