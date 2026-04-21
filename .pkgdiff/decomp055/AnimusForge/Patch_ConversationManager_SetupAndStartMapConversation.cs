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
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Invalid comparison between Unknown and I4
		try
		{
			Logger.LogTrace("Conversation_Intercept", ">>> SetupAndStartMapConversation Prefix 正在执行 <<<");
			if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("Conversation_Intercept", "Encounter redirect is suspended; allow native SetupAndStartMapConversation.");
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
			MobileParty mainParty = MobileParty.MainParty;
			if (((mainParty != null) ? mainParty.MapEvent : null) != null)
			{
				return true;
			}
			if (PlayerEncounter.CampaignBattleResult != null)
			{
				return true;
			}
			if (PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null)
			{
				return true;
			}
			PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
			if ((int)encounterState != 0 && (int)encounterState != 1)
			{
				return true;
			}
			Hero val = null;
			if (__args != null)
			{
				foreach (object obj in __args)
				{
					if (obj == null)
					{
						continue;
					}
					object obj2 = AccessTools.Property(obj.GetType(), "LeaderHero")?.GetValue(obj);
					val = (Hero)((obj2 is Hero) ? obj2 : null);
					if (val != null)
					{
						break;
					}
					object obj3 = AccessTools.Property(obj.GetType(), "Party")?.GetValue(obj);
					if (obj3 != null)
					{
						object obj4 = AccessTools.Property(obj3.GetType(), "LeaderHero")?.GetValue(obj3);
						val = (Hero)((obj4 is Hero) ? obj4 : null);
						if (val != null)
						{
							break;
						}
					}
					object obj5 = AccessTools.Property(obj.GetType(), "MobileParty")?.GetValue(obj);
					if (obj5 != null)
					{
						object obj6 = AccessTools.Property(obj5.GetType(), "LeaderHero")?.GetValue(obj5);
						val = (Hero)((obj6 is Hero) ? obj6 : null);
						if (val != null)
						{
							break;
						}
					}
				}
			}
			if (val != null)
			{
				Logger.LogTrace("Conversation_Intercept", $"SetupAndStartMapConversation 遭遇对象: {val.Name}, IsLord={val.IsLord}, IsMainHero={val == Hero.MainHero}");
				if (val != Hero.MainHero && val.IsLord)
				{
					LordEncounterBehavior.SetTarget(val);
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
