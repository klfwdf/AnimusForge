using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x0200020E RID: 526
	public sealed class SystemAcl : CommonAcl
	{
		// Token: 0x06001ED3 RID: 7891 RVA: 0x0006CA48 File Offset: 0x0006AC48
		public SystemAcl(bool isContainer, bool isDS, int capacity)
			: this(isContainer, isDS, isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision, capacity)
		{
		}

		// Token: 0x06001ED4 RID: 7892 RVA: 0x0006CA62 File Offset: 0x0006AC62
		public SystemAcl(bool isContainer, bool isDS, byte revision, int capacity)
			: base(isContainer, isDS, revision, capacity)
		{
		}

		// Token: 0x06001ED5 RID: 7893 RVA: 0x0006CA6F File Offset: 0x0006AC6F
		public SystemAcl(bool isContainer, bool isDS, RawAcl rawAcl)
			: this(isContainer, isDS, rawAcl, false)
		{
		}

		// Token: 0x06001ED6 RID: 7894 RVA: 0x0006CA7B File Offset: 0x0006AC7B
		internal SystemAcl(bool isContainer, bool isDS, RawAcl rawAcl, bool trusted)
			: base(isContainer, isDS, rawAcl, trusted, false)
		{
		}

		// Token: 0x06001ED7 RID: 7895 RVA: 0x0006CA89 File Offset: 0x0006AC89
		public void AddAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
		{
			base.CheckFlags(inheritanceFlags, propagationFlags);
			base.AddQualifiedAce(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
		}

		// Token: 0x06001ED8 RID: 7896 RVA: 0x0006CAB9 File Offset: 0x0006ACB9
		public void SetAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
		{
			base.CheckFlags(inheritanceFlags, propagationFlags);
			base.SetQualifiedAce(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
		}

		// Token: 0x06001ED9 RID: 7897 RVA: 0x0006CAEC File Offset: 0x0006ACEC
		public bool RemoveAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
		{
			return base.RemoveQualifiedAces(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), true, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
		}

		// Token: 0x06001EDA RID: 7898 RVA: 0x0006CB1E File Offset: 0x0006AD1E
		public void RemoveAuditSpecific(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
		{
			base.RemoveQualifiedAcesSpecific(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
		}

		// Token: 0x06001EDB RID: 7899 RVA: 0x0006CB44 File Offset: 0x0006AD44
		public void AddAudit(SecurityIdentifier sid, ObjectAuditRule rule)
		{
			this.AddAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
		}

		// Token: 0x06001EDC RID: 7900 RVA: 0x0006CB84 File Offset: 0x0006AD84
		public void AddAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
		{
			if (!base.IsDS)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_OnlyValidForDS"));
			}
			base.CheckFlags(inheritanceFlags, propagationFlags);
			base.AddQualifiedAce(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
		}

		// Token: 0x06001EDD RID: 7901 RVA: 0x0006CBD4 File Offset: 0x0006ADD4
		public void SetAudit(SecurityIdentifier sid, ObjectAuditRule rule)
		{
			this.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
		}

		// Token: 0x06001EDE RID: 7902 RVA: 0x0006CC14 File Offset: 0x0006AE14
		public void SetAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
		{
			if (!base.IsDS)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_OnlyValidForDS"));
			}
			base.CheckFlags(inheritanceFlags, propagationFlags);
			base.SetQualifiedAce(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
		}

		// Token: 0x06001EDF RID: 7903 RVA: 0x0006CC64 File Offset: 0x0006AE64
		public bool RemoveAudit(SecurityIdentifier sid, ObjectAuditRule rule)
		{
			return this.RemoveAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
		}

		// Token: 0x06001EE0 RID: 7904 RVA: 0x0006CCA4 File Offset: 0x0006AEA4
		public bool RemoveAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
		{
			if (!base.IsDS)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_OnlyValidForDS"));
			}
			return base.RemoveQualifiedAces(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), true, objectFlags, objectType, inheritedObjectType);
		}

		// Token: 0x06001EE1 RID: 7905 RVA: 0x0006CCEC File Offset: 0x0006AEEC
		public void RemoveAuditSpecific(SecurityIdentifier sid, ObjectAuditRule rule)
		{
			this.RemoveAuditSpecific(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
		}

		// Token: 0x06001EE2 RID: 7906 RVA: 0x0006CD2A File Offset: 0x0006AF2A
		public void RemoveAuditSpecific(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
		{
			if (!base.IsDS)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_OnlyValidForDS"));
			}
			base.RemoveQualifiedAcesSpecific(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
		}
	}
}
