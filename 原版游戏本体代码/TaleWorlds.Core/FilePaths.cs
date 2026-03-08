using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x0200005E RID: 94
	public static class FilePaths
	{
		// Token: 0x17000297 RID: 663
		// (get) Token: 0x0600072F RID: 1839 RVA: 0x00018D7B File Offset: 0x00016F7B
		public static PlatformDirectoryPath SavePath
		{
			get
			{
				return new PlatformDirectoryPath(PlatformFileType.User, "Game Saves");
			}
		}

		// Token: 0x17000298 RID: 664
		// (get) Token: 0x06000730 RID: 1840 RVA: 0x00018D88 File Offset: 0x00016F88
		public static PlatformDirectoryPath RecordingsPath
		{
			get
			{
				return new PlatformDirectoryPath(PlatformFileType.User, "Recordings");
			}
		}

		// Token: 0x17000299 RID: 665
		// (get) Token: 0x06000731 RID: 1841 RVA: 0x00018D95 File Offset: 0x00016F95
		public static PlatformDirectoryPath StatisticsPath
		{
			get
			{
				return new PlatformDirectoryPath(PlatformFileType.User, "Statistics");
			}
		}

		// Token: 0x0400039C RID: 924
		public const string SaveDirectoryName = "Game Saves";

		// Token: 0x0400039D RID: 925
		public const string RecordingsDirectoryName = "Recordings";

		// Token: 0x0400039E RID: 926
		public const string StatisticsDirectoryName = "Statistics";
	}
}
