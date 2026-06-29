using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

[HarmonyPatch(typeof(ConversationManager), "SetupAndStartMapConversation")]
public static class Patch_ConversationManager_SetupAndStartMapConversation
{
	public static void ManualPatch(Harmony harmony)
	{
		try
		{
			List<MethodInfo> declaredMethods = AccessTools.GetDeclaredMethods(typeof(ConversationManager));
			MethodInfo methodInfo = null;
			foreach (MethodInfo item in declaredMethods)
			{
				if (!(item.Name != "SetupAndStartMapConversation"))
				{
					ParameterInfo[] parameters = item.GetParameters();
					if (parameters.Length == 3)
					{
						methodInfo = item;
						break;
					}
				}
			}
			if (methodInfo == null)
			{
				Logger.LogTrace("System", "❌ 未找到 SetupAndStartMapConversation(3参数) 目标方法，跳过手动注册。");
				return;
			}
			MethodInfo method = AccessTools.Method(typeof(Patch_ConversationManager_SetupAndStartMapConversation), "Prefix");
			harmony.Patch(methodInfo, new HarmonyMethod(method));
			Logger.LogTrace("System", "✅ 手动注册 Patch_ConversationManager_SetupAndStartMapConversation 成功。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ 手动注册 Patch_ConversationManager_SetupAndStartMapConversation 失败: " + ex.Message);
		}
	}

	public static bool Prefix(object __instance, object[] __args)
	{
		try
		{
			Logger.LogTrace("Conversation_Intercept", ">>> SetupAndStartMapConversation Prefix 正在执行 <<<");
			if (LordEncounterBehavior.HasPendingNativeEncounterAttackForExternal())
			{
				Logger.LogTrace("Conversation_Intercept", "Native encounter attack is pending; suppress SetupAndStartMapConversation.");
				return false;
			}
			if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("Conversation_Intercept", "Encounter redirect is suspended; allow native SetupAndStartMapConversation.");
				return true;
			}
			if (LordEncounterBehavior.IsNativeSettlementRequestMeetingContext())
			{
				Logger.LogTrace("Conversation_Intercept", "Native hostile settlement request meeting detected; allow native SetupAndStartMapConversation.");
				return true;
			}
			if (LordEncounterBehavior.IsCustomEncounterMenuDisabledForCurrentEncounter())
			{
				Logger.LogTrace("Conversation_Intercept", "Custom encounter menu is disabled for current encounter; allow native SetupAndStartMapConversation.");
				return true;
			}
			if (LordEncounterBehavior.IsOpeningConversation)
			{
				return true;
			}
			if (PlayerEncounter.Current == null)
			{
				return true;
			}
			if (LordEncounterRedirectGuard.IsSuppressed())
			{
				return true;
			}
			if (MapSeaContextGuard.IsCurrentPlayerEncounterAtSea())
			{
				Logger.LogTrace("Conversation_Intercept", "Sea encounter context detected; allow native SetupAndStartMapConversation.");
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
			Hero hero = EncounterConversationTargetResolver.TryResolveLordFromArgumentsThenEncounterLeader(__instance, __args);
			try
			{
				hero = hero ?? PlayerEncounter.EncounteredParty?.LeaderHero;
			}
			catch
			{
				hero = null;
			}
			if (__args != null)
			{
				foreach (object obj in __args)
				{
					if (hero != null)
					{
						break;
					}
					if (obj == null)
					{
						continue;
					}
					hero = AccessTools.Property(obj.GetType(), "LeaderHero")?.GetValue(obj) as Hero;
					if (hero != null)
					{
						break;
					}
					object obj2 = AccessTools.Property(obj.GetType(), "Party")?.GetValue(obj);
					if (obj2 != null)
					{
						hero = AccessTools.Property(obj2.GetType(), "LeaderHero")?.GetValue(obj2) as Hero;
						if (hero != null)
						{
							break;
						}
					}
					object obj3 = AccessTools.Property(obj.GetType(), "MobileParty")?.GetValue(obj);
					if (obj3 != null)
					{
						hero = AccessTools.Property(obj3.GetType(), "LeaderHero")?.GetValue(obj3) as Hero;
						if (hero != null)
						{
							break;
						}
					}
				}
			}
			if (hero != null)
			{
				Logger.LogTrace("Conversation_Intercept", $"SetupAndStartMapConversation 遭遇对象: {hero.Name}, IsLord={hero.IsLord}, IsMainHero={hero == Hero.MainHero}");
				if (LordEncounterBehavior.IsNativeSettlementRequestMeetingContext(hero))
				{
					Logger.LogTrace("Conversation_Intercept", $"SetupAndStartMapConversation 命中敌对定居点原版会面，放行原版对话: {hero.Name}");
					return true;
				}
				PartyBase encounterParty = null;
				try
				{
					encounterParty = PlayerEncounter.EncounteredParty;
				}
				catch
				{
					encounterParty = null;
				}
				if (LordEncounterBehavior.IsEligibleCustomLordEncounterTarget(hero, encounterParty))
				{
					ProactiveNpcRequestBehavior.MarkEncounterOpened(hero);
					LordEncounterBehavior.SetTarget(hero);
					if (LordEncounterBehavior.OpenEncounterMenu(hero))
					{
						Logger.LogTrace("Conversation_Intercept", $"SetupAndStartMapConversation 已重定向至自定义会面菜单: {hero.Name}");
						return false;
					}
				}
				else if (hero != Hero.MainHero && hero.IsLord)
				{
					Logger.LogTrace("Conversation_Intercept", $"SetupAndStartMapConversation 遭遇对象不是可接管的王国贵族遭遇，放行原版对话: {hero.Name}");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("Conversation_Intercept", "[ERROR] " + ex.ToString());
		}
		return true;
	}
}
