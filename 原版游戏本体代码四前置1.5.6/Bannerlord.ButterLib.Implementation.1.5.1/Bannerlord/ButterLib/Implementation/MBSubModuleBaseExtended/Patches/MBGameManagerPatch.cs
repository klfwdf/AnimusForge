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
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.Implementation.MBSubModuleBaseExtended.Patches;

internal sealed class MBGameManagerPatch
{
	private static ILogger _log = null;

	private static readonly MethodInfo? miTargetMethodGameStart = AccessTools2.Method("TaleWorlds.MountAndBlade.MBGameManager:OnGameStart");

	private static readonly MethodInfo? miTargetMethodGameEnd = AccessTools2.Method("TaleWorlds.MountAndBlade.MBGameManager:OnGameEnd");

	private static readonly MethodInfo? miPatchMethod = SymbolExtensions2.GetMethodInfo((IEnumerable<CodeInstruction> x, MethodBase y) => Transpiler(x, y));

	private static readonly MethodInfo? miMBSubModuleBaseOnGameStartEvent = AccessTools2.Method("TaleWorlds.MountAndBlade.MBSubModuleBase:OnGameStart");

	private static readonly MethodInfo? miMBSubModuleBaseOnGameEndEvent = AccessTools2.Method("TaleWorlds.MountAndBlade.MBSubModuleBase:OnGameEnd");

	private static readonly MethodInfo? miDelayedOnGameStartEventCaller = SymbolExtensions2.GetMethodInfo((Game x, IGameStarter y) => DelayedOnGameStartEvent(x, y));

	private static readonly MethodInfo? miDelayedOnGameEndEventCaller = SymbolExtensions2.GetMethodInfo((Game x) => DelayedOnGameEndEvent(x));

	internal static bool Enable(Harmony harmony)
	{
		_log = (ButterLibSubModule.Instance?.GetServiceProvider() ?? ButterLibSubModule.Instance?.GetTempServiceProvider())?.GetService<ILogger<ModulePatch>>() ?? NullLogger<ModulePatch>.Instance;
		if (CheckRequiredMethodInfos() && (object)harmony.Patch(miTargetMethodGameStart, null, null, new HarmonyMethod(miPatchMethod)) != null)
		{
			return (object)harmony.Patch(miTargetMethodGameEnd, null, null, new HarmonyMethod(miPatchMethod)) != null;
		}
		return false;
	}

	internal static bool Disable(Harmony harmony)
	{
		if (CheckRequiredMethodInfos())
		{
			harmony.Unpatch(miTargetMethodGameStart, miPatchMethod);
			harmony.Unpatch(miTargetMethodGameEnd, miPatchMethod);
		}
		return true;
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		string text = "TaleWorlds.MountAndBlade.MBGameManager." + __originalMethod.Name;
		string name = __originalMethod.Name;
		MethodInfo miToSearchFor;
		CodeInstruction[] ciToAdd;
		if (!(name == "OnGameStart"))
		{
			if (!(name == "OnGameEnd"))
			{
				_log.LogError("Error while applying Harmony transpiler for {Method} - unexpected target method!", text);
				return instructions;
			}
			miToSearchFor = miMBSubModuleBaseOnGameEndEvent;
			ciToAdd = new CodeInstruction[2]
			{
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Call, miDelayedOnGameEndEventCaller)
			};
		}
		else
		{
			miToSearchFor = miMBSubModuleBaseOnGameStartEvent;
			ciToAdd = new CodeInstruction[3]
			{
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Call, miDelayedOnGameStartEventCaller)
			};
		}
		return AddDelayedEvent(instructions, miToSearchFor, ciToAdd, text);
	}

	private static bool CheckRequiredMethodInfos()
	{
		return MBSubModuleBaseExSubSystem.NotNull(_log, miTargetMethodGameStart, "miTargetMethodGameStart") & MBSubModuleBaseExSubSystem.NotNull(_log, miTargetMethodGameEnd, "miTargetMethodGameEnd") & MBSubModuleBaseExSubSystem.NotNull(_log, miPatchMethod, "miPatchMethod") & MBSubModuleBaseExSubSystem.NotNull(_log, miMBSubModuleBaseOnGameStartEvent, "miMBSubModuleBaseOnGameStartEvent") & MBSubModuleBaseExSubSystem.NotNull(_log, miDelayedOnGameStartEventCaller, "miDelayedOnGameStartEventCaller");
	}

	private static IEnumerable<CodeInstruction> AddDelayedEvent(IEnumerable<CodeInstruction> instructions, MethodInfo miToSearchFor, CodeInstruction[] ciToAdd, string originalMethodName)
	{
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		try
		{
			int num = -1;
			int num2 = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (num < 0 && list[i].Calls(miToSearchFor))
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
				_log.LogDebug("Transpiler for {Method} could not find code hooks!", originalMethodName);
				MBSubModuleBaseExSubSystem.LogNoHooksIssue(_log, num, num2, list, MethodBase.GetCurrentMethod());
			}
			else
			{
				list.InsertRange(num2 + 1, ciToAdd);
				list[num2 + 1].MoveLabelsFrom(list[num2 + ciToAdd.Length + 1]);
			}
			return list;
		}
		catch (Exception exception)
		{
			_log.LogError(exception, "Error while applying Harmony transpiler for {Method}", originalMethodName);
			return list;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void DelayedOnGameStartEvent(Game game, IGameStarter gameStarter)
	{
		foreach (IMBSubModuleBaseEx item in TaleWorlds.MountAndBlade.Module.CurrentModule.CollectSubModules().OfType<IMBSubModuleBaseEx>())
		{
			item.OnGameStartDelayed(game, gameStarter);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void DelayedOnGameEndEvent(Game game)
	{
		foreach (IMBSubModuleBaseEx item in TaleWorlds.MountAndBlade.Module.CurrentModule.CollectSubModules().OfType<IMBSubModuleBaseEx>())
		{
			item.OnGameEndDelayed(game);
		}
	}
}
