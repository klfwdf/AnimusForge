using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.Library
{
	// Token: 0x02000080 RID: 128
	public struct PlatformFilePath
	{
		// Token: 0x06000481 RID: 1153 RVA: 0x0000FF68 File Offset: 0x0000E168
		public PlatformFilePath(PlatformDirectoryPath folderPath, string fileName)
		{
			this.FolderPath = folderPath;
			this.FileName = fileName;
		}

		// Token: 0x06000482 RID: 1154 RVA: 0x0000FF78 File Offset: 0x0000E178
		public static PlatformFilePath operator +(PlatformFilePath path, string str)
		{
			return new PlatformFilePath(path.FolderPath, path.FileName + str);
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000483 RID: 1155 RVA: 0x0000FF91 File Offset: 0x0000E191
		public string FileFullPath
		{
			get
			{
				return Common.PlatformFileHelper.GetFileFullPath(this);
			}
		}

		// Token: 0x06000484 RID: 1156 RVA: 0x0000FFA4 File Offset: 0x0000E1A4
		public string GetFileNameWithoutExtension()
		{
			int num = this.FileName.LastIndexOf('.');
			if (num == -1)
			{
				return this.FileName;
			}
			return this.FileName.Substring(0, num);
		}

		// Token: 0x06000485 RID: 1157 RVA: 0x0000FFD7 File Offset: 0x0000E1D7
		public override string ToString()
		{
			return this.FolderPath.ToString() + " - " + this.FileName;
		}

		// Token: 0x0400016A RID: 362
		public PlatformDirectoryPath FolderPath;

		// Token: 0x0400016B RID: 363
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
		public string FileName;
	}
}
