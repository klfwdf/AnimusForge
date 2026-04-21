using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace AnimusForge;

[HarmonyPatch(typeof(ConversationManager), "OpenMapConversation")]
public static class Patch_ConversationManager_OpenMapConversation
{
	public static void ManualPatch(Harmony harmony)
	{
		try
		{
			MethodInfo method = AccessTools.Method(typeof(Patch_ConversationManager_OpenMapConversation), "Prefix");
			int num = 0;
			List<MethodInfo> declaredMethods = AccessTools.GetDeclaredMethods(typeof(ConversationManager));
			foreach (MethodInfo item in declaredMethods)
			{
				if (!(item?.Name != "OpenMapConversation"))
				{
					harmony.Patch(item, new HarmonyMethod(method));
					num++;
				}
			}
			if (num > 0)
			{
				Logger.LogTrace("System", $"✅ 手动注册 Patch_ConversationManager_OpenMapConversation 成功。(count={num})");
			}
			else
			{
				Logger.LogTrace("System", "❌ 未找到 OpenMapConversation 目标方法，跳过手动注册。");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ 手动注册 Patch_ConversationManager_OpenMapConversation 失败: " + ex.Message);
		}
	}

	public static bool Prefix(MethodBase __originalMethod, object[] __args)
	{
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Invalid comparison between Unknown and I4
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Logger.LogTrace("Conversation_Intercept", ">>> OpenMapConversation Prefix 正在执行 (" + __originalMethod?.Name + ") <<<");
			if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("Conversation_Intercept", "Encounter redirect is suspended; allow native OpenMapConversation.");
				return true;
			}
			if (LordEncounterBehavior.IsCustomEncounterMenuDisabledForCurrentEncounter())
			{
				Logger.LogTrace("Conversation_Intercept", "Custom encounter menu is disabled for current encounter; allow native OpenMapConversation.");
				return true;
			}
			if (LordEncounterBehavior.IsOpeningConversation)
			{
				Logger.LogTrace("Conversation_Intercept", "检测到 IsOpeningConversation=true，放行对话。");
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
			if (PlayerEncounter.Current != null)
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty != null)
				{
					val = encounteredParty.LeaderHero;
					if (val != null)
					{
						Logger.LogTrace("Conversation_Intercept", $"遭遇对象: {val.Name}, IsLord={val.IsLord}, IsMainHero={val == Hero.MainHero}");
					}
				}
				else
				{
					Logger.LogTrace("Conversation_Intercept", "PlayerEncounter.EncounteredParty 为空。");
				}
			}
			else
			{
				Logger.LogTrace("Conversation_Intercept", "PlayerEncounter.Current 为空。");
			}
			if (val == null && __args != null)
			{
				foreach (object obj in __args)
				{
					CharacterObject val2 = null;
					CharacterObject val3 = (CharacterObject)((obj is CharacterObject) ? obj : null);
					if (val3 != null)
					{
						val2 = val3;
					}
					else
					{
						Hero val4 = (Hero)((obj is Hero) ? obj : null);
						if (val4 != null)
						{
							val2 = val4.CharacterObject;
						}
						else if (obj is ConversationCharacterData val5)
						{
							val2 = val5.Character;
						}
					}
					if (val2 != null && ((BasicCharacterObject)val2).IsHero && val2.HeroObject != Hero.MainHero && val2.HeroObject.IsLord)
					{
						val = val2.HeroObject;
						break;
					}
				}
			}
			if (val != null && val != Hero.MainHero && val.IsLord)
			{
				Logger.LogTrace("Conversation_Intercept", $"检测到 OpenMapConversation 调用，目标领主: {val.Name}");
				LordEncounterBehavior.SetTarget(val);
				LordEncounterBehavior.OpenEncounterMenu(val);
				return false;
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("Conversation_Intercept", "[ERROR] " + ex.ToString());
		}
		return true;
	}
}
