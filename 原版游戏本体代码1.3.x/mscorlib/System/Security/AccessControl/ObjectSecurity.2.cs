using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000227 RID: 551
	public abstract class ObjectSecurity<T> : NativeObjectSecurity where T : struct
	{
		// Token: 0x06001FC8 RID: 8136 RVA: 0x0006EE72 File Offset: 0x0006D072
		protected ObjectSecurity(bool isContainer, ResourceType resourceType)
			: base(isContainer, resourceType, null, null)
		{
		}

		// Token: 0x06001FC9 RID: 8137 RVA: 0x0006EE7E File Offset: 0x0006D07E
		protected ObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections)
			: base(isContainer, resourceType, name, includeSections, null, null)
		{
		}

		// Token: 0x06001FCA RID: 8138 RVA: 0x0006EE8D File Offset: 0x0006D08D
		protected ObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
			: base(isContainer, resourceType, name, includeSections, exceptionFromErrorCode, exceptionContext)
		{
		}

		// Token: 0x06001FCB RID: 8139 RVA: 0x0006EE9E File Offset: 0x0006D09E
		[SecuritySafeCritical]
		protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle safeHandle, AccessControlSections includeSections)
			: base(isContainer, resourceType, safeHandle, includeSections, null, null)
		{
		}

		// Token: 0x06001FCC RID: 8140 RVA: 0x0006EEAD File Offset: 0x0006D0AD
		[SecuritySafeCritical]
		protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle safeHandle, AccessControlSections includeSections, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
			: base(isContainer, resourceType, safeHandle, includeSections, exceptionFromErrorCode, exceptionContext)
		{
		}

		// Token: 0x06001FCD RID: 8141 RVA: 0x0006EEBE File Offset: 0x0006D0BE
		public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		{
			return new AccessRule<T>(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
		}

		// Token: 0x06001FCE RID: 8142 RVA: 0x0006EECE File Offset: 0x0006D0CE
		public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
		{
			return new AuditRule<T>(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
		}

		// Token: 0x06001FCF RID: 8143 RVA: 0x0006EEE0 File Offset: 0x0006D0E0
		private AccessControlSections GetAccessControlSectionsFromChanges()
		{
			AccessControlSections accessControlSections = AccessControlSections.None;
			if (base.AccessRulesModified)
			{
				accessControlSections = AccessControlSections.Access;
			}
			if (base.AuditRulesModified)
			{
				accessControlSections |= AccessControlSections.Audit;
			}
			if (base.OwnerModified)
			{
				accessControlSections |= AccessControlSections.Owner;
			}
			if (base.GroupModified)
			{
				accessControlSections |= AccessControlSections.Group;
			}
			return accessControlSections;
		}

		// Token: 0x06001FD0 RID: 8144 RVA: 0x0006EF20 File Offset: 0x0006D120
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
		protected internal void Persist(SafeHandle handle)
		{
			base.WriteLock();
			try
			{
				AccessControlSections accessControlSectionsFromChanges = this.GetAccessControlSectionsFromChanges();
				base.Persist(handle, accessControlSectionsFromChanges);
				base.OwnerModified = (base.GroupModified = (base.AuditRulesModified = (base.AccessRulesModified = false)));
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FD1 RID: 8145 RVA: 0x0006EF80 File Offset: 0x0006D180
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
		protected internal void Persist(string name)
		{
			base.WriteLock();
			try
			{
				AccessControlSections accessControlSectionsFromChanges = this.GetAccessControlSectionsFromChanges();
				base.Persist(name, accessControlSectionsFromChanges);
				base.OwnerModified = (base.GroupModified = (base.AuditRulesModified = (base.AccessRulesModified = false)));
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FD2 RID: 8146 RVA: 0x0006EFE0 File Offset: 0x0006D1E0
		public virtual void AddAccessRule(AccessRule<T> rule)
		{
			base.AddAccessRule(rule);
		}

		// Token: 0x06001FD3 RID: 8147 RVA: 0x0006EFE9 File Offset: 0x0006D1E9
		public virtual void SetAccessRule(AccessRule<T> rule)
		{
			base.SetAccessRule(rule);
		}

		// Token: 0x06001FD4 RID: 8148 RVA: 0x0006EFF2 File Offset: 0x0006D1F2
		public virtual void ResetAccessRule(AccessRule<T> rule)
		{
			base.ResetAccessRule(rule);
		}

		// Token: 0x06001FD5 RID: 8149 RVA: 0x0006EFFB File Offset: 0x0006D1FB
		public virtual bool RemoveAccessRule(AccessRule<T> rule)
		{
			return base.RemoveAccessRule(rule);
		}

		// Token: 0x06001FD6 RID: 8150 RVA: 0x0006F004 File Offset: 0x0006D204
		public virtual void RemoveAccessRuleAll(AccessRule<T> rule)
		{
			base.RemoveAccessRuleAll(rule);
		}

		// Token: 0x06001FD7 RID: 8151 RVA: 0x0006F00D File Offset: 0x0006D20D
		public virtual void RemoveAccessRuleSpecific(AccessRule<T> rule)
		{
			base.RemoveAccessRuleSpecific(rule);
		}

		// Token: 0x06001FD8 RID: 8152 RVA: 0x0006F016 File Offset: 0x0006D216
		public virtual void AddAuditRule(AuditRule<T> rule)
		{
			base.AddAuditRule(rule);
		}

		// Token: 0x06001FD9 RID: 8153 RVA: 0x0006F01F File Offset: 0x0006D21F
		public virtual void SetAuditRule(AuditRule<T> rule)
		{
			base.SetAuditRule(rule);
		}

		// Token: 0x06001FDA RID: 8154 RVA: 0x0006F028 File Offset: 0x0006D228
		public virtual bool RemoveAuditRule(AuditRule<T> rule)
		{
			return base.RemoveAuditRule(rule);
		}

		// Token: 0x06001FDB RID: 8155 RVA: 0x0006F031 File Offset: 0x0006D231
		public virtual void RemoveAuditRuleAll(AuditRule<T> rule)
		{
			base.RemoveAuditRuleAll(rule);
		}

		// Token: 0x06001FDC RID: 8156 RVA: 0x0006F03A File Offset: 0x0006D23A
		public virtual void RemoveAuditRuleSpecific(AuditRule<T> rule)
		{
			base.RemoveAuditRuleSpecific(rule);
		}

		// Token: 0x170003B3 RID: 947
		// (get) Token: 0x06001FDD RID: 8157 RVA: 0x0006F043 File Offset: 0x0006D243
		public override Type AccessRightType
		{
			get
			{
				return typeof(T);
			}
		}

		// Token: 0x170003B4 RID: 948
		// (get) Token: 0x06001FDE RID: 8158 RVA: 0x0006F04F File Offset: 0x0006D24F
		public override Type AccessRuleType
		{
			get
			{
				return typeof(AccessRule<T>);
			}
		}

		// Token: 0x170003B5 RID: 949
		// (get) Token: 0x06001FDF RID: 8159 RVA: 0x0006F05B File Offset: 0x0006D25B
		public override Type AuditRuleType
		{
			get
			{
				return typeof(AuditRule<T>);
			}
		}
	}
}
