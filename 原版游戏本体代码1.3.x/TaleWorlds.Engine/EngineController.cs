using System;
using TaleWorlds.Engine.InputSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine
{
	// Token: 0x02000042 RID: 66
	public static class EngineController
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060006AD RID: 1709 RVA: 0x00003BBC File Offset: 0x00001DBC
		// (remove) Token: 0x060006AE RID: 1710 RVA: 0x00003BF0 File Offset: 0x00001DF0
		public static event Action ConfigChange;

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x060006AF RID: 1711 RVA: 0x00003C24 File Offset: 0x00001E24
		// (remove) Token: 0x060006B0 RID: 1712 RVA: 0x00003C58 File Offset: 0x00001E58
		public static event Action<bool> OnConstrainedStateChanged;

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x060006B1 RID: 1713 RVA: 0x00003C8C File Offset: 0x00001E8C
		// (remove) Token: 0x060006B2 RID: 1714 RVA: 0x00003CC0 File Offset: 0x00001EC0
		public static event Action OnDLCInstalledCallback;

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x060006B3 RID: 1715 RVA: 0x00003CF4 File Offset: 0x00001EF4
		// (remove) Token: 0x060006B4 RID: 1716 RVA: 0x00003D28 File Offset: 0x00001F28
		public static event Action OnDLCLoadedCallback;

		// Token: 0x060006B5 RID: 1717 RVA: 0x00003D5B File Offset: 0x00001F5B
		internal static void OnApplicationTick(float dt)
		{
			Input.Update();
			Screen.Update();
		}

		// Token: 0x060006B6 RID: 1718 RVA: 0x00003D68 File Offset: 0x00001F68
		[EngineCallback(null, false)]
		internal static void Initialize()
		{
			IInputContext debugInput = null;
			Input.Initialize(new EngineInputManager(), debugInput);
			Common.PlatformFileHelper = new PlatformFileHelperPC(Utilities.GetApplicationName());
		}

		// Token: 0x060006B7 RID: 1719 RVA: 0x00003D91 File Offset: 0x00001F91
		[EngineCallback(null, false)]
		internal static void OnConfigChange()
		{
			NativeConfig.OnConfigChanged();
			if (EngineController.ConfigChange != null)
			{
				EngineController.ConfigChange();
			}
		}

		// Token: 0x060006B8 RID: 1720 RVA: 0x00003DA9 File Offset: 0x00001FA9
		[EngineCallback(null, false)]
		internal static void OnConstrainedStateChange(bool isConstrained)
		{
			Action<bool> onConstrainedStateChanged = EngineController.OnConstrainedStateChanged;
			if (onConstrainedStateChanged == null)
			{
				return;
			}
			onConstrainedStateChanged(isConstrained);
		}

		// Token: 0x060006B9 RID: 1721 RVA: 0x00003DBB File Offset: 0x00001FBB
		[EngineCallback(null, false)]
		internal static void OnDLCInstalled()
		{
			Action onDLCInstalledCallback = EngineController.OnDLCInstalledCallback;
			if (onDLCInstalledCallback == null)
			{
				return;
			}
			onDLCInstalledCallback();
		}

		// Token: 0x060006BA RID: 1722 RVA: 0x00003DCC File Offset: 0x00001FCC
		[EngineCallback(null, false)]
		internal static void OnDLCLoaded()
		{
			Action onDLCLoadedCallback = EngineController.OnDLCLoadedCallback;
			if (onDLCLoadedCallback == null)
			{
				return;
			}
			onDLCLoadedCallback();
		}

		// Token: 0x060006BB RID: 1723 RVA: 0x00003DE0 File Offset: 0x00001FE0
		[EngineCallback(null, false)]
		public static string GetVersionStr()
		{
			return ApplicationVersion.FromParametersFile(null).ToString();
		}

		// Token: 0x060006BC RID: 1724 RVA: 0x00003E04 File Offset: 0x00002004
		[EngineCallback(null, false)]
		public static string GetApplicationPlatformName()
		{
			return ApplicationPlatform.CurrentPlatform.ToString();
		}

		// Token: 0x060006BD RID: 1725 RVA: 0x00003E24 File Offset: 0x00002024
		[EngineCallback(null, false)]
		public static string GetModulesVersionStr()
		{
			string text = "";
			foreach (ModuleInfo moduleInfo in ModuleHelper.GetModules(null))
			{
				text = string.Concat(new object[] { text, moduleInfo.Name, "#", moduleInfo.Version, "\n" });
			}
			return text;
		}

		// Token: 0x060006BE RID: 1726 RVA: 0x00003EB0 File Offset: 0x000020B0
		[EngineCallback(null, false)]
		internal static void OnControllerDisconnection()
		{
			ScreenManager.OnControllerDisconnect();
		}
	}
}
