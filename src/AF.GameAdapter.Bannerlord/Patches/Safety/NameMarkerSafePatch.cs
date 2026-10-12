using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class NameMarkerSafePatch
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
			Type type = AccessTools.TypeByName("SandBox.View.Missions.NameMarkers.DefaultMissionNameMarkerHandler");
			if (!(type == null))
			{
				MethodInfo methodInfo = AccessTools.Method(type, "OnTick");
				if (!(methodInfo == null))
				{
					Harmony harmony = new Harmony("AnimusForge.namemarker.safety");
					HarmonyMethod prefix = new HarmonyMethod(typeof(NameMarkerSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, prefix);
					_patched = true;
					Logger.LogTrace("System", "✅ NameMarkerSafePatch Patched DefaultMissionNameMarkerHandler.OnTick successfully.");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ NameMarkerSafePatch patch failed: " + ex.Message);
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
			if (flag || current.Scene == null || current.Agents == null)
			{
				return false;
			}
			if (DuelBehavior.IsArenaMissionActive)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}
}
