using System;using System.Collections.Generic;using System.Linq;using System.Text;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.Core;using TaleWorlds.MountAndBlade;
namespace AnimusForge;
internal sealed class PersonaEquipmentPromptCaptureAdapter {
 private readonly MyPersonaIntroLivePort _my;private readonly ScenePersonaIntroLivePort _scene;
 internal PersonaEquipmentPromptCaptureAdapter(MyPersonaIntroLivePort my, ScenePersonaIntroLivePort scene){_my=my;_scene=scene;}
 private string BuildAgeBracketLabel(float age) => _my.BuildAgeBracketLabel(age);
 private string BuildHeroEquipmentSummaryForPrompt(Hero hero, int maxEntries = 8) => _my.BuildHeroEquipmentSummaryForPrompt(hero, maxEntries);
 private string BuildHeroIdentityTitleForPrompt(Hero hero) => _my.BuildHeroIdentityTitleForPrompt(hero);
 private string BuildNobleEtiquettePromptForHero(Hero npcHero) => _my.BuildNobleEtiquettePromptForHero(npcHero);
 private string BuildNpcPlayerKinshipPromptLine(Hero npcHero, bool includeSameClanFallback) => _my.BuildNpcPlayerKinshipPromptLine(npcHero, includeSameClanFallback);
 private string GetClanTierReputationLabel(int tier) => _my.GetClanTierReputationLabel(tier);
 private string GetHeroCultureNameForPrompt(Hero hero) => _my.GetHeroCultureNameForPrompt(hero);
 private void GetHeroFactionAndLiegeForPrompt(Hero hero, out string factionName, out string liegeName) => _my.GetHeroFactionAndLiegeForPrompt(hero, out factionName, out liegeName);
 private string GetNpcPersonaDescription(Hero hero) => _my.GetNpcPersonaDescription(hero);
 private string GetNpcPersonaContext(Hero hero) => _my.GetNpcPersonaContext?.Invoke(hero) ?? "";
 private string BuildCeremonyRoleFactForPrompt(int agentIndex) => _scene.BuildCeremonyRoleFactForPrompt(agentIndex);
 private string BuildHeroPartyPrisonersLineForPrompt(Hero hero, bool secondPerson, bool includeDetails = true) => _scene.BuildHeroPartyPrisonersLineForPrompt(hero, secondPerson, includeDetails);
 private string BuildHeroPartyTroopsLineForPrompt(Hero hero, bool secondPerson, bool includeDetails = true, string leaderDisplayNameOverride = null) => _scene.BuildHeroPartyTroopsLineForPrompt(hero, secondPerson, includeDetails, leaderDisplayNameOverride);
 private string BuildHeroPregnancySelfKnowledgeForPrompt(Hero hero) => _scene.BuildHeroPregnancySelfKnowledgeForPrompt(hero);
 private string BuildNearbyPresentNpcLineForPrompt(NpcDataPacket selfNpc, IEnumerable<NpcDataPacket> presentNpcs) => _scene.BuildNearbyPresentNpcLineForPrompt(selfNpc, presentNpcs);
 private string BuildNonHeroEquipmentSummaryForPrompt(NpcDataPacket npc, int maxEntries = 8) => _scene.BuildNonHeroEquipmentSummaryForPrompt(npc, maxEntries);
 private string BuildNpcCurrentMountLineForPrompt(NpcDataPacket npc) => _scene.BuildNpcCurrentMountLineForPrompt(npc);
 private string BuildNpcInventorySummaryHeader(string npcName, bool isHeroNpcLord = false) => _scene.BuildNpcInventorySummaryHeaderName(npcName, isHeroNpcLord);
 private string BuildNpcInventorySummaryHeader(Hero hero) => _scene.BuildNpcInventorySummaryHeaderHero(hero);
 private string BuildPartyPrisonersLineForPrompt(PartyBase partyBase, string subject, string noPrisonersText, bool includeDetails = true) => _scene.BuildPartyPrisonersLineForPrompt(partyBase, subject, noPrisonersText, includeDetails);
 private string BuildPartyTroopsLineForPrompt(PartyBase partyBase, string leadingText, string noTroopsText, bool includeDetails = true, string leaderDisplayNameOverride = null) => _scene.BuildPartyTroopsLineForPrompt(partyBase, leadingText, noTroopsText, includeDetails, leaderDisplayNameOverride);
 private string BuildPlayerCommandRelationshipLineForPrompt(NpcDataPacket npc, Hero hero) => _scene.BuildPlayerCommandRelationshipLineForPrompt(npc, hero);
 private string BuildPlayerCompanionPartyRoleLabelForPrompt(Hero companionHero) => _scene.BuildPlayerCompanionPartyRoleLabelForPrompt(companionHero);
 private string BuildPlayerCurrentMountLineForPrompt() => _scene.BuildPlayerCurrentMountLineForPrompt();
 private string BuildPlayerFactionWarLineForPrompt(Hero observerHero, NpcDataPacket observerNpc) => _scene.BuildPlayerFactionWarLineForPrompt(observerHero, observerNpc);
 private string BuildPlayerSceneIdentitySentenceForPrompt(Hero playerHero) => _scene.BuildPlayerSceneIdentitySentenceForPrompt(playerHero);
 private string BuildPlayerTownPartyStayHintForPrompt(Hero playerHero) => _scene.BuildPlayerTownPartyStayHintForPrompt(playerHero);
 private string BuildPlayerVassalageRelationLineForPrompt(Hero observerHero, NpcDataPacket observerNpc) => _scene.BuildPlayerVassalageRelationLineForPrompt(observerHero, observerNpc);
 private string BuildPrisonerContextLineForPrompt(NpcDataPacket npc, Hero hero) => _scene.BuildPrisonerContextLineForPrompt(npc, hero);
 private string BuildSceneLocationAndSettlementLineForPrompt(Hero perspectiveHero) => _scene.BuildSceneLocationAndSettlementLineForPrompt(perspectiveHero);
 private string BuildSceneObserverInlineStateForPrompt(Hero observerHero, NpcDataPacket observerNpc) => _scene.BuildSceneObserverInlineStateForPrompt(observerHero, observerNpc);
 private string BuildSettlementFlavorLineForPrompt(Hero perspectiveHero) => _scene.BuildSettlementFlavorLineForPrompt(perspectiveHero);
 private string BuildSettlementRulerPresenceLineForPrompt() => _scene.BuildSettlementRulerPresenceLineForPrompt();
 private string BuildWildernessNonHeroPartyRepresentativePrompt(int agentIndex) => _scene.BuildWildernessNonHeroPartyRepresentativePrompt(agentIndex);
 private bool DoesSceneObserverKnowPlayerIdentityForPrompt(Hero observerHero, NpcDataPacket observerNpc) => _scene.DoesSceneObserverKnowPlayerIdentityForPrompt(observerHero, observerNpc);
 private string GetSceneNpcGivenNameForPrompt(NpcDataPacket npc) => _scene.GetSceneNpcGivenNameForPrompt(npc);
 private string GetSceneNpcHistoryNameForPrompt(NpcDataPacket npc) => _scene.GetSceneNpcHistoryNameForPrompt(npc);
 private string GetSceneNpcIdentityNameForPrompt(NpcDataPacket npc) => _scene.GetSceneNpcIdentityNameForPrompt(npc);
 private bool IsHeroInPlayerMainPartyForPrompt(Hero hero) => _scene.IsHeroInPlayerMainPartyForPrompt(hero);
 private IFaction ResolveNpcPerspectiveFactionForPlayerCrimePrompt(Hero observerHero, NpcDataPacket observerNpc) => _scene.ResolveNpcPerspectiveFactionForPlayerCrimePrompt(observerHero, observerNpc);
 private PartyBase ResolveWildernessNonHeroPartyBaseForPrompt(int agentIndex) => _scene.ResolveWildernessNonHeroPartyBaseForPrompt(agentIndex);
 private bool ShouldForceDetailedPlayerIntroForObserver(Hero observerHero) => _scene.ShouldForceDetailedPlayerIntroForObserver(observerHero);
 private bool ShouldHideSceneReputationForPrompt(NpcDataPacket npc, Hero hero) => _scene.ShouldHideSceneReputationForPrompt(npc, hero);
 private bool ShouldIncludePlayerPartyRosterForScenePrompt(Hero observerHero, bool partyTransferTopicSelected) => _scene.ShouldIncludePlayerPartyRosterForScenePrompt(observerHero, partyTransferTopicSelected);
 private bool ShouldUseCompactPlayerPartyRosterForScenePrompt(bool partyTransferTopicSelected) => _scene.ShouldUseCompactPlayerPartyRosterForScenePrompt(partyTransferTopicSelected);
	internal SystemNpcIntroSnapshot CaptureSystemNpcIntro(Hero npcHero, bool includeTradePricing)
	{
        long generation = SaveRuntimeGuard.CaptureGeneration();
		if (npcHero == null)
		{
			return null;
		}
		string clanName = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_1");
		int clanTier = 0;
		try
		{
			clanTier = npcHero.Clan?.Tier ?? 0;
			string rawClanName = (npcHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(rawClanName))
			{
				clanName = rawClanName;
			}
		}
		catch
		{
		}
		GetHeroFactionAndLiegeForPrompt(npcHero, out var factionName, out var liegeName);
		string factionDisplay = (factionName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(factionDisplay))
		{
			factionDisplay = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_2");
		}
		string liegeDisplay = (liegeName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(liegeDisplay) && liegeDisplay != AdapterPromptCaptureTexts.PcapCompat("Equipment_LiegeNone") && !liegeDisplay.EndsWith(AdapterPromptCaptureTexts.PcapCompat("Equipment_LiegeSelfSuffix"), StringComparison.Ordinal)
			&& !liegeDisplay.Equals("None", StringComparison.OrdinalIgnoreCase) && !liegeDisplay.EndsWith("(self)", StringComparison.OrdinalIgnoreCase))
		{
			factionDisplay = factionDisplay + AdapterPromptCaptureTexts.PcapText("equipment_liege_prefix") + liegeDisplay + AdapterPromptCaptureTexts.PcapText("equipment_close");
		}
		string heroName = (npcHero.Name?.ToString() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(heroName))
		{
			heroName = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_3");
		}
		string identityTitle = BuildHeroIdentityTitleForPrompt(npcHero);
		string reputationText = GetClanTierReputationLabel(clanTier) + AdapterPromptCaptureTexts.PcapFormat("equipment_reputation_level", " ({LEVEL} level)", ("LEVEL", Math.Max(0, clanTier)));
		string equipmentText = BuildHeroEquipmentSummaryForPrompt(npcHero);
		string ageText = BuildAgeBracketLabel(npcHero.Age);
		string cultureText = GetHeroCultureNameForPrompt(npcHero);
		string cultureSuffix = AdapterPromptCaptureTexts.PcapText("equipment_culture_suffix", "");
		if (!string.IsNullOrWhiteSpace(cultureText) && !string.IsNullOrEmpty(cultureSuffix) && !cultureText.EndsWith(cultureSuffix, StringComparison.Ordinal))
		{
			cultureText += cultureSuffix;
		}
		string personaDescription = GetNpcPersonaDescription(npcHero);
		string personaContext = GetNpcPersonaContext(npcHero);
		string description = string.IsNullOrWhiteSpace(personaDescription) ? AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_4") : personaDescription;
		string nobleEtiquettePrompt = BuildNobleEtiquettePromptForHero(npcHero);
		string clanRole = npcHero.IsFemale ? AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_5") : AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_6");
		try
		{
			Hero leader = npcHero.Clan?.Leader;
			if (leader != null && leader == npcHero)
			{
				clanRole = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_7");
			}
		}
		catch
		{
		}
		string inventorySummary = "";
		if (includeTradePricing && RewardSystemBehavior.Instance != null)
		{
			try
			{
				MentionedWorldEntities mentions = AIConfigHandler.GetLatestAuxiliaryMentionedEntitiesForExternal();
				int promptListMax = PromptListRetrievalService.GetMaxCandidateCount();
				inventorySummary = RewardSystemBehavior.Instance.BuildFilteredInventorySummaryForAI(npcHero, mentions, promptListMax, includePrivateBattleEquipment: includeTradePricing);
			}
			catch
			{
				inventorySummary = "";
			}
		}
        string relationshipLine = BuildNpcPlayerKinshipPromptLine(npcHero, includeSameClanFallback: true);
        return new SystemNpcIntroSnapshot {
            Generation = generation,
            clanName = AdapterPromptCaptureTexts.PcapProjectKnown(clanName),
            factionDisplay = AdapterPromptCaptureTexts.PcapProjectKnown(factionDisplay),
            heroName = AdapterPromptCaptureTexts.PcapProjectKnown(heroName),
            identityTitle = identityTitle,
            reputationText = reputationText,
            equipmentText = equipmentText,
            ageText = ageText,
            cultureText = AdapterPromptCaptureTexts.PcapProjectKnown(cultureText),
            description = description,
            personaContext = AdapterPromptCaptureTexts.PcapProjectKnown(personaContext),
            nobleEtiquettePrompt = nobleEtiquettePrompt,
            clanRole = AdapterPromptCaptureTexts.PcapProjectKnown(clanRole),
            relationshipLine = relationshipLine
        };
 }
internal string BuildNpcSystemIntro(Hero npcHero, bool includeTradePricing) => ComposeSystemNpcIfCurrent(CaptureSystemNpcIntro(npcHero, includeTradePricing));
 internal static string ComposeSystemNpcIfCurrent(SystemNpcIntroSnapshot snapshot) => snapshot != null && SaveRuntimeGuard.IsCurrentGeneration(snapshot.Generation) ? PersonaIntroMessageComposer.ComposeSystemNpc(snapshot) : "";

	internal SceneNpcIntroSnapshot CaptureSceneNpcIntro(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
	{
        long generation = SaveRuntimeGuard.CaptureGeneration();
		if (npc == null)
		{
			return null;
		}
		// This value belongs to the current prompt request. Do not read the
		// AsyncLocal fallback here: main-prompt assembly may resume on another thread.
		MentionedWorldEntities inventoryMentions = promptMentions;
		string name = GetSceneNpcIdentityNameForPrompt(npc);
		string givenName = hero == null ? GetSceneNpcGivenNameForPrompt(npc) : "";
		string identity = (npc.RoleDesc ?? "").Trim();
		if (string.IsNullOrWhiteSpace(identity))
		{
			identity = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_8");
		}
		string description = npc.GetPersonaDescription();
        if (string.IsNullOrWhiteSpace(description)) description = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_4");
		string personaContext = npc.PersonaContext ?? "";
		string faction = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_2");
		string clan = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_1");
		string clanRole = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_9");
		string reputation = MyBehavior.GetClanTierReputationLabelForExternal(0) + AdapterPromptCaptureTexts.PcapFormat("equipment_reputation_level", " ({LEVEL} level)", ("LEVEL", 0));
		string culture = (npc.CultureId ?? "").Trim();
		string age = MyBehavior.BuildAgeBracketLabelForExternal(npc.Age);
		string equipment = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_10");
		string inventorySummary = "";
		string currentDateText = "";
		if (hero != null)
		{
			try
			{
				string clanName = (hero.Clan?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(clanName))
				{
					clan = clanName;
				}
				string clanSuffix = AdapterPromptCaptureTexts.PcapText("equipment_clan_suffix");
				if (!string.IsNullOrWhiteSpace(clan) && !clan.EndsWith(AdapterPromptCaptureTexts.PcapCompat("Location_ClanSuffix"), StringComparison.Ordinal)
					&& !clan.EndsWith(clanSuffix, StringComparison.OrdinalIgnoreCase))
				{
					clan += clanSuffix;
				}
				int tier = hero.Clan?.Tier ?? 0;
				reputation = MyBehavior.GetClanTierReputationLabelForExternal(tier) + AdapterPromptCaptureTexts.PcapFormat("equipment_reputation_level", " ({LEVEL} level)", ("LEVEL", Math.Max(0, tier)));
				string heroIdentity = MyBehavior.BuildHeroIdentityTitleForExternal(hero);
				if (!string.IsNullOrWhiteSpace(heroIdentity))
				{
					identity = heroIdentity.Trim();
				}
				string factionName = (hero.Clan?.Kingdom?.Name?.ToString() ?? "").Trim();
				if (string.IsNullOrWhiteSpace(factionName))
				{
					factionName = (hero.MapFaction?.Name?.ToString() ?? "").Trim();
				}
				if (string.IsNullOrWhiteSpace(factionName))
				{
					factionName = clan;
				}
				if (!string.IsNullOrWhiteSpace(factionName))
				{
					faction = factionName;
				}
				if (hero.Clan?.Leader == hero)
				{
					clanRole = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_7");
				}
				string cultureName = (hero.Culture?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(cultureName))
				{
					culture = cultureName;
				}
				age = MyBehavior.BuildAgeBracketLabelForExternal(hero.Age);
				equipment = MyBehavior.BuildHeroEquipmentSummaryForExternal(hero);
				if (includeInventorySummary && RewardSystemBehavior.Instance != null)
				{
					int promptListMax = PromptListRetrievalService.GetMaxCandidateCount();
					inventorySummary = (RewardSystemBehavior.Instance.BuildFilteredInventorySummaryForAI(hero, inventoryMentions, promptListMax, includePrivateBattleEquipment: includeTradePricing) ?? "").Trim();
				}
			}
			catch
			{
			}
		}
		else
		{
			string unnamedRank = (npc.UnnamedRank ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(unnamedRank))
			{
				clan = unnamedRank;
			}
			string clanSuffix = AdapterPromptCaptureTexts.PcapText("equipment_clan_suffix");
			if (!string.IsNullOrWhiteSpace(clan) && clan != AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_1")
				&& !clan.EndsWith(AdapterPromptCaptureTexts.PcapCompat("Location_ClanSuffix"), StringComparison.Ordinal) && !clan.EndsWith(clanSuffix, StringComparison.OrdinalIgnoreCase))
			{
				clan += clanSuffix;
			}
			if (!string.IsNullOrWhiteSpace(culture))
			{
				faction = culture;
			}
			equipment = BuildNonHeroEquipmentSummaryForPrompt(npc);
			try
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
				CharacterObject characterObject = agent?.Character as CharacterObject;
				if (includeInventorySummary && RewardSystemBehavior.Instance != null && characterObject != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out var _))
				{
					int promptListMax = PromptListRetrievalService.GetMaxCandidateCount();
					string text = RewardSystemBehavior.Instance.BuildFilteredSettlementMerchantInventorySummaryForAI(characterObject, inventoryMentions, promptListMax);
					if (!string.IsNullOrWhiteSpace(text))
					{
						inventorySummary = text.Trim();
					}
				}
			}
			catch
			{
			}
		}
		if (string.IsNullOrWhiteSpace(culture))
		{
			culture = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_11");
		}
		string nonHeroCultureSuffix = AdapterPromptCaptureTexts.PcapText("equipment_culture_suffix", "");
		if (!string.IsNullOrEmpty(nonHeroCultureSuffix) && !culture.EndsWith(nonHeroCultureSuffix, StringComparison.Ordinal))
		{
			culture += nonHeroCultureSuffix;
		}
		if (string.IsNullOrWhiteSpace(age))
		{
			age = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_10");
		}
		if (string.IsNullOrWhiteSpace(equipment))
		{
			equipment = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_10");
		}
		try
		{
			currentDateText = (MyBehavior.BuildCurrentDateFactForExternal() ?? "").Replace("\r", "").Trim();
			if (currentDateText.StartsWith(AdapterPromptCaptureTexts.PcapCompat("Equipment_CurrentDatePrefix"), StringComparison.Ordinal))
			{
				currentDateText = currentDateText.Substring(AdapterPromptCaptureTexts.PcapCompat("Equipment_CurrentDatePrefix").Length).Trim();
			}
            else if (currentDateText.StartsWith("Current game date: ", StringComparison.OrdinalIgnoreCase))
                currentDateText = currentDateText.Substring("Current game date: ".Length).Trim();
			int parenIndex = currentDateText.IndexOfAny(new[] { AdapterPromptCaptureTexts.PcapCompat("Equipment_DateOpenParen")[0], '(' });
			if (parenIndex >= 0)
			{
				currentDateText = currentDateText.Substring(0, parenIndex).Trim();
			}
		}
		catch
		{
			currentDateText = "";
		}
		bool hideReputation = ShouldHideSceneReputationForPrompt(npc, hero);
        string pregnancySelfKnowledge="",heroJoinPartyRuntimeFact="",partyRepresentativePrompt="",npcTroopsLine="",npcPrisonersLine="";
        bool isFemale=false,heroInPlayerParty=false,hasNonHeroParty=false;
        if(hero!=null){pregnancySelfKnowledge=BuildHeroPregnancySelfKnowledgeForPrompt(hero);isFemale=clanRole!=AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_7")&&hero.IsFemale;heroJoinPartyRuntimeFact=AIConfigHandler.BuildRuntimeHeroJoinPartyInstructionForExternal(hero);}
        else partyRepresentativePrompt=BuildWildernessNonHeroPartyRepresentativePrompt(npc.AgentIndex);
        string ceremonyRole=BuildCeremonyRoleFactForPrompt(npc.AgentIndex);
        string npcCurrentMountLine=BuildNpcCurrentMountLineForPrompt(npc);
        if(hero!=null){try{heroInPlayerParty=IsHeroInPlayerMainPartyForPrompt(hero);if(!heroInPlayerParty){npcTroopsLine=BuildHeroPartyTroopsLineForPrompt(hero,secondPerson:true);npcPrisonersLine=BuildHeroPartyPrisonersLineForPrompt(hero,secondPerson:true);}}catch{npcTroopsLine="";npcPrisonersLine="";}}
        else {try{PartyBase party=ResolveWildernessNonHeroPartyBaseForPrompt(npc.AgentIndex);hasNonHeroParty=party!=null;if(hasNonHeroParty){npcTroopsLine=BuildPartyTroopsLineForPrompt(party,AdapterPromptCaptureTexts.PcapText("equipment_own_party_total"),AdapterPromptCaptureTexts.PcapText("equipment_own_party_none"),includeDetails:true);npcPrisonersLine=BuildPartyPrisonersLineForPrompt(party,AdapterPromptCaptureTexts.PcapText("equipment_own_party"),AdapterPromptCaptureTexts.PcapText("equipment_own_prisoners_none"),includeDetails:true);}}catch{}}
        string nobleEtiquettePrompt=hero!=null?MyBehavior.BuildNobleEtiquettePromptForExternal(hero):"";
        string sceneLocationLine=BuildSceneLocationAndSettlementLineForPrompt(hero);
        string settlementHeroNpcLine=(ShoutUtils.BuildCurrentSettlementHeroNpcLineForPrompt()??"").Replace("\r","").Replace("\n"," ").Trim();
        string settlementFlavorLine=BuildSettlementFlavorLineForPrompt(hero);
        string settlementRulerPresenceLine=BuildSettlementRulerPresenceLineForPrompt();
        bool includePlayerPartyRoster=ShouldIncludePlayerPartyRosterForScenePrompt(hero,partyTransferTopicSelected);
        bool useCompactPlayerPartyRoster=includePlayerPartyRoster&&ShouldUseCompactPlayerPartyRosterForScenePrompt(partyTransferTopicSelected);
        string playerIntroLine=BuildScenePlayerIntroForPrompt(hero,npc,includeTradePricing,includePlayerPartyRoster,useCompactPlayerPartyRoster);
        string prisonerContextLine=BuildPrisonerContextLineForPrompt(npc,hero);
        string playerCommandRelationshipLine=BuildPlayerCommandRelationshipLineForPrompt(npc,hero);
        string nearbyPresentNpcLine=BuildNearbyPresentNpcLineForPrompt(npc,presentNpcs);
        string inventoryHeader=includeInventorySummary&&!string.IsNullOrWhiteSpace(inventorySummary)?(hero!=null?BuildNpcInventorySummaryHeader(hero):BuildNpcInventorySummaryHeader(GetSceneNpcHistoryNameForPrompt(npc))):"";
        return new SceneNpcIntroSnapshot {
            Generation = generation,
            name = name,
            givenName = givenName,
            identity = AdapterPromptCaptureTexts.PcapProjectKnown(identity),
            description = description,
            personaContext = AdapterPromptCaptureTexts.PcapProjectKnown(personaContext),
            faction = AdapterPromptCaptureTexts.PcapProjectKnown(faction),
            clan = AdapterPromptCaptureTexts.PcapProjectKnown(clan),
            clanRole = AdapterPromptCaptureTexts.PcapProjectKnown(clanRole),
            reputation = reputation,
            culture = AdapterPromptCaptureTexts.PcapProjectKnown(culture),
            age = AdapterPromptCaptureTexts.PcapProjectKnown(age),
            equipment = AdapterPromptCaptureTexts.PcapProjectKnown(equipment),
            inventorySummary = inventorySummary,
            currentDateText = currentDateText,
            pregnancySelfKnowledge = pregnancySelfKnowledge,
            heroJoinPartyRuntimeFact = heroJoinPartyRuntimeFact,
            partyRepresentativePrompt = partyRepresentativePrompt,
            ceremonyRole = ceremonyRole,
            npcCurrentMountLine = npcCurrentMountLine,
            npcTroopsLine = npcTroopsLine,
            npcPrisonersLine = npcPrisonersLine,
            nobleEtiquettePrompt = nobleEtiquettePrompt,
            sceneLocationLine = sceneLocationLine,
            settlementHeroNpcLine = settlementHeroNpcLine,
            settlementFlavorLine = settlementFlavorLine,
            settlementRulerPresenceLine = settlementRulerPresenceLine,
            playerIntroLine = playerIntroLine,
            prisonerContextLine = prisonerContextLine,
            playerCommandRelationshipLine = playerCommandRelationshipLine,
            nearbyPresentNpcLine = nearbyPresentNpcLine,
            inventoryHeader = inventoryHeader,
            isFemale = isFemale,
            hideReputation = hideReputation,
            heroInPlayerParty = heroInPlayerParty,
            hasNonHeroParty = hasNonHeroParty,
            includeInventorySummary = includeInventorySummary,
            partyTransferTopicSelected = partyTransferTopicSelected,
            hasHero=hero!=null
        };
 }
internal string BuildSceneNpcRoleIntroForPrompt(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null) => ComposeSceneNpcIfCurrent(CaptureSceneNpcIntro(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions));
 internal static string ComposeSceneNpcIfCurrent(SceneNpcIntroSnapshot snapshot) => snapshot != null && SaveRuntimeGuard.IsCurrentGeneration(snapshot.Generation) ? PersonaIntroMessageComposer.ComposeSceneNpc(snapshot) : "";

	internal ScenePlayerIntroSnapshot CaptureScenePlayerIntro(Hero observerHero, NpcDataPacket observerNpc, bool includeTradePricing = false, bool includePlayerPartyRoster = false, bool useCompactPlayerPartyRoster = false)
	{
        long generation = SaveRuntimeGuard.CaptureGeneration();
		Hero playerHero = Hero.MainHero;
		if (playerHero == null)
		{
			return null;
		}
		string culture = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_11");
		string age = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_10");
		string genderText = playerHero.IsFemale ? AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_12") : AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_13");
		string equipment = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_10");
		string equipmentValueInline = "";
		string identitySentence = BuildPlayerSceneIdentitySentenceForPrompt(playerHero);
		int clanTier = 0;
		string clanName = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_1");
		bool isClanLeader = false;
		try
		{
			string cultureName = (playerHero.Culture?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(cultureName))
			{
				culture = cultureName;
			}
		}
		catch
		{
		}
		string playerCultureSuffix = AdapterPromptCaptureTexts.PcapText("equipment_culture_suffix", "");
		if (!string.IsNullOrEmpty(playerCultureSuffix) && !culture.EndsWith(playerCultureSuffix, StringComparison.Ordinal))
		{
			culture += playerCultureSuffix;
		}
		try
		{
			age = MyBehavior.BuildAgeBracketLabelForExternal(playerHero.Age);
		}
		catch
		{
			age = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_10");
		}
		try
		{
			equipment = MyBehavior.BuildHeroEquipmentSummaryForExternal(playerHero);
		}
		catch
		{
			equipment = AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_10");
		}
		if (includeTradePricing)
		{
			try
			{
				if (RewardSystemBehavior.Instance != null)
				{
					equipmentValueInline = (RewardSystemBehavior.Instance.BuildVisibleEquipmentGuidePriceSummaryForAI(playerHero) ?? "").Trim();
				}
			}
			catch
			{
			}
		}
		try
		{
			clanTier = playerHero.Clan?.Tier ?? 0;
			string rawClanName = (playerHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(rawClanName))
			{
				clanName = rawClanName;
			}
			string playerClanSuffix = AdapterPromptCaptureTexts.PcapText("equipment_clan_suffix");
			if (!string.IsNullOrWhiteSpace(clanName) && clanName != AdapterPromptCaptureTexts.PcapCompat("Project_Equipment_1")
				&& !clanName.EndsWith(AdapterPromptCaptureTexts.PcapCompat("Location_ClanSuffix"), StringComparison.Ordinal) && !clanName.EndsWith(playerClanSuffix, StringComparison.OrdinalIgnoreCase))
			{
				clanName += playerClanSuffix;
			}
			isClanLeader = playerHero.Clan?.Leader == playerHero;
		}
		catch
		{
		}
        bool forceDetailed=ShouldForceDetailedPlayerIntroForObserver(observerHero);
        bool knowsPlayerIdentity=forceDetailed||DoesSceneObserverKnowPlayerIdentityForPrompt(observerHero,observerNpc);
        string playerPublicName="",reputation="";
        if(knowsPlayerIdentity){playerPublicName=(playerHero.Name?.ToString()??"").Trim();reputation=MyBehavior.GetClanTierReputationLabelForExternal(clanTier)+AdapterPromptCaptureTexts.PcapFormat("equipment_reputation_level", " ({LEVEL} level)", ("LEVEL", Math.Max(0,clanTier)));}
        string playerCurrentMountLine=BuildPlayerCurrentMountLineForPrompt();
        bool isCompanion=observerHero!=null&&observerHero.IsPlayerCompanion;
        string companionRole=isCompanion?BuildPlayerCompanionPartyRoleLabelForPrompt(observerHero):"";
        string vassalageRelationLine=BuildPlayerVassalageRelationLineForPrompt(observerHero,observerNpc);
        string inlineState=BuildSceneObserverInlineStateForPrompt(observerHero,observerNpc);
        string factionWarLine=BuildPlayerFactionWarLineForPrompt(observerHero,observerNpc);
        string crimeRatingLine=MyBehavior.BuildPlayerCrimeRatingPromptLineForExternal(ResolveNpcPerspectiveFactionForPlayerCrimePrompt(observerHero,observerNpc));
        string playerTroopsLine="",playerPrisonersLine="",townPartyStayHint="";
        if(includePlayerPartyRoster){try{playerTroopsLine=useCompactPlayerPartyRoster?BuildPlayerTownPartyStayHintForPrompt(playerHero):BuildHeroPartyTroopsLineForPrompt(playerHero,secondPerson:false,includeDetails:true);playerPrisonersLine=BuildHeroPartyPrisonersLineForPrompt(playerHero,secondPerson:false,includeDetails:!useCompactPlayerPartyRoster);}catch{playerTroopsLine="";playerPrisonersLine="";}}
        else townPartyStayHint=BuildPlayerTownPartyStayHintForPrompt(playerHero);
        return new ScenePlayerIntroSnapshot {
            Generation = generation,
            culture = AdapterPromptCaptureTexts.PcapProjectKnown(culture),
            age = AdapterPromptCaptureTexts.PcapProjectKnown(age),
            genderText = AdapterPromptCaptureTexts.PcapProjectKnown(genderText),
            equipment = AdapterPromptCaptureTexts.PcapProjectKnown(equipment),
            equipmentValueInline = equipmentValueInline,
            identitySentence = identitySentence,
            clanName = AdapterPromptCaptureTexts.PcapProjectKnown(clanName),
            playerPublicName = playerPublicName,
            reputation = reputation,
            playerCurrentMountLine = playerCurrentMountLine,
            companionRole = companionRole,
            vassalageRelationLine = vassalageRelationLine,
            inlineState = inlineState,
            factionWarLine = factionWarLine,
            crimeRatingLine = crimeRatingLine,
            playerTroopsLine = playerTroopsLine,
            playerPrisonersLine = playerPrisonersLine,
            townPartyStayHint = townPartyStayHint,
            knowsPlayerIdentity = knowsPlayerIdentity,
            isClanLeader = isClanLeader,
            isCompanion = isCompanion,
            includePlayerPartyRoster = includePlayerPartyRoster,
            clanTier = clanTier
        };
 }
internal string BuildScenePlayerIntroForPrompt(Hero observerHero, NpcDataPacket observerNpc, bool includeTradePricing = false, bool includePlayerPartyRoster = false, bool useCompactPlayerPartyRoster = false) => ComposeScenePlayerIfCurrent(CaptureScenePlayerIntro(observerHero, observerNpc, includeTradePricing, includePlayerPartyRoster, useCompactPlayerPartyRoster));
 internal static string ComposeScenePlayerIfCurrent(ScenePlayerIntroSnapshot snapshot) => snapshot != null && SaveRuntimeGuard.IsCurrentGeneration(snapshot.Generation) ? PersonaIntroMessageComposer.ComposeScenePlayer(snapshot) : "";

internal static string CaptureHeroIdentityTitle(Hero hero, HeroIdentityPromptLivePort port)
	{
		if (hero == null)
		{
			return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Unknown);
		}
		try
		{
			if (hero.Occupation == Occupation.Wanderer)
			{
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Wanderer);
			}
		}
		catch
		{
		}
		try
		{
			Clan clan = hero.Clan;
			Kingdom kingdom = clan?.Kingdom;
			if (clan != null && clan.IsUnderMercenaryService && kingdom != null)
			{
				string text = (kingdom.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Mercenary, text);
				}
			}
		}
		catch
		{
		}
		try
		{
			if (port.ResolveRuledKingdom(hero, out Kingdom ruledKingdom))
			{
				string kingdomName = (ruledKingdom.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(kingdomName))
				{
					return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Ruler, kingdomName);
				}
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Ruler);
			}
			Clan clan = hero.Clan;
			if (clan != null && clan.Leader == hero)
			{
				string clanName = (clan.Name?.ToString() ?? "").Trim();
				if (string.IsNullOrWhiteSpace(clanName))
				{
					return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.ClanLeader);
				}
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.ClanLeader, clanName);
			}
			string text2 = (hero.MapFaction?.Name?.ToString() ?? "").Trim();
			if (hero.IsLord)
			{
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Lord, text2);
				}
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Lord);
			}
			if (hero.IsWanderer)
			{
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Wanderer);
			}
			if (hero.IsNotable)
			{
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Notable);
			}
			switch (hero.Occupation)
			{
			case Occupation.Merchant:
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Merchant);
			case Occupation.Artisan:
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Artisan);
			case Occupation.GangLeader:
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.GangLeader);
			case Occupation.Headman:
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Headman);
			case Occupation.Preacher:
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Preacher);
			case Occupation.RuralNotable:
				return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.RuralNotable);
			}
		}
		catch
		{
		}
		return PersonaIntroTextRules.ComposeHeroIdentityTitle(HeroPromptIdentityKind.Common);
	}
internal static bool CaptureSceneReputationVisibility(NpcDataPacket npc, Hero hero)
	{
		try
		{
			if (hero != null)
			{
				if (hero.IsWanderer)
				{
					return PersonaIntroTextRules.ShouldHideSceneReputation(true, null);
				}
				if (hero.Occupation == Occupation.Headman)
				{
					return PersonaIntroTextRules.ShouldHideSceneReputation(true, null);
				}
			}
		}
		catch
		{
		}
		try
		{
			string role = (npc?.RoleDesc ?? "").Trim();
			return PersonaIntroTextRules.ShouldHideSceneReputation(false, role);
		}
		catch
		{
		}
		return PersonaIntroTextRules.ShouldHideSceneReputation(false, null);
	}
}
