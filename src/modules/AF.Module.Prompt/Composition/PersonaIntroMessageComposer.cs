using System;using System.Text;
namespace AnimusForge;
internal sealed class SystemNpcIntroSnapshot {
 internal long Generation;
 internal string clanName="";
 internal string factionDisplay="";
 internal string heroName="";
 internal string identityTitle="";
 internal string reputationText="";
 internal string equipmentText="";
 internal string ageText="";
 internal string cultureText="";
 internal string description="";
 internal string personaContext="";
 internal string nobleEtiquettePrompt="";
 internal string clanRole="";
 internal string relationshipLine="";
 internal int clanTier;
}

internal sealed class SceneNpcIntroSnapshot {
 internal long Generation;
 internal string name="";
 internal string givenName="";
 internal string identity="";
 internal string description="";
 internal string personaContext="";
 internal string faction="";
 internal string clan="";
 internal string clanRole="";
 internal string reputation="";
 internal string culture="";
 internal string age="";
 internal string equipment="";
 internal string inventorySummary="";
 internal string currentDateText="";
 internal string pregnancySelfKnowledge="";
 internal string heroJoinPartyRuntimeFact="";
 internal string partyRepresentativePrompt="";
 internal string ceremonyRole="";
 internal string npcCurrentMountLine="";
 internal string npcTroopsLine="";
 internal string npcPrisonersLine="";
 internal string nobleEtiquettePrompt="";
 internal string sceneLocationLine="";
 internal string settlementHeroNpcLine="";
 internal string settlementFlavorLine="";
 internal string settlementRulerPresenceLine="";
 internal string playerIntroLine="";
 internal string prisonerContextLine="";
 internal string playerCommandRelationshipLine="";
 internal string nearbyPresentNpcLine="";
 internal string inventoryHeader="";
 internal bool hasHero;
 internal bool isFemale;
 internal bool hideReputation;
 internal bool heroInPlayerParty;
 internal bool hasNonHeroParty;
 internal bool includeInventorySummary;
 internal bool partyTransferTopicSelected;
 internal int clanTier;
}

internal sealed class ScenePlayerIntroSnapshot {
 internal long Generation;
 internal string culture="";
 internal string age="";
 internal string genderText="";
 internal string equipment="";
 internal string equipmentValueInline="";
 internal string identitySentence="";
 internal string clanName="";
 internal string playerPublicName="";
 internal string reputation="";
 internal string playerCurrentMountLine="";
 internal string companionRole="";
 internal string vassalageRelationLine="";
 internal string inlineState="";
 internal string factionWarLine="";
 internal string crimeRatingLine="";
 internal string playerTroopsLine="";
 internal string playerPrisonersLine="";
 internal string townPartyStayHint="";
 internal bool knowsPlayerIdentity;
 internal bool isClanLeader;
 internal bool isCompanion;
 internal bool includePlayerPartyRoster;
 internal int clanTier;
}
internal static class PersonaIntroMessageComposer {
 internal static string ComposeSystemNpc(SystemNpcIntroSnapshot s) {
 if(s==null)return "";
 string clanName=s.clanName;
 string factionDisplay=s.factionDisplay;
 string heroName=s.heroName;
 string identityTitle=s.identityTitle;
 string reputationText=s.reputationText;
 string equipmentText=s.equipmentText;
 string ageText=s.ageText;
 string cultureText=s.cultureText;
 string description=s.description;
 string personaContext=s.personaContext;
 string nobleEtiquettePrompt=s.nobleEtiquettePrompt;
 string clanRole=s.clanRole;

		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(PromptComposerTexts.Get("persona_system_npc_prefix"))
			.Append(factionDisplay)
			.Append(PromptComposerTexts.Get("persona_system_npc_of"))
			.Append(clanName)
			.Append(PromptComposerTexts.Get("persona_system_npc_of"))
			.Append(heroName)
			.Append(PromptComposerTexts.Get("persona_system_npc_clan_role"))
			.Append(clanRole)
			.Append(PromptComposerTexts.Get("persona_system_npc_identity"))
			.Append(identityTitle)
			.Append(PromptComposerTexts.Get("persona_system_npc_reputation"))
			.Append(reputationText)
			.Append(PromptComposerTexts.Get("persona_system_npc_equipment"))
			.Append(equipmentText)
			.Append(PromptComposerTexts.Get("persona_system_npc_personality"))
			.Append(description);
		if (!string.IsNullOrWhiteSpace(personaContext)) stringBuilder.Append(PromptComposerTexts.Get("residual_period")).Append(personaContext);
		string npcPlayerRelationshipLine = s.relationshipLine;
		if (!string.IsNullOrWhiteSpace(npcPlayerRelationshipLine))
		{
			stringBuilder.Append(PromptComposerTexts.Get("residual_period")).Append(npcPlayerRelationshipLine);
		}
		if (!string.IsNullOrWhiteSpace(nobleEtiquettePrompt))
		{
			stringBuilder.Append(PromptComposerTexts.Get("residual_period")).Append(nobleEtiquettePrompt).Append(PromptComposerTexts.Get("persona_system_npc_age"));
		}
		else
		{
			stringBuilder.Append(PromptComposerTexts.Get("persona_age_intro"));
		}
		stringBuilder
			.Append(ageText)
			.Append(PromptComposerTexts.Get("persona_system_npc_culture"))
			.Append(cultureText)
			.Append(PromptComposerTexts.Get("residual_period"));
		return stringBuilder.ToString().Trim();
}

 internal static string ComposeSceneNpc(SceneNpcIntroSnapshot s) {
 if(s==null)return "";
 string name=s.name;
 string givenName=s.givenName;
 string identity=s.identity;
 string description=s.description;
 string personaContext=s.personaContext;
 string faction=s.faction;
 string clan=s.clan;
 string clanRole=s.clanRole;
 string reputation=s.reputation;
 string culture=s.culture;
 string age=s.age;
 string equipment=s.equipment;
 string inventorySummary=s.inventorySummary;
 string currentDateText=s.currentDateText;

bool hideReputation=s.hideReputation,includeInventorySummary=s.includeInventorySummary,partyTransferTopicSelected=s.partyTransferTopicSelected;
		StringBuilder stringBuilder = new StringBuilder();
		if (s.hasHero)
		{
			string pregnancySelfKnowledge = s.pregnancySelfKnowledge;
			string clanRoleText = (PromptComposerTexts.IsLabel(clanRole, "persona_scene_npc_clan_leader")) ? PromptComposerTexts.Get("persona_scene_npc_clan_leader") : (s.isFemale ? PromptComposerTexts.Get("persona_scene_npc_female_member") : PromptComposerTexts.Get("persona_scene_npc_male_member"));
			stringBuilder.Append(PromptComposerTexts.Get("persona_scene_npc_name"))
				.Append(name)
				.Append(PromptComposerTexts.Get("persona_scene_npc_is"))
				.Append(faction)
				.Append(PromptComposerTexts.Get("persona_system_npc_of"))
				.Append(clan)
				.Append(PromptComposerTexts.Get("persona_system_npc_of"))
				.Append(clanRoleText)
				.Append(PromptComposerTexts.Get("persona_system_npc_identity"))
				.Append(identity);
			if (!hideReputation && !string.IsNullOrWhiteSpace(reputation))
			{
				stringBuilder.Append(PromptComposerTexts.Get("persona_system_npc_reputation")).Append(reputation);
			}
			string heroJoinPartyRuntimeFact = s.heroJoinPartyRuntimeFact;
			if (!string.IsNullOrWhiteSpace(heroJoinPartyRuntimeFact))
			{
				stringBuilder.Append(PromptComposerTexts.Get("residual_period")).Append(heroJoinPartyRuntimeFact.Trim().TrimEnd('。'));
			}
			if (!string.IsNullOrWhiteSpace(pregnancySelfKnowledge))
			{
				stringBuilder.Append(PromptComposerTexts.Get("residual_period")).Append(pregnancySelfKnowledge);
			}
			stringBuilder.Append(PromptComposerTexts.Get("residual_period"));
		}
		else
		{
			stringBuilder.Append(PromptComposerTexts.Get("persona_scene_nonhero_prefix"))
				.Append(name);
			if (!string.IsNullOrWhiteSpace(givenName) && !string.Equals(givenName, name, StringComparison.Ordinal))
			{
				stringBuilder.Append(PromptComposerTexts.Get("persona_scene_nonhero_given_name")).Append(givenName);
			}
			stringBuilder.Append(PromptComposerTexts.Get("persona_system_npc_identity"))
				.Append(identity);
			if (!hideReputation && !string.IsNullOrWhiteSpace(reputation))
			{
				stringBuilder.Append(PromptComposerTexts.Get("persona_system_npc_reputation")).Append(reputation);
			}
			stringBuilder.Append(PromptComposerTexts.Get("residual_period"));
			string partyRepresentativePrompt = s.partyRepresentativePrompt;
			if (!string.IsNullOrWhiteSpace(partyRepresentativePrompt))
			{
stringBuilder.Append(partyRepresentativePrompt);
				}
			}
			string ceremonyRole = s.ceremonyRole;
			if (!string.IsNullOrWhiteSpace(ceremonyRole))
			{
				stringBuilder.Append(ceremonyRole);
			}
			stringBuilder.AppendLine()
				.Append(PromptComposerTexts.Get("persona_scene_npc_equipment"))
			.Append(equipment)
			.Append(PromptComposerTexts.Get("residual_period"));
		string npcCurrentMountLine = s.npcCurrentMountLine;
		if (!string.IsNullOrWhiteSpace(npcCurrentMountLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(npcCurrentMountLine);
		}
		if (s.hasHero)
		{
			try
			{
				if (!s.heroInPlayerParty)
				{
					bool hasPartyRestrictionContext = false;
					string npcTroopsLine = s.npcTroopsLine;
					string npcPrisonersLine = s.npcPrisonersLine;
					if (!string.IsNullOrWhiteSpace(npcTroopsLine))
					{
						stringBuilder.Append(npcTroopsLine).Append(PromptComposerTexts.Get("residual_period"));
						hasPartyRestrictionContext = true;
					}
					if (!string.IsNullOrWhiteSpace(npcPrisonersLine))
					{
						stringBuilder.Append(npcPrisonersLine).Append(PromptComposerTexts.Get("residual_period"));
						hasPartyRestrictionContext = true;
					}
					if (hasPartyRestrictionContext && !partyTransferTopicSelected)
					{
						stringBuilder.Append(PromptComposerTexts.Get("persona_scene_npc_party_transfer_note"));
					}
				}
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				bool nonHeroParty = s.hasNonHeroParty;
				if (nonHeroParty)
				{
					string npcTroopsLine = s.npcTroopsLine;
					if (!string.IsNullOrWhiteSpace(npcTroopsLine))
					{
						stringBuilder.Append(npcTroopsLine).Append(PromptComposerTexts.Get("residual_period"));
					}
					string npcPrisonersLine = s.npcPrisonersLine;
					if (!string.IsNullOrWhiteSpace(npcPrisonersLine) && !string.Equals(npcPrisonersLine, PromptComposerTexts.Get("persona_scene_nonhero_prisoner_none"), StringComparison.Ordinal))
					{
						stringBuilder.Append(npcPrisonersLine).Append(PromptComposerTexts.Get("residual_period"));
					}
				}
			}
			catch
			{
			}
		}
		if (s.hasHero)
		{
			string nobleEtiquettePrompt = s.nobleEtiquettePrompt;
			stringBuilder.AppendLine()
				.Append(PromptComposerTexts.Get("persona_scene_npc_personality"))
				.Append(description);
			if (!string.IsNullOrWhiteSpace(personaContext)) stringBuilder.Append(PromptComposerTexts.Get("residual_period")).Append(personaContext);
			if (!string.IsNullOrWhiteSpace(nobleEtiquettePrompt))
			{
				stringBuilder.Append(PromptComposerTexts.Get("residual_period")).Append(nobleEtiquettePrompt);
			}
			else
			{
				stringBuilder.Append(PromptComposerTexts.Get("residual_period"));
			}
		}
		stringBuilder.AppendLine()
			.Append(PromptComposerTexts.Get("persona_system_npc_age"))
			.Append(age)
			.Append(PromptComposerTexts.Get("persona_system_npc_culture"))
			.Append(culture)
			.Append(PromptComposerTexts.Get("residual_period"));
		if (!string.IsNullOrWhiteSpace(currentDateText))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(PromptComposerTexts.Get("persona_scene_npc_current_time")).Append(currentDateText).Append(PromptComposerTexts.Get("residual_period"));
		}
		string sceneLocationLine = s.sceneLocationLine;
		if (!string.IsNullOrWhiteSpace(sceneLocationLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(sceneLocationLine);
		}
		string settlementHeroNpcLine = s.settlementHeroNpcLine;
		if (!string.IsNullOrWhiteSpace(settlementHeroNpcLine))
		{
			string prefix = PromptComposerTexts.Get("persona_scene_settlement_heronpc_prefix_raw");
			if (settlementHeroNpcLine.StartsWith(prefix, StringComparison.Ordinal))
			{
				settlementHeroNpcLine = settlementHeroNpcLine.Substring(prefix.Length).Trim();
			}
			if (!string.IsNullOrWhiteSpace(settlementHeroNpcLine))
			{
				stringBuilder.AppendLine();
				stringBuilder.Append(PromptComposerTexts.Get("persona_scene_settlement_heronpc_prefix")).Append(settlementHeroNpcLine);
			}
		}
		string settlementFlavorLine = s.settlementFlavorLine;
		if (!string.IsNullOrWhiteSpace(settlementFlavorLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(settlementFlavorLine);
		}
		string settlementRulerPresenceLine = s.settlementRulerPresenceLine;
		if (!string.IsNullOrWhiteSpace(settlementRulerPresenceLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(settlementRulerPresenceLine);
		}


		string playerIntroLine = s.playerIntroLine;
		if (!string.IsNullOrWhiteSpace(playerIntroLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(playerIntroLine);
		}
		string prisonerContextLine = s.prisonerContextLine;
		if (!string.IsNullOrWhiteSpace(prisonerContextLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(prisonerContextLine);
		}
		string playerCommandRelationshipLine = s.playerCommandRelationshipLine;
		if (!string.IsNullOrWhiteSpace(playerCommandRelationshipLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(playerCommandRelationshipLine);
		}
		string nearbyPresentNpcLine = s.nearbyPresentNpcLine;
		if (!string.IsNullOrWhiteSpace(nearbyPresentNpcLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(nearbyPresentNpcLine);
		}
		if (includeInventorySummary && !string.IsNullOrWhiteSpace(inventorySummary))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(s.inventoryHeader);
			stringBuilder.AppendLine();
			stringBuilder.Append(inventorySummary);
		}
		return stringBuilder.ToString().Trim();
}

 internal static string ComposeScenePlayer(ScenePlayerIntroSnapshot s) {
 if(s==null)return "";
 string culture=s.culture;
 string age=s.age;
 string genderText=s.genderText;
 string equipment=s.equipment;
 string equipmentValueInline=s.equipmentValueInline;
 string identitySentence=s.identitySentence;
 string clanName=s.clanName;

int clanTier=s.clanTier;bool isClanLeader=s.isClanLeader,includePlayerPartyRoster=s.includePlayerPartyRoster;
		StringBuilder stringBuilder = new StringBuilder();

		bool knowsPlayerIdentity = s.knowsPlayerIdentity;
		if (knowsPlayerIdentity)
		{
			string playerPublicName = s.playerPublicName;
			string reputation = s.reputation;
			stringBuilder.Append(PromptComposerTexts.Get("persona_player_standing_prefix"))
				.Append(culture)
				.Append(PromptComposerTexts.Get("persona_player_knows_name"))
				.Append(string.IsNullOrWhiteSpace(playerPublicName) ? PromptComposerTexts.Get("persona_player_unknown_name") : playerPublicName)
				.Append(PromptComposerTexts.Get("persona_player_reputation"))
				.Append(reputation)
				.Append(PromptComposerTexts.Get("persona_scene_npc_is"))
				.Append(clanName)
				.Append(PromptComposerTexts.Get("persona_system_npc_of"))
				.Append(isClanLeader ? PromptComposerTexts.Get("persona_scene_npc_clan_leader") : PromptComposerTexts.Get("persona_player_clan_member"))
				.Append(PromptComposerTexts.Get("persona_player_appearance"))
				.Append(genderText)
				.Append(age)
				.Append(PromptComposerTexts.Get("persona_player_equipment"))
				.Append(equipment)
				.Append(PromptComposerTexts.Get("residual_period"));
		}
		else if (clanTier >= 2)
		{
			stringBuilder.Append(PromptComposerTexts.Get("persona_player_standing_prefix"))
				.Append(culture)
				.Append(PromptComposerTexts.Get("persona_unknown_name_appearance"))
				.Append(genderText)
				.Append(age)
				.Append(PromptComposerTexts.Get("persona_player_equipment"))
				.Append(equipment)
				.Append(PromptComposerTexts.Get("residual_period"));
		}
		else
		{
			stringBuilder.Append(PromptComposerTexts.Get("persona_player_standing_prefix"))
				.Append(culture)
				.Append(PromptComposerTexts.Get("persona_player_not_noble_hint"))
				.Append(genderText)
				.Append(age)
				.Append(PromptComposerTexts.Get("persona_player_equipment"))
				.Append(equipment)
				.Append(PromptComposerTexts.Get("residual_period"));
		}
		string playerCurrentMountLine = s.playerCurrentMountLine;
		if (!string.IsNullOrWhiteSpace(playerCurrentMountLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(playerCurrentMountLine);
		}
		if (s.isCompanion)
		{
			stringBuilder.Append(PromptComposerTexts.Get("persona_player_companion_prefix"))
				.Append(s.companionRole)
				.Append(PromptComposerTexts.Get("persona_player_companion_suffix"));
		}
		if (!string.IsNullOrWhiteSpace(equipmentValueInline))
		{
			stringBuilder.Append(equipmentValueInline).Append(PromptComposerTexts.Get("residual_period"));
		}
		if (knowsPlayerIdentity && !string.IsNullOrWhiteSpace(identitySentence))
		{
			stringBuilder.Append(identitySentence);
		}
		string vassalageRelationLine = s.vassalageRelationLine;
		if (!string.IsNullOrWhiteSpace(vassalageRelationLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(vassalageRelationLine);
		}
		string inlineState = s.inlineState;
		if (!string.IsNullOrWhiteSpace(inlineState))
		{
			stringBuilder.Append(inlineState);
		}
		string factionWarLine = s.factionWarLine;
		if (!string.IsNullOrWhiteSpace(factionWarLine))
		{
			stringBuilder.Append(factionWarLine);
		}
		string crimeRatingLine = s.crimeRatingLine;
		if (!string.IsNullOrWhiteSpace(crimeRatingLine))
		{
			stringBuilder.Append(crimeRatingLine);
		}
		if (includePlayerPartyRoster)
		{
			try
			{
				string playerTroopsLine = s.playerTroopsLine;
				string playerPrisonersLine = s.playerPrisonersLine;
				if (!string.IsNullOrWhiteSpace(playerTroopsLine))
				{
					stringBuilder.Append(playerTroopsLine).Append(PromptComposerTexts.Get("residual_period"));
				}
				if (!string.IsNullOrWhiteSpace(playerPrisonersLine))
				{
					stringBuilder.Append(playerPrisonersLine).Append(PromptComposerTexts.Get("residual_period"));
				}
			}
			catch
			{
			}
		}
		else
		{
			string townPartyStayHint = s.townPartyStayHint;
			if (!string.IsNullOrWhiteSpace(townPartyStayHint))
			{
				stringBuilder.Append(townPartyStayHint).Append(PromptComposerTexts.Get("residual_period"));
			}
		}
		return stringBuilder.ToString().Trim();
}
}
