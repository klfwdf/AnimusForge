using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Engine.GauntletUI
{
	// Token: 0x0200000A RID: 10
	public static class UIConfig
	{
		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600005B RID: 91 RVA: 0x00003315 File Offset: 0x00001515
		// (set) Token: 0x0600005C RID: 92 RVA: 0x0000331C File Offset: 0x0000151C
		public static bool DoNotUseGeneratedPrefabs { get; set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x0600005D RID: 93 RVA: 0x00003324 File Offset: 0x00001524
		// (set) Token: 0x0600005E RID: 94 RVA: 0x0000332B File Offset: 0x0000152B
		public static bool DebugModeEnabled { get; set; }

		// Token: 0x0600005F RID: 95 RVA: 0x00003333 File Offset: 0x00001533
		public static bool GetIsUsingGeneratedPrefabs()
		{
			return !NativeConfig.GetUIDoNotUseGeneratedPrefabs && !UIConfig.DoNotUseGeneratedPrefabs;
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00003346 File Offset: 0x00001546
		public static bool GetIsHotReloadEnabled()
		{
			return NativeConfig.GetUIDebugMode || UIConfig.DebugModeEnabled;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00003358 File Offset: 0x00001558
		[CommandLineFunctionality.CommandLineArgumentFunction("set_debug_mode", "ui")]
		public static string SetDebugMode(List<string> args)
		{
			string result = "Format is \"ui.set_debug_mode [1/0]\".";
			if (args.Count != 1)
			{
				return result;
			}
			int num;
			if (int.TryParse(args[0], out num) && (num == 1 || num == 0))
			{
				UIConfig.DebugModeEnabled = num == 1;
				return "Success.";
			}
			return result;
		}

		// Token: 0x06000062 RID: 98 RVA: 0x000033A0 File Offset: 0x000015A0
		[CommandLineFunctionality.CommandLineArgumentFunction("use_generated_prefabs", "ui")]
		public static string SetUsingGeneratedPrefabs(List<string> args)
		{
			string result = "Format is \"ui.use_generated_prefabs [1/0].\"";
			if (args.Count != 1)
			{
				return result;
			}
			int num;
			if (int.TryParse(args[0], out num) && (num == 1 || num == 0))
			{
				UIConfig.DoNotUseGeneratedPrefabs = num == 0;
				return "Success.";
			}
			return result;
		}
	}
}
