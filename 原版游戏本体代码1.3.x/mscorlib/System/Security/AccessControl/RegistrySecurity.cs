using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl
{
	// Token: 0x0200022F RID: 559
	public sealed class RegistrySecurity : NativeObjectSecurity
	{
		// Token: 0x06002022 RID: 8226 RVA: 0x00071094 File Offset: 0x0006F294
		public RegistrySecurity()
			: base(true, ResourceType.RegistryKey)
		{
		}

		// Token: 0x06002023 RID: 8227 RVA: 0x0007109E File Offset: 0x0006F29E
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
		internal RegistrySecurity(SafeRegistryHandle hKey, string name, AccessControlSections includeSections)
			: base(true, ResourceType.RegistryKey, hKey, includeSections, new NativeObjectSecurity.ExceptionFromErrorCode(RegistrySecurity._HandleErrorCode), null)
		{
			new RegistryPermission(RegistryPermissionAccess.NoAccess, AccessControlActions.View, name).Demand();
		}

		// Token: 0x06002024 RID: 8228 RVA: 0x000710C4 File Offset: 0x0006F2C4
		[SecurityCritical]
		private static Exception _HandleErrorCode(int errorCode, string name, SafeHandle handle, object context)
		{
			Exception result = null;
			if (errorCode != 2)
			{
				if (errorCode != 6)
				{
					if (errorCode == 123)
					{
						result = new ArgumentException(Environment.GetResourceString("Arg_RegInvalidKeyName", new object[] { "name" }));
					}
				}
				else
				{
					result = new ArgumentException(Environment.GetResourceString("AccessControl_InvalidHandle"));
				}
			}
			else
			{
				result = new IOException(Environment.GetResourceString("Arg_RegKeyNotFound", new object[] { errorCode }));
			}
			return result;
		}

		// Token: 0x06002025 RID: 8229 RVA: 0x00071134 File Offset: 0x0006F334
		public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		{
			return new RegistryAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
		}

		// Token: 0x06002026 RID: 8230 RVA: 0x00071144 File Offset: 0x0006F344
		public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
		{
			return new RegistryAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
		}

		// Token: 0x06002027 RID: 8231 RVA: 0x00071154 File Offset: 0x0006F354
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

		// Token: 0x06002028 RID: 8232 RVA: 0x00071194 File Offset: 0x0006F394
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
		internal void Persist(SafeRegistryHandle hKey, string keyName)
		{
			new RegistryPermission(RegistryPermissionAccess.NoAccess, AccessControlActions.Change, keyName).Demand();
			base.WriteLock();
			try
			{
				AccessControlSections accessControlSectionsFromChanges = this.GetAccessControlSectionsFromChanges();
				if (accessControlSectionsFromChanges != AccessControlSections.None)
				{
					base.Persist(hKey, accessControlSectionsFromChanges);
					base.OwnerModified = (base.GroupModified = (base.AuditRulesModified = (base.AccessRulesModified = false)));
				}
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06002029 RID: 8233 RVA: 0x00071204 File Offset: 0x0006F404
		public void AddAccessRule(RegistryAccessRule rule)
		{
			base.AddAccessRule(rule);
		}

		// Token: 0x0600202A RID: 8234 RVA: 0x0007120D File Offset: 0x0006F40D
		public void SetAccessRule(RegistryAccessRule rule)
		{
			base.SetAccessRule(rule);
		}

		// Token: 0x0600202B RID: 8235 RVA: 0x00071216 File Offset: 0x0006F416
		public void ResetAccessRule(RegistryAccessRule rule)
		{
			base.ResetAccessRule(rule);
		}

		// Token: 0x0600202C RID: 8236 RVA: 0x0007121F File Offset: 0x0006F41F
		public bool RemoveAccessRule(RegistryAccessRule rule)
		{
			return base.RemoveAccessRule(rule);
		}

		// Token: 0x0600202D RID: 8237 RVA: 0x00071228 File Offset: 0x0006F428
		public void RemoveAccessRuleAll(RegistryAccessRule rule)
		{
			base.RemoveAccessRuleAll(rule);
		}

		// Token: 0x0600202E RID: 8238 RVA: 0x00071231 File Offset: 0x0006F431
		public void RemoveAccessRuleSpecific(RegistryAccessRule rule)
		{
			base.RemoveAccessRuleSpecific(rule);
		}

		// Token: 0x0600202F RID: 8239 RVA: 0x0007123A File Offset: 0x0006F43A
		public void AddAuditRule(RegistryAuditRule rule)
		{
			base.AddAuditRule(rule);
		}

		// Token: 0x06002030 RID: 8240 RVA: 0x00071243 File Offset: 0x0006F443
		public void SetAuditRule(RegistryAuditRule rule)
		{
			base.SetAuditRule(rule);
		}

		// Token: 0x06002031 RID: 8241 RVA: 0x0007124C File Offset: 0x0006F44C
		public bool RemoveAuditRule(RegistryAuditRule rule)
		{
			return base.RemoveAuditRule(rule);
		}

		// Token: 0x06002032 RID: 8242 RVA: 0x00071255 File Offset: 0x0006F455
		public void RemoveAuditRuleAll(RegistryAuditRule rule)
		{
			base.RemoveAuditRuleAll(rule);
		}

		// Token: 0x06002033 RID: 8243 RVA: 0x0007125E File Offset: 0x0006F45E
		public void RemoveAuditRuleSpecific(RegistryAuditRule rule)
		{
			base.RemoveAuditRuleSpecific(rule);
		}

		// Token: 0x170003BA RID: 954
		// (get) Token: 0x06002034 RID: 8244 RVA: 0x00071267 File Offset: 0x0006F467
		public override Type AccessRightType
		{
			get
			{
				return typeof(RegistryRights);
			}
		}

		// Token: 0x170003BB RID: 955
		// (get) Token: 0x06002035 RID: 8245 RVA: 0x00071273 File Offset: 0x0006F473
		public override Type AccessRuleType
		{
			get
			{
				return typeof(RegistryAccessRule);
			}
		}

		// Token: 0x170003BC RID: 956
		// (get) Token: 0x06002036 RID: 8246 RVA: 0x0007127F File Offset: 0x0006F47F
		public override Type AuditRuleType
		{
			get
			{
				return typeof(RegistryAuditRule);
			}
		}
	}
}
