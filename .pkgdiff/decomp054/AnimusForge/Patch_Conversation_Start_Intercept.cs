using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace AnimusForge;

[HarmonyPatch(typeof(ConversationManager), "StartConversation")]
public static class Patch_Conversation_Start_Intercept
{
	public static bool Prefix(object __instance, object[] __args)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Invalid comparison between Unknown and I4
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Invalid comparison between Unknown and I4
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
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
			MenuContext currentMenuContext = Campaign.Current.CurrentMenuContext;
			if (((currentMenuContext != null) ? currentMenuContext.GameMenu.StringId : null) == "AnimusForge_lord_encounter")
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
				if ((int)encounterState != 0 && (int)encounterState != 1)
				{
					return true;
				}
				if (PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null)
				{
					return true;
				}
				MapEvent val = PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle;
				if (val != null && (val.HasWinner || val.IsFinalized))
				{
					return true;
				}
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				Hero val2 = ((encounteredParty != null) ? encounteredParty.LeaderHero : null);
				if (val2 != null && val2 != Hero.MainHero && val2.IsLord)
				{
					LordEncounterBehavior.OpenEncounterMenu(val2);
					return false;
				}
			}
			CharacterObject val3 = null;
			if (__args != null)
			{
				foreach (object obj in __args)
				{
					CharacterObject val4 = null;
					CharacterObject val5 = (CharacterObject)((obj is CharacterObject) ? obj : null);
					if (val5 != null)
					{
						val4 = val5;
					}
					else
					{
						Hero val6 = (Hero)((obj is Hero) ? obj : null);
						if (val6 != null)
						{
							val4 = val6.CharacterObject;
						}
						else if (obj is ConversationCharacterData val7)
						{
							val4 = val7.Character;
						}
					}
					if (val4 != null && ((BasicCharacterObject)val4).IsHero && val4.HeroObject != Hero.MainHero && val4.HeroObject.IsLord)
					{
						val3 = val4;
						break;
					}
				}
			}
			if (val3 != null && PlayerEncounter.Current != null)
			{
				PlayerEncounterState encounterState2 = PlayerEncounter.Current.EncounterState;
				if ((int)encounterState2 != 0 && (int)encounterState2 != 1)
				{
					return true;
				}
				if (PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null)
				{
					return true;
				}
				MapEvent val8 = PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle;
				if (val8 != null && (val8.HasWinner || val8.IsFinalized))
				{
					return true;
				}
				LordEncounterBehavior.OpenEncounterMenu(val3.HeroObject);
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
