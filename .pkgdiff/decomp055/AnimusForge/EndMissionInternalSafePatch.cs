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
			HarmonyMethod finalizer = new HarmonyMethod(typeof(EndMissionInternalSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
			harmony.Patch(methodInfo, null, null, null, finalizer);
			_patched = true;
			Logger.LogTrace("System", "✅ EndMissionInternalSafePatch 已对 Mission.EndMissionInternal 打补丁 (Finalizer)。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ EndMissionInternalSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static Exception Finalizer(Exception __exception)
	{
		try
		{
			if (PlayerEncounter.Current != null && (PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null))
			{
				LordEncounterRedirectGuard.SuppressForSeconds(1f);
			}
			bool flag = false;
			try
			{
				flag = LordEncounterBehavior.IsEncounterMeetingMissionActive || MeetingBattleRuntime.IsMeetingActive;
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				try
				{
					flag = Mission.Current != null && Mission.Current.GetMissionBehavior<MeetingBattleLockMissionBehavior>() != null;
				}
				catch
				{
					flag = false;
				}
			}
			if (__exception is NullReferenceException && (DuelBehavior.IsArenaMissionActive || flag))
			{
				Logger.LogTrace("System", $"⚠\ufe0f EndMissionInternalSafePatch 捕获并吞掉 NullReferenceException (ArenaOrMeetingMission)。 meeting={flag}");
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}
}
