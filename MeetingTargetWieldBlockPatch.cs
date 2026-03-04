using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class MeetingTargetWieldBlockPatch
{
	private static bool _patched;

	private static float _lastLogTime;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type typeFromHandle = typeof(Agent);
			if (typeFromHandle == null)
			{
				return;
			}
			MethodInfo method = typeof(MeetingTargetWieldBlockPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
			if (method == null)
			{
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.meeting.target.wieldblock");
			int num = 0;
			MethodInfo[] methods = typeFromHandle.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo[] array = methods;
			foreach (MethodInfo methodInfo in array)
			{
				if (methodInfo == null)
				{
					continue;
				}
				string name = methodInfo.Name;
				if (!(name != "TryToWieldWeaponInSlot") || !(name != "TryToWieldWeaponInHand") || !(name != "WieldInitialWeapons"))
				{
					try
					{
						harmony.Patch(methodInfo, new HarmonyMethod(method));
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
				Logger.LogTrace("System", $"✅ MeetingTargetWieldBlockPatch 已打补丁。Patched={num}");
			}
			else
			{
				Logger.LogTrace("System", "❌ MeetingTargetWieldBlockPatch 未找到可用目标方法。");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ MeetingTargetWieldBlockPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(Agent __instance)
	{
		try
		{
			if (!ShouldBlockTargetWield(__instance))
			{
				return true;
			}
			float applicationTime = Time.ApplicationTime;
			if (applicationTime - _lastLogTime > 1f)
			{
				_lastLogTime = applicationTime;
				string text = "";
				try
				{
					text = __instance?.Name.ToString() ?? "";
				}
				catch
				{
				}
				Logger.Log("MeetingBattle", "Blocked target lord wield attempt. Agent=" + text);
			}
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static bool ShouldBlockTargetWield(Agent agent)
	{
		if (agent == null || !agent.IsHuman)
		{
			return false;
		}
		try
		{
			if (!LordEncounterBehavior.IsEncounterMeetingMissionActive)
			{
				return false;
			}
			if (!MeetingBattleRuntime.IsMeetingActive || MeetingBattleRuntime.IsCombatEscalated)
			{
				return false;
			}
			Hero targetHero = MeetingBattleRuntime.TargetHero;
			if (targetHero == null)
			{
				return false;
			}
			return ((agent.Character is CharacterObject characterObject) ? characterObject.HeroObject : null) == targetHero;
		}
		catch
		{
			return false;
		}
	}
}
