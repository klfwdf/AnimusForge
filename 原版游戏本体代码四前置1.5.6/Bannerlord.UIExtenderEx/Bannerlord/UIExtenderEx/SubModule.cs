using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BUTR.MessageBoxPInvoke.Helpers;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.UIExtenderEx.Utils;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.UIExtenderEx;

public class SubModule : MBSubModuleBase
{
	private const string SWarningTitle = "{=eySpdc25EE}Warning from Bannerlord.UIExtenderEx!";

	private const string SMessageContinue = "{=eXs6FLm5DP}It's strongly recommended to terminate the game now. Do you wish to terminate it?";

	static SubModule()
	{
		try
		{
			Assembly.Load("TaleWorlds.Engine.GauntletUI");
		}
		catch (Exception arg)
		{
			MessageUtils.Fail($"Failed to load 'TaleWorlds.Engine.GauntletUI'! Exception: {arg}");
		}
		UIConfig.DoNotUseGeneratedPrefabs = true;
	}

	public SubModule()
	{
		ValidateLoadOrder();
	}

	private static void ValidateLoadOrder()
	{
		List<ModuleInfoExtendedHelper> list = ModuleInfoHelper.GetLoadedModules().ToList();
		if (list.Count == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!ModuleInfoHelper.ValidateLoadOrder(typeof(SubModule), out string report))
		{
			stringBuilder.AppendLine(report);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(new TextObject("{=eXs6FLm5DP}It's strongly recommended to terminate the game now. Do you wish to terminate it?")?.ToString() ?? "ERROR");
			MessageBoxResult messageBoxResult = MessageBoxDialog.Show(stringBuilder.ToString(), new TextObject("{=eySpdc25EE}Warning from Bannerlord.UIExtenderEx!")?.ToString() ?? "ERROR", MessageBoxButtons.YesNo);
			if (messageBoxResult == MessageBoxResult.Yes)
			{
				Environment.Exit(1);
			}
		}
	}
}
