using System;
using System.IO;
using System.Security.Permissions;

namespace System.Security.AccessControl
{
	// Token: 0x0200021D RID: 541
	public sealed class DirectorySecurity : FileSystemSecurity
	{
		// Token: 0x06001F5D RID: 8029 RVA: 0x0006DBCC File Offset: 0x0006BDCC
		[SecuritySafeCritical]
		public DirectorySecurity()
			: base(true)
		{
		}

		// Token: 0x06001F5E RID: 8030 RVA: 0x0006DBD8 File Offset: 0x0006BDD8
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
		public DirectorySecurity(string name, AccessControlSections includeSections)
			: base(true, name, includeSections, true)
		{
			string fullPathInternal = Path.GetFullPathInternal(name);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.NoAccess, AccessControlActions.View, fullPathInternal, false, false);
		}
	}
}
