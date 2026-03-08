using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000062 RID: 98
	public static class ManagedDllFolder
	{
		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060002C0 RID: 704 RVA: 0x000086D0 File Offset: 0x000068D0
		public static string Name
		{
			get
			{
				if (!string.IsNullOrEmpty(ManagedDllFolder._overridenFolder))
				{
					return ManagedDllFolder._overridenFolder;
				}
				if (ApplicationPlatform.CurrentPlatform == Platform.Orbis)
				{
					return "/app0/";
				}
				if (ApplicationPlatform.CurrentPlatform == Platform.Durango)
				{
					return "/";
				}
				return "";
			}
		}

		// Token: 0x060002C1 RID: 705 RVA: 0x00008705 File Offset: 0x00006905
		public static void OverrideManagedDllFolder(string overridenFolder)
		{
			ManagedDllFolder._overridenFolder = overridenFolder;
		}

		// Token: 0x04000121 RID: 289
		private static string _overridenFolder;
	}
}
