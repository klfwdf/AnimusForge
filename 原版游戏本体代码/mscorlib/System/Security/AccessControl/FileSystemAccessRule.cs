using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000219 RID: 537
	public sealed class FileSystemAccessRule : AccessRule
	{
		// Token: 0x06001F34 RID: 7988 RVA: 0x0006D5C3 File Offset: 0x0006B7C3
		public FileSystemAccessRule(IdentityReference identity, FileSystemRights fileSystemRights, AccessControlType type)
			: this(identity, FileSystemAccessRule.AccessMaskFromRights(fileSystemRights, type), false, InheritanceFlags.None, PropagationFlags.None, type)
		{
		}

		// Token: 0x06001F35 RID: 7989 RVA: 0x0006D5D7 File Offset: 0x0006B7D7
		public FileSystemAccessRule(string identity, FileSystemRights fileSystemRights, AccessControlType type)
			: this(new NTAccount(identity), FileSystemAccessRule.AccessMaskFromRights(fileSystemRights, type), false, InheritanceFlags.None, PropagationFlags.None, type)
		{
		}

		// Token: 0x06001F36 RID: 7990 RVA: 0x0006D5F0 File Offset: 0x0006B7F0
		public FileSystemAccessRule(IdentityReference identity, FileSystemRights fileSystemRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
			: this(identity, FileSystemAccessRule.AccessMaskFromRights(fileSystemRights, type), false, inheritanceFlags, propagationFlags, type)
		{
		}

		// Token: 0x06001F37 RID: 7991 RVA: 0x0006D607 File Offset: 0x0006B807
		public FileSystemAccessRule(string identity, FileSystemRights fileSystemRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
			: this(new NTAccount(identity), FileSystemAccessRule.AccessMaskFromRights(fileSystemRights, type), false, inheritanceFlags, propagationFlags, type)
		{
		}

		// Token: 0x06001F38 RID: 7992 RVA: 0x0006D623 File Offset: 0x0006B823
		internal FileSystemAccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, type)
		{
		}

		// Token: 0x1700039A RID: 922
		// (get) Token: 0x06001F39 RID: 7993 RVA: 0x0006D634 File Offset: 0x0006B834
		public FileSystemRights FileSystemRights
		{
			get
			{
				return FileSystemAccessRule.RightsFromAccessMask(base.AccessMask);
			}
		}

		// Token: 0x06001F3A RID: 7994 RVA: 0x0006D644 File Offset: 0x0006B844
		internal static int AccessMaskFromRights(FileSystemRights fileSystemRights, AccessControlType controlType)
		{
			if (fileSystemRights < (FileSystemRights)0 || fileSystemRights > FileSystemRights.FullControl)
			{
				throw new ArgumentOutOfRangeException("fileSystemRights", Environment.GetResourceString("Argument_InvalidEnumValue", new object[] { fileSystemRights, "FileSystemRights" }));
			}
			if (controlType == AccessControlType.Allow)
			{
				fileSystemRights |= FileSystemRights.Synchronize;
			}
			else if (controlType == AccessControlType.Deny && fileSystemRights != FileSystemRights.FullControl && fileSystemRights != (FileSystemRights.ReadData | FileSystemRights.WriteData | FileSystemRights.AppendData | FileSystemRights.ReadExtendedAttributes | FileSystemRights.WriteExtendedAttributes | FileSystemRights.ExecuteFile | FileSystemRights.ReadAttributes | FileSystemRights.WriteAttributes | FileSystemRights.Delete | FileSystemRights.ReadPermissions | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership | FileSystemRights.Synchronize))
			{
				fileSystemRights &= ~FileSystemRights.Synchronize;
			}
			return (int)fileSystemRights;
		}

		// Token: 0x06001F3B RID: 7995 RVA: 0x0006D6B5 File Offset: 0x0006B8B5
		internal static FileSystemRights RightsFromAccessMask(int accessMask)
		{
			return (FileSystemRights)accessMask;
		}
	}
}
