using System;
using System.Collections.Generic;

namespace System
{
	// Token: 0x0200003A RID: 58
	public static class AppContext
	{
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000202 RID: 514 RVA: 0x00005260 File Offset: 0x00003460
		public static string BaseDirectory
		{
			get
			{
				return ((string)AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY")) ?? AppDomain.CurrentDomain.BaseDirectory;
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000203 RID: 515 RVA: 0x00005284 File Offset: 0x00003484
		public static string TargetFrameworkName
		{
			get
			{
				return AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
			}
		}

		// Token: 0x06000204 RID: 516 RVA: 0x00005295 File Offset: 0x00003495
		public static object GetData(string name)
		{
			return AppDomain.CurrentDomain.GetData(name);
		}

		// Token: 0x06000205 RID: 517 RVA: 0x000052A4 File Offset: 0x000034A4
		private static void InitializeDefaultSwitchValues()
		{
			Dictionary<string, AppContext.SwitchValueState> obj = AppContext.s_switchMap;
			lock (obj)
			{
				if (!AppContext.s_defaultsInitialized)
				{
					AppContextDefaultValues.PopulateDefaultValues();
					AppContext.s_defaultsInitialized = true;
				}
			}
		}

		// Token: 0x06000206 RID: 518 RVA: 0x000052F4 File Offset: 0x000034F4
		public static bool TryGetSwitch(string switchName, out bool isEnabled)
		{
			if (switchName == null)
			{
				throw new ArgumentNullException("switchName");
			}
			if (switchName.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "switchName");
			}
			if (!AppContext.s_defaultsInitialized)
			{
				AppContext.InitializeDefaultSwitchValues();
			}
			isEnabled = false;
			Dictionary<string, AppContext.SwitchValueState> obj = AppContext.s_switchMap;
			lock (obj)
			{
				AppContext.SwitchValueState switchValueState;
				if (AppContext.s_switchMap.TryGetValue(switchName, out switchValueState))
				{
					if (switchValueState == AppContext.SwitchValueState.UnknownValue)
					{
						isEnabled = false;
						return false;
					}
					isEnabled = (switchValueState & AppContext.SwitchValueState.HasTrueValue) == AppContext.SwitchValueState.HasTrueValue;
					if ((switchValueState & AppContext.SwitchValueState.HasLookedForOverride) == AppContext.SwitchValueState.HasLookedForOverride)
					{
						return true;
					}
					bool flag2;
					if (AppContextDefaultValues.TryGetSwitchOverride(switchName, out flag2))
					{
						isEnabled = flag2;
					}
					AppContext.s_switchMap[switchName] = (isEnabled ? AppContext.SwitchValueState.HasTrueValue : AppContext.SwitchValueState.HasFalseValue) | AppContext.SwitchValueState.HasLookedForOverride;
					return true;
				}
				else
				{
					bool flag3;
					if (AppContextDefaultValues.TryGetSwitchOverride(switchName, out flag3))
					{
						isEnabled = flag3;
						AppContext.s_switchMap[switchName] = (isEnabled ? AppContext.SwitchValueState.HasTrueValue : AppContext.SwitchValueState.HasFalseValue) | AppContext.SwitchValueState.HasLookedForOverride;
						return true;
					}
					AppContext.s_switchMap[switchName] = AppContext.SwitchValueState.UnknownValue;
				}
			}
			return false;
		}

		// Token: 0x06000207 RID: 519 RVA: 0x000053F8 File Offset: 0x000035F8
		public static void SetSwitch(string switchName, bool isEnabled)
		{
			if (switchName == null)
			{
				throw new ArgumentNullException("switchName");
			}
			if (switchName.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "switchName");
			}
			if (!AppContext.s_defaultsInitialized)
			{
				AppContext.InitializeDefaultSwitchValues();
			}
			AppContext.SwitchValueState value = (isEnabled ? AppContext.SwitchValueState.HasTrueValue : AppContext.SwitchValueState.HasFalseValue) | AppContext.SwitchValueState.HasLookedForOverride;
			Dictionary<string, AppContext.SwitchValueState> obj = AppContext.s_switchMap;
			lock (obj)
			{
				AppContext.s_switchMap[switchName] = value;
			}
		}

		// Token: 0x06000208 RID: 520 RVA: 0x00005480 File Offset: 0x00003680
		internal static void DefineSwitchDefault(string switchName, bool isEnabled)
		{
			AppContext.s_switchMap[switchName] = (isEnabled ? AppContext.SwitchValueState.HasTrueValue : AppContext.SwitchValueState.HasFalseValue);
		}

		// Token: 0x06000209 RID: 521 RVA: 0x00005494 File Offset: 0x00003694
		internal static void DefineSwitchOverride(string switchName, bool isEnabled)
		{
			AppContext.s_switchMap[switchName] = (isEnabled ? AppContext.SwitchValueState.HasTrueValue : AppContext.SwitchValueState.HasFalseValue) | AppContext.SwitchValueState.HasLookedForOverride;
		}

		// Token: 0x040001C8 RID: 456
		private static readonly Dictionary<string, AppContext.SwitchValueState> s_switchMap = new Dictionary<string, AppContext.SwitchValueState>();

		// Token: 0x040001C9 RID: 457
		private static volatile bool s_defaultsInitialized = false;

		// Token: 0x02000ABF RID: 2751
		[Flags]
		private enum SwitchValueState
		{
			// Token: 0x040030C1 RID: 12481
			HasFalseValue = 1,
			// Token: 0x040030C2 RID: 12482
			HasTrueValue = 2,
			// Token: 0x040030C3 RID: 12483
			HasLookedForOverride = 4,
			// Token: 0x040030C4 RID: 12484
			UnknownValue = 8
		}
	}
}
