using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;

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
		try
		{
			Logger.LogTrace("Conversation_Intercept", ">>> OpenMapConversation Prefix 正在执行 (" + __originalMethod?.Name + ") <<<");
			if (LordEncounterBehavior.HasPendingNativeEncounterAttackForExternal())
			{
				Logger.LogTrace("Conversation_Intercept", "Native encounter attack is pending; suppress OpenMapConversation and skip custom encounter menu redirect.");
				return false;
			}
			if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("Conversation_Intercept", "Encounter redirect is suspended; allow native OpenMapConversation.");
				return true;
			}
			if (LordEncounterBehavior.IsNativeSettlementRequestMeetingContext())
			{
				Logger.LogTrace("Conversation_Intercept", "Native hostile settlement request meeting detected; allow native OpenMapConversation.");
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
			if (MapSeaContextGuard.IsCurrentPlayerEncounterAtSea())
			{
				Logger.LogTrace("Conversation_Intercept", "Sea encounter context detected; allow native OpenMapConversation.");
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
			Hero hero = EncounterConversationTargetResolver.TryResolveLordFromArgumentsThenEncounterLeader(null, __args);
			if (hero == null && PlayerEncounter.Current != null)
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
			if (hero != null && LordEncounterBehavior.IsNativeSettlementRequestMeetingContext(hero))
			{
				Logger.LogTrace("Conversation_Intercept", $"OpenMapConversation 命中敌对定居点原版会面，放行原版对话: {hero.Name}");
				return true;
			}
			PartyBase encounterPartyForEligibility = null;
			try
			{
				encounterPartyForEligibility = PlayerEncounter.EncounteredParty;
			}
			catch
			{
				encounterPartyForEligibility = null;
			}
			if (hero != null && LordEncounterBehavior.IsEligibleCustomLordEncounterTarget(hero, encounterPartyForEligibility))
			{
				Logger.LogTrace("Conversation_Intercept", $"检测到 OpenMapConversation 原版对话调用，重定向至自定义会面菜单: {hero.Name}");
				ProactiveNpcRequestBehavior.MarkEncounterOpened(hero);
				LordEncounterBehavior.SetTarget(hero);
				if (LordEncounterBehavior.OpenEncounterMenu(hero))
				{
					return false;
				}
				return true;
			}
			if (hero != null && hero != Hero.MainHero && hero.IsLord)
			{
				Logger.LogTrace("Conversation_Intercept", $"OpenMapConversation 遭遇对象不是可接管的王国贵族遭遇，放行原版对话: {hero.Name}");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("Conversation_Intercept", "[ERROR] " + ex.ToString());
		}
		return true;
	}
}
