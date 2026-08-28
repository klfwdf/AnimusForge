using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace AnimusForge;

public static class Patch_Conversation_Start_Intercept
{
	private static volatile bool _nativeConversationMissionArmPatchReady;

	internal static bool IsNativeConversationMissionArmPatchReady => _nativeConversationMissionArmPatchReady;

	public static void ManualPatch(Harmony harmony)
	{
		TryPatchNativeConversationMissionArm(harmony);
		try
		{
			MethodInfo prefix = AccessTools.Method(typeof(Patch_Conversation_Start_Intercept), "Prefix");
			HashSet<MethodBase> hashSet = new HashSet<MethodBase>();
			TryAddDeclaredMethod(hashSet, "StartConversation");
			int num = 0;
			foreach (MethodBase item in hashSet)
			{
				harmony.Patch(item, new HarmonyMethod(prefix));
				num++;
				Logger.LogTrace("System", "✅ 手动注册 Patch_Conversation_Start_Intercept -> " + DescribeMethod(item));
			}
			if (num == 0)
			{
				Logger.LogTrace("System", "❌ 未找到可用的会话启动入口，跳过 Patch_Conversation_Start_Intercept。");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ 手动注册 Patch_Conversation_Start_Intercept 失败: " + ex.Message);
		}
	}

	public static void PrefixOpenConversationMission(object[] __args)
	{
		try
		{
			if (__args == null || __args.Length < 2 || !(__args[1] is ConversationCharacterData conversationPartnerData))
			{
				return;
			}
			Hero target = conversationPartnerData.Character?.HeroObject;
			if (LordEncounterBehavior.ArmNativeSettlementMeetingForMissionStart(target))
			{
				Logger.LogTrace("Patch_Conversation_Start_Intercept", "Armed the exact native settlement conversation mission before CampaignMission.OpenConversationMission.");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Patch_Conversation_Start_Intercept", "Failed to arm native settlement conversation mission: " + ex.Message);
		}
	}

	private static void TryPatchNativeConversationMissionArm(Harmony harmony)
	{
		_nativeConversationMissionArmPatchReady = false;
		try
		{
			MethodInfo target = null;
			foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(CampaignMission)))
			{
				ParameterInfo[] parameters = method.GetParameters();
				if (method.IsStatic
					&& method.Name == "OpenConversationMission"
					&& method.ReturnType == typeof(IMission)
					&& parameters.Length == 5
					&& parameters[0].ParameterType == typeof(ConversationCharacterData)
					&& parameters[1].ParameterType == typeof(ConversationCharacterData)
					&& parameters[2].ParameterType == typeof(string)
					&& parameters[3].ParameterType == typeof(string)
					&& parameters[4].ParameterType == typeof(bool))
				{
					target = method;
					break;
				}
			}
			MethodInfo prefix = AccessTools.Method(typeof(Patch_Conversation_Start_Intercept), nameof(PrefixOpenConversationMission));
			if (target == null || prefix == null)
			{
				Logger.LogTrace("System", "Native settlement conversation mission arm patch failed closed because CampaignMission.OpenConversationMission was not found.");
				return;
			}
			harmony.Patch(target, new HarmonyMethod(prefix));
			_nativeConversationMissionArmPatchReady = true;
			Logger.LogTrace("System", "Native settlement conversation mission arm patch installed.");
		}
		catch (Exception ex)
		{
			_nativeConversationMissionArmPatchReady = false;
			Logger.LogTrace("System", "Native settlement conversation mission arm patch failed closed: " + ex.Message);
		}
	}

	public static bool Prefix(MethodBase __originalMethod, object __instance, object[] __args)
	{
		try
		{
			if (LordEncounterBehavior.HasPendingNativeEncounterAttackForExternal())
			{
				Logger.LogTrace("Patch_Conversation_Start_Intercept", "Native encounter attack is pending; suppress native " + __originalMethod?.Name + ".");
				return false;
			}
			Hero explicitPrisoner = EncounterConversationTargetResolver.TryResolveExplicitPrisonerFromArguments(__args);
			if (explicitPrisoner != null)
			{
				Logger.LogTrace("Patch_Conversation_Start_Intercept", $"Explicit prisoner conversation detected; allow native {__originalMethod?.Name}: {explicitPrisoner.Name}");
				return true;
			}
			if (ShouldAllowNativeConversationStart(__originalMethod))
			{
				return true;
			}
			Hero hero = TryResolveConversationLord(__instance, __args);
			if (hero == null)
			{
				return true;
			}
			if (LordEncounterBehavior.IsNativeSettlementRequestMeetingContext(hero))
			{
				Logger.LogTrace("Patch_Conversation_Start_Intercept", "Native hostile settlement request meeting detected; allow native " + __originalMethod?.Name + ".");
				return true;
			}
			Logger.LogTrace("Patch_Conversation_Start_Intercept", $"检测到 {__originalMethod?.Name} 原版对话启动，重定向至自定义会面菜单: {hero.Name}");
			ProactiveNpcRequestBehavior.MarkEncounterOpened(hero);
			LordEncounterBehavior.SetTarget(hero);
			if (LordEncounterBehavior.OpenEncounterMenu(hero))
			{
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("Patch_Conversation_Start_Intercept", "[ERROR] " + ex);
		}
		return true;
	}

	private static bool ShouldAllowNativeConversationStart(MethodBase originalMethod)
	{
		if (LordEncounterBehavior.IsEncounterRedirectSuspended())
		{
			Logger.LogTrace("Patch_Conversation_Start_Intercept", "Encounter redirect is suspended; allow native " + originalMethod?.Name + ".");
			return true;
		}
		if (LordEncounterBehavior.IsCustomEncounterMenuDisabledForCurrentEncounter())
		{
			Logger.LogTrace("Patch_Conversation_Start_Intercept", "Custom encounter menu is disabled for current encounter; allow native " + originalMethod?.Name + ".");
			return true;
		}
		if (LordEncounterBehavior.IsOpeningConversation)
		{
			Logger.LogTrace("Patch_Conversation_Start_Intercept", "IsOpeningConversation=true; allow native " + originalMethod?.Name + ".");
			return true;
		}
		if (Campaign.Current.CurrentMenuContext?.GameMenu.StringId == "AnimusForge_lord_encounter")
		{
			return true;
		}
		if (LordEncounterBehavior.IsNativeSettlementRequestMeetingContext())
		{
			Logger.LogTrace("Patch_Conversation_Start_Intercept", "Native hostile settlement request meeting detected; allow native " + originalMethod?.Name + ".");
			return true;
		}
		if (LordEncounterRedirectGuard.IsSuppressed())
		{
			return true;
		}
		if (MapSeaContextGuard.IsCurrentPlayerEncounterAtSea())
		{
			Logger.LogTrace("Patch_Conversation_Start_Intercept", "Sea encounter context detected; allow native " + originalMethod?.Name + ".");
			return true;
		}
		if (PlayerEncounter.Current == null)
		{
			return true;
		}
		if (PlayerEncounterCompat.HasCampaignBattleResult())
		{
			return true;
		}
		if (PlayerEncounterCompat.HasResolvedEncounterBattleContext())
		{
			return true;
		}
		PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
		if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
		{
			return true;
		}
		MapEvent currentEncounterBattle = GetCurrentEncounterBattle();
		return currentEncounterBattle != null && (currentEncounterBattle.HasWinner || currentEncounterBattle.IsFinalized);
	}

	private static Hero TryResolveConversationLord(object instance, object[] args)
	{
		Hero hero = EncounterConversationTargetResolver.TryResolveLordFromArgumentsThenEncounterLeader(instance, args);
		return IsValidLord(hero) ? hero : null;
	}

	private static bool IsValidLord(Hero hero)
	{
		PartyBase encounteredParty = null;
		try
		{
			encounteredParty = PlayerEncounter.EncounteredParty;
		}
		catch
		{
			encounteredParty = null;
		}
		if (encounteredParty != null && encounteredParty.LeaderHero != null)
		{
			return LordEncounterBehavior.IsEligibleCustomLordEncounterTarget(hero, encounteredParty);
		}
		return LordEncounterBehavior.IsEligibleCustomLordEncounterTarget(hero);
	}

	private static void TryAddDeclaredMethod(HashSet<MethodBase> methods, string methodName, int? parameterCount = null)
	{
		foreach (MethodInfo declaredMethod in AccessTools.GetDeclaredMethods(typeof(ConversationManager)))
		{
			if (!(declaredMethod.Name != methodName))
			{
				if (!parameterCount.HasValue || declaredMethod.GetParameters().Length == parameterCount.Value)
				{
					methods.Add(declaredMethod);
				}
			}
		}
	}

	private static string DescribeMethod(MethodBase method)
	{
		if (method == null)
		{
			return "null";
		}
		ParameterInfo[] parameters = method.GetParameters();
		List<string> list = new List<string>(parameters.Length);
		foreach (ParameterInfo parameterInfo in parameters)
		{
			list.Add(parameterInfo.ParameterType.Name + " " + parameterInfo.Name);
		}
		return method.DeclaringType?.Name + "." + method.Name + "(" + string.Join(", ", list) + ")";
	}

	private static MapEvent GetCurrentEncounterBattle()
	{
		return PlayerEncounterCompat.GetCurrentMapEventSafe();
	}
}
