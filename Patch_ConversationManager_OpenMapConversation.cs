using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;

namespace Voxforge;

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
			if (MobileParty.MainParty?.MapEvent != null)
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
			if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
			{
				return true;
			}
			Hero hero = null;
			if (PlayerEncounter.Current != null)
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty != null)
				{
					hero = encounteredParty.LeaderHero;
					if (hero != null)
					{
						Logger.LogTrace("Conversation_Intercept", $"遭遇对象: {hero.Name}, IsLord={hero.IsLord}, IsMainHero={hero == Hero.MainHero}");
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
			if (hero == null && __args != null)
			{
				foreach (object obj in __args)
				{
					CharacterObject characterObject = null;
					if (obj is CharacterObject characterObject2)
					{
						characterObject = characterObject2;
					}
					else if (obj is Hero hero2)
					{
						characterObject = hero2.CharacterObject;
					}
					else if (obj is ConversationCharacterData conversationCharacterData)
					{
						characterObject = conversationCharacterData.Character;
					}
					if (characterObject != null && characterObject.IsHero && characterObject.HeroObject != Hero.MainHero && characterObject.HeroObject.IsLord)
					{
						hero = characterObject.HeroObject;
						break;
					}
				}
			}
			if (hero != null && hero != Hero.MainHero && hero.IsLord)
			{
				Logger.LogTrace("Conversation_Intercept", $"检测到 OpenMapConversation 调用，目标领主: {hero.Name}");
				LordEncounterBehavior.SetTarget(hero);
				LordEncounterBehavior.OpenEncounterMenu(hero);
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
