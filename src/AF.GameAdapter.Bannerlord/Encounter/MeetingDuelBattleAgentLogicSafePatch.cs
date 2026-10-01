using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Engine;

namespace AnimusForge;

public static class MeetingDuelBattleAgentLogicSafePatch
{
	private static bool _patched;

	private static float _lastSuppressedLogTime;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("SandBox.Missions.MissionLogics.BattleAgentLogic");
			if (type == null)
			{
				Logger.LogTrace("System", "❌ MeetingDuelBattleAgentLogicSafePatch: 找不到 BattleAgentLogic 类型。");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.battleagentlogic.meetingduel.safety");
			HarmonyMethod finalizer = new HarmonyMethod(typeof(MeetingDuelBattleAgentLogicSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
			int num = 0;
			foreach (MethodInfo declaredMethod in AccessTools.GetDeclaredMethods(type))
			{
				if (declaredMethod == null)
				{
					continue;
				}
				int num2 = 0;
				try
				{
					num2 = declaredMethod.GetParameters().Length;
				}
				catch
				{
				}
				if ((declaredMethod.Name == "OnAgentBuild" && num2 == 2) || (declaredMethod.Name == "OnScoreHit" && num2 == 10) || (declaredMethod.Name == "OnAgentRemoved" && num2 == 4))
				{
					try
					{
						harmony.Patch(declaredMethod, null, null, null, finalizer);
						num++;
					}
					catch
					{
					}
				}
			}
			_patched = num > 0;
			if (_patched)
			{
				Logger.LogTrace("System", $"✅ MeetingDuelBattleAgentLogicSafePatch 已打补丁。Patched={num}");
			}
			else
			{
				Logger.LogTrace("System", "❌ MeetingDuelBattleAgentLogicSafePatch 未找到可用目标方法。");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ MeetingDuelBattleAgentLogicSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static Exception Finalizer(Exception __exception)
	{
		if (!(__exception is NullReferenceException))
		{
			return __exception;
		}
		try
		{
			if (!ShouldSuppressNullRef())
			{
				return __exception;
			}
			float num = 0f;
			try
			{
				num = Time.ApplicationTime;
			}
			catch
			{
			}
			if (num - _lastSuppressedLogTime > 1f)
			{
				_lastSuppressedLogTime = num;
				Logger.Log("MeetingBattle", "Suppressed BattleAgentLogic NullReferenceException in meeting/formal-duel mission (no PlayerMapEvent).");
			}
			return null;
		}
		catch
		{
			return __exception;
		}
	}

	private static bool ShouldSuppressNullRef()
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		try
		{
			flag = DuelBehavior.IsFormalDuelActive;
		}
		catch
		{
		}
		try
		{
			flag2 = LordEncounterBehavior.IsEncounterMeetingMissionActive;
		}
		catch
		{
		}
		try
		{
			flag3 = MeetingBattleRuntime.IsMeetingActive;
		}
		catch
		{
			flag3 = false;
		}
		try
		{
			flag4 = MeetingBattleRuntime.IsCombatEscalated;
		}
		catch
		{
			flag4 = false;
		}
		if (!flag && !flag2 && !flag3 && !flag4)
		{
			return false;
		}
		return true;
	}
}
