using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000222 RID: 546
	public abstract class NativeObjectSecurity : CommonObjectSecurity
	{
		// Token: 0x06001F7C RID: 8060 RVA: 0x0006DE37 File Offset: 0x0006C037
		protected NativeObjectSecurity(bool isContainer, ResourceType resourceType)
			: base(isContainer)
		{
			this._resourceType = resourceType;
		}

		// Token: 0x06001F7D RID: 8061 RVA: 0x0006DE73 File Offset: 0x0006C073
		protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
			: this(isContainer, resourceType)
		{
			this._exceptionContext = exceptionContext;
			this._exceptionFromErrorCode = exceptionFromErrorCode;
		}

		// Token: 0x06001F7E RID: 8062 RVA: 0x0006DE8C File Offset: 0x0006C08C
		[SecurityCritical]
		internal NativeObjectSecurity(ResourceType resourceType, CommonSecurityDescriptor securityDescriptor)
			: this(resourceType, securityDescriptor, null)
		{
		}

		// Token: 0x06001F7F RID: 8063 RVA: 0x0006DE98 File Offset: 0x0006C098
		[SecurityCritical]
		internal NativeObjectSecurity(ResourceType resourceType, CommonSecurityDescriptor securityDescriptor, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode)
			: base(securityDescriptor)
		{
			this._resourceType = resourceType;
			this._exceptionFromErrorCode = exceptionFromErrorCode;
		}

		// Token: 0x06001F80 RID: 8064 RVA: 0x0006DEE8 File Offset: 0x0006C0E8
		[SecuritySafeCritical]
		protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
			: this(resourceType, NativeObjectSecurity.CreateInternal(resourceType, isContainer, name, null, includeSections, true, exceptionFromErrorCode, exceptionContext), exceptionFromErrorCode)
		{
		}

		// Token: 0x06001F81 RID: 8065 RVA: 0x0006DF0E File Offset: 0x0006C10E
		protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections)
			: this(isContainer, resourceType, name, includeSections, null, null)
		{
		}

		// Token: 0x06001F82 RID: 8066 RVA: 0x0006DF20 File Offset: 0x0006C120
		[SecuritySafeCritical]
		protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle handle, AccessControlSections includeSections, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
			: this(resourceType, NativeObjectSecurity.CreateInternal(resourceType, isContainer, null, handle, includeSections, false, exceptionFromErrorCode, exceptionContext), exceptionFromErrorCode)
		{
		}

		// Token: 0x06001F83 RID: 8067 RVA: 0x0006DF46 File Offset: 0x0006C146
		[SecuritySafeCritical]
		protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle handle, AccessControlSections includeSections)
			: this(isContainer, resourceType, handle, includeSections, null, null)
		{
		}

		// Token: 0x06001F84 RID: 8068 RVA: 0x0006DF58 File Offset: 0x0006C158
		[SecurityCritical]
		private static CommonSecurityDescriptor CreateInternal(ResourceType resourceType, bool isContainer, string name, SafeHandle handle, AccessControlSections includeSections, bool createByName, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
		{
			if (createByName && name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (!createByName && handle == null)
			{
				throw new ArgumentNullException("handle");
			}
			RawSecurityDescriptor rawSecurityDescriptor;
			int securityInfo = Win32.GetSecurityInfo(resourceType, name, handle, includeSections, out rawSecurityDescriptor);
			if (securityInfo != 0)
			{
				Exception ex = null;
				if (exceptionFromErrorCode != null)
				{
					ex = exceptionFromErrorCode(securityInfo, name, handle, exceptionContext);
				}
				if (ex == null)
				{
					if (securityInfo == 5)
					{
						ex = new UnauthorizedAccessException();
					}
					else if (securityInfo == 1307)
					{
						ex = new InvalidOperationException(Environment.GetResourceString("AccessControl_InvalidOwner"));
					}
					else if (securityInfo == 1308)
					{
						ex = new InvalidOperationException(Environment.GetResourceString("AccessControl_InvalidGroup"));
					}
					else if (securityInfo == 87)
					{
						ex = new InvalidOperationException(Environment.GetResourceString("AccessControl_UnexpectedError", new object[] { securityInfo }));
					}
					else if (securityInfo == 123)
					{
						ex = new ArgumentException(Environment.GetResourceString("Argument_InvalidName"), "name");
					}
					else if (securityInfo == 2)
					{
						ex = ((name == null) ? new FileNotFoundException() : new FileNotFoundException(name));
					}
					else if (securityInfo == 1350)
					{
						ex = new NotSupportedException(Environment.GetResourceString("AccessControl_NoAssociatedSecurity"));
					}
					else
					{
						ex = new InvalidOperationException(Environment.GetResourceString("AccessControl_UnexpectedError", new object[] { securityInfo }));
					}
				}
				throw ex;
			}
			return new CommonSecurityDescriptor(isContainer, false, rawSecurityDescriptor, true);
		}

		// Token: 0x06001F85 RID: 8069 RVA: 0x0006E09C File Offset: 0x0006C29C
		[SecurityCritical]
		private void Persist(string name, SafeHandle handle, AccessControlSections includeSections, object exceptionContext)
		{
			base.WriteLock();
			try
			{
				SecurityInfos securityInfos = (SecurityInfos)0;
				SecurityIdentifier owner = null;
				SecurityIdentifier group = null;
				SystemAcl sacl = null;
				DiscretionaryAcl dacl = null;
				if ((includeSections & AccessControlSections.Owner) != AccessControlSections.None && this._securityDescriptor.Owner != null)
				{
					securityInfos |= SecurityInfos.Owner;
					owner = this._securityDescriptor.Owner;
				}
				if ((includeSections & AccessControlSections.Group) != AccessControlSections.None && this._securityDescriptor.Group != null)
				{
					securityInfos |= SecurityInfos.Group;
					group = this._securityDescriptor.Group;
				}
				if ((includeSections & AccessControlSections.Audit) != AccessControlSections.None)
				{
					securityInfos |= SecurityInfos.SystemAcl;
					if (this._securityDescriptor.IsSystemAclPresent && this._securityDescriptor.SystemAcl != null && this._securityDescriptor.SystemAcl.Count > 0)
					{
						sacl = this._securityDescriptor.SystemAcl;
					}
					else
					{
						sacl = null;
					}
					if ((this._securityDescriptor.ControlFlags & ControlFlags.SystemAclProtected) != ControlFlags.None)
					{
						securityInfos |= (SecurityInfos)this.ProtectedSystemAcl;
					}
					else
					{
						securityInfos |= (SecurityInfos)this.UnprotectedSystemAcl;
					}
				}
				if ((includeSections & AccessControlSections.Access) != AccessControlSections.None && this._securityDescriptor.IsDiscretionaryAclPresent)
				{
					securityInfos |= SecurityInfos.DiscretionaryAcl;
					if (this._securityDescriptor.DiscretionaryAcl.EveryOneFullAccessForNullDacl)
					{
						dacl = null;
					}
					else
					{
						dacl = this._securityDescriptor.DiscretionaryAcl;
					}
					if ((this._securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) != ControlFlags.None)
					{
						securityInfos |= (SecurityInfos)this.ProtectedDiscretionaryAcl;
					}
					else
					{
						securityInfos |= (SecurityInfos)this.UnprotectedDiscretionaryAcl;
					}
				}
				if (securityInfos != (SecurityInfos)0)
				{
					int num = Win32.SetSecurityInfo(this._resourceType, name, handle, securityInfos, owner, group, sacl, dacl);
					if (num != 0)
					{
						Exception ex = null;
						if (this._exceptionFromErrorCode != null)
						{
							ex = this._exceptionFromErrorCode(num, name, handle, exceptionContext);
						}
						if (ex == null)
						{
							if (num == 5)
							{
								ex = new UnauthorizedAccessException();
							}
							else if (num == 1307)
							{
								ex = new InvalidOperationException(Environment.GetResourceString("AccessControl_InvalidOwner"));
							}
							else if (num == 1308)
							{
								ex = new InvalidOperationException(Environment.GetResourceString("AccessControl_InvalidGroup"));
							}
							else if (num == 123)
							{
								ex = new ArgumentException(Environment.GetResourceString("Argument_InvalidName"), "name");
							}
							else if (num == 6)
							{
								ex = new NotSupportedException(Environment.GetResourceString("AccessControl_InvalidHandle"));
							}
							else if (num == 2)
							{
								ex = new FileNotFoundException();
							}
							else if (num == 1350)
							{
								ex = new NotSupportedException(Environment.GetResourceString("AccessControl_NoAssociatedSecurity"));
							}
							else
							{
								ex = new InvalidOperationException(Environment.GetResourceString("AccessControl_UnexpectedError", new object[] { num }));
							}
						}
						throw ex;
					}
					base.OwnerModified = false;
					base.GroupModified = false;
					base.AccessRulesModified = false;
					base.AuditRulesModified = false;
				}
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001F86 RID: 8070 RVA: 0x0006E32C File Offset: 0x0006C52C
		protected sealed override void Persist(string name, AccessControlSections includeSections)
		{
			this.Persist(name, includeSections, this._exceptionContext);
		}

		// Token: 0x06001F87 RID: 8071 RVA: 0x0006E33C File Offset: 0x0006C53C
		[SecuritySafeCritical]
		protected void Persist(string name, AccessControlSections includeSections, object exceptionContext)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.Persist(name, null, includeSections, exceptionContext);
		}

		// Token: 0x06001F88 RID: 8072 RVA: 0x0006E356 File Offset: 0x0006C556
		[SecuritySafeCritical]
		protected sealed override void Persist(SafeHandle handle, AccessControlSections includeSections)
		{
			this.Persist(handle, includeSections, this._exceptionContext);
		}

		// Token: 0x06001F89 RID: 8073 RVA: 0x0006E366 File Offset: 0x0006C566
		[SecuritySafeCritical]
		protected void Persist(SafeHandle handle, AccessControlSections includeSections, object exceptionContext)
		{
			if (handle == null)
			{
				throw new ArgumentNullException("handle");
			}
			this.Persist(null, handle, includeSections, exceptionContext);
		}

		// Token: 0x04000B50 RID: 2896
		private readonly ResourceType _resourceType;

		// Token: 0x04000B51 RID: 2897
		private NativeObjectSecurity.ExceptionFromErrorCode _exceptionFromErrorCode;

		// Token: 0x04000B52 RID: 2898
		private object _exceptionContext;

		// Token: 0x04000B53 RID: 2899
		private readonly uint ProtectedDiscretionaryAcl = 2147483648U;

		// Token: 0x04000B54 RID: 2900
		private readonly uint ProtectedSystemAcl = 1073741824U;

		// Token: 0x04000B55 RID: 2901
		private readonly uint UnprotectedDiscretionaryAcl = 536870912U;

		// Token: 0x04000B56 RID: 2902
		private readonly uint UnprotectedSystemAcl = 268435456U;

		// Token: 0x02000B32 RID: 2866
		// (Invoke) Token: 0x06006B6B RID: 27499
		[SecuritySafeCritical]
		protected internal delegate Exception ExceptionFromErrorCode(int errorCode, string name, SafeHandle handle, object context);
	}
}
