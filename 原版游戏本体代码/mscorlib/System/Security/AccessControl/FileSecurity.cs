using System;
using System.IO;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl
{
	// Token: 0x0200021C RID: 540
	public sealed class FileSecurity : FileSystemSecurity
	{
		// Token: 0x06001F5A RID: 8026 RVA: 0x0006DB7B File Offset: 0x0006BD7B
		[SecuritySafeCritical]
		public FileSecurity()
			: base(false)
		{
		}

		// Token: 0x06001F5B RID: 8027 RVA: 0x0006DB84 File Offset: 0x0006BD84
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
		public FileSecurity(string fileName, AccessControlSections includeSections)
			: base(false, fileName, includeSections, false)
		{
			string fullPathInternal = Path.GetFullPathInternal(fileName);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.NoAccess, AccessControlActions.View, fullPathInternal, false, false);
		}

		// Token: 0x06001F5C RID: 8028 RVA: 0x0006DBAC File Offset: 0x0006BDAC
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
		internal FileSecurity(SafeFileHandle handle, string fullPath, AccessControlSections includeSections)
			: base(false, handle, includeSections, false)
		{
			if (fullPath != null)
			{
				FileIOPermission.QuickDemand(FileIOPermissionAccess.NoAccess, AccessControlActions.View, fullPath, false, true);
				return;
			}
			FileIOPermission.QuickDemand(PermissionState.Unrestricted);
		}
	}
}
