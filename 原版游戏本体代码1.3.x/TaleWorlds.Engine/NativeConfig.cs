using System;
using TaleWorlds.Engine.Options;

namespace TaleWorlds.Engine
{
	// Token: 0x02000071 RID: 113
	public static class NativeConfig
	{
		// Token: 0x17000061 RID: 97
		// (get) Token: 0x06000A4A RID: 2634 RVA: 0x0000A77E File Offset: 0x0000897E
		// (set) Token: 0x06000A4B RID: 2635 RVA: 0x0000A785 File Offset: 0x00008985
		public static bool CheatMode { get; private set; }

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x06000A4C RID: 2636 RVA: 0x0000A78D File Offset: 0x0000898D
		// (set) Token: 0x06000A4D RID: 2637 RVA: 0x0000A794 File Offset: 0x00008994
		public static bool IsDevelopmentMode { get; private set; }

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x06000A4E RID: 2638 RVA: 0x0000A79C File Offset: 0x0000899C
		// (set) Token: 0x06000A4F RID: 2639 RVA: 0x0000A7A3 File Offset: 0x000089A3
		public static bool LocalizationDebugMode { get; private set; }

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000A50 RID: 2640 RVA: 0x0000A7AB File Offset: 0x000089AB
		// (set) Token: 0x06000A51 RID: 2641 RVA: 0x0000A7B2 File Offset: 0x000089B2
		public static bool GetUIDebugMode { get; private set; }

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000A52 RID: 2642 RVA: 0x0000A7BA File Offset: 0x000089BA
		// (set) Token: 0x06000A53 RID: 2643 RVA: 0x0000A7C1 File Offset: 0x000089C1
		public static bool DisableSound { get; private set; }

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000A54 RID: 2644 RVA: 0x0000A7C9 File Offset: 0x000089C9
		// (set) Token: 0x06000A55 RID: 2645 RVA: 0x0000A7D0 File Offset: 0x000089D0
		public static bool EnableEditMode { get; private set; }

		// Token: 0x06000A56 RID: 2646 RVA: 0x0000A7D8 File Offset: 0x000089D8
		public static void OnConfigChanged()
		{
			NativeConfig.CheatMode = EngineApplicationInterface.IConfig.GetCheatMode();
			NativeConfig.IsDevelopmentMode = EngineApplicationInterface.IConfig.GetDevelopmentMode();
			NativeConfig.GetUIDebugMode = EngineApplicationInterface.IConfig.GetUIDebugMode();
			NativeConfig.LocalizationDebugMode = EngineApplicationInterface.IConfig.GetLocalizationDebugMode();
			NativeConfig.EnableEditMode = EngineApplicationInterface.IConfig.GetEnableEditMode();
			NativeConfig.DisableSound = EngineApplicationInterface.IConfig.GetDisableSound();
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x06000A57 RID: 2647 RVA: 0x0000A83F File Offset: 0x00008A3F
		public static bool TableauCacheEnabled
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetTableauCacheMode();
			}
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000A58 RID: 2648 RVA: 0x0000A84B File Offset: 0x00008A4B
		public static bool DoLocalizationCheckAtStartup
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetDoLocalizationCheckAtStartup();
			}
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x06000A59 RID: 2649 RVA: 0x0000A857 File Offset: 0x00008A57
		public static bool EnableClothSimulation
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetEnableClothSimulation();
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x06000A5A RID: 2650 RVA: 0x0000A863 File Offset: 0x00008A63
		public static int CharacterDetail
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetCharacterDetail();
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x06000A5B RID: 2651 RVA: 0x0000A86F File Offset: 0x00008A6F
		public static bool InvertMouse
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetInvertMouse();
			}
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x06000A5C RID: 2652 RVA: 0x0000A87B File Offset: 0x00008A7B
		public static string LastOpenedScene
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetLastOpenedScene();
			}
		}

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x06000A5D RID: 2653 RVA: 0x0000A887 File Offset: 0x00008A87
		public static int AutoSaveInMinutes
		{
			get
			{
				return EngineApplicationInterface.IConfig.AutoSaveInMinutes();
			}
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x06000A5E RID: 2654 RVA: 0x0000A893 File Offset: 0x00008A93
		public static bool GetUIDoNotUseGeneratedPrefabs
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetUIDoNotUseGeneratedPrefabs();
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x06000A5F RID: 2655 RVA: 0x0000A89F File Offset: 0x00008A9F
		public static string DebugLoginUsername
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetDebugLoginUserName();
			}
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x06000A60 RID: 2656 RVA: 0x0000A8AB File Offset: 0x00008AAB
		public static string DebugLogicPassword
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetDebugLoginPassword();
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x06000A61 RID: 2657 RVA: 0x0000A8B7 File Offset: 0x00008AB7
		public static bool DisableGuiMessages
		{
			get
			{
				return EngineApplicationInterface.IConfig.GetDisableGuiMessages();
			}
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x06000A62 RID: 2658 RVA: 0x0000A8C3 File Offset: 0x00008AC3
		public static NativeOptions.ConfigQuality AutoGFXQuality
		{
			get
			{
				return (NativeOptions.ConfigQuality)EngineApplicationInterface.IConfig.GetAutoGFXQuality();
			}
		}

		// Token: 0x06000A63 RID: 2659 RVA: 0x0000A8CF File Offset: 0x00008ACF
		public static void SetAutoConfigWrtHardware()
		{
			EngineApplicationInterface.IConfig.SetAutoConfigWrtHardware();
		}
	}
}
