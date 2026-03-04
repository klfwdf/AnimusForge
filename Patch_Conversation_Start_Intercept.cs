using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;

namespace AnimusForge;

[HarmonyPatch(typeof(ConversationManager), "StartConversation")]
public static class Patch_Conversation_Start_Intercept
{
	public static bool Prefix(object __instance, object[] __args)
	{
		try
		{
			if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("Patch_Conversation_Start_Intercept", "Encounter redirect is suspended; allow native StartConversation.");
				return true;
			}
			if (LordEncounterBehavior.IsCustomEncounterMenuDisabledForCurrentEncounter())
			{
				Logger.LogTrace("Patch_Conversation_Start_Intercept", "Custom encounter menu is disabled for current encounter; allow native StartConversation.");
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
			if (PlayerEncounter.CampaignBattleResult != null)
			{
				return true;
			}
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
				if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
				{
					return true;
				}
				if (PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null)
				{
					return true;
				}
				MapEvent mapEvent = PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle;
				if (mapEvent != null && (mapEvent.HasWinner || mapEvent.IsFinalized))
				{
					return true;
				}
				Hero hero = PlayerEncounter.EncounteredParty?.LeaderHero;
				if (hero != null && hero != Hero.MainHero && hero.IsLord)
				{
					LordEncounterBehavior.OpenEncounterMenu(hero);
					return false;
				}
			}
			CharacterObject characterObject = null;
			if (__args != null)
			{
				foreach (object obj in __args)
				{
					CharacterObject characterObject2 = null;
					if (obj is CharacterObject characterObject3)
					{
						characterObject2 = characterObject3;
					}
					else if (obj is Hero hero2)
					{
						characterObject2 = hero2.CharacterObject;
					}
					else if (obj is ConversationCharacterData conversationCharacterData)
					{
						characterObject2 = conversationCharacterData.Character;
					}
					if (characterObject2 != null && characterObject2.IsHero && characterObject2.HeroObject != Hero.MainHero && characterObject2.HeroObject.IsLord)
					{
						characterObject = characterObject2;
						break;
					}
				}
			}
			if (characterObject != null && PlayerEncounter.Current != null)
			{
				PlayerEncounterState encounterState2 = PlayerEncounter.Current.EncounterState;
				if (encounterState2 != PlayerEncounterState.Begin && encounterState2 != PlayerEncounterState.Wait)
				{
					return true;
				}
				if (PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null)
				{
					return true;
				}
				MapEvent mapEvent2 = PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle;
				if (mapEvent2 != null && (mapEvent2.HasWinner || mapEvent2.IsFinalized))
				{
					return true;
				}
				LordEncounterBehavior.OpenEncounterMenu(characterObject.HeroObject);
				return false;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Patch_Conversation_Start_Intercept", "[ERROR] " + ex.ToString());
		}
		return true;
	}
}
