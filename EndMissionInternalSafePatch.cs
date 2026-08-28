using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class EndMissionInternalSafePatch
{
	private static bool _patched;

	// EndMissionInternal 会先让各 MissionBehavior.OnEndMissionInternal() 跑一遍再清理 agent/mission object，
	// 而 AF 自己的 MeetingBattleLockMissionBehavior.OnEndMission() 会在那一步就把 IsEncounterMeetingMissionActive 清成 false。
	// 如果异常发生在那之后，Finalizer 里再读实时标志已经晚了，必须通过 Harmony __state 保留本次调用入口快照。

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.Mission");
			if (type == null)
			{
				Logger.LogTrace("System", "❌ EndMissionInternalSafePatch: 找不到 Mission 类型。");
				return;
			}
			MethodInfo methodInfo = AccessTools.Method(type, "EndMissionInternal");
			if (methodInfo == null)
			{
				Logger.LogTrace("System", "❌ EndMissionInternalSafePatch: 找不到 EndMissionInternal 目标方法。");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.mission.endmissioninternal.safety");
			HarmonyMethod prefix = new HarmonyMethod(typeof(EndMissionInternalSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
			HarmonyMethod finalizer = new HarmonyMethod(typeof(EndMissionInternalSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
			harmony.Patch(methodInfo, prefix, null, null, finalizer);
			_patched = true;
			Logger.LogTrace("System", "✅ EndMissionInternalSafePatch 已对 Mission.EndMissionInternal 打补丁 (Prefix + Finalizer)。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ EndMissionInternalSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static void Prefix(Mission __instance, out bool __state)
	{
		__state = false;
		try
		{
			__state = LordEncounterBehavior.IsEncounterMeetingMissionActive || MeetingBattleRuntime.IsMeetingActive;
		}
		catch
		{
		}
		if (!__state)
		{
			try
			{
				__state = __instance != null && __instance.GetMissionBehavior<MeetingBattleLockMissionBehavior>() != null;
			}
			catch
			{
			}
		}
		if (!__state)
		{
			try
			{
				__state = DuelBehavior.IsArenaMissionActive;
			}
			catch
			{
			}
		}
	}

	public static Exception Finalizer(Exception __exception, bool __state)
	{
		try
		{
			if (PlayerEncounter.Current != null && (PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null))
			{
				LordEncounterRedirectGuard.SuppressForSeconds(1f);
			}
			if (__exception is NullReferenceException && __state)
			{
				Logger.LogTrace("System", $"⚠\ufe0f EndMissionInternalSafePatch 捕获并吞掉 NullReferenceException (ArenaOrMeetingMission)。 entrySnapshot={__state}");
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}
}
