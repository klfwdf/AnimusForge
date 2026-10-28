using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bannerlord.ButterLib.ExceptionHandler.DebuggerDetection;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

namespace Bannerlord.ButterLib.ExceptionHandler;

internal sealed class BEWPatch
{
	private static bool _cachedDebuggerAttached;

	private static int _lastCheckTicks;

	private static readonly string[] BEW = new string[1] { "org.calradia.admiralnelson.betterexceptionwindow" };

	private static readonly MethodInfo? ManagedApplicationTickMethod = HarmonyLib.BUTR.Extensions.AccessTools2.Method("TaleWorlds.DotNet.Managed:ApplicationTick");

	private static readonly MethodInfo? ModuleOnApplicationTickMethod = HarmonyLib.BUTR.Extensions.AccessTools2.Method("TaleWorlds.MountAndBlade.Module:OnApplicationTick");

	private static readonly MethodInfo? ScreenManagerTickMethod = HarmonyLib.BUTR.Extensions.AccessTools2.Method("TaleWorlds.ScreenSystem.ScreenManager:Tick");

	private static readonly MethodInfo? ManagedScriptHolderTickComponentsMethod = HarmonyLib.BUTR.Extensions.AccessTools2.Method("TaleWorlds.Engine.ManagedScriptHolder:TickComponents");

	private static readonly MethodInfo? MissionTickMethod = HarmonyLib.BUTR.Extensions.AccessTools2.Method("TaleWorlds.MountAndBlade.Mission:Tick");

	public static readonly MethodInfo? FinalizerMethod = HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((Exception x) => Finalizer(x));

	public static bool IsDebuggerAttached()
	{
		int tickCount = Environment.TickCount;
		if (tickCount - _lastCheckTicks < 100)
		{
			return _cachedDebuggerAttached;
		}
		_cachedDebuggerAttached = false;
		if (Debugger.IsAttached)
		{
			_cachedDebuggerAttached = true;
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			_cachedDebuggerAttached = ProcessDebug.CheckProcessDebugObjectHandle();
		}
		_lastCheckTicks = tickCount;
		return _cachedDebuggerAttached;
	}

	private static void Finalizer(Exception? __exception)
	{
		if (__exception != null)
		{
			ExceptionHandlerSubSystem? instance = ExceptionHandlerSubSystem.Instance;
			if (instance == null || !instance.DisableWhenDebuggerIsAttached || !IsDebuggerAttached())
			{
				ExceptionReporter.Show(__exception);
			}
		}
	}

	internal static void Enable(Harmony harmony)
	{
		harmony.Patch(ManagedApplicationTickMethod, null, null, null, new HarmonyMethod(FinalizerMethod, -1, BEW));
		harmony.Patch(ModuleOnApplicationTickMethod, null, null, null, new HarmonyMethod(FinalizerMethod, -1, BEW));
		harmony.Patch(ScreenManagerTickMethod, null, null, null, new HarmonyMethod(FinalizerMethod, -1, BEW));
		harmony.Patch(ManagedScriptHolderTickComponentsMethod, null, null, null, new HarmonyMethod(FinalizerMethod, -1, BEW));
		harmony.Patch(MissionTickMethod, null, null, null, new HarmonyMethod(FinalizerMethod, -1, BEW));
		harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method("ManagedCallbacks.LibraryCallbacksGenerated:Managed_ApplicationTick"), null, null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(BEWPatch), "BlankTranspiler"));
		harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method("ManagedCallbacks.EngineCallbacksGenerated:ScreenManager_Tick") ?? HarmonyLib.BUTR.Extensions.AccessTools2.Method("ManagedCallbacks.EngineCallbacksGenerated:EngineScreenManager_Tick"), null, null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(BEWPatch), "BlankTranspiler"));
		harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method("ManagedCallbacks.EngineCallbacksGenerated:ManagedScriptHolder_TickComponents"), null, null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(BEWPatch), "BlankTranspiler"));
		harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method("TaleWorlds.MountAndBlade.MissionState:FinishMissionLoading"), null, null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(BEWPatch), "BlankTranspiler"));
		harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method("TaleWorlds.MountAndBlade.MissionState:TickMissionAux"), null, null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(BEWPatch), "BlankTranspiler"));
		harmony.TryPatch(HarmonyLib.BUTR.Extensions.AccessTools2.Method("TaleWorlds.MountAndBlade.MissionState:TickMission"), null, null, HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(BEWPatch), "BlankTranspiler"));
	}

	internal static void Disable(Harmony harmony)
	{
		harmony.Unpatch(ManagedApplicationTickMethod, FinalizerMethod);
		harmony.Unpatch(ModuleOnApplicationTickMethod, FinalizerMethod);
		harmony.Unpatch(ScreenManagerTickMethod, FinalizerMethod);
		harmony.Unpatch(ManagedScriptHolderTickComponentsMethod, FinalizerMethod);
		harmony.Unpatch(MissionTickMethod, FinalizerMethod);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IEnumerable<CodeInstruction> BlankTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		return instructions;
	}
}
