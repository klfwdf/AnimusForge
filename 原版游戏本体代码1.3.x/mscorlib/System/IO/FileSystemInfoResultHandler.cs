using System;
using System.Security;
using Microsoft.Win32;

namespace System.IO
{
	// Token: 0x02000194 RID: 404
	internal class FileSystemInfoResultHandler : SearchResultHandler<FileSystemInfo>
	{
		// Token: 0x060018B3 RID: 6323 RVA: 0x00050C85 File Offset: 0x0004EE85
		[SecurityCritical]
		internal override bool IsResultIncluded(ref Win32Native.WIN32_FIND_DATA findData)
		{
			return findData.IsFile || findData.IsNormalDirectory;
		}

		// Token: 0x060018B4 RID: 6324 RVA: 0x00050C97 File Offset: 0x0004EE97
		[SecurityCritical]
		internal override FileSystemInfo CreateObject(Directory.SearchData searchData, ref Win32Native.WIN32_FIND_DATA findData)
		{
			if (!findData.IsFile)
			{
				return DirectoryInfoResultHandler.CreateDirectoryInfo(searchData, ref findData);
			}
			return FileInfoResultHandler.CreateFileInfo(searchData, ref findData);
		}
	}
}
