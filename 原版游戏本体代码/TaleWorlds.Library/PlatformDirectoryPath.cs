using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.Library
{
	// Token: 0x0200007D RID: 125
	public struct PlatformDirectoryPath
	{
		// Token: 0x0600046A RID: 1130 RVA: 0x0000F9E8 File Offset: 0x0000DBE8
		public PlatformDirectoryPath(PlatformFileType type, string path)
		{
			this.Type = type;
			this.Path = path;
		}

		// Token: 0x0600046B RID: 1131 RVA: 0x0000F9F8 File Offset: 0x0000DBF8
		public static PlatformDirectoryPath operator +(PlatformDirectoryPath path, string str)
		{
			return new PlatformDirectoryPath(path.Type, path.Path + str);
		}

		// Token: 0x0600046C RID: 1132 RVA: 0x0000FA11 File Offset: 0x0000DC11
		public override string ToString()
		{
			return this.Type + " " + this.Path;
		}

		// Token: 0x04000162 RID: 354
		public PlatformFileType Type;

		// Token: 0x04000163 RID: 355
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
		public string Path;
	}
}
