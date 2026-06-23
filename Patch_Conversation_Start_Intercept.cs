using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

public static class Patch_Conversation_Start_Intercept
{
	public static void ManualPatch(Harmony harmony)
	{
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

	public static bool Prefix(MethodBase __originalMethod, object __instance, object[] __args)
	{
		try
		{
			if (LordEncounterBehavior.HasPendingNativeEncounterAttackForExternal())
			{
				Logger.LogTrace("Patch_Conversation_Start_Intercept", "Native encounter attack is pending; suppress native " + __originalMethod?.Name + ".");
				return false;
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
		if (LordEncounterRedirectGuard.IsSuppressed())
		{
			return true;
		}
		if (PlayerEncounter.Current == null)
		{
			return true;
		}
		if (MobileParty.MainParty?.MapEvent != null || MapEvent.PlayerMapEvent != null)
		{
			return true;
		}
		if (PlayerEncounterCompat.HasCampaignBattleResult())
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
		Hero hero = PlayerEncounter.EncounteredParty?.LeaderHero;
		if (IsValidLord(hero))
		{
			return hero;
		}
		if (args != null)
		{
			foreach (object arg in args)
			{
				hero = TryResolveHeroFromObject(arg);
				if (IsValidLord(hero))
				{
					return hero;
				}
			}
		}
		hero = TryResolveHeroFromObject(instance);
		if (IsValidLord(hero))
		{
			return hero;
		}
		return null;
	}

	private static Hero TryResolveHeroFromObject(object value)
	{
		if (value == null)
		{
			return null;
		}
		if (value is Hero hero)
		{
			return hero;
		}
		if (value is CharacterObject characterObject)
		{
			return characterObject.HeroObject;
		}
		if (value is ConversationCharacterData conversationCharacterData)
		{
			return conversationCharacterData.Character?.HeroObject;
		}
		object obj = AccessTools.Property(value.GetType(), "LeaderHero")?.GetValue(value);
		if (obj is Hero hero2)
		{
			return hero2;
		}
		object obj2 = AccessTools.Property(value.GetType(), "HeroObject")?.GetValue(value);
		if (obj2 is Hero hero3)
		{
			return hero3;
		}
		object obj3 = AccessTools.Property(value.GetType(), "Character")?.GetValue(value);
		if (obj3 is CharacterObject characterObject2)
		{
			return characterObject2.HeroObject;
		}
		object obj4 = AccessTools.Property(value.GetType(), "Party")?.GetValue(value);
		Hero hero4 = TryResolveHeroFromObject(obj4);
		if (hero4 != null)
		{
			return hero4;
		}
		object obj5 = AccessTools.Property(value.GetType(), "MobileParty")?.GetValue(value);
		Hero hero5 = TryResolveHeroFromObject(obj5);
		if (hero5 != null)
		{
			return hero5;
		}
		return null;
	}

	private static bool IsValidLord(Hero hero)
	{
		return hero != null && hero != Hero.MainHero && hero.IsLord;
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
