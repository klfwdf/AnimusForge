using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.DotNet;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class InteractionComponentSafePatch
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
			Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.View.MissionViews.MissionMainAgentInteractionComponent");
			if (!(type == null))
			{
				MethodInfo methodInfo = AccessTools.Method(type, "FocusTick");
				if (!(methodInfo == null))
				{
					Harmony harmony = new Harmony("AnimusForge.interactioncomponent.safety");
					HarmonyMethod prefix = new HarmonyMethod(typeof(InteractionComponentSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
					HarmonyMethod finalizer = new HarmonyMethod(typeof(InteractionComponentSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, prefix, null, null, finalizer);
					_patched = true;
					Logger.LogTrace("System", "✅ InteractionComponentSafePatch 已对 MissionMainAgentInteractionComponent.FocusTick 打补丁（含 Finalizer）。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ InteractionComponentSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix()
	{
		try
		{
			Mission current = Mission.Current;
			if (current == null)
			{
				return false;
			}
			bool flag = false;
			try
			{
				flag = current.MissionEnded;
			}
			catch
			{
			}
			if (flag)
			{
				bool flag2 = false;
				try
				{
					flag2 = LordEncounterBehavior.IsEncounterMeetingMissionActive || DuelBehavior.IsArenaMissionActive;
				}
				catch
				{
				}
				if (flag2)
				{
					return false;
				}
			}
			if (current.MainAgent == null)
			{
				return false;
			}
			if ((NativeObject)(object)current.Scene == (NativeObject)null)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	public static Exception Finalizer(Exception __exception)
	{
		if (__exception != null)
		{
			return null;
		}
		return null;
	}
}
