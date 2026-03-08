using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000009 RID: 9
	public static class ApplicationPlatform
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		// (set) Token: 0x06000004 RID: 4 RVA: 0x0000205F File Offset: 0x0000025F
		public static EngineType CurrentEngine { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000005 RID: 5 RVA: 0x00002067 File Offset: 0x00000267
		// (set) Token: 0x06000006 RID: 6 RVA: 0x0000206E File Offset: 0x0000026E
		public static Platform CurrentPlatform { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000007 RID: 7 RVA: 0x00002076 File Offset: 0x00000276
		// (set) Token: 0x06000008 RID: 8 RVA: 0x0000207D File Offset: 0x0000027D
		public static Runtime CurrentRuntimeLibrary { get; private set; }

		// Token: 0x06000009 RID: 9 RVA: 0x00002085 File Offset: 0x00000285
		public static void Initialize(EngineType engineType, Platform currentPlatform, Runtime currentRuntimeLibrary)
		{
			ApplicationPlatform.CurrentEngine = engineType;
			ApplicationPlatform.CurrentPlatform = currentPlatform;
			ApplicationPlatform.CurrentRuntimeLibrary = currentRuntimeLibrary;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00002099 File Offset: 0x00000299
		public static bool IsPlatformWindows()
		{
			return ApplicationPlatform.CurrentPlatform == Platform.WindowsEpic || ApplicationPlatform.CurrentPlatform == Platform.WindowsNoPlatform || ApplicationPlatform.CurrentPlatform == Platform.WindowsSteam || ApplicationPlatform.CurrentPlatform == Platform.WindowsGOG || ApplicationPlatform.CurrentPlatform == Platform.GDKDesktop;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000020C4 File Offset: 0x000002C4
		public static bool IsPlatformConsole()
		{
			return ApplicationPlatform.CurrentPlatform == Platform.Orbis || ApplicationPlatform.CurrentPlatform == Platform.Durango;
		}
	}
}
