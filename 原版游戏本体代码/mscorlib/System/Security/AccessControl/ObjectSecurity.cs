using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

namespace System.Security.AccessControl
{
	// Token: 0x02000224 RID: 548
	public abstract class ObjectSecurity
	{
		// Token: 0x06001F8A RID: 8074 RVA: 0x0006E380 File Offset: 0x0006C580
		protected ObjectSecurity()
		{
		}

		// Token: 0x06001F8B RID: 8075 RVA: 0x0006E394 File Offset: 0x0006C594
		protected ObjectSecurity(bool isContainer, bool isDS)
			: this()
		{
			DiscretionaryAcl discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, 5);
			this._securityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, ControlFlags.None, null, null, null, discretionaryAcl);
		}

		// Token: 0x06001F8C RID: 8076 RVA: 0x0006E3C2 File Offset: 0x0006C5C2
		protected ObjectSecurity(CommonSecurityDescriptor securityDescriptor)
			: this()
		{
			if (securityDescriptor == null)
			{
				throw new ArgumentNullException("securityDescriptor");
			}
			this._securityDescriptor = securityDescriptor;
		}

		// Token: 0x06001F8D RID: 8077 RVA: 0x0006E3E0 File Offset: 0x0006C5E0
		private void UpdateWithNewSecurityDescriptor(RawSecurityDescriptor newOne, AccessControlSections includeSections)
		{
			if ((includeSections & AccessControlSections.Owner) != AccessControlSections.None)
			{
				this._ownerModified = true;
				this._securityDescriptor.Owner = newOne.Owner;
			}
			if ((includeSections & AccessControlSections.Group) != AccessControlSections.None)
			{
				this._groupModified = true;
				this._securityDescriptor.Group = newOne.Group;
			}
			if ((includeSections & AccessControlSections.Audit) != AccessControlSections.None)
			{
				this._saclModified = true;
				if (newOne.SystemAcl != null)
				{
					this._securityDescriptor.SystemAcl = new SystemAcl(this.IsContainer, this.IsDS, newOne.SystemAcl, true);
				}
				else
				{
					this._securityDescriptor.SystemAcl = null;
				}
				this._securityDescriptor.UpdateControlFlags(ObjectSecurity.SACL_CONTROL_FLAGS, newOne.ControlFlags & ObjectSecurity.SACL_CONTROL_FLAGS);
			}
			if ((includeSections & AccessControlSections.Access) != AccessControlSections.None)
			{
				this._daclModified = true;
				if (newOne.DiscretionaryAcl != null)
				{
					this._securityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(this.IsContainer, this.IsDS, newOne.DiscretionaryAcl, true);
				}
				else
				{
					this._securityDescriptor.DiscretionaryAcl = null;
				}
				ControlFlags controlFlags = this._securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent;
				this._securityDescriptor.UpdateControlFlags(ObjectSecurity.DACL_CONTROL_FLAGS, (newOne.ControlFlags | controlFlags) & ObjectSecurity.DACL_CONTROL_FLAGS);
			}
		}

		// Token: 0x06001F8E RID: 8078 RVA: 0x0006E4F9 File Offset: 0x0006C6F9
		protected void ReadLock()
		{
			this._lock.AcquireReaderLock(-1);
		}

		// Token: 0x06001F8F RID: 8079 RVA: 0x0006E507 File Offset: 0x0006C707
		protected void ReadUnlock()
		{
			this._lock.ReleaseReaderLock();
		}

		// Token: 0x06001F90 RID: 8080 RVA: 0x0006E514 File Offset: 0x0006C714
		protected void WriteLock()
		{
			this._lock.AcquireWriterLock(-1);
		}

		// Token: 0x06001F91 RID: 8081 RVA: 0x0006E522 File Offset: 0x0006C722
		protected void WriteUnlock()
		{
			this._lock.ReleaseWriterLock();
		}

		// Token: 0x170003A4 RID: 932
		// (get) Token: 0x06001F92 RID: 8082 RVA: 0x0006E52F File Offset: 0x0006C72F
		// (set) Token: 0x06001F93 RID: 8083 RVA: 0x0006E561 File Offset: 0x0006C761
		protected bool OwnerModified
		{
			get
			{
				if (!this._lock.IsReaderLockHeld && !this._lock.IsWriterLockHeld)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MustLockForReadOrWrite"));
				}
				return this._ownerModified;
			}
			set
			{
				if (!this._lock.IsWriterLockHeld)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MustLockForWrite"));
				}
				this._ownerModified = value;
			}
		}

		// Token: 0x170003A5 RID: 933
		// (get) Token: 0x06001F94 RID: 8084 RVA: 0x0006E587 File Offset: 0x0006C787
		// (set) Token: 0x06001F95 RID: 8085 RVA: 0x0006E5B9 File Offset: 0x0006C7B9
		protected bool GroupModified
		{
			get
			{
				if (!this._lock.IsReaderLockHeld && !this._lock.IsWriterLockHeld)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MustLockForReadOrWrite"));
				}
				return this._groupModified;
			}
			set
			{
				if (!this._lock.IsWriterLockHeld)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MustLockForWrite"));
				}
				this._groupModified = value;
			}
		}

		// Token: 0x170003A6 RID: 934
		// (get) Token: 0x06001F96 RID: 8086 RVA: 0x0006E5DF File Offset: 0x0006C7DF
		// (set) Token: 0x06001F97 RID: 8087 RVA: 0x0006E611 File Offset: 0x0006C811
		protected bool AuditRulesModified
		{
			get
			{
				if (!this._lock.IsReaderLockHeld && !this._lock.IsWriterLockHeld)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MustLockForReadOrWrite"));
				}
				return this._saclModified;
			}
			set
			{
				if (!this._lock.IsWriterLockHeld)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MustLockForWrite"));
				}
				this._saclModified = value;
			}
		}

		// Token: 0x170003A7 RID: 935
		// (get) Token: 0x06001F98 RID: 8088 RVA: 0x0006E637 File Offset: 0x0006C837
		// (set) Token: 0x06001F99 RID: 8089 RVA: 0x0006E669 File Offset: 0x0006C869
		protected bool AccessRulesModified
		{
			get
			{
				if (!this._lock.IsReaderLockHeld && !this._lock.IsWriterLockHeld)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MustLockForReadOrWrite"));
				}
				return this._daclModified;
			}
			set
			{
				if (!this._lock.IsWriterLockHeld)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MustLockForWrite"));
				}
				this._daclModified = value;
			}
		}

		// Token: 0x170003A8 RID: 936
		// (get) Token: 0x06001F9A RID: 8090 RVA: 0x0006E68F File Offset: 0x0006C88F
		protected bool IsContainer
		{
			get
			{
				return this._securityDescriptor.IsContainer;
			}
		}

		// Token: 0x170003A9 RID: 937
		// (get) Token: 0x06001F9B RID: 8091 RVA: 0x0006E69C File Offset: 0x0006C89C
		protected bool IsDS
		{
			get
			{
				return this._securityDescriptor.IsDS;
			}
		}

		// Token: 0x06001F9C RID: 8092 RVA: 0x0006E6A9 File Offset: 0x0006C8A9
		protected virtual void Persist(string name, AccessControlSections includeSections)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001F9D RID: 8093 RVA: 0x0006E6B0 File Offset: 0x0006C8B0
		[SecuritySafeCritical]
		[HandleProcessCorruptedStateExceptions]
		protected virtual void Persist(bool enableOwnershipPrivilege, string name, AccessControlSections includeSections)
		{
			Privilege privilege = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				if (enableOwnershipPrivilege)
				{
					privilege = new Privilege("SeTakeOwnershipPrivilege");
					try
					{
						privilege.Enable();
					}
					catch (PrivilegeNotHeldException)
					{
					}
				}
				this.Persist(name, includeSections);
			}
			catch
			{
				if (privilege != null)
				{
					privilege.Revert();
				}
				throw;
			}
			finally
			{
				if (privilege != null)
				{
					privilege.Revert();
				}
			}
		}

		// Token: 0x06001F9E RID: 8094 RVA: 0x0006E728 File Offset: 0x0006C928
		[SecuritySafeCritical]
		protected virtual void Persist(SafeHandle handle, AccessControlSections includeSections)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001F9F RID: 8095 RVA: 0x0006E730 File Offset: 0x0006C930
		public IdentityReference GetOwner(Type targetType)
		{
			this.ReadLock();
			IdentityReference result;
			try
			{
				if (this._securityDescriptor.Owner == null)
				{
					result = null;
				}
				else
				{
					result = this._securityDescriptor.Owner.Translate(targetType);
				}
			}
			finally
			{
				this.ReadUnlock();
			}
			return result;
		}

		// Token: 0x06001FA0 RID: 8096 RVA: 0x0006E788 File Offset: 0x0006C988
		public void SetOwner(IdentityReference identity)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}
			this.WriteLock();
			try
			{
				this._securityDescriptor.Owner = identity.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
				this._ownerModified = true;
			}
			finally
			{
				this.WriteUnlock();
			}
		}

		// Token: 0x06001FA1 RID: 8097 RVA: 0x0006E7F0 File Offset: 0x0006C9F0
		public IdentityReference GetGroup(Type targetType)
		{
			this.ReadLock();
			IdentityReference result;
			try
			{
				if (this._securityDescriptor.Group == null)
				{
					result = null;
				}
				else
				{
					result = this._securityDescriptor.Group.Translate(targetType);
				}
			}
			finally
			{
				this.ReadUnlock();
			}
			return result;
		}

		// Token: 0x06001FA2 RID: 8098 RVA: 0x0006E848 File Offset: 0x0006CA48
		public void SetGroup(IdentityReference identity)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}
			this.WriteLock();
			try
			{
				this._securityDescriptor.Group = identity.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
				this._groupModified = true;
			}
			finally
			{
				this.WriteUnlock();
			}
		}

		// Token: 0x06001FA3 RID: 8099 RVA: 0x0006E8B0 File Offset: 0x0006CAB0
		public virtual void PurgeAccessRules(IdentityReference identity)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}
			this.WriteLock();
			try
			{
				this._securityDescriptor.PurgeAccessControl(identity.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier);
				this._daclModified = true;
			}
			finally
			{
				this.WriteUnlock();
			}
		}

		// Token: 0x06001FA4 RID: 8100 RVA: 0x0006E918 File Offset: 0x0006CB18
		public virtual void PurgeAuditRules(IdentityReference identity)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}
			this.WriteLock();
			try
			{
				this._securityDescriptor.PurgeAudit(identity.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier);
				this._saclModified = true;
			}
			finally
			{
				this.WriteUnlock();
			}
		}

		// Token: 0x170003AA RID: 938
		// (get) Token: 0x06001FA5 RID: 8101 RVA: 0x0006E980 File Offset: 0x0006CB80
		public bool AreAccessRulesProtected
		{
			get
			{
				this.ReadLock();
				bool result;
				try
				{
					result = (this._securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) > ControlFlags.None;
				}
				finally
				{
					this.ReadUnlock();
				}
				return result;
			}
		}

		// Token: 0x06001FA6 RID: 8102 RVA: 0x0006E9C4 File Offset: 0x0006CBC4
		public void SetAccessRuleProtection(bool isProtected, bool preserveInheritance)
		{
			this.WriteLock();
			try
			{
				this._securityDescriptor.SetDiscretionaryAclProtection(isProtected, preserveInheritance);
				this._daclModified = true;
			}
			finally
			{
				this.WriteUnlock();
			}
		}

		// Token: 0x170003AB RID: 939
		// (get) Token: 0x06001FA7 RID: 8103 RVA: 0x0006EA04 File Offset: 0x0006CC04
		public bool AreAuditRulesProtected
		{
			get
			{
				this.ReadLock();
				bool result;
				try
				{
					result = (this._securityDescriptor.ControlFlags & ControlFlags.SystemAclProtected) > ControlFlags.None;
				}
				finally
				{
					this.ReadUnlock();
				}
				return result;
			}
		}

		// Token: 0x06001FA8 RID: 8104 RVA: 0x0006EA48 File Offset: 0x0006CC48
		public void SetAuditRuleProtection(bool isProtected, bool preserveInheritance)
		{
			this.WriteLock();
			try
			{
				this._securityDescriptor.SetSystemAclProtection(isProtected, preserveInheritance);
				this._saclModified = true;
			}
			finally
			{
				this.WriteUnlock();
			}
		}

		// Token: 0x170003AC RID: 940
		// (get) Token: 0x06001FA9 RID: 8105 RVA: 0x0006EA88 File Offset: 0x0006CC88
		public bool AreAccessRulesCanonical
		{
			get
			{
				this.ReadLock();
				bool isDiscretionaryAclCanonical;
				try
				{
					isDiscretionaryAclCanonical = this._securityDescriptor.IsDiscretionaryAclCanonical;
				}
				finally
				{
					this.ReadUnlock();
				}
				return isDiscretionaryAclCanonical;
			}
		}

		// Token: 0x170003AD RID: 941
		// (get) Token: 0x06001FAA RID: 8106 RVA: 0x0006EAC4 File Offset: 0x0006CCC4
		public bool AreAuditRulesCanonical
		{
			get
			{
				this.ReadLock();
				bool isSystemAclCanonical;
				try
				{
					isSystemAclCanonical = this._securityDescriptor.IsSystemAclCanonical;
				}
				finally
				{
					this.ReadUnlock();
				}
				return isSystemAclCanonical;
			}
		}

		// Token: 0x06001FAB RID: 8107 RVA: 0x0006EB00 File Offset: 0x0006CD00
		public static bool IsSddlConversionSupported()
		{
			return true;
		}

		// Token: 0x06001FAC RID: 8108 RVA: 0x0006EB04 File Offset: 0x0006CD04
		public string GetSecurityDescriptorSddlForm(AccessControlSections includeSections)
		{
			this.ReadLock();
			string sddlForm;
			try
			{
				sddlForm = this._securityDescriptor.GetSddlForm(includeSections);
			}
			finally
			{
				this.ReadUnlock();
			}
			return sddlForm;
		}

		// Token: 0x06001FAD RID: 8109 RVA: 0x0006EB40 File Offset: 0x0006CD40
		public void SetSecurityDescriptorSddlForm(string sddlForm)
		{
			this.SetSecurityDescriptorSddlForm(sddlForm, AccessControlSections.All);
		}

		// Token: 0x06001FAE RID: 8110 RVA: 0x0006EB4C File Offset: 0x0006CD4C
		public void SetSecurityDescriptorSddlForm(string sddlForm, AccessControlSections includeSections)
		{
			if (sddlForm == null)
			{
				throw new ArgumentNullException("sddlForm");
			}
			if ((includeSections & AccessControlSections.All) == AccessControlSections.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumAtLeastOneFlag"), "includeSections");
			}
			this.WriteLock();
			try
			{
				this.UpdateWithNewSecurityDescriptor(new RawSecurityDescriptor(sddlForm), includeSections);
			}
			finally
			{
				this.WriteUnlock();
			}
		}

		// Token: 0x06001FAF RID: 8111 RVA: 0x0006EBB0 File Offset: 0x0006CDB0
		public byte[] GetSecurityDescriptorBinaryForm()
		{
			this.ReadLock();
			byte[] result;
			try
			{
				byte[] array = new byte[this._securityDescriptor.BinaryLength];
				this._securityDescriptor.GetBinaryForm(array, 0);
				result = array;
			}
			finally
			{
				this.ReadUnlock();
			}
			return result;
		}

		// Token: 0x06001FB0 RID: 8112 RVA: 0x0006EC00 File Offset: 0x0006CE00
		public void SetSecurityDescriptorBinaryForm(byte[] binaryForm)
		{
			this.SetSecurityDescriptorBinaryForm(binaryForm, AccessControlSections.All);
		}

		// Token: 0x06001FB1 RID: 8113 RVA: 0x0006EC0C File Offset: 0x0006CE0C
		public void SetSecurityDescriptorBinaryForm(byte[] binaryForm, AccessControlSections includeSections)
		{
			if (binaryForm == null)
			{
				throw new ArgumentNullException("binaryForm");
			}
			if ((includeSections & AccessControlSections.All) == AccessControlSections.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumAtLeastOneFlag"), "includeSections");
			}
			this.WriteLock();
			try
			{
				this.UpdateWithNewSecurityDescriptor(new RawSecurityDescriptor(binaryForm, 0), includeSections);
			}
			finally
			{
				this.WriteUnlock();
			}
		}

		// Token: 0x170003AE RID: 942
		// (get) Token: 0x06001FB2 RID: 8114
		public abstract Type AccessRightType { get; }

		// Token: 0x170003AF RID: 943
		// (get) Token: 0x06001FB3 RID: 8115
		public abstract Type AccessRuleType { get; }

		// Token: 0x170003B0 RID: 944
		// (get) Token: 0x06001FB4 RID: 8116
		public abstract Type AuditRuleType { get; }

		// Token: 0x06001FB5 RID: 8117
		protected abstract bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified);

		// Token: 0x06001FB6 RID: 8118
		protected abstract bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified);

		// Token: 0x06001FB7 RID: 8119 RVA: 0x0006EC70 File Offset: 0x0006CE70
		public virtual bool ModifyAccessRule(AccessControlModification modification, AccessRule rule, out bool modified)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			if (!this.AccessRuleType.IsAssignableFrom(rule.GetType()))
			{
				throw new ArgumentException(Environment.GetResourceString("AccessControl_InvalidAccessRuleType"), "rule");
			}
			this.WriteLock();
			bool result;
			try
			{
				result = this.ModifyAccess(modification, rule, out modified);
			}
			finally
			{
				this.WriteUnlock();
			}
			return result;
		}

		// Token: 0x06001FB8 RID: 8120 RVA: 0x0006ECE0 File Offset: 0x0006CEE0
		public virtual bool ModifyAuditRule(AccessControlModification modification, AuditRule rule, out bool modified)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			if (!this.AuditRuleType.IsAssignableFrom(rule.GetType()))
			{
				throw new ArgumentException(Environment.GetResourceString("AccessControl_InvalidAuditRuleType"), "rule");
			}
			this.WriteLock();
			bool result;
			try
			{
				result = this.ModifyAudit(modification, rule, out modified);
			}
			finally
			{
				this.WriteUnlock();
			}
			return result;
		}

		// Token: 0x06001FB9 RID: 8121
		public abstract AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);

		// Token: 0x06001FBA RID: 8122
		public abstract AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags);

		// Token: 0x04000B5E RID: 2910
		private readonly ReaderWriterLock _lock = new ReaderWriterLock();

		// Token: 0x04000B5F RID: 2911
		internal CommonSecurityDescriptor _securityDescriptor;

		// Token: 0x04000B60 RID: 2912
		private bool _ownerModified;

		// Token: 0x04000B61 RID: 2913
		private bool _groupModified;

		// Token: 0x04000B62 RID: 2914
		private bool _saclModified;

		// Token: 0x04000B63 RID: 2915
		private bool _daclModified;

		// Token: 0x04000B64 RID: 2916
		private static readonly ControlFlags SACL_CONTROL_FLAGS = ControlFlags.SystemAclPresent | ControlFlags.SystemAclAutoInherited | ControlFlags.SystemAclProtected;

		// Token: 0x04000B65 RID: 2917
		private static readonly ControlFlags DACL_CONTROL_FLAGS = ControlFlags.DiscretionaryAclPresent | ControlFlags.DiscretionaryAclAutoInherited | ControlFlags.DiscretionaryAclProtected;
	}
}
