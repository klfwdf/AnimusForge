using System;
using System.Reflection;
using HarmonyLib;

namespace Voxforge;

public static class PassageUsePointSafePatch
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
			Type type = AccessTools.TypeByName("SandBox.Objects.PassageUsePoint");
			if (!(type == null))
			{
				MethodInfo methodInfo = AccessTools.Method(type, "AfterMissionStart");
				if (!(methodInfo == null))
				{
					Harmony harmony = new Harmony("Voxforge.passageusepoint.safety");
					HarmonyMethod prefix = new HarmonyMethod(typeof(PassageUsePointSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
					harmony.Patch(methodInfo, prefix);
					_patched = true;
					Logger.LogTrace("System", "✅ PassageUsePointSafePatch 已对 PassageUsePoint.AfterMissionStart 打补丁。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ PassageUsePointSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix()
	{
		try
		{
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
