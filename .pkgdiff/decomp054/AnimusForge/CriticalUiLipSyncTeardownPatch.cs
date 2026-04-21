using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.Core;

namespace AnimusForge;

public static class CriticalUiLipSyncTeardownPatch
{
	private static bool _patched;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(GameStateManager), "PushState", new Type[2]
			{
				typeof(GameState),
				typeof(int)
			});
			MethodInfo methodInfo2 = AccessTools.Method(typeof(GameStateManager), "CleanAndPushState", new Type[2]
			{
				typeof(GameState),
				typeof(int)
			});
			MethodInfo methodInfo3 = AccessTools.Method(typeof(BarterManager), "BeginPlayerBarter");
			if (methodInfo == null || methodInfo2 == null || methodInfo3 == null)
			{
				Logger.LogTrace("System", "CriticalUiLipSyncTeardownPatch: missing target method.");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.criticalui.lipsyncteardown");
			HarmonyMethod prefix = new HarmonyMethod(typeof(CriticalUiLipSyncTeardownPatch).GetMethod("BeforePushState", BindingFlags.Static | BindingFlags.Public));
			HarmonyMethod prefix2 = new HarmonyMethod(typeof(CriticalUiLipSyncTeardownPatch).GetMethod("BeforeCleanAndPushState", BindingFlags.Static | BindingFlags.Public));
			HarmonyMethod prefix3 = new HarmonyMethod(typeof(CriticalUiLipSyncTeardownPatch).GetMethod("BeforeBeginPlayerBarter", BindingFlags.Static | BindingFlags.Public));
			harmony.Patch(methodInfo, prefix);
			harmony.Patch(methodInfo2, prefix2);
			harmony.Patch(methodInfo3, prefix3);
			_patched = true;
			Logger.LogTrace("System", "CriticalUiLipSyncTeardownPatch enabled.");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "CriticalUiLipSyncTeardownPatch failed: " + ex.Message);
		}
	}

	public static void BeforePushState(GameState gameState)
	{
		try
		{
			ShoutBehavior.NotifyCriticalUiTransition("PUSHSTATE:" + (((object)gameState)?.GetType().Name ?? "null"));
		}
		catch
		{
		}
	}

	public static void BeforeCleanAndPushState(GameState gameState)
	{
		try
		{
			ShoutBehavior.NotifyCriticalUiTransition("CLEANPUSH:" + (((object)gameState)?.GetType().Name ?? "null"));
		}
		catch
		{
		}
	}

	public static void BeforeBeginPlayerBarter()
	{
		try
		{
			ShoutBehavior.NotifyCriticalUiTransition("BEGIN_BARTER");
		}
		catch
		{
		}
	}
}
