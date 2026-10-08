using System;
using AnimusForge.Refactor.Modules;
using System.Text;
using TaleWorlds.Engine;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using SandBox;
using RichExecutions.Core;
using RichExecutions.Scene;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Settlements;
namespace AnimusForge.Refactor.Adapters;
internal sealed class SceneAgentIdentityPromptCaptureAdapter
{
internal bool TryGetNativeConversationPersistentHistoryTargetForExternal(out Hero targetHero, out string targetName, out string memoryId)
	{
		targetHero = null;
		targetName = "";
		memoryId = "";
		try
		{
			if (!TryResolveNativeConversationTarget(out var hero, out var character, out var npcName))
			{
				return false;
			}
			targetHero = hero ?? character?.HeroObject;
			targetName = (targetHero?.Name?.ToString() ?? character?.Name?.ToString() ?? npcName ?? "").Trim();
			if (targetHero != null)
			{
				memoryId = (targetHero.StringId ?? "").Trim();
				return !string.IsNullOrWhiteSpace(memoryId) || !string.IsNullOrWhiteSpace(targetName);
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, character);
			NpcDataPacket npc = BuildNativeConversationNpcData(targetHero, character);
			if (npc != null && targetAgentIndex >= 0)
			{
				npc.AgentIndex = targetAgentIndex;
			}
			// 读档后原生自由对话的短期 session 表会清空；右上角历史必须回到同一个 af_nonhero 持久记忆 ID 读取。
			if (TryResolveWildernessNonHeroMemory(npc, targetHero, character, targetAgentIndex, out var nonHeroMemoryId, out var nonHeroMemoryName))
			{
				memoryId = nonHeroMemoryId;
				if (!string.IsNullOrWhiteSpace(nonHeroMemoryName))
				{
					targetName = nonHeroMemoryName.Trim();
				}
				Logger.Log("NativeConversationHistory", "persistent_target kind=nonhero memoryId=" + memoryId + " targetName=" + targetName + " agent=" + targetAgentIndex);
				return !string.IsNullOrWhiteSpace(memoryId);
			}
			return !string.IsNullOrWhiteSpace(targetName);
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationHistory", "[WARN] persistent target resolve failed: " + ex.Message);
			targetHero = null;
			targetName = "";
			memoryId = "";
			return false;
		}
	}

internal static bool TryGetNativeConversationLinkTargetForExternal(out Hero targetHero, out CharacterObject targetCharacter)
	{
		targetHero = null;
		targetCharacter = null;
		try
		{
			if (!TryResolveNativeConversationTarget(out var hero, out var character, out _))
			{
				return false;
			}
			targetHero = hero ?? character?.HeroObject;
			targetCharacter = character;
			return targetHero != null || targetCharacter != null;
		}
		catch
		{
			targetHero = null;
			targetCharacter = null;
			return false;
		}
	}

internal static bool TryGetNativeConversationHistoryTargetForExternal(out Hero targetHero, out string targetName)
	{
		targetHero = null;
		targetName = "";
		try
		{
			if (!TryResolveNativeConversationTarget(out var hero, out var character, out var npcName))
			{
				return false;
			}
			targetHero = hero ?? character?.HeroObject;
			targetName = (targetHero?.Name?.ToString() ?? character?.Name?.ToString() ?? npcName ?? "").Trim();
			return targetHero != null || !string.IsNullOrWhiteSpace(targetName);
		}
		catch
		{
			targetHero = null;
			targetName = "";
			return false;
		}
	}

internal static string FormatNativeConversationDisplayTextForExternal(string rawVisibleText, Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			return EncyclopediaEntityLinkFormatter.FormatNativeConversationText(rawVisibleText, targetHero, targetCharacter);
		}
		catch (Exception ex)
		{
			// A display-only failure must fall back to safe plain text and never invalidate a completed dialogue turn.
			Logger.LogTrace("NativeConversation", "[WARN] Could not format encyclopedia links for a visible reply: " + ex.Message);
			return EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(rawVisibleText);
		}
	}

internal static string FormatNativeConversationDisplayTextForExternal(string rawVisibleText)
	{
		try
		{
			TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var _);
			return FormatNativeConversationDisplayTextForExternal(rawVisibleText, targetHero, targetCharacter);
		}
		catch (Exception ex)
		{
			// A display-only failure must fall back to safe plain text and never invalidate a completed dialogue turn.
			Logger.LogTrace("NativeConversation", "[WARN] Could not format encyclopedia links for a visible reply: " + ex.Message);
			return EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(rawVisibleText);
		}
	}

internal static void LogShoutLorePrequery(string phase, Agent agent, CharacterObject character, string kingdomIdOverride, string inputText, string secondaryInput = null)
	{
		try
		{
			string source = ((character != null) ? "character" : "invalid");
			string agentIdx = ((agent != null) ? agent.Index.ToString() : "-1");
			string charId = (character?.StringId ?? "").Trim();
			string cultureId = (character?.Culture?.StringId ?? "neutral").Trim().ToLowerInvariant();
			string role = "commoner";
			try
			{
				if (character != null)
				{
					role = (character.IsSoldier ? "soldier" : character.Occupation.ToString().Trim().ToLowerInvariant());
				}
			}
			catch
			{
				role = "commoner";
			}
			string kingdom = (kingdomIdOverride ?? "").Trim().ToLowerInvariant();
			string traceId = DateTime.UtcNow.Ticks.ToString() + "_" + agentIdx;
			string text = (secondaryInput ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (text.Length > 72)
			{
				text = text.Substring(0, 72);
			}
			Logger.Log("LoreMatch", $"shout_lore_prequery phase={phase} traceId={traceId} source={source} agentIndex={agentIdx} charId={charId} culture={cultureId} kingdomOverride={kingdom} role={role} inputLen={(inputText ?? "").Length} npcRecall={(string.IsNullOrWhiteSpace(text) ? "off" : "on")} secondaryLen={text.Length}");
		}
		catch
		{
		}
	}

internal static string BuildPatienceBadgeForNpc(NpcDataPacket npc, Agent liveAgent)
	{
		if (npc == null)
		{
			return "";
		}
		if (npc.IsHero)
		{
			Hero hero = null;
			try
			{
				if (liveAgent != null && liveAgent.Character is CharacterObject { HeroObject: not null } characterObject)
				{
					hero = characterObject.HeroObject;
				}
			}
			catch
			{
			}
			if (hero == null)
			{
				try
				{
					hero = ResolveHeroFromAgentIndex(npc.AgentIndex);
				}
				catch
				{
				}
			}
			return MyBehavior.BuildScenePatienceBadgeForHeroExternal(hero);
		}
		return MyBehavior.BuildScenePatienceBadgeForUnnamedExternal(npc.UnnamedKey, npc.Name);
	}

internal static int GetShoutThoughtMinTokens()
{
	try
	{
		return Math.Max(40, DuelSettings.GetSettings()?.ShoutThoughtMinTokens ?? 200);
	}
	catch
	{
		return 200;
	}
}
internal static bool IsShoutInnerThoughtPromptDisabled()
{
	try
	{
		return DuelSettings.GetSettings()?.DisableShoutInnerThoughtPrompt == true;
	}
	catch
	{
		return false;
	}
}
internal static void GetSceneReplyLengthLimits(DuelSettings settings, out int minTokens, out int maxTokens)
{
	minTokens = Math.Max(1, settings?.ShoutMinTokens ?? DuelSettings.DefaultShoutMinTokens);
	maxTokens = Math.Max(1, settings?.ShoutMaxTokens ?? DuelSettings.DefaultShoutMaxTokens);
	if (maxTokens < minTokens)
	{
		maxTokens = minTokens;
	}
}
internal static bool HasGcczImmediatePromptExtras(string baseExtras)
{
	try
	{
		return !string.IsNullOrWhiteSpace(baseExtras) && AfGcczShoutBridge.IsActive() && AfGcczShoutBridge.HasInjectedRuleBlock(baseExtras);
	}
	catch
	{
		return false;
	}
}
internal static string BuildGcczImmediateIdentityOverrideBlock(Hero contextHero, CharacterObject npcCharacter, int targetAgentIndex, string baseExtras)
{
	if (!HasGcczImmediatePromptExtras(baseExtras))
	{
		return "";
	}
	try
	{
		return (AfGcczShoutBridge.BuildImmediateReactionIdentityOverride(contextHero, npcCharacter, targetAgentIndex) ?? "").Trim();
	}
	catch
	{
		return "";
	}
}

internal static bool TryResolveWildernessNonHeroRewardParty(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out PartyBase party)
	{
		party = null;
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null || targetCharacter == null)
			{
				return false;
			}
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return false;
			}
			PartyBase partyBase = null;
			if (targetAgentIndex >= 0)
			{
				partyBase = MyBehavior.ResolvePartyTransferCounterpartyForExternal(null, targetCharacter, targetAgentIndex);
			}
			if (partyBase == null && targetAgentIndex < 0 && ConversationActionBoundaryBannerlordAdapter.IsNativeConversationWorldMapContext())
			{
				MobileParty mobileParty = TryResolveWildernessNonHeroMobileParty(targetAgentIndex);
				partyBase = mobileParty?.Party;
			}
			if (partyBase == null || partyBase == PartyBase.MainParty || partyBase.MobileParty == null || partyBase.MobileParty == MobileParty.MainParty || partyBase.ItemRoster == null)
			{
				return false;
			}
			party = partyBase;
			return true;
		}
		catch
		{
			party = null;
			return false;
		}
	}
internal static string BuildWildernessNonHeroPartyRepresentativePrompt(int agentIndex)
	{
		try
		{
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return "";
			}
		}
		catch
		{
		}
		try
		{
			MobileParty party = TryResolveWildernessNonHeroMobileParty(agentIndex);
			if (party == null || party == MobileParty.MainParty || party.Party == PartyBase.MainParty)
			{
				return "";
			}
			string partyName = (party.Name?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(partyName))
			{
				partyName = (party.MapFaction?.Name?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			}
			if (string.IsNullOrWhiteSpace(partyName))
			{
				return "";
			}
			return "你正作为" + partyName + "的代表进行交涉。";
		}
		catch
		{
			return "";
		}
	}
internal static PartyBase ResolveWildernessNonHeroPartyBaseForPrompt(int agentIndex)
	{
		try
		{
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return null;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty party = TryResolveWildernessNonHeroMobileParty(agentIndex);
			if (party == null || party == MobileParty.MainParty || party.Party == PartyBase.MainParty)
			{
				return null;
			}
			return party.Party;
		}
		catch
		{
			return null;
		}
	}
internal static string BuildNativeConversationNpcListBlockForPrompt(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc = null)
	{
		try
		{
			List<NpcDataPacket> promptNpcs = FilterScenePresentNpcsForPrompt(presentNpcs, selfNpc);
			if (promptNpcs.Count == 0)
			{
				return "";
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("【站在你旁边的人】：");
			foreach (NpcDataPacket npc in promptNpcs)
			{
				if (npc != null)
				{
					sb.AppendLine(BuildSceneNpcListLineForPrompt(npc));
				}
			}
			string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(promptNpcs);
			if (!string.IsNullOrWhiteSpace(sceneNamingNote))
			{
				sb.AppendLine(sceneNamingNote);
			}
			return sb.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}
internal static string BuildNativeConversationNonHeroVoiceKey(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex)
	{
		try
		{
			if (targetHero != null || targetCharacter?.HeroObject != null || npc?.IsHero == true)
			{
				return "";
			}
			if (TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out var memoryId, out var _))
			{
				return memoryId;
			}
			string key = (npc?.UnnamedKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key))
			{
				key = SceneHistoryPromptCaptureAdapter.CaptureNativeConversationNonHeroUnnamedKey(targetCharacter, npc?.Name, agentIndex, TryResolveWildernessNonHeroMobileParty);
			}
			if (string.IsNullOrWhiteSpace(key))
			{
				key = SceneHistoryPromptCaptureAdapter.CaptureNativeConversationHistoryKey(null, targetCharacter, npc?.Name, agentIndex, npc);
			}
			string partyKey = BuildWildernessNonHeroPartyMemoryKey(agentIndex);
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(partyKey) && key.IndexOf("|party:", StringComparison.OrdinalIgnoreCase) < 0)
			{
				key = key + "|party:" + partyKey;
			}
			return (key ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}
internal static string BuildSceneNpcListLineForPrompt(NpcDataPacket npc)
	{ return ScenePromptMessageProjectionComposer.BuildSceneNpcListLineForPrompt(npc); }
internal static string BuildPlayerRelationIdentitySuffixForNpcListLine(Hero npcHero, string playerDisplayName)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (npcHero == null || mainHero == null || npcHero == mainHero)
			{
				return "";
			}
			Clan playerClan = Clan.PlayerClan ?? mainHero.Clan;
			string playerName = (playerDisplayName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = "玩家";
			}
			if (npcHero.IsPlayerCompanion || (playerClan != null && npcHero.CompanionOf == playerClan))
			{
				return "、" + playerName + "的同伴";
			}
			if (playerClan != null && npcHero.Clan == playerClan)
			{
				return "、" + playerName + "的家族成员";
			}
		}
		catch
		{
		}
		return "";
	}
internal static string BuildPlayerDisplayNameForSceneNpcListIdentity(NpcDataPacket currentNpc, Dictionary<int, Hero> resolvedHeroes)
	{
		Hero observerHero = null;
		try
		{
			if (currentNpc?.IsHero == true)
			{
				resolvedHeroes?.TryGetValue(currentNpc.AgentIndex, out observerHero);
				if (observerHero == null && currentNpc.AgentIndex >= 0)
				{
					observerHero = ResolveHeroFromAgentIndex(currentNpc.AgentIndex);
				}
			}
			if (ShouldForceDetailedPlayerIntroForObserver(observerHero) || DoesSceneObserverKnowPlayerIdentityForPrompt(observerHero, currentNpc))
			{
				string knownName = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(knownName))
				{
					return knownName;
				}
			}
			string publicName = (observerHero != null
				? PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(observerHero)
				: PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt()) ?? "";
			if (!string.IsNullOrWhiteSpace(publicName))
			{
				return publicName.Trim();
			}
		}
		catch
		{
		}
		return "玩家";
	}
internal static string BuildDistanceToCurrentNpcForNpcListLine(NpcDataPacket npc, NpcDataPacket currentNpc)
	{ return ScenePromptMessageProjectionComposer.BuildDistanceToCurrentNpcForNpcListLine(npc, currentNpc); }
internal static List<NpcDataPacket> FilterScenePresentNpcsForPrompt(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc)
	{
		List<NpcDataPacket> result = new List<NpcDataPacket>();
		foreach (NpcDataPacket npc in presentNpcs ?? Enumerable.Empty<NpcDataPacket>())
		{
			if (npc != null && !IsSameSceneNpcForPrompt(npc, selfNpc))
			{
				result.Add(npc);
			}
		}
		return result;
	}
internal static string BuildSceneNpcListLineWithRuntimeFactsForPrompt(NpcDataPacket npc, NpcDataPacket currentNpc, string playerDisplayName, Dictionary<int, Hero> resolvedHeroes = null, bool useAllegianceLabel = false)
	{
		string line = BuildSceneNpcListLineForPrompt(npc);
		if (npc == null)
		{
			return line;
		}
		try
		{
			if (!npc.IsHero && !IsInspectionPrisonerNpcForPrompt(npc))
			{
				string key = (npc.UnnamedKey ?? "").Trim().ToLower();
				string kingdomId = "";
				string lordId = "";
				int kIdx = key.IndexOf(":kingdom:", StringComparison.OrdinalIgnoreCase);
				if (kIdx >= 0)
				{
					kingdomId = key.Substring(kIdx + ":kingdom:".Length).Trim().ToLower();
					int cut = kingdomId.IndexOf(':');
					if (cut >= 0)
					{
						kingdomId = kingdomId.Substring(0, cut);
					}
				}
				int lIdx = key.IndexOf(":lord:", StringComparison.OrdinalIgnoreCase);
				if (lIdx >= 0)
				{
					lordId = key.Substring(lIdx + ":lord:".Length).Trim().ToLower();
					int cut2 = lordId.IndexOf(':');
					if (cut2 >= 0)
					{
						lordId = lordId.Substring(0, cut2);
					}
				}
				if (string.IsNullOrWhiteSpace(kingdomId))
				{
					kingdomId = (npc.CultureId ?? "").Trim().ToLower();
				}
				string kingdomName = kingdomId;
				string rulerName = "";
				try
				{
					Kingdom kObj = Kingdom.All?.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim().ToLower(), kingdomId, StringComparison.OrdinalIgnoreCase));
					if (kObj != null)
					{
						kingdomName = (kObj.Name?.ToString() ?? kingdomName).Trim();
						rulerName = (kObj.Leader?.Name?.ToString() ?? "").Trim();
					}
				}
				catch
				{
				}
				if (!string.IsNullOrWhiteSpace(kingdomName))
				{
					line = line + " | 势力: " + kingdomName;
				}
				if (!string.IsNullOrWhiteSpace(rulerName))
				{
					line = line + " | " + (useAllegianceLabel ? "效忠于" : "统治者") + ": " + rulerName;
				}
				if (string.IsNullOrWhiteSpace(lordId))
				{
					try
					{
						lordId = (Settlement.CurrentSettlement?.OwnerClan?.Leader?.StringId ?? "").Trim().ToLower();
					}
					catch
					{
						lordId = "";
					}
				}
				if (!string.IsNullOrWhiteSpace(lordId))
				{
					string lordName;
					try
					{
						lordName = (Hero.Find(lordId)?.Name?.ToString() ?? "").Trim();
					}
					catch
					{
						lordName = "";
					}
					if (!string.IsNullOrWhiteSpace(lordName))
					{
						line = line + " | 隶属领主: " + lordName;
					}
				}
			}
		}
		catch
		{
		}
		if (npc.IsHero)
		{
			try
			{
				Hero hero = null;
				if (resolvedHeroes != null)
				{
					resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
				}
				if (hero == null && npc.AgentIndex >= 0)
				{
					hero = ResolveHeroFromAgentIndex(npc.AgentIndex);
				}
				line += BuildPlayerRelationIdentitySuffixForNpcListLine(hero, playerDisplayName);
				if (hero?.IsPrisoner ?? false)
				{
					string captor = hero.PartyBelongedToAsPrisoner?.LeaderHero?.Name?.ToString();
					line += ((!string.IsNullOrEmpty(captor)) ? (" | 状态: 囚犯（被" + captor + "关押）") : " | 状态: 囚犯");
				}
				line += SceneHistoryPromptCaptureAdapter.BuildPlayerMarriageFactForNpcListLine(hero);
			}
			catch
			{
			}
		}
		line += BuildDistanceToCurrentNpcForNpcListLine(npc, currentNpc);
		return line;
	}
internal static string BuildScenePresentNpcListBlockForPrompt(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc, Dictionary<int, Hero> resolvedHeroes = null, bool useAllegianceLabel = false)
	{
		try
		{
			List<NpcDataPacket> promptNpcs = FilterScenePresentNpcsForPrompt(presentNpcs, selfNpc);
			if (promptNpcs.Count == 0)
			{
				return "";
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("【站在你旁边的人】：");
			string playerDisplayName = BuildPlayerDisplayNameForSceneNpcListIdentity(selfNpc, resolvedHeroes);
			foreach (NpcDataPacket npc in promptNpcs)
			{
				sb.AppendLine(BuildSceneNpcListLineWithRuntimeFactsForPrompt(npc, selfNpc, playerDisplayName, resolvedHeroes, useAllegianceLabel));
			}
			string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(promptNpcs);
			if (!string.IsNullOrWhiteSpace(sceneNamingNote))
			{
				sb.AppendLine(sceneNamingNote);
			}
			return sb.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}
internal static string BuildSceneNonHeroNamingNoteForPrompt(IEnumerable<NpcDataPacket> npcs)
	{ return ScenePromptMessageProjectionComposer.BuildSceneNonHeroNamingNoteForPrompt(npcs); }

    internal delegate void PersonaReader(Hero hero, out string personality, out string background);
    private readonly PersonaReader _readPersona;
    internal SceneAgentIdentityPromptCaptureAdapter(PersonaReader readPersona)
    { _readPersona = readPersona ?? throw new ArgumentNullException(nameof(readPersona)); }
	internal static bool IsUsableNativeConversationFallbackAgent(Agent agent, Hero targetHero, CharacterObject targetCharacter) => SceneShoutInputController.IsUsableNativeConversationFallbackAgent(agent, targetHero, targetCharacter);

	internal static bool IsValidNativeConversationTargetAgent(Agent agent, Hero targetHero, CharacterObject targetCharacter) => SceneShoutInputController.IsValidNativeConversationTargetAgent(agent, targetHero, targetCharacter);

	internal static int TryResolveNativeConversationAgentIndex(Hero targetHero, CharacterObject targetCharacter) => SceneShoutInputController.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);

	internal static float ResolveNativeConversationNonHeroAge(CharacterObject targetCharacter)
	{
		float age = 0f;
		try
		{
			age = targetCharacter?.Age ?? 0f;
		}
		catch
		{
			age = 0f;
		}
		if (age >= 18f && age <= 55f)
		{
			return age;
		}
		float fallbackAge = 30f;
		try
		{
			if (targetCharacter != null && !targetCharacter.IsSoldier)
			{
				switch (targetCharacter.Occupation)
				{
				case Occupation.Weaponsmith:
				case Occupation.Blacksmith:
				case Occupation.Armorer:
				case Occupation.GoodsTrader:
				case Occupation.HorseTrader:
				case Occupation.Artisan:
				case Occupation.Merchant:
					fallbackAge = 38f;
					break;
				case Occupation.Headman:
				case Occupation.Preacher:
				case Occupation.GangLeader:
				case Occupation.RuralNotable:
					fallbackAge = 46f;
					break;
				default:
					fallbackAge = 30f;
					break;
				}
			}
		}
		catch
		{
			fallbackAge = 30f;
		}
		return Math.Max(18f, Math.Min(55f, fallbackAge));
	}

	internal NpcDataPacket BuildNativeConversationNpcData(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			int agentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			var agents = Mission.Current?.Agents;
			if (agentIndex >= 0 && agents != null)
			{
				Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
				NpcDataPacket sceneNpc = ShoutUtils.ExtractNpcData(agent);
				if (sceneNpc != null)
				{
					sceneNpc.AgentIndex = agentIndex;
					return sceneNpc;
				}
			}
		}
		catch
		{
		}
		string npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
		string role = "";
		string personality = "";
		string background = "";
		try
		{
			role = targetHero != null ? PersonaIdentityPromptCaptureAdapter.CaptureHeroIdentityTitle(targetHero) : (targetCharacter?.Occupation.ToString() ?? "");
		}
		catch
		{
			role = "";
		}
		if (targetHero != null)
		{
			try
			{
				_readPersona(targetHero, out personality, out background);
			}
			catch
			{
				personality = "";
				background = "";
			}
		}
		Hero npcHero = targetHero ?? targetCharacter?.HeroObject;
		bool isNonHero = npcHero == null;
		string troopId = targetCharacter?.StringId ?? targetHero?.CharacterObject?.StringId ?? "";
		int unnamedAgentIndex = isNonHero ? TryResolveNativeConversationAgentIndex(targetHero, targetCharacter) : -1;
		string unnamedKey = isNonHero ? SceneHistoryPromptCaptureAdapter.CaptureNativeConversationNonHeroUnnamedKey(targetCharacter, npcName, unnamedAgentIndex, TryResolveWildernessNonHeroMobileParty) : "";
		string unnamedRank = "";
		if (isNonHero)
		{
			try
			{
				unnamedRank = targetCharacter != null && targetCharacter.IsSoldier ? "soldier" : "commoner";
			}
			catch
			{
				unnamedRank = "";
			}
		}
		float age = npcHero?.Age ?? (isNonHero ? ResolveNativeConversationNonHeroAge(targetCharacter) : 0f);
		return new NpcDataPacket
		{
			Name = npcName,
			RoleDesc = string.IsNullOrWhiteSpace(role) ? "原版对话对象" : role,
			PersonalityDesc = (personality ?? "").Trim(),
			BackgroundDesc = (background ?? "").Trim(),
			AgentIndex = -1,
			IsHero = !isNonHero,
			CultureId = targetHero?.Culture?.StringId ?? targetCharacter?.Culture?.StringId ?? "neutral",
			UnnamedKey = unnamedKey,
			TroopId = troopId,
			UnnamedRank = unnamedRank,
			IsFemale = targetHero?.IsFemale ?? targetCharacter?.IsFemale ?? false,
			Age = age,
			PromptGivenName = npcName,
			PromptDisplayName = npcName
		};
	}

	internal static string GetSceneNpcHistoryNameForPrompt(NpcDataPacket npc)
	{
		string text = (ShoutUtils.GetPromptHistoryName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? ConversationActionPostprocessOwner.GetSceneNpcIdentityNameForPrompt(npc) : text;
	}
    internal static bool CaptureEncounterMeetingActive()
    {
        try { return LordEncounterBehavior.IsEncounterMeetingMissionActive; }
        catch { return false; }
    }
	internal static MobileParty TryResolveWildernessNonHeroMobileParty(int agentIndex)
	{
		try
		{
			Agent agent = (agentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive()) : null;
			PartyBase partyBase = agent?.Origin?.BattleCombatant as PartyBase;
			if (partyBase != null && partyBase.IsMobile && partyBase.MobileParty != null)
			{
				return partyBase.MobileParty;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			if (encounteredParty != null && encounteredParty.IsMobile && encounteredParty.MobileParty != null)
			{
				return encounteredParty.MobileParty;
			}
		}
		catch
		{
		}
		return null;
	}

	internal static Hero ResolveHeroFromAgentIndex(int agentIndex)
	{
		try
		{
			BasicCharacterObject basicCharacterObject = (Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex))?.Character;
			if (basicCharacterObject == null || !basicCharacterObject.IsHero)
			{
				return null;
			}
			return (basicCharacterObject is CharacterObject characterObject) ? characterObject.HeroObject : null;
		}
		catch
		{
			return null;
		}
	}
    internal static (bool InnerThoughtDisabled, int ThoughtMinTokens) CaptureSceneReplyTaskSettings()
    {
        try
        {
            var settings=DuelSettings.GetSettings();
            return (settings?.DisableShoutInnerThoughtPrompt == true, Math.Max(40, settings?.ShoutThoughtMinTokens ?? 200));
        }
        catch { return (false,200); }
    }
	internal static bool IsWildernessNonHeroMemoryScope(int agentIndex)
	{
		try
		{
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return false;
			}
		}
		catch
		{
		}
		return TryResolveWildernessNonHeroMobileParty(agentIndex) != null;
	}

	internal static string BuildWildernessNonHeroPartyTrace(MobileParty party)
	{
		try
		{
			if (party == null)
			{
				return "party=null";
			}
			PartyBase partyBase = party.Party;
			return "partyStringId=" + (party.StringId ?? "")
				+ " partyName=" + ((party.Name?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim())
				+ " partyIndex=" + (partyBase?.Index ?? -1)
				+ " mapFaction=" + (party.MapFaction?.StringId ?? "")
				+ " leader=" + (party.LeaderHero?.StringId ?? "");
		}
		catch
		{
			return "party=trace_failed";
		}
	}

	internal static string BuildWildernessNonHeroPartyMemoryKey(int agentIndex)
	{
		try
		{
			MobileParty party = TryResolveWildernessNonHeroMobileParty(agentIndex);
			string partyStringId = NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(party?.StringId);
			if (!string.IsNullOrWhiteSpace(partyStringId))
			{
				// MobileParty.StringId 是 Bannerlord 为每支 MobileParty 分配并随存档保存的唯一对象 ID。
				// 只用 StringId，不用显示名称；同名劫匪/商队可以同名，但 StringId 不同。
				LogNonHeroMemoryTrace("stage=party_key ok=1 mode=party_string_id agent=" + agentIndex + " key=party_string_id:" + partyStringId + " " + BuildWildernessNonHeroPartyTrace(party));
				return "party_string_id:" + partyStringId;
			}
			string savedPartyKey = MyBehavior.GetOrCreateWildernessNonHeroPartyMemoryKeyForExternal(party);
			if (!string.IsNullOrWhiteSpace(savedPartyKey))
			{
				// 备用路径：如果某些非原版部队没有 StringId，再使用本模组存档内 GUID。
				LogNonHeroMemoryTrace("stage=party_key ok=1 mode=party_guid agent=" + agentIndex + " key=" + savedPartyKey + " " + BuildWildernessNonHeroPartyTrace(party));
				return savedPartyKey;
			}
			PartyBase partyBase = party?.Party;
			if (partyBase != null && partyBase.Index >= 0)
			{
				// 仅作为极端 fallback。party_index 不能作为持久记忆主键，读档后可能查不到旧记忆。
				LogNonHeroMemoryTrace("stage=party_key ok=1 mode=party_index agent=" + agentIndex + " key=party_index:" + partyBase.Index + " " + BuildWildernessNonHeroPartyTrace(party));
				return "party_index:" + partyBase.Index;
			}
			// 不要退回显示名称：刷新的劫匪、商队经常同名，会再次共享记忆。
			LogNonHeroMemoryTrace("stage=party_key ok=0 reason=no_stable_party_key agent=" + agentIndex + " " + BuildWildernessNonHeroPartyTrace(party));
			return "";
		}
		catch (Exception ex)
		{
			LogNonHeroMemoryTrace("stage=party_key ok=0 reason=exception agent=" + agentIndex + " error=" + ex.Message);
			return "";
		}
	}

	internal static string BuildWildernessNonHeroCharacterMemoryKey(CharacterObject character, MobileParty party)
	{
		string troopKey = NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(character?.StringId);
		if (string.IsNullOrWhiteSpace(troopKey))
		{
			return "";
		}
		string key = "troop:" + troopKey;
		string factionKey = NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(party?.MapFaction?.StringId);
		if (string.IsNullOrWhiteSpace(factionKey))
		{
			factionKey = NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(character?.Culture?.StringId);
		}
		if (!string.IsNullOrWhiteSpace(factionKey))
		{
			key += ":kingdom:" + factionKey;
		}
		string leaderKey = NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(party?.LeaderHero?.StringId);
		if (!string.IsNullOrWhiteSpace(leaderKey))
		{
			key += ":lord:" + leaderKey;
		}
		return key;
	}

	internal static bool TryResolveWildernessNonHeroMemory(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, out string memoryId, out string memoryName)
	{
		memoryId = "";
		memoryName = "";
		try
		{
			if (targetHero != null || targetCharacter?.HeroObject != null || npc?.IsHero == true)
			{
				return false;
			}
			if (!IsWildernessNonHeroMemoryScope(agentIndex))
			{
				LogNonHeroMemoryTrace("stage=resolve ok=0 reason=not_wilderness_scope agent=" + agentIndex);
				return false;
			}
			MobileParty party = TryResolveWildernessNonHeroMobileParty(agentIndex);
			NpcDataPacket data = npc;
			CharacterObject character = targetCharacter;
			if ((data == null || string.IsNullOrWhiteSpace(data.UnnamedKey)) && agentIndex >= 0)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
				data = ShoutUtils.ExtractNpcData(agent) ?? data;
				character ??= agent?.Character as CharacterObject;
			}
			string key = (data?.UnnamedKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key))
			{
				key = BuildWildernessNonHeroCharacterMemoryKey(character, party);
			}
			if (string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(data?.TroopId))
			{
				key = "troop:" + NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(data.TroopId);
			}
			if (string.IsNullOrWhiteSpace(key))
			{
				LogNonHeroMemoryTrace("stage=resolve ok=0 reason=no_base_key agent=" + agentIndex + " troop=" + (character?.StringId ?? data?.TroopId ?? "") + " " + BuildWildernessNonHeroPartyTrace(party));
				return false;
			}
			string partyKey = BuildWildernessNonHeroPartyMemoryKey(agentIndex);
			if (string.IsNullOrWhiteSpace(partyKey))
			{
				LogNonHeroMemoryTrace("stage=resolve ok=0 reason=no_party_key agent=" + agentIndex + " baseKey=" + NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(key) + " " + BuildWildernessNonHeroPartyTrace(party));
				return false;
			}
			key = key + "|party:" + partyKey;
			memoryId = MyBehavior.BuildNonHeroMemoryIdForExternal(key);
			if (string.IsNullOrWhiteSpace(memoryId))
			{
				LogNonHeroMemoryTrace("stage=resolve ok=0 reason=no_memory_id agent=" + agentIndex + " baseKey=" + NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(key) + " partyKey=" + partyKey + " " + BuildWildernessNonHeroPartyTrace(party));
				return false;
			}
			// 读档稳定性以 party_string_id 为准；这里仅把同一支部队的旧 GUID/未标注 StringId 记录迁到新 key。
			// 不迁移显示名称或兵种共享 key，避免同名劫匪、商队再次合并记忆。
			MyBehavior.MigrateNonHeroPartyScopedMemoryForExternal(memoryId, partyKey);
			string oldGuidPartyKey = MyBehavior.GetExistingWildernessNonHeroPartyMemoryKeyForExternal(party);
			if (!string.IsNullOrWhiteSpace(oldGuidPartyKey) && !string.Equals(oldGuidPartyKey, partyKey, StringComparison.OrdinalIgnoreCase))
			{
				MyBehavior.MigrateNonHeroPartyScopedMemoryForExternal(memoryId, oldGuidPartyKey);
			}
			memoryName = (data != null ? GetSceneNpcHistoryNameForPrompt(data) : "").Trim();
			if (string.IsNullOrWhiteSpace(memoryName))
			{
				memoryName = (targetCharacter?.Name?.ToString() ?? data?.Name ?? "NPC").Trim();
			}
			if (string.IsNullOrWhiteSpace(memoryName))
			{
				memoryName = "NPC";
			}
			LogNonHeroMemoryTrace("stage=resolve ok=1 agent=" + agentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " baseKey=" + NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(key) + " partyKey=" + partyKey + " troop=" + (character?.StringId ?? data?.TroopId ?? "") + " oldGuidKey=" + (oldGuidPartyKey ?? "") + " " + BuildWildernessNonHeroPartyTrace(party));
			return true;
		}
		catch (Exception ex)
		{
			LogNonHeroMemoryTrace("stage=resolve ok=0 reason=exception agent=" + agentIndex + " error=" + ex.Message);
			memoryId = "";
			memoryName = "";
			return false;
		}
	}

	internal static void LogNonHeroMemoryTrace(string message)
	{
		try
		{
			if (Logger.IsVerboseModLogicEnabled)
			{
				Logger.Log("Logic", "[NonHeroMemoryTrace] " + (message ?? ""));
			}
		}
		catch
		{
		}
	}
	internal static string ResolveSceneHeroIdFromAgentIndex(int agentIndex)
	{
		var agents = Mission.Current?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			return "";
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			CharacterObject characterObject = agent?.Character as CharacterObject;
			return SceneHistoryProjectionOwner.NormalizeSceneHeroId(characterObject?.HeroObject?.StringId);
		}
		catch
		{
			return "";
		}
	}

	internal static void FillSceneMessageHeroIdentity(ConversationMessage message)
	{
		if (message == null)
		{
			return;
		}
		message.SpeakerHeroId = SceneHistoryProjectionOwner.NormalizeSceneHeroId(message.SpeakerHeroId);
		if (string.IsNullOrWhiteSpace(message.SpeakerHeroId) && message.SpeakerAgentIndex >= 0)
		{
			message.SpeakerHeroId = ResolveSceneHeroIdFromAgentIndex(message.SpeakerAgentIndex);
		}
		message.TargetHeroId = SceneHistoryProjectionOwner.NormalizeSceneHeroId(message.TargetHeroId);
		if (string.IsNullOrWhiteSpace(message.TargetHeroId) && message.TargetAgentIndex >= 0)
		{
			message.TargetHeroId = ResolveSceneHeroIdFromAgentIndex(message.TargetAgentIndex);
		}
		if (message.VisibleAgentIndices == null)
		{
			message.VisibleAgentIndices = new List<int>();
		}
		List<string> visibleHeroIds = new List<string>();
		HashSet<string> seenHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (message.VisibleHeroIds != null)
		{
			foreach (string heroId in message.VisibleHeroIds)
			{
				string normalizedHeroId = SceneHistoryProjectionOwner.NormalizeSceneHeroId(heroId);
				if (!string.IsNullOrWhiteSpace(normalizedHeroId) && seenHeroIds.Add(normalizedHeroId))
				{
					visibleHeroIds.Add(normalizedHeroId);
				}
			}
		}
		foreach (int visibleAgentIndex in message.VisibleAgentIndices)
		{
			string visibleHeroId = ResolveSceneHeroIdFromAgentIndex(visibleAgentIndex);
			if (!string.IsNullOrWhiteSpace(visibleHeroId) && seenHeroIds.Add(visibleHeroId))
			{
				visibleHeroIds.Add(visibleHeroId);
			}
		}
		message.VisibleHeroIds = visibleHeroIds;
	}
	internal static float GetPlayerDistanceToAgentForScenePrompt(int agentIndex)
	{
		var agents = Mission.Current?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			return -1f;
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			return SceneShoutInputController.TryGetPlayerPlanarDistanceMeters(agent, out var distanceMeters) ? distanceMeters : (-1f);
		}
		catch
		{
			return -1f;
		}
	}
	internal static string ResolveSceneHistorySpeakerNameForPrompt(int speakerAgentIndex, string fallbackSpeakerName, IEnumerable<NpcDataPacket> nearbyData = null)
	{
		NpcDataPacket npcDataPacket = nearbyData?.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == speakerAgentIndex);
		string text = (npcDataPacket != null) ? GetSceneNpcHistoryNameForPrompt(npcDataPacket) : ((fallbackSpeakerName ?? "").Trim());
		return string.IsNullOrWhiteSpace(text) ? "某NPC" : text;
	}
	internal static string ResolveSceneTargetNameForPrompt(int targetAgentIndex, string fallbackTargetName, IEnumerable<NpcDataPacket> nearbyData = null)
	{
		NpcDataPacket npcDataPacket = nearbyData?.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == targetAgentIndex);
		string text = (npcDataPacket != null) ? GetSceneNpcHistoryNameForPrompt(npcDataPacket) : ((fallbackTargetName ?? "").Trim());
		return string.IsNullOrWhiteSpace(text) ? (fallbackTargetName ?? "").Trim() : text;
	}
	internal static bool TryResolveNativeConversationTarget(out Hero targetHero, out CharacterObject targetCharacter, out string npcName)
	{
		targetHero = null;
		targetCharacter = null;
		npcName = "";
		try
		{
			targetHero = Hero.OneToOneConversationHero;
		}
		catch
		{
			targetHero = null;
		}
		try
		{
			targetCharacter = CharacterObject.OneToOneConversationCharacter;
		}
		catch
		{
			targetCharacter = null;
		}
		if (targetHero == null)
		{
			targetHero = targetCharacter?.HeroObject;
		}
		if ((targetHero == null && targetCharacter == null) || IsNativeConversationSelfTarget(targetHero, targetCharacter))
		{
			if (TryResolveNativeConversationTargetFromAgent(out var agentHero, out var agentCharacter, out var agentName))
			{
				targetHero = agentHero;
				targetCharacter = agentCharacter;
				npcName = agentName;
			}
		}
		if (targetHero == null)
		{
			try
			{
				targetHero = PlayerEncounter.EncounteredParty?.LeaderHero;
			}
			catch
			{
				targetHero = null;
			}
		}
		if (targetCharacter == null)
		{
			targetCharacter = targetHero?.CharacterObject;
		}
		if (targetCharacter == null)
		{
			try
			{
				targetCharacter = TaleWorlds.CampaignSystem.Conversation.ConversationHelper.GetConversationCharacterPartyLeader(PlayerEncounter.EncounteredParty);
			}
			catch
			{
				targetCharacter = null;
			}
		}
		if (targetHero == null)
		{
			targetHero = targetCharacter?.HeroObject;
		}
		if (targetHero == null && TryResolveNativeConversationTargetFromAgent(out var directAgentHero, out var directAgentCharacter, out var directAgentName) && !IsNativeConversationSelfTarget(directAgentHero, directAgentCharacter))
		{
			targetHero = directAgentHero;
			targetCharacter = directAgentCharacter;
			npcName = directAgentName;
		}
		if (IsNativeConversationSelfTarget(targetHero, targetCharacter))
		{
			if (TryResolveNativeConversationTargetFromAgent(out var agentHero, out var agentCharacter, out var agentName) && !IsNativeConversationSelfTarget(agentHero, agentCharacter))
			{
				targetHero = agentHero;
				targetCharacter = agentCharacter;
				npcName = agentName;
			}
			else
			{
				targetHero = null;
				targetCharacter = null;
				npcName = "";
				return false;
			}
		}
		npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
		return targetHero != null || targetCharacter != null;
	}
	internal static bool IsNativeConversationSelfTarget(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			if (targetHero != null && targetHero == Hero.MainHero)
			{
				return true;
			}
			CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? Hero.MainHero?.CharacterObject;
			return targetCharacter != null && playerCharacter != null && targetCharacter == playerCharacter;
		}
		catch
		{
			return false;
		}
	}
	internal static bool TryResolveNativeConversationTargetFromAgent(out Hero targetHero, out CharacterObject targetCharacter, out string npcName)
	{
		targetHero = null;
		targetCharacter = null;
		npcName = "";
		try
		{
			Agent agent = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
			if (agent == null || agent.IsMainAgent || agent.Character == null)
			{
				return false;
			}
			targetCharacter = agent.Character as CharacterObject;
			targetHero = targetCharacter?.HeroObject;
			if (IsNativeConversationSelfTarget(targetHero, targetCharacter))
			{
				targetHero = null;
				targetCharacter = null;
				return false;
			}
			npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? agent.Name ?? "").Trim();
			return targetHero != null || targetCharacter != null;
		}
		catch
		{
			targetHero = null;
			targetCharacter = null;
			npcName = "";
			return false;
		}
	}

internal static string TryGetKingdomIdOverrideFromAgent(Agent agent)
	{
		string text = null;
		try
		{
			text = (((!(agent?.Origin?.BattleCombatant is PartyBase partyBase)) ? null : partyBase.MapFaction?.StringId) ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				text = (currentSettlement?.OwnerClan?.Kingdom?.StringId ?? currentSettlement?.MapFaction?.StringId ?? "").Trim().ToLower();
				if (!string.IsNullOrEmpty(text))
				{
					Logger.Log("LoreMatch", "kingdomIdOverride_from_settlement settlement=" + currentSettlement?.StringId + " mapFaction=" + currentSettlement?.MapFaction?.StringId + " -> " + text);
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				Logger.Log("LoreMatch", "kingdomIdOverride_missing");
				text = null;
			}
		}
		catch
		{
			text = null;
		}
		return text;
	}

internal static string BuildNpcCurrentMountLineForPrompt(NpcDataPacket npc)
	{
		try
		{
			Agent agent = ResolveSceneAgentForMountPrompt(npc?.AgentIndex ?? -1);
			return TryGetLiveRiddenMountNameForPrompt(agent, out var mountName) ? ("【当前坐骑】你骑着：" + mountName + "。") : "";
		}
		catch
		{
			return "";
		}
	}
internal static string BuildPlayerCurrentMountLineForPrompt()
	{
		try
		{
			return TryGetLiveRiddenMountNameForPrompt(Agent.Main, out var mountName) ? ("【当前坐骑】面前此人骑着：" + mountName + "。") : "";
		}
		catch
		{
			return "";
		}
	}
internal static Agent ResolveSceneAgentForMountPrompt(int agentIndex)
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (agentIndex < 0 || agents == null)
			{
				return null;
			}
			return agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
		}
		catch
		{
			return null;
		}
	}
internal static bool TryGetLiveRiddenMountNameForPrompt(Agent riderAgent, out string mountName)
	{
		mountName = "";
		try
		{
			if (riderAgent == null || !riderAgent.IsActive())
			{
				return false;
			}
			Agent mountAgent = riderAgent.MountAgent;
			if (mountAgent == null || !mountAgent.IsActive())
			{
				return false;
			}
			if (!TryGetMountEquipmentNameForPrompt(mountAgent, out mountName))
			{
				TryGetMountEquipmentNameForPrompt(riderAgent, out mountName);
			}
			if (string.IsNullOrWhiteSpace(mountName))
			{
				mountName = NormalizeInlinePromptText(mountAgent.Name?.ToString());
			}
			return !string.IsNullOrWhiteSpace(mountName);
		}
		catch
		{
			mountName = "";
			return false;
		}
	}
internal static bool TryGetMountEquipmentNameForPrompt(Agent agent, out string mountName)
	{
		mountName = "";
		try
		{
			Equipment equipment = agent?.SpawnEquipment;
			if (equipment == null)
			{
				return false;
			}
			EquipmentElement mountElement = equipment[EquipmentIndex.ArmorItemEndSlot];
			if (mountElement.IsEmpty || mountElement.Item == null || mountElement.Item.HorseComponent == null)
			{
				return false;
			}
			mountName = NormalizeInlinePromptText(mountElement.GetModifiedItemName()?.ToString());
			if (string.IsNullOrWhiteSpace(mountName))
			{
				mountName = NormalizeInlinePromptText(mountElement.Item.Name?.ToString());
			}
			if (string.IsNullOrWhiteSpace(mountName))
			{
				mountName = NormalizeInlinePromptText(mountElement.Item.StringId);
			}
			return !string.IsNullOrWhiteSpace(mountName);
		}
		catch
		{
			mountName = "";
			return false;
		}
	}
internal static string NormalizeInlinePromptText(string value)
	{
		return Regex.Replace((value ?? "").Replace("\r", " ").Replace("\n", " "), "[ \\t]{2,}", " ").Trim();
	}

internal static bool IsSceneAgentPromptActive(Agent agent)
	{
		try
		{
			return agent != null && agent.IsActive() && agent.State == AgentState.Active && agent.Health > 0f
				&& !RichExecutions.Core.VengeanceIntegration.IsExecutedVictim(agent);
		}
		catch
		{
			try
			{
				return agent != null && agent.IsActive();
			}
			catch
			{
				return false;
			}
		}
	}
internal static bool IsSceneAgentSittingForPrompt(Agent agent)
	{
		try
		{
			return agent != null && agent.IsSitting();
		}
		catch
		{
			return false;
		}
	}
internal static bool IsNativeConversationTargetForActionPrompt(NpcDataPacket npc, Hero hero)
	{
		try
		{
			if (npc == null || Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
			if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var targetName))
			{
				return false;
			}
			if (hero != null && (targetHero == hero || targetCharacter?.HeroObject == hero))
			{
				return true;
			}
			if (npc.AgentIndex >= 0 && TryResolveNativeConversationAgentIndex(targetHero, targetCharacter) == npc.AgentIndex)
			{
				return true;
			}
			string npcName = ConversationActionPostprocessOwner.GetSceneNpcIdentityNameForPrompt(npc);
			string resolvedName = (targetName ?? targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
			return !string.IsNullOrWhiteSpace(npcName) && !string.IsNullOrWhiteSpace(resolvedName) && string.Equals(npcName.Trim(), resolvedName, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}
internal static bool IsSceneAgentUsingObjectForPrompt(Agent agent)
	{
		try
		{
			return agent != null && (agent.IsUsingGameObject || agent.CurrentlyUsedGameObject != null);
		}
		catch
		{
			return false;
		}
	}
internal static bool IsSceneAgentPlayingMusicForPrompt(Agent agent, string actionSearchText)
	{
		try
		{
			if (agent?.CurrentlyUsedGameObject is PlayMusicPoint)
			{
				return true;
			}
		}
		catch
		{
		}
		return ContainsSceneActionKeyword(actionSearchText, "playmusic", "play_music", "music", "musician", "instrument", "lute", "lyre", "drum", "flute", "horn", "演奏", "乐器", "音乐");
	}
internal static bool IsSceneAgentMovingForPrompt(Agent agent, string actionSearchText, Agent.ActionCodeType lowerBodyAction)
	{
		try
		{
			if (agent != null && agent.GetCurrentVelocity().LengthSquared > 0.01f)
			{
				return true;
			}
		}
		catch
		{
		}
		return lowerBodyAction == Agent.ActionCodeType.Dash
			|| lowerBodyAction == Agent.ActionCodeType.JumpStart
			|| lowerBodyAction == Agent.ActionCodeType.Jump
			|| lowerBodyAction == Agent.ActionCodeType.JumpEnd
			|| lowerBodyAction == Agent.ActionCodeType.Mount
			|| lowerBodyAction == Agent.ActionCodeType.Dismount
			|| ContainsSceneActionKeyword(actionSearchText, "walkingbehavior", "patrol", "escortagentbehavior", "followagentbehavior", "goto", "move");
	}
internal static bool IsSceneAgentWaitingForPrompt(Agent agent, string actionSearchText, Agent.ActionCodeType lowerBodyAction, Agent.ActionCodeType upperBodyAction)
	{
		try
		{
			if (agent == null || agent.GetCurrentVelocity().LengthSquared > 0.01f)
			{
				return false;
			}
		}
		catch
		{
		}
		return lowerBodyAction == Agent.ActionCodeType.Idle
			|| lowerBodyAction == Agent.ActionCodeType.Guard
			|| upperBodyAction == Agent.ActionCodeType.Idle
			|| ContainsSceneActionKeyword(actionSearchText, "idleagentbehavior", "wait");
	}
internal static bool IsSceneAgentDownOrHitForPrompt(Agent agent, Agent.ActionCodeType lowerBodyAction, Agent.ActionCodeType upperBodyAction)
	{
		try
		{
			if (agent != null && agent.IsInBeingStruckAction)
			{
				return true;
			}
		}
		catch
		{
		}
		return lowerBodyAction == Agent.ActionCodeType.Fall
			|| upperBodyAction == Agent.ActionCodeType.Fall
			|| IsSceneAgentStrikeAction(lowerBodyAction)
			|| IsSceneAgentStrikeAction(upperBodyAction);
	}
internal static bool IsSceneAgentAttackAction(Agent.ActionCodeType actionType)
	{
		return actionType == Agent.ActionCodeType.ReadyRanged
			|| actionType == Agent.ActionCodeType.ReleaseRanged
			|| actionType == Agent.ActionCodeType.ReleaseThrowing
			|| actionType == Agent.ActionCodeType.ReadyMelee
			|| actionType == Agent.ActionCodeType.ReleaseMelee
			|| actionType == Agent.ActionCodeType.Kick
			|| actionType == Agent.ActionCodeType.KickContinue
			|| actionType == Agent.ActionCodeType.KickHit
			|| actionType == Agent.ActionCodeType.WeaponBash
			|| actionType == Agent.ActionCodeType.HitObject;
	}
internal static bool IsSceneAgentDefenseAction(Agent.ActionCodeType actionType)
	{
		int value = (int)actionType;
		return (value >= (int)Agent.ActionCodeType.DefendFist && value <= (int)Agent.ActionCodeType.DefendLeftStaff)
			|| actionType == Agent.ActionCodeType.ParriedMelee
			|| actionType == Agent.ActionCodeType.BlockedMelee
			|| actionType == Agent.ActionCodeType.Guard;
	}
internal static bool IsSceneAgentStrikeAction(Agent.ActionCodeType actionType)
	{
		return actionType == Agent.ActionCodeType.StrikeLight
			|| actionType == Agent.ActionCodeType.StrikeMedium
			|| actionType == Agent.ActionCodeType.StrikeHeavy
			|| actionType == Agent.ActionCodeType.StrikeKnockBack
			|| actionType == Agent.ActionCodeType.MountStrike;
	}
internal static Agent.ActionCodeType GetSceneAgentCurrentActionType(Agent agent, int channelNo)
	{
		try
		{
			return agent != null ? agent.GetCurrentActionType(channelNo) : Agent.ActionCodeType.Other;
		}
		catch
		{
			return Agent.ActionCodeType.Other;
		}
	}
internal static string BuildSceneAgentActionSearchText(Agent agent)
	{
		if (agent == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		AppendSceneAgentActionSearchPart(stringBuilder, GetSceneAgentCurrentActionType(agent, 0).ToString());
		AppendSceneAgentActionSearchPart(stringBuilder, GetSceneAgentCurrentActionType(agent, 1).ToString());
		AppendSceneAgentCurrentAnimationName(stringBuilder, agent, 0);
		AppendSceneAgentCurrentAnimationName(stringBuilder, agent, 1);
		AppendSceneAgentUsableObjectSearchText(stringBuilder, agent);
		AppendSceneAgentNavigatorSearchText(stringBuilder, agent);
		return stringBuilder.ToString().ToLowerInvariant();
	}
internal static void AppendSceneAgentCurrentAnimationName(StringBuilder stringBuilder, Agent agent, int channelNo)
	{
		try
		{
			ActionIndexCache action = agent.GetCurrentAction(channelNo);
			if (action == ActionIndexCache.act_none)
			{
				return;
			}
			AppendSceneAgentActionSearchPart(stringBuilder, agent.ActionSet.GetAnimationName(action));
		}
		catch
		{
		}
	}
internal static void AppendSceneAgentUsableObjectSearchText(StringBuilder stringBuilder, Agent agent)
	{
		object usedObject = null;
		try
		{
			usedObject = agent?.CurrentlyUsedGameObject;
		}
		catch
		{
		}
		if (usedObject == null)
		{
			return;
		}
		AppendSceneAgentActionSearchPart(stringBuilder, usedObject.GetType().Name);
		try
		{
			WeakGameEntity gameEntity = agent.CurrentlyUsedGameObject.GameEntity;
			if (gameEntity.IsValid)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, gameEntity.Name);
			}
		}
		catch
		{
		}
		try
		{
			if (usedObject is AnimationPoint animationPoint)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.ArriveAction);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.LoopStartAction);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.PairLoopStartAction);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.LeaveAction);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.LeftHandItem);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.RightHandItem);
			}
			if (usedObject is DynamicObjectAnimationPoint dynamicObjectAnimationPoint)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, dynamicObjectAnimationPoint.ArriveAction);
				AppendSceneAgentActionSearchPart(stringBuilder, dynamicObjectAnimationPoint.LoopStartAction);
				AppendSceneAgentActionSearchPart(stringBuilder, dynamicObjectAnimationPoint.LeaveAction);
			}
		}
		catch
		{
		}
	}
internal static void AppendSceneAgentNavigatorSearchText(StringBuilder stringBuilder, Agent agent)
	{
		try
		{
			AgentNavigator agentNavigator = agent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			AgentBehavior activeBehavior = agentNavigator?.GetActiveBehavior();
			if (activeBehavior != null)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, activeBehavior.GetType().Name);
			}
			UsableMachine targetMachine = agentNavigator?.TargetUsableMachine;
			if (targetMachine != null)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, targetMachine.GetType().Name);
				WeakGameEntity gameEntity = targetMachine.GameEntity;
				if (gameEntity.IsValid)
				{
					AppendSceneAgentActionSearchPart(stringBuilder, gameEntity.Name);
				}
			}
		}
		catch
		{
		}
	}
internal static void AppendSceneAgentActionSearchPart(StringBuilder stringBuilder, string value)
	{
		if (stringBuilder == null || string.IsNullOrWhiteSpace(value))
		{
			return;
		}
		stringBuilder.Append(' ').Append(value.Trim());
	}
internal static bool ContainsSceneActionKeyword(string text, params string[] keywords)
	{
		if (string.IsNullOrWhiteSpace(text) || keywords == null)
		{
			return false;
		}
		foreach (string keyword in keywords)
		{
			if (!string.IsNullOrWhiteSpace(keyword) && text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}
internal static bool ContainsSceneEatingOrDrinkingKeyword(string text)
	{
		return ContainsSceneActionTokenKeyword(text, "drink", "drinking", "eat", "eating", "food", "beer", "ale", "wine", "mug", "cup", "bowl", "meal", "feast", "bread", "tavern_drink")
			|| ContainsSceneActionKeyword(text, "喝酒", "饮酒", "进食", "吃饭", "吃东西", "啤酒", "麦酒", "酒杯", "酒桶", "饭碗", "食物");
	}
internal static bool ContainsSceneActionTokenKeyword(string text, params string[] keywords)
	{
		if (string.IsNullOrWhiteSpace(text) || keywords == null)
		{
			return false;
		}
		foreach (string keyword in keywords)
		{
			if (ContainsSceneActionTokenKeyword(text, keyword))
			{
				return true;
			}
		}
		return false;
	}
internal static bool ContainsSceneActionTokenKeyword(string text, string keyword)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword))
		{
			return false;
		}
		int index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
		while (index >= 0)
		{
			int before = index - 1;
			int after = index + keyword.Length;
			bool beforeOk = before < 0 || !char.IsLetterOrDigit(text[before]);
			bool afterOk = after >= text.Length || !char.IsLetterOrDigit(text[after]);
			if (beforeOk && afterOk)
			{
				return true;
			}
			index = text.IndexOf(keyword, index + 1, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}
internal static void AddSceneAgentActionLabel(List<string> labels, HashSet<string> seen, string label)
	{
		if (labels == null || seen == null || string.IsNullOrWhiteSpace(label))
		{
			return;
		}
		label = label.Trim();
		if (seen.Add(label))
		{
			labels.Add(label);
		}
	}
internal static string BuildCeremonyRoleFactForPrompt(int agentIndex)
	{
		try
		{
			TownExecutionMissionBehavior ceremony = Mission.Current?.GetMissionBehavior<TownExecutionMissionBehavior>();
			if (ceremony?.Request == null || ceremony.State == ExecutionSessionState.Cancelled)
			{
				return string.Empty;
			}
			Agent speaker = Mission.Current.Agents?.FirstOrDefault(agent => agent != null && agent.Index == agentIndex);
			if (speaker == null)
			{
				return string.Empty;
			}
			ExecutionRequest request = ceremony.Request;
			string charge = (request.Charge?.GetName()?.ToString() ?? string.Empty).Trim();
			string method = (request.Method?.GetName()?.ToString() ?? string.Empty).Trim();
			string victim = (request.Victim?.Name?.ToString() ?? string.Empty).Trim();
			string venue = (request.Venue?.Name?.ToString() ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(charge)) charge = "未说明的罪名";
			if (string.IsNullOrWhiteSpace(method)) method = "公开处决";
			if (string.IsNullOrWhiteSpace(victim)) victim = "死刑犯";
			if (string.IsNullOrWhiteSpace(venue)) venue = "这座城镇";
			string fact;
			if (ceremony.IsCeremonyExecutioner(speaker))
			{
				fact = "你是这场公开处决的刽子手，正在" + venue + "对" + victim + "执行" + charge + "的判决，处刑方式是" + method;
			}
			else if (ceremony.IsCeremonyVictim(speaker))
			{
				fact = "你是这场公开处决的死刑犯，因" + charge + "在" + venue + "等候" + method;
			}
			else if (ceremony.IsCeremonyGuard(speaker))
			{
				fact = "你是这场公开处决的卫兵，正在" + venue + "看押因" + charge + "等候" + method + "的" + victim;
			}
			else if (ceremony.IsCeremonyCrowd(speaker))
			{
				fact = "你是" + venue + "围观这场公开处决的平民，被处决的是因" + charge + "等候" + method + "的" + victim;
			}
			else
			{
				return string.Empty;
			}
			bool waiting = ceremony.State == ExecutionSessionState.WaitingForPlayer ||
				ceremony.State == ExecutionSessionState.Preparing;
			return waiting
				? fact + "。判决已经宣布，但处刑还没有开始。"
				: fact + "。处刑已经开始。";
		}
		catch
		{
			return string.Empty;
		}
	}

    internal sealed class ActionReadPorts
    {
        internal readonly SceneMovementController Movement;
        internal readonly SceneAudioLipSyncController Audio;
        internal readonly SceneInteractionLifecycleController Interactions;
        internal readonly SceneAttentionController Attention;
        internal ActionReadPorts(SceneMovementController movement, SceneAudioLipSyncController audio, SceneInteractionLifecycleController interactions, SceneAttentionController attention)
        { Movement=movement; Audio=audio; Interactions=interactions; Attention=attention; }
    }

internal static string BuildSceneAgentSelfActionFactForPrompt(Func<ActionReadPorts> readPorts, NpcDataPacket npc, Hero hero = null)
	{
		try
		{
			if (npc == null)
			{
				return "";
			}
			var agents = Mission.Current?.Agents;
			Agent agent = (npc.AgentIndex >= 0 && agents != null)
				? agents.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex)
				: null;
			List<string> labels = agent != null ? CollectSceneAgentSelfActionLabels(readPorts, agent) : new List<string>();
			if (labels.Count == 0 && IsNativeConversationTargetForActionPrompt(npc, hero))
			{
				AddSceneAgentActionLabel(labels, new HashSet<string>(labels), "正在对话");
			}
			return labels.Count > 0 ? "【当前动作】你当前状态：" + string.Join("、", labels) + "。" : "";
		}
		catch
		{
			return "";
		}
	}
internal static List<string> CollectSceneAgentSelfActionLabels(Func<ActionReadPorts> readPorts, Agent agent)
	{
		List<string> labels = new List<string>();
		HashSet<string> seen = new HashSet<string>();
		if (agent == null)
		{
			return labels;
		}
		if (!IsSceneAgentPromptActive(agent))
		{
			AddSceneAgentActionLabel(labels, seen, "倒地/受击/死亡");
			return labels;
		}
		string actionSearchText = BuildSceneAgentActionSearchText(agent);
		Agent.ActionCodeType lowerBodyAction = GetSceneAgentCurrentActionType(agent, 0);
		Agent.ActionCodeType upperBodyAction = GetSceneAgentCurrentActionType(agent, 1);
		if (IsSceneAgentSittingForPrompt(agent))
		{
			AddSceneAgentActionLabel(labels, seen, "坐着");
		}
		if (IsSceneAgentConversingForPrompt(readPorts, agent, actionSearchText))
		{
			AddSceneAgentActionLabel(labels, seen, "正在对话");
		}
		if (IsSceneAgentUsingObjectForPrompt(agent))
		{
			AddSceneAgentActionLabel(labels, seen, "正在使用物体");
		}
		if (ContainsSceneActionKeyword(actionSearchText, "dance", "dancing", "perform", "performance", "performer", "bard", "minstrel", "entertain", "sing", "song", "stage", "跳舞", "表演", "歌唱"))
		{
			AddSceneAgentActionLabel(labels, seen, "正在跳舞/表演");
		}
		if (ContainsSceneEatingOrDrinkingKeyword(actionSearchText))
		{
			AddSceneAgentActionLabel(labels, seen, "正在喝酒/吃东西");
		}
		if (IsSceneAgentPlayingMusicForPrompt(agent, actionSearchText))
		{
			AddSceneAgentActionLabel(labels, seen, "正在演奏");
		}
		if (IsSceneAgentMovingForPrompt(agent, actionSearchText, lowerBodyAction))
		{
			AddSceneAgentActionLabel(labels, seen, "正在行走/移动");
		}
		if (IsSceneAgentAttackAction(lowerBodyAction) || IsSceneAgentAttackAction(upperBodyAction) || ContainsSceneActionKeyword(actionSearchText, "fightbehavior", "combat", "attack"))
		{
			AddSceneAgentActionLabel(labels, seen, "正在战斗/攻击");
		}
		if (IsSceneAgentDefenseAction(lowerBodyAction) || IsSceneAgentDefenseAction(upperBodyAction) || ContainsSceneActionKeyword(actionSearchText, "defend", "block", "parry"))
		{
			AddSceneAgentActionLabel(labels, seen, "正在防御/格挡");
		}
		if (IsSceneAgentDownOrHitForPrompt(agent, lowerBodyAction, upperBodyAction))
		{
			AddSceneAgentActionLabel(labels, seen, "倒地/受击/死亡");
		}
		ActionReadPorts instance = readPorts();
		if (instance != null)
		{
			if (instance.Movement.IsAgentFollowingPlayerBySceneCommand(agent) || ContainsSceneActionKeyword(actionSearchText, "followagentbehavior"))
			{
				AddSceneAgentActionLabel(labels, seen, "正在跟随");
			}
			if (instance.Movement.IsAgentBusyWithSceneGuideErrand(agent.Index) || SceneMovementController.GetSceneGuideEscortBehavior(agent) != null)
			{
				AddSceneAgentActionLabel(labels, seen, "正在带路");
			}
			if (instance.Movement.HasGuideArrivalHold(agent.Index))
			{
				AddSceneAgentActionLabel(labels, seen, "正在等待");
			}
		}
		if (labels.Count == 0 && IsSceneAgentWaitingForPrompt(agent, actionSearchText, lowerBodyAction, upperBodyAction))
		{
			AddSceneAgentActionLabel(labels, seen, "正在等待");
		}
		return labels;
	}
internal static bool IsSceneAgentConversingForPrompt(Func<ActionReadPorts> readPorts, Agent agent, string actionSearchText)
	{
		if (agent == null)
		{
			return false;
		}
		try
		{
			ActionReadPorts instance = readPorts();
			if (instance != null)
			{
				if (instance.Audio.IsSpeaking(agent.Index) || instance.Interactions.TryGetInteractionSession(agent.Index, out _) || instance.Attention.IsStaringAgentIndex(agent.Index))
				{
					return true;
				}
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true && TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var _))
			{
				int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
				if (targetAgentIndex == agent.Index)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return ContainsSceneActionKeyword(actionSearchText, "conversation", "talkbehavior", "talk");
	}

internal static string BuildHeroPregnancySelfKnowledgeForPrompt(Hero hero)
	{
		try
		{
			return hero != null && hero.IsPregnant
				? "你当前正在怀孕，并且清楚知道自己已经怀孕；这是实时事实，不能否认或遗忘"
				: "";
		}
		catch
		{
			return "";
		}
	}
internal static bool IsHeroInPlayerMainPartyForPrompt(Hero hero)
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			return hero != null && hero != Hero.MainHero && mainParty != null && hero.PartyBelongedTo == mainParty;
		}
		catch
		{
			return false;
		}
	}
internal static string BuildPlayerCommandRelationshipLineForPrompt(NpcDataPacket npc, Hero hero)
	{
		try
		{
			if (!TryResolvePlayerCommandRelationshipForPrompt(npc, hero, out var relationship))
			{
				return "";
			}
			string playerName = (SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerName) || string.Equals(playerName, "玩家", StringComparison.Ordinal))
			{
				playerName = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = "玩家";
			}
			if (string.Equals(relationship, "exercise_opponent", StringComparison.OrdinalIgnoreCase))
			{
				return "【上下级关系】你认得" + playerName + "：你原本来自他的队伍。当前只是军事演习中被临时编入对抗队，你仍知道他是平日里的指挥官和这次演习的组织者；回应时不要把他当陌生人、外人或真正敌军。";
			}
			if (string.Equals(relationship, "player_side", StringComparison.OrdinalIgnoreCase))
			{
				return "【上下级关系】你认得" + playerName + "：当前战斗中你和他同属己方阵营，你知道他是己方指挥者，不应把他当陌生人、外人或敌人。";
			}
			return "【上下级关系】你认得" + playerName + "：他是你当前部队的指挥官和上级。你应以士兵面对长官的口吻回应，不要把他当陌生平民或外人。";
		}
		catch
		{
			return "";
		}
	}
internal static bool ShouldIncludePlayerPartyRosterForScenePrompt(Hero observerHero, bool partyTransferTopicSelected)
	{
		if (partyTransferTopicSelected)
		{
			return true;
		}
		if (AIConfigHandler.IsPlayerCompanionOrFamilyTradeTarget(observerHero))
		{
			return true;
		}
		return !IsSettlementSceneForPlayerPartyRosterSuppression();
	}
internal static bool ShouldUseCompactPlayerPartyRosterForScenePrompt(bool partyTransferTopicSelected)
	{
		if (partyTransferTopicSelected)
		{
			return false;
		}
		return IsSettlementSceneForPlayerPartyRosterSuppression();
	}
internal static bool IsSettlementSceneForPlayerPartyRosterSuppression()
	{
		try
		{
			if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Settlement currentSettlement = MobileParty.MainParty?.CurrentSettlement;
			if (currentSettlement != null && !currentSettlement.IsHideout)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
internal static string BuildPlayerTownPartyStayHintForPrompt(Hero playerHero)
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			if (settlement == null || !settlement.IsTown)
			{
				return "";
			}
			MobileParty party = MobileParty.MainParty;
			if (party?.MemberRoster == null)
			{
				return "";
			}
			if (party.CurrentSettlement != null && party.CurrentSettlement != settlement)
			{
				return "";
			}
			int totalMen = Math.Max(0, party.MemberRoster.TotalManCount);
			if (totalMen <= 0)
			{
				return "";
			}
			string playerName = (playerHero?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = (SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(playerName) || string.Equals(playerName, "玩家", StringComparison.Ordinal))
			{
				playerName = "此人";
			}
			return playerName + "拥有一支" + totalMen + "人的部队暂时驻扎在城外，你不清楚他的军队的具体明细";
		}
		catch
		{
			return "";
		}
	}
internal static string BuildPrisonerContextLineForPrompt(NpcDataPacket npc, Hero hero)
	{
		try
		{
			Agent agent = null;
			try
			{
				agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && npc != null && a.Index == npc.AgentIndex);
			}
			catch
			{
				agent = null;
			}
			if (CastleAftermathRuntimeBridge.IsPrisonerAgent(agent))
			{
				string castleSituation = SiegeAiInterventionBehavior.BuildCastleNpcSituationPromptForAgent(
					hero,
					agent?.Character as CharacterObject,
					npc?.AgentIndex ?? -1);
				if (!string.IsNullOrWhiteSpace(castleSituation))
				{
					return castleSituation;
				}
			}
			string heroPrisonerStatusLine = SharedPromptCaptureBannerlordAdapter.BuildHeroPrisonerStatusPromptLineForExternal(hero);
			if (!string.IsNullOrWhiteSpace(heroPrisonerStatusLine))
			{
				return heroPrisonerStatusLine;
			}
			if (!IsInspectionPrisonerAgentForPrompt(agent))
			{
				return "";
			}
			string playerName = (SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerName) || string.Equals(playerName, "玩家", StringComparison.Ordinal))
			{
				playerName = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = "玩家";
			}
			string captor = playerName;
			if (hero?.PartyBelongedToAsPrisoner?.LeaderHero != null)
			{
				string captorName = (hero.PartyBelongedToAsPrisoner.LeaderHero.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(captorName))
				{
					captor = captorName;
				}
			}
			return "【俘虏处境】你现在是" + captor + "的俘虏，被押在当前阅兵/检阅场景中；你不属于" + playerName + "的正常部队，也不是他的下属或友方士兵。你应清楚自己处在被控制、被看押、行动受限的状态，并按俘虏身份回应。";
		}
		catch
		{
			return "";
		}
	}
internal static bool IsInspectionPrisonerNpcForPrompt(NpcDataPacket npc)
	{
		try
		{
			if (npc == null || npc.AgentIndex < 0)
			{
				return false;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
			return IsInspectionPrisonerAgentForPrompt(agent);
		}
		catch
		{
			return false;
		}
	}
internal static bool TryResolvePlayerCommandRelationshipForPrompt(NpcDataPacket npc, Hero hero, out string relationship)
	{
		relationship = "";
		try
		{
			if (hero != null && IsHeroInPlayerMainPartyForPrompt(hero))
			{
				relationship = "direct_command";
				return true;
			}
			Agent agent = null;
			try
			{
				agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && npc != null && a.Index == npc.AgentIndex);
			}
			catch
			{
				agent = null;
			}
			if (agent == null || agent.IsMainAgent)
			{
				return false;
			}
			CharacterObject character = agent.Character as CharacterObject;
			bool isSoldierLike = character?.IsSoldier == true || string.Equals((npc?.UnnamedRank ?? "").Trim(), "soldier", StringComparison.OrdinalIgnoreCase) || string.Equals((npc?.RoleDesc ?? "").Trim(), "士兵", StringComparison.Ordinal);
			if (!isSoldierLike && hero?.IsPlayerCompanion != true)
			{
				return false;
			}
			if (IsInspectionPrisonerAgentForPrompt(agent))
			{
				return false;
			}
			try
			{
				if (agent.Origin?.IsUnderPlayersCommand == true)
				{
					relationship = "direct_command";
					return true;
				}
			}
			catch
			{
			}
			PartyBase party = null;
			try
			{
				party = agent.Origin?.BattleCombatant as PartyBase;
			}
			catch
			{
				party = null;
			}
			if (party != null)
			{
				if (ReferenceEquals(party, PartyBase.MainParty) || ReferenceEquals(party.MobileParty, MobileParty.MainParty))
				{
					relationship = "direct_command";
					return true;
				}
				if (TryResolveMilitaryExerciseCommandRelationshipForPrompt(party, out relationship))
				{
					return true;
				}
				return false;
			}
			Mission mission = Mission.Current;
			if (mission?.PlayerTeam != null && agent.Team == mission.PlayerTeam)
			{
				relationship = "player_side";
				return true;
			}
		}
		catch
		{
			relationship = "";
		}
		return false;
	}
internal static bool TryResolveMilitaryExerciseCommandRelationshipForPrompt(PartyBase party, out string relationship)
	{
		relationship = "";
		try
		{
			if (party == null || !MilitaryExerciseBehavior.IsCurrentExerciseRuntime())
			{
				return false;
			}
			MilitaryExerciseBehavior.MilitaryExerciseRuntime runtime = MilitaryExerciseBehavior.GetCurrentRuntime();
			if (runtime == null)
			{
				return false;
			}
			if (ReferenceEquals(party, runtime.OpponentDummyParty?.Party) || ReferenceEquals(party.MobileParty, runtime.OpponentDummyParty))
			{
				relationship = "exercise_opponent";
				return true;
			}
			if (ReferenceEquals(party, runtime.HoldingDummyParty?.Party) || ReferenceEquals(party.MobileParty, runtime.HoldingDummyParty))
			{
				relationship = "direct_command";
				return true;
			}
		}
		catch
		{
			relationship = "";
		}
		return false;
	}
internal static bool IsInspectionPrisonerAgentForPrompt(Agent agent)
	{
		try
		{
			bool isLord;
			if (TroopInspectionMissionLogic.TryResolveInspectionPrisoner(agent, out isLord))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (agent?.Origin is PrisonerAgentOrigin)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
internal static string JoinPromptSections(params string[] sections)
	{
		if (sections == null || sections.Length == 0)
		{
			return "";
		}
		List<string> list = new List<string>();
		foreach (string section in sections)
		{
			string text = (section ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		return string.Join("\n", list);
	}
internal static string BuildPlayerCustomPromptRuleBlock()
	{
		string text = (DuelSettings.GetSettings()?.PlayerCustomPromptRule ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "请遵循以下规则参与互动：\n" + text;
	}
    internal sealed class RoleIntroCapturePorts
    {
        internal readonly SceneAgentIdentityPromptCaptureAdapter Identity;
        internal readonly Func<PersonaEquipmentPromptCaptureAdapter> Equipment;
        internal readonly Func<ActionReadPorts> Actions;
        internal RoleIntroCapturePorts(SceneAgentIdentityPromptCaptureAdapter identity, Func<PersonaEquipmentPromptCaptureAdapter> equipment, Func<ActionReadPorts> actions)
        { Identity = identity ?? throw new ArgumentNullException(nameof(identity)); Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment)); Actions = actions ?? throw new ArgumentNullException(nameof(actions)); }
    }

internal static string BuildSceneSystemTopPromptIntroForSingle(RoleIntroCapturePorts ports, NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
	string fullIntro = ports.Equipment().BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
	SplitSceneNpcRoleIntroSections(fullIntro, hero != null, out var stableIntro, out var _);
	string ordinaryVoiceContext = AfGcczShoutBridge.BuildOrdinarySpeakerVoiceContext(hero, npc);
	if (!string.IsNullOrWhiteSpace(ordinaryVoiceContext))
	{
		stableIntro = MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock("", stableIntro, ordinaryVoiceContext);
	}
	string selfActionFact = BuildSceneAgentSelfActionFactForPrompt(ports.Actions, npc, hero);
	if (!string.IsNullOrWhiteSpace(selfActionFact))
	{
		stableIntro = (stableIntro ?? "").TrimEnd();
		stableIntro = string.IsNullOrWhiteSpace(stableIntro) ? selfActionFact : stableIntro + Environment.NewLine + selfActionFact;
	}
	return stableIntro;
}

internal static string BuildHeroStableRoleContextForExternal(RoleIntroCapturePorts ports, Hero hero)
{
	if (hero == null)
	{
		return "";
	}
	try
	{
		NpcDataPacket npc = ports.Identity.BuildNativeConversationNpcData(hero, hero.CharacterObject);
		if (npc == null)
		{
			return "";
		}
		npc.AgentIndex = -1;
		if (string.IsNullOrWhiteSpace(npc.PersonalityDesc) || string.IsNullOrWhiteSpace(npc.BackgroundDesc))
		{
			ScenePersonaPreparationAdapter.BuildHeroPersonaFallback(hero, out var fallbackPersonality, out var fallbackBackground);
			if (string.IsNullOrWhiteSpace(npc.PersonalityDesc))
			{
				npc.PersonalityDesc = fallbackPersonality ?? "";
			}
			if (string.IsNullOrWhiteSpace(npc.BackgroundDesc))
			{
				npc.BackgroundDesc = fallbackBackground ?? "";
			}
		}
		return BuildSceneSystemTopPromptIntroForSingle(ports, npc, hero, new List<NpcDataPacket> { npc });
	}
	catch (Exception ex)
	{
		Logger.Log("ShoutBehavior", "[CourierContext][WARN] build stable hero role context failed hero=" + (hero.StringId ?? "") + " error=" + ex.Message);
		return "";
	}
}

internal static string BuildSceneSystemTopPromptIntroForGroup(RoleIntroCapturePorts ports, IEnumerable<NpcDataPacket> npcs, Dictionary<int, Hero> resolvedHeroes, bool partyTransferTopicSelected = false)
{
		if (npcs == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (NpcDataPacket npc in npcs)
		{
			if (npc == null)
			{
				continue;
			}
			Hero hero = null;
			if (npc.IsHero && resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
			}
			string fullIntro = ports.Equipment().BuildSceneNpcRoleIntroForPrompt(npc, hero, npcs, includeInventorySummary: false, includeTradePricing: false, partyTransferTopicSelected: partyTransferTopicSelected);
			SplitSceneNpcRoleIntroSections(fullIntro, hero != null, out var intro, out var _);
			string ordinaryVoiceContext = AfGcczShoutBridge.BuildOrdinarySpeakerVoiceContext(hero, npc);
			if (!string.IsNullOrWhiteSpace(ordinaryVoiceContext))
			{
				intro = MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock("", intro, ordinaryVoiceContext);
			}
			if (string.IsNullOrWhiteSpace(intro))
			{
				continue;
			}
			string selfActionFact = BuildSceneAgentSelfActionFactForPrompt(ports.Actions, npc, hero);
			if (!string.IsNullOrWhiteSpace(selfActionFact))
			{
				intro = intro.TrimEnd() + Environment.NewLine + selfActionFact;
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine(intro);
	}
	return stringBuilder.ToString().Trim();
}

internal static string BuildSceneUserRuntimeContextForSingle(RoleIntroCapturePorts ports, NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
	string fullIntro = ports.Equipment().BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
	SplitSceneNpcRoleIntroSections(fullIntro, hero != null, out var _, out var runtimeIntro);
	return runtimeIntro;
}

internal static string BuildCompactSceneUserRuntimeContextForShortReply(RoleIntroCapturePorts ports, NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
	string fullIntro = ports.Equipment().BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
	SplitSceneNpcRoleIntroSections(fullIntro, hero != null, out var _, out var runtimeIntro);
	if (string.IsNullOrWhiteSpace(runtimeIntro))
	{
		return "";
	}
	string[] lines = runtimeIntro.Replace("\r", "").Split('\n');
	List<string> keptLines = new List<string>();
	for (int i = 0; i < lines.Length; i++)
	{
		string line = (lines[i] ?? "").Trim();
		if (string.IsNullOrWhiteSpace(line))
		{
			continue;
		}
		if (line.StartsWith("你身上穿着", StringComparison.Ordinal)
			|| line.StartsWith("现在的时间是", StringComparison.Ordinal)
			|| line.StartsWith("你面前站着一个", StringComparison.Ordinal)
			|| line.StartsWith("【当前臣属关系】", StringComparison.Ordinal))
		{
			keptLines.Add(line);
		}
	}
	return string.Join("\n", keptLines).Trim();
}

internal static string BuildSceneUserRuntimeContextForGroup(RoleIntroCapturePorts ports, IEnumerable<NpcDataPacket> npcs, Dictionary<int, Hero> resolvedHeroes, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
	if (npcs == null)
	{
		return "";
	}
	StringBuilder stringBuilder = new StringBuilder();
	foreach (NpcDataPacket npc in npcs)
	{
		if (npc == null)
		{
			continue;
		}
		Hero hero = null;
		if (npc.IsHero && resolvedHeroes != null)
		{
			resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
		}
		string runtimeIntro = BuildSceneUserRuntimeContextForSingle(ports, npc, hero, npcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
		if (string.IsNullOrWhiteSpace(runtimeIntro))
		{
			continue;
		}
		if (stringBuilder.Length > 0)
		{
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine(runtimeIntro);
	}
	return stringBuilder.ToString().Trim();
}

internal static void SplitSceneNpcRoleIntroSections(string fullIntro, bool isHeroNpc, out string stableIntro, out string runtimeIntro)
{
	stableIntro = "";
	runtimeIntro = "";
	string[] lines = (fullIntro ?? "").Replace("\r", "").Split('\n');
	List<string> stableLines = new List<string>();
	List<string> runtimeLines = new List<string>();
	List<string> normalizedLines = lines.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).ToList();
	if (normalizedLines.Count <= 0)
	{
		return;
	}
	if (normalizedLines.Count > 0)
	{
		stableLines.Add(normalizedLines[0]);
	}
	if (normalizedLines.Count > 1)
	{
		runtimeLines.Add(normalizedLines[1]);
	}
	if (isHeroNpc)
	{
		if (normalizedLines.Count > 2)
		{
			stableLines.Add(normalizedLines[2]);
		}
		if (normalizedLines.Count > 3)
		{
			stableLines.Add(normalizedLines[3]);
		}
		if (normalizedLines.Count > 4)
		{
			stableLines.Add(normalizedLines[4]);
		}
		for (int i = 5; i < normalizedLines.Count; i++)
		{
			runtimeLines.Add(normalizedLines[i]);
		}
	}
	else
	{
		if (normalizedLines.Count > 2)
		{
			stableLines.Add(normalizedLines[2]);
		}
		for (int i = 3; i < normalizedLines.Count; i++)
		{
			runtimeLines.Add(normalizedLines[i]);
		}
	}
	stableIntro = string.Join("\n", stableLines.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	runtimeIntro = string.Join("\n", runtimeLines.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
}

internal static string BuildPlayerSceneIdentitySentenceForPrompt(Hero playerHero)
{
		if (playerHero == null)
		{
			return "";
		}
		try
		{
			Clan clan = playerHero.Clan;
			Kingdom kingdom = clan?.Kingdom;
			if (clan != null && clan.IsUnderMercenaryService && kingdom != null)
			{
				string kingdomName = (kingdom.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(kingdomName))
				{
					return "而且他还是" + kingdomName + "的雇佣兵。";
				}
			}
		}
		catch
		{
		}
		try
		{
			Kingdom kingdom = playerHero.Clan?.Kingdom;
			string factionName = (kingdom?.Name?.ToString() ?? "").Trim();
			if (playerHero.IsFactionLeader && kingdom != null && kingdom.Leader == playerHero && !string.IsNullOrWhiteSpace(factionName))
			{
				return "而且他还是" + factionName + "的统治者。";
			}
			if (playerHero.IsLord && !playerHero.IsFactionLeader && kingdom != null && !string.IsNullOrWhiteSpace(factionName))
			{
				return "而且他还是" + factionName + "的封臣。";
			}
		}
		catch
		{
		}
		string text = (PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.Equals(text, "玩家", StringComparison.Ordinal))
		{
			text = (playerHero.Name?.ToString() ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "此人";
		}
		return text + "没有效忠于任何人。";
	}

internal static bool ShouldForceDetailedPlayerIntroForObserver(Hero observerHero)
{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (observerHero == null || mainHero == null)
			{
				return false;
			}
			return observerHero.Clan != null && observerHero.Clan == mainHero.Clan;
		}
		catch
		{
			return false;
		}
	}

internal static bool DoesSceneObserverKnowPlayerIdentityForPrompt(Hero observerHero, NpcDataPacket observerNpc)
{
		try
		{
			if (observerHero != null)
			{
				return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerHero);
			}
			CharacterObject observerCharacter = null;
			try
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && observerNpc != null && a.Index == observerNpc.AgentIndex);
				observerCharacter = agent?.Character as CharacterObject;
			}
			catch
			{
			}
			if (observerCharacter?.HeroObject != null)
			{
				return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerCharacter.HeroObject);
			}
			string observerKey = PromptRuleCaptureBannerlordAdapter.ResolveRuleTargetKey(null, observerCharacter, observerNpc?.AgentIndex ?? -1);
			string cultureId = (observerCharacter?.Culture?.StringId ?? observerNpc?.CultureId ?? "").Trim();
			return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerKey, cultureId);
		}
		catch
		{
			return false;
		}
	}

internal static string GetSceneNpcPatienceNameForPrompt(NpcDataPacket npc)
{
		string text = (ShoutUtils.GetPromptPatienceName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? GetSceneNpcHistoryNameForPrompt(npc) : text;
	}
internal static string NormalizeSceneNpcMatchValue(string value)
	{
		return (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
	}
internal static bool IsSameSceneNpcForPrompt(NpcDataPacket candidate, NpcDataPacket selfNpc)
	{
		if (candidate == null || selfNpc == null)
		{
			return false;
		}
		if (object.ReferenceEquals(candidate, selfNpc))
		{
			return true;
		}
		if (candidate.AgentIndex >= 0 && selfNpc.AgentIndex >= 0 && candidate.AgentIndex == selfNpc.AgentIndex)
		{
			return true;
		}
		string candidateUnnamedKey = NormalizeSceneNpcMatchValue(candidate.UnnamedKey);
		string selfUnnamedKey = NormalizeSceneNpcMatchValue(selfNpc.UnnamedKey);
		if (!candidate.IsHero && !selfNpc.IsHero && !string.IsNullOrWhiteSpace(candidateUnnamedKey) && string.Equals(candidateUnnamedKey, selfUnnamedKey, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string candidateTroopId = NormalizeSceneNpcMatchValue(candidate.TroopId);
		string selfTroopId = NormalizeSceneNpcMatchValue(selfNpc.TroopId);
		if (candidate.IsHero && selfNpc.IsHero && !string.IsNullOrWhiteSpace(candidateTroopId) && string.Equals(candidateTroopId, selfTroopId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (candidate.AgentIndex < 0 && selfNpc.AgentIndex < 0 && candidate.IsHero == selfNpc.IsHero)
		{
			string candidateName = NormalizeSceneNpcMatchValue(candidate.Name);
			string selfName = NormalizeSceneNpcMatchValue(selfNpc.Name);
			if (!string.IsNullOrWhiteSpace(candidateName) && string.Equals(candidateName, selfName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
internal static string BuildSceneObserverInlineStateForPrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			bool canSpeak;
			string stateText;
			if (observerHero != null)
			{
				return MyBehavior.TryGetSceneHeroInlineStateForExternal(observerHero, out stateText, out canSpeak) ? (stateText ?? "").Trim() : "";
			}
			if (observerNpc != null)
			{
				return MyBehavior.TryGetSceneUnnamedInlineStateForExternal(observerNpc.UnnamedKey, observerNpc.Name, GetSceneNpcPatienceNameForPrompt(observerNpc), out stateText, out canSpeak) ? (stateText ?? "").Trim() : "";
			}
		}
		catch
		{
		}
		return "";
	}
internal static IFaction ResolveNpcPerspectiveFactionForPlayerCrimePrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			if (observerHero != null)
			{
				return observerHero.Clan?.Kingdom ?? observerHero.MapFaction;
			}
			if (observerNpc == null)
			{
				return null;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == observerNpc.AgentIndex);
			PartyBase party = agent?.Origin?.BattleCombatant as PartyBase;
			return party?.MapFaction;
		}
		catch
		{
			return null;
		}
	}
internal static string BuildPlayerFactionWarLineForPrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			IFaction npcFaction = ResolveNpcPerspectiveFactionForPlayerCrimePrompt(observerHero, observerNpc);
			Hero playerHero = Hero.MainHero;
			IFaction playerFaction = playerHero?.Clan?.Kingdom ?? playerHero?.MapFaction ?? Clan.PlayerClan?.Kingdom ?? Clan.PlayerClan?.MapFaction;
			if (npcFaction == null || playerFaction == null || !string.Equals(SceneLocationPromptCaptureAdapter.NormalizeFactionRelationLabelForPrompt(npcFaction, playerFaction), "敌对", StringComparison.Ordinal))
			{
				return "";
			}
			string npcFactionName = BuildFactionNameForPrompt(npcFaction);
			string playerFactionName = BuildFactionNameForPrompt(playerFaction);
			if (string.IsNullOrWhiteSpace(npcFactionName) || string.IsNullOrWhiteSpace(playerFactionName))
			{
				return "";
			}
			return "【当前外交状态】你所属阵营（" + npcFactionName + "）正在与面前此人所属阵营（" + playerFactionName + "）交战；政治立场上他属于敌对阵营，不要把这次会面说成双方没有冲突。";
		}
		catch
		{
			return "";
		}
	}
internal static string BuildPlayerVassalageRelationLineForPrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			int targetAgentIndex = observerNpc?.AgentIndex ?? -1;
			Agent agent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex) : null;
			CharacterObject observerCharacter = agent?.Character as CharacterObject;
			string kingdomIdOverride = TryGetKingdomIdOverrideFromAgent(agent);
			return MyBehavior.BuildPlayerVassalageRelationPromptLineForExternal(observerHero, observerCharacter, kingdomIdOverride, "面前此人所在王国", targetAgentIndex);
		}
		catch
		{
			return "";
		}
	}
internal static Kingdom ResolveNpcPerspectiveKingdomForPrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			IFaction faction = ResolveNpcPerspectiveFactionForPlayerCrimePrompt(observerHero, observerNpc);
			if (faction is Kingdom kingdom)
			{
				return kingdom;
			}
			if (faction is Clan clan)
			{
				return clan.Kingdom;
			}
		}
		catch
		{
		}
		return null;
	}
internal static string BuildFactionNameForPrompt(IFaction faction)
	{
		try
		{
			string text = (faction?.Name?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			return (faction?.StringId ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}
internal static string BuildPlayerCompanionPartyRoleLabelForPrompt(Hero companionHero)
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (companionHero == null || mainParty == null)
			{
				return "下属";
			}
			PartyRole role = PartyRole.None;
			if (mainParty.GetRoleHolder(PartyRole.Scout) == companionHero)
			{
				role = PartyRole.Scout;
			}
			else if (mainParty.GetRoleHolder(PartyRole.Engineer) == companionHero)
			{
				role = PartyRole.Engineer;
			}
			else if (mainParty.GetRoleHolder(PartyRole.Surgeon) == companionHero)
			{
				role = PartyRole.Surgeon;
			}
			else if (mainParty.GetRoleHolder(PartyRole.Quartermaster) == companionHero)
			{
				role = PartyRole.Quartermaster;
			}
			switch (role)
			{
			case PartyRole.Scout:
				return "斥候";
			case PartyRole.Engineer:
				return "工程师";
			case PartyRole.Surgeon:
				return "医师";
			case PartyRole.Quartermaster:
				return "军需官";
			default:
				return "下属";
			}
		}
		catch
		{
			return "下属";
		}
	}
}
