using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.MBSubModuleBaseExtended;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.Implementation.MBSubModuleBaseExtended.Patches;

internal sealed class ModulePatch
{
	private static ILogger _log = null;

	private static readonly Type? TargetType = typeof(TaleWorlds.MountAndBlade.Module);

	private static readonly MethodInfo? miTargetMethodUnLoad = AccessTools2.Method(TargetType, "FinalizeSubModulesBases") ?? AccessTools2.Method(TargetType, "FinalizeSubModules");

	private static readonly MethodInfo? miPatchMethodUnLoad = SymbolExtensions2.GetMethodInfo((TaleWorlds.MountAndBlade.Module x) => FinalizeSubModulesPostfix(x));

	private static readonly MethodInfo? miTargetMethodScreenAsRoot = AccessTools2.Method(TargetType, "SetInitialModuleScreenAsRootScreen");

	private static readonly MethodInfo? miPatchMethodScreenAsRoot = SymbolExtensions2.GetMethodInfo((IEnumerable<CodeInstruction> x) => Transpiler(x));

	private static readonly MethodInfo? miMBSubModuleBaseScreenAsRootEvent = AccessTools2.Method("TaleWorlds.MountAndBlade.MBSubModuleBase:OnBeforeInitialModuleScreenSetAsRoot");

	private static readonly MethodInfo? miDelayedScreenAsRootEventCaller = SymbolExtensions2.GetMethodInfo((TaleWorlds.MountAndBlade.Module x) => DelayedScreenAsRootEvent(x));

	internal static bool Enable(Harmony harmony)
	{
		_log = (ButterLibSubModule.Instance?.GetServiceProvider() ?? ButterLibSubModule.Instance?.GetTempServiceProvider())?.GetService<ILogger<ModulePatch>>() ?? NullLogger<ModulePatch>.Instance;
		if (CheckRequiredMethodInfos() && (object)harmony.Patch(miTargetMethodUnLoad, null, new HarmonyMethod(miPatchMethodUnLoad)) != null)
		{
			return (object)harmony.Patch(miTargetMethodScreenAsRoot, null, null, new HarmonyMethod(miPatchMethodScreenAsRoot)) != null;
		}
		return false;
	}

	internal static bool Disable(Harmony harmony)
	{
		if (CheckRequiredMethodInfos())
		{
			harmony.Unpatch(miTargetMethodUnLoad, miPatchMethodUnLoad);
			harmony.Unpatch(miTargetMethodScreenAsRoot, miPatchMethodScreenAsRoot);
		}
		return true;
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		try
		{
			int num = -1;
			int num2 = -1;
			List<CodeInstruction> list = new List<CodeInstruction>(instructions);
			for (int i = 0; i < list.Count; i++)
			{
				if (num < 0 && list[i].Calls(miMBSubModuleBaseScreenAsRootEvent))
				{
					num = i;
				}
				else if (num2 < 0 && list[i].opcode == OpCodes.Endfinally)
				{
					num2 = i;
					break;
				}
			}
			if (num < 0 || num2 < 0)
			{
				_log.LogDebug("Transpiler for TaleWorlds.MountAndBlade.Module.SetInitialModuleScreenAsRootScreen could not find code hooks!");
				MBSubModuleBaseExSubSystem.LogNoHooksIssue(_log, num, num2, list, MethodBase.GetCurrentMethod());
			}
			else
			{
				list.InsertRange(num2 + 1, new global::_003C_003Ez__ReadOnlyArray<CodeInstruction>(new CodeInstruction[2]
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, miDelayedScreenAsRootEventCaller)
				}));
				list[num2 + 1].MoveLabelsFrom(list[num2 + 3]);
			}
			return list.AsEnumerable();
		}
		catch (Exception exception)
		{
			_log.LogError(exception, "Error while applying Harmony transpiler for TaleWorlds.MountAndBlade.Module.SetInitialModuleScreenAsRootScreen");
			return instructions;
		}
	}

	private static bool CheckRequiredMethodInfos()
	{
		return MBSubModuleBaseExSubSystem.NotNull(_log, TargetType, "TargetType") & MBSubModuleBaseExSubSystem.NotNull(_log, miTargetMethodUnLoad, "miTargetMethodUnLoad") & MBSubModuleBaseExSubSystem.NotNull(_log, miPatchMethodUnLoad, "miPatchMethodUnLoad") & MBSubModuleBaseExSubSystem.NotNull(_log, miTargetMethodScreenAsRoot, "miTargetMethodScreenAsRoot") & MBSubModuleBaseExSubSystem.NotNull(_log, miPatchMethodScreenAsRoot, "miPatchMethodScreenAsRoot") & MBSubModuleBaseExSubSystem.NotNull(_log, miMBSubModuleBaseScreenAsRootEvent, "miMBSubModuleBaseScreenAsRootEvent") & MBSubModuleBaseExSubSystem.NotNull(_log, miDelayedScreenAsRootEventCaller, "miDelayedScreenAsRootEventCaller");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void FinalizeSubModulesPostfix(TaleWorlds.MountAndBlade.Module __instance)
	{
		foreach (IMBSubModuleBaseEx item in __instance.CollectSubModules().OfType<IMBSubModuleBaseEx>())
		{
			item.OnAllSubModulesUnLoaded();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void DelayedScreenAsRootEvent(TaleWorlds.MountAndBlade.Module instance)
	{
		foreach (IMBSubModuleBaseEx item in instance.CollectSubModules().OfType<IMBSubModuleBaseEx>())
		{
			item.OnBeforeInitialModuleScreenSetAsRootDelayed();
		}
	}
}
