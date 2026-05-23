using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl
{
	// Token: 0x02000221 RID: 545
	public sealed class MutexSecurity : NativeObjectSecurity
	{
		// Token: 0x06001F66 RID: 8038 RVA: 0x0006DC61 File Offset: 0x0006BE61
		public MutexSecurity()
			: base(true, ResourceType.KernelObject)
		{
		}

		// Token: 0x06001F67 RID: 8039 RVA: 0x0006DC6B File Offset: 0x0006BE6B
		[SecuritySafeCritical]
		public MutexSecurity(string name, AccessControlSections includeSections)
			: base(true, ResourceType.KernelObject, name, includeSections, new NativeObjectSecurity.ExceptionFromErrorCode(MutexSecurity._HandleErrorCode), null)
		{
		}

		// Token: 0x06001F68 RID: 8040 RVA: 0x0006DC84 File Offset: 0x0006BE84
		[SecurityCritical]
		internal MutexSecurity(SafeWaitHandle handle, AccessControlSections includeSections)
			: base(true, ResourceType.KernelObject, handle, includeSections, new NativeObjectSecurity.ExceptionFromErrorCode(MutexSecurity._HandleErrorCode), null)
		{
		}

		// Token: 0x06001F69 RID: 8041 RVA: 0x0006DCA0 File Offset: 0x0006BEA0
		[SecurityCritical]
		private static Exception _HandleErrorCode(int errorCode, string name, SafeHandle handle, object context)
		{
			Exception result = null;
			if (errorCode == 2 || errorCode == 6 || errorCode == 123)
			{
				if (name != null && name.Length != 0)
				{
					result = new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", new object[] { name }));
				}
				else
				{
					result = new WaitHandleCannotBeOpenedException();
				}
			}
			return result;
		}

		// Token: 0x06001F6A RID: 8042 RVA: 0x0006DCEA File Offset: 0x0006BEEA
		public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		{
			return new MutexAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
		}

		// Token: 0x06001F6B RID: 8043 RVA: 0x0006DCFA File Offset: 0x0006BEFA
		public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
		{
			return new MutexAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
		}

		// Token: 0x06001F6C RID: 8044 RVA: 0x0006DD0C File Offset: 0x0006BF0C
		internal AccessControlSections GetAccessControlSectionsFromChanges()
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

		// Token: 0x06001F6D RID: 8045 RVA: 0x0006DD4C File Offset: 0x0006BF4C
		[SecurityCritical]
		internal void Persist(SafeWaitHandle handle)
		{
			base.WriteLock();
			try
			{
				AccessControlSections accessControlSectionsFromChanges = this.GetAccessControlSectionsFromChanges();
				if (accessControlSectionsFromChanges != AccessControlSections.None)
				{
					base.Persist(handle, accessControlSectionsFromChanges);
					base.OwnerModified = (base.GroupModified = (base.AuditRulesModified = (base.AccessRulesModified = false)));
				}
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001F6E RID: 8046 RVA: 0x0006DDB0 File Offset: 0x0006BFB0
		public void AddAccessRule(MutexAccessRule rule)
		{
			base.AddAccessRule(rule);
		}

		// Token: 0x06001F6F RID: 8047 RVA: 0x0006DDB9 File Offset: 0x0006BFB9
		public void SetAccessRule(MutexAccessRule rule)
		{
			base.SetAccessRule(rule);
		}

		// Token: 0x06001F70 RID: 8048 RVA: 0x0006DDC2 File Offset: 0x0006BFC2
		public void ResetAccessRule(MutexAccessRule rule)
		{
			base.ResetAccessRule(rule);
		}

		// Token: 0x06001F71 RID: 8049 RVA: 0x0006DDCB File Offset: 0x0006BFCB
		public bool RemoveAccessRule(MutexAccessRule rule)
		{
			return base.RemoveAccessRule(rule);
		}

		// Token: 0x06001F72 RID: 8050 RVA: 0x0006DDD4 File Offset: 0x0006BFD4
		public void RemoveAccessRuleAll(MutexAccessRule rule)
		{
			base.RemoveAccessRuleAll(rule);
		}

		// Token: 0x06001F73 RID: 8051 RVA: 0x0006DDDD File Offset: 0x0006BFDD
		public void RemoveAccessRuleSpecific(MutexAccessRule rule)
		{
			base.RemoveAccessRuleSpecific(rule);
		}

		// Token: 0x06001F74 RID: 8052 RVA: 0x0006DDE6 File Offset: 0x0006BFE6
		public void AddAuditRule(MutexAuditRule rule)
		{
			base.AddAuditRule(rule);
		}

		// Token: 0x06001F75 RID: 8053 RVA: 0x0006DDEF File Offset: 0x0006BFEF
		public void SetAuditRule(MutexAuditRule rule)
		{
			base.SetAuditRule(rule);
		}

		// Token: 0x06001F76 RID: 8054 RVA: 0x0006DDF8 File Offset: 0x0006BFF8
		public bool RemoveAuditRule(MutexAuditRule rule)
		{
			return base.RemoveAuditRule(rule);
		}

		// Token: 0x06001F77 RID: 8055 RVA: 0x0006DE01 File Offset: 0x0006C001
		public void RemoveAuditRuleAll(MutexAuditRule rule)
		{
			base.RemoveAuditRuleAll(rule);
		}

		// Token: 0x06001F78 RID: 8056 RVA: 0x0006DE0A File Offset: 0x0006C00A
		public void RemoveAuditRuleSpecific(MutexAuditRule rule)
		{
			base.RemoveAuditRuleSpecific(rule);
		}

		// Token: 0x170003A1 RID: 929
		// (get) Token: 0x06001F79 RID: 8057 RVA: 0x0006DE13 File Offset: 0x0006C013
		public override Type AccessRightType
		{
			get
			{
				return typeof(MutexRights);
			}
		}

		// Token: 0x170003A2 RID: 930
		// (get) Token: 0x06001F7A RID: 8058 RVA: 0x0006DE1F File Offset: 0x0006C01F
		public override Type AccessRuleType
		{
			get
			{
				return typeof(MutexAccessRule);
			}
		}

		// Token: 0x170003A3 RID: 931
		// (get) Token: 0x06001F7B RID: 8059 RVA: 0x0006DE2B File Offset: 0x0006C02B
		public override Type AuditRuleType
		{
			get
			{
				return typeof(MutexAuditRule);
			}
		}
	}
}
