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
				Logger.LogTrace("System", $"✅ Manual registration of Patch_ConversationManager_OpenMapConversation succeeded.(count={num})");
			}
			else
			{
				Logger.LogTrace("System", "❌ OpenMapConversation target method not found; skipping manual registration.");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ Manual registration of Patch_ConversationManager_OpenMapConversation failed: " + ex.Message);
		}
	}

	public static bool Prefix(MethodBase __originalMethod, object[] __args)
	{
		try
		{
			Logger.LogTrace("Conversation_Intercept", ">>> OpenMapConversation Prefix executing (" + __originalMethod?.Name + ") <<<");
			if (PlayerCaptivityConversation.IsOpening) return true;
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
			if (LordEncounterBehavior.TryGetCustomEncounterMenuDisableReason(out string disableReason))
			{
				Hero disabledTarget = EncounterConversationTargetResolver.TryResolveLordFromArgumentsThenEncounterLeader(null, __args);
				LordEncounterBehavior.LogEncounterDiagnostic("OpenMapConversation", disableReason, null, disabledTarget);
				Logger.LogTrace("Conversation_Intercept", "Custom encounter menu is disabled for current encounter; allow native OpenMapConversation.");
				return true;
			}
			if (LordEncounterBehavior.IsOpeningConversation)
			{
				Logger.LogTrace("Conversation_Intercept", "Detected IsOpeningConversation=true; allowing conversation.");
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
			Hero explicitPrisoner = EncounterConversationTargetResolver.TryResolveExplicitPrisonerFromArguments(__args);
			if (explicitPrisoner != null)
			{
				Logger.LogTrace("Conversation_Intercept", $"Explicit prisoner conversation detected; allow native OpenMapConversation: {explicitPrisoner.Name}");
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
						Logger.LogTrace("Conversation_Intercept", $"encounter target: {hero.Name}, IsLord={hero.IsLord}, IsMainHero={hero == Hero.MainHero}");
					}
				}
				else
				{
					Logger.LogTrace("Conversation_Intercept", "PlayerEncounter.EncounteredParty is null.");
				}
			}
			else
			{
				Logger.LogTrace("Conversation_Intercept", "PlayerEncounter.Current is null.");
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
				Logger.LogTrace("Conversation_Intercept", $"OpenMapConversation Matched vanilla meeting in a hostile settlement; allowing vanilla conversation: {hero.Name}");
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
				Logger.LogTrace("Conversation_Intercept", $"Detected vanilla OpenMapConversation call; redirecting to the custom meeting menu: {hero.Name}");
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
				Logger.LogTrace("Conversation_Intercept", $"OpenMapConversation Encounter target is not an eligible kingdom noble encounter; allowing vanilla conversation: {hero.Name}");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("Conversation_Intercept", "[ERROR] " + ex.ToString());
		}
		return true;
	}
}
