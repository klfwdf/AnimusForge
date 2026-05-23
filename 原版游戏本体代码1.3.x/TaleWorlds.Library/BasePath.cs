using System;
using System.IO;
using System.Reflection;

namespace TaleWorlds.Library
{
	// Token: 0x02000033 RID: 51
	public static class BasePath
	{
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060001B6 RID: 438 RVA: 0x00006ED4 File Offset: 0x000050D4
		public static string Name
		{
			get
			{
				if (ApplicationPlatform.CurrentEngine == EngineType.UnrealEngine)
				{
					return Path.GetFullPath(Path.GetDirectoryName(typeof(BasePath).Assembly.Location) + "/../../");
				}
				if (ApplicationPlatform.CurrentPlatform == Platform.Orbis)
				{
					return "/app0/";
				}
				if (ApplicationPlatform.CurrentPlatform == Platform.Durango)
				{
					return "/";
				}
				if (ApplicationPlatform.CurrentPlatform == Platform.Web)
				{
					return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../../";
				}
				return "../../";
			}
		}
	}
}
