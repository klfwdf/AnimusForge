using System;

namespace TaleWorlds.Engine
{
	// Token: 0x02000056 RID: 86
	public static class LoadingWindow
	{
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060008AE RID: 2222 RVA: 0x00006E88 File Offset: 0x00005088
		// (set) Token: 0x060008AF RID: 2223 RVA: 0x00006E8F File Offset: 0x0000508F
		public static bool IsLoadingWindowActive { get; private set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060008B0 RID: 2224 RVA: 0x00006E97 File Offset: 0x00005097
		// (set) Token: 0x060008B1 RID: 2225 RVA: 0x00006E9E File Offset: 0x0000509E
		public static ILoadingWindowManager LoadingWindowManager { get; private set; }

		// Token: 0x060008B3 RID: 2227 RVA: 0x00006EA8 File Offset: 0x000050A8
		public static void InitializeWith<T>() where T : class, ILoadingWindowManager, new()
		{
			LoadingWindow.Destroy();
			LoadingWindow.LoadingWindowManager = Activator.CreateInstance<T>();
			LoadingWindow.LoadingWindowManager.Initialize();
			if (LoadingWindow.IsLoadingWindowActive)
			{
				LoadingWindow.LoadingWindowManager.EnableLoadingWindow();
			}
		}

		// Token: 0x060008B4 RID: 2228 RVA: 0x00006ED9 File Offset: 0x000050D9
		public static void Destroy()
		{
			ILoadingWindowManager loadingWindowManager = LoadingWindow.LoadingWindowManager;
			if (loadingWindowManager != null)
			{
				loadingWindowManager.DisableLoadingWindow();
			}
			ILoadingWindowManager loadingWindowManager2 = LoadingWindow.LoadingWindowManager;
			if (loadingWindowManager2 != null)
			{
				loadingWindowManager2.Destroy();
			}
			LoadingWindow.LoadingWindowManager = null;
		}

		// Token: 0x060008B5 RID: 2229 RVA: 0x00006F01 File Offset: 0x00005101
		public static void DisableGlobalLoadingWindow()
		{
			if (LoadingWindow.LoadingWindowManager == null)
			{
				return;
			}
			if (LoadingWindow.IsLoadingWindowActive)
			{
				LoadingWindow.LoadingWindowManager.DisableLoadingWindow();
				Utilities.DisableGlobalLoadingWindow();
				Utilities.OnLoadingWindowDisabled();
			}
			LoadingWindow.IsLoadingWindowActive = false;
			Utilities.DebugSetGlobalLoadingWindowState(false);
		}

		// Token: 0x060008B6 RID: 2230 RVA: 0x00006F32 File Offset: 0x00005132
		public static void EnableGlobalLoadingWindow()
		{
			if (LoadingWindow.LoadingWindowManager == null)
			{
				return;
			}
			LoadingWindow.IsLoadingWindowActive = true;
			Utilities.DebugSetGlobalLoadingWindowState(true);
			if (LoadingWindow.IsLoadingWindowActive)
			{
				LoadingWindow.LoadingWindowManager.EnableLoadingWindow();
				Utilities.OnLoadingWindowEnabled();
			}
		}

		// Token: 0x060008B7 RID: 2231 RVA: 0x00006F5E File Offset: 0x0000515E
		public static void SetCurrentModeIsMultiplayer(bool isMultiplayer)
		{
			ILoadingWindowManager loadingWindowManager = LoadingWindow.LoadingWindowManager;
			if (loadingWindowManager == null)
			{
				return;
			}
			loadingWindowManager.SetCurrentModeIsMultiplayer(isMultiplayer);
		}
	}
}
