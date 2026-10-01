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
 private void GetNpcPersonaStrings(Hero hero, out string personality, out string background) => _my.GetNpcPersonaStrings(hero, out personality, out background);
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
		string clanName = "无家族";
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
			factionDisplay = "无（独立）";
		}
		string liegeDisplay = (liegeName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(liegeDisplay) && liegeDisplay != "无" && !liegeDisplay.EndsWith("（本人）", StringComparison.Ordinal))
		{
			factionDisplay = factionDisplay + "（效忠：" + liegeDisplay + "）";
		}
		string heroName = (npcHero.Name?.ToString() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(heroName))
		{
			heroName = "未知人物";
		}
		string identityTitle = BuildHeroIdentityTitleForPrompt(npcHero);
		string reputationText = GetClanTierReputationLabel(clanTier) + $"（{Math.Max(0, clanTier)} level）";
		string equipmentText = BuildHeroEquipmentSummaryForPrompt(npcHero);
		string ageText = BuildAgeBracketLabel(npcHero.Age);
		string cultureText = GetHeroCultureNameForPrompt(npcHero);
		if (!string.IsNullOrWhiteSpace(cultureText) && !cultureText.EndsWith("人", StringComparison.Ordinal))
		{
			cultureText += "人";
		}
		GetNpcPersonaStrings(npcHero, out var personality, out var background);
		string personalityText = string.IsNullOrWhiteSpace(personality) ? "暂无记录" : personality.Trim();
		string backgroundText = string.IsNullOrWhiteSpace(background) ? "暂无记录" : background.Trim();
		string nobleEtiquettePrompt = BuildNobleEtiquettePromptForHero(npcHero);
		string clanRole = npcHero.IsFemale ? "女性成员" : "男性成员";
		try
		{
			Hero leader = npcHero.Clan?.Leader;
			if (leader != null && leader == npcHero)
			{
				clanRole = "族长";
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
            clanName = clanName,
            factionDisplay = factionDisplay,
            heroName = heroName,
            identityTitle = identityTitle,
            reputationText = reputationText,
            equipmentText = equipmentText,
            ageText = ageText,
            cultureText = cultureText,
            personalityText = personalityText,
            backgroundText = backgroundText,
            nobleEtiquettePrompt = nobleEtiquettePrompt,
            clanRole = clanRole,
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
			identity = "未知身份";
		}
		string personality = string.IsNullOrWhiteSpace(npc.PersonalityDesc) ? "暂无记录" : npc.PersonalityDesc.Trim();
		string background = string.IsNullOrWhiteSpace(npc.BackgroundDesc) ? "暂无记录" : npc.BackgroundDesc.Trim();
		string faction = "无（独立）";
		string clan = "无家族";
		string clanRole = "成员";
		string reputation = MyBehavior.GetClanTierReputationLabelForExternal(0) + "（0 level）";
		string culture = (npc.CultureId ?? "").Trim();
		string age = MyBehavior.BuildAgeBracketLabelForExternal(npc.Age);
		string equipment = "未知";
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
				if (!string.IsNullOrWhiteSpace(clan) && !clan.EndsWith("家族", StringComparison.Ordinal))
				{
					clan += "家族";
				}
				int tier = hero.Clan?.Tier ?? 0;
				reputation = MyBehavior.GetClanTierReputationLabelForExternal(tier) + $"（{Math.Max(0, tier)} level）";
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
					clanRole = "族长";
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
			if (!string.IsNullOrWhiteSpace(clan) && clan != "无家族" && !clan.EndsWith("家族", StringComparison.Ordinal))
			{
				clan += "家族";
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
			culture = "未知文化";
		}
		if (!culture.EndsWith("人", StringComparison.Ordinal))
		{
			culture += "人";
		}
		if (string.IsNullOrWhiteSpace(age))
		{
			age = "未知";
		}
		if (string.IsNullOrWhiteSpace(equipment))
		{
			equipment = "未知";
		}
		try
		{
			currentDateText = (MyBehavior.BuildCurrentDateFactForExternal() ?? "").Replace("\r", "").Trim();
			if (currentDateText.StartsWith("当前游戏日期：", StringComparison.Ordinal))
			{
				currentDateText = currentDateText.Substring("当前游戏日期：".Length).Trim();
			}
			int parenIndex = currentDateText.IndexOf('（');
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
        if(hero!=null){pregnancySelfKnowledge=BuildHeroPregnancySelfKnowledgeForPrompt(hero);isFemale=clanRole!="族长"&&hero.IsFemale;heroJoinPartyRuntimeFact=AIConfigHandler.BuildRuntimeHeroJoinPartyInstructionForExternal(hero);}
        else partyRepresentativePrompt=BuildWildernessNonHeroPartyRepresentativePrompt(npc.AgentIndex);
        string ceremonyRole=BuildCeremonyRoleFactForPrompt(npc.AgentIndex);
        string npcCurrentMountLine=BuildNpcCurrentMountLineForPrompt(npc);
        if(hero!=null){try{heroInPlayerParty=IsHeroInPlayerMainPartyForPrompt(hero);if(!heroInPlayerParty){npcTroopsLine=BuildHeroPartyTroopsLineForPrompt(hero,secondPerson:true);npcPrisonersLine=BuildHeroPartyPrisonersLineForPrompt(hero,secondPerson:true);}}catch{npcTroopsLine="";npcPrisonersLine="";}}
        else {try{PartyBase party=ResolveWildernessNonHeroPartyBaseForPrompt(npc.AgentIndex);hasNonHeroParty=party!=null;if(hasNonHeroParty){npcTroopsLine=BuildPartyTroopsLineForPrompt(party,"你所属队伍共有","你所属队伍无可战兵力",includeDetails:true);npcPrisonersLine=BuildPartyPrisonersLineForPrompt(party,"你所属队伍","你所属队伍无俘虏",includeDetails:true);}}catch{}}
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
            identity = identity,
            personality = personality,
            background = background,
            faction = faction,
            clan = clan,
            clanRole = clanRole,
            reputation = reputation,
            culture = culture,
            age = age,
            equipment = equipment,
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
		string culture = "未知文化";
		string age = "未知";
		string genderText = playerHero.IsFemale ? "女性" : "男性";
		string equipment = "未知";
		string equipmentValueInline = "";
		string identitySentence = BuildPlayerSceneIdentitySentenceForPrompt(playerHero);
		int clanTier = 0;
		string clanName = "无家族";
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
		if (!culture.EndsWith("人", StringComparison.Ordinal))
		{
			culture += "人";
		}
		try
		{
			age = MyBehavior.BuildAgeBracketLabelForExternal(playerHero.Age);
		}
		catch
		{
			age = "未知";
		}
		try
		{
			equipment = MyBehavior.BuildHeroEquipmentSummaryForExternal(playerHero);
		}
		catch
		{
			equipment = "未知";
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
			if (!string.IsNullOrWhiteSpace(clanName) && clanName != "无家族" && !clanName.EndsWith("家族", StringComparison.Ordinal))
			{
				clanName += "家族";
			}
			isClanLeader = playerHero.Clan?.Leader == playerHero;
		}
		catch
		{
		}
        bool forceDetailed=ShouldForceDetailedPlayerIntroForObserver(observerHero);
        bool knowsPlayerIdentity=forceDetailed||DoesSceneObserverKnowPlayerIdentityForPrompt(observerHero,observerNpc);
        string playerPublicName="",reputation="";
        if(knowsPlayerIdentity){playerPublicName=(playerHero.Name?.ToString()??"").Trim();reputation=MyBehavior.GetClanTierReputationLabelForExternal(clanTier)+$"（{Math.Max(0,clanTier)} level）";}
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
            culture = culture,
            age = age,
            genderText = genderText,
            equipment = equipment,
            equipmentValueInline = equipmentValueInline,
            identitySentence = identitySentence,
            clanName = clanName,
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
