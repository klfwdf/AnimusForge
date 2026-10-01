using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
namespace AnimusForge;
public partial class ShoutBehavior
{
    private SceneConversationHistoryOwner _sceneConversationHistoryOwner;
    private SceneConversationHistoryOwner SceneHistoryOwner
    {
        get { lock (_historyLock) { return _sceneConversationHistoryOwner ??= new SceneConversationHistoryOwner(_historyLock); } }
    }
    private static string CaptureNativeConversationHistoryKey(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, NpcDataPacket npc = null)
    {
        var input = new NativeHistoryIdentitySnapshot { AgentIndex = targetAgentIndex, NpcName = npcName };
        try
        {
            if (targetAgentIndex < 0 && npc?.AgentIndex >= 0) targetAgentIndex = npc.AgentIndex;
            input.AgentIndex = targetAgentIndex;
            Hero hero = targetHero ?? targetCharacter?.HeroObject;
            CharacterObject character = targetCharacter;
            var agents = Mission.Current?.Agents;
            if ((hero == null || character == null) && targetAgentIndex >= 0 && agents != null)
            {
                Agent agent = agents.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive());
                CharacterObject agentCharacter = agent?.Character as CharacterObject;
                if (character == null) character = agentCharacter;
                hero ??= agentCharacter?.HeroObject;
                npc ??= ShoutUtils.ExtractNpcData(agent);
            }
            if (hero != null) { input.HasHero = true; input.HeroId = hero.StringId; }
            else if (TryResolveWildernessNonHeroMemory(npc, null, character, targetAgentIndex, out var memoryId, out _))
            { input.HasWildernessMemory = true; input.WildernessMemoryId = memoryId; }
            else
            {
                input.UnnamedKey = npc != null && !npc.IsHero ? (npc.UnnamedKey ?? "").Trim() : "";
                if (string.IsNullOrWhiteSpace(input.UnnamedKey)) input.UnnamedKey = BuildNativeConversationNonHeroUnnamedKey(character, npcName, targetAgentIndex);
                input.CharacterId = character?.StringId;
            }
        }
        catch
        {
            input.CaptureFailed = true;
            input.FailureHeroId = targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "";
            input.FailureKey = targetCharacter?.StringId ?? npcName ?? "";
        }
        return NativeHistoryIdentityProjectionOwner.BuildKey(input);
    }
    private static string CaptureNativeConversationNonHeroUnnamedKey(CharacterObject character, string npcName, int agentIndex)
    {
        try
        {
            if (character?.HeroObject != null) return NativeHistoryIdentityProjectionOwner.BuildUnnamedKey(true, "", "", false, "", "", "");
            string troop = character?.StringId;
            string faction = "", leader = "";
            try
            {
                MobileParty party = TryResolveWildernessNonHeroMobileParty(agentIndex);
                if (party != null && party != MobileParty.MainParty) { faction = party.MapFaction?.StringId; leader = party.LeaderHero?.StringId; }
            }
            catch { faction = ""; leader = ""; }
            bool soldier = false; string culture = "", name = "";
            if (string.IsNullOrWhiteSpace(NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(troop)))
            {
                culture = character?.Culture?.StringId;
                try { soldier = character != null && character.IsSoldier; } catch { }
                name = npcName ?? character?.Name?.ToString() ?? "npc";
            }
            return NativeHistoryIdentityProjectionOwner.BuildUnnamedKey(false, troop, culture, soldier, name, faction, leader);
        }
        catch { return ""; }
    }
    private static void AppendNativeConversationSessionHistoryCaptured(Hero targetHero, CharacterObject targetCharacter, string npcName, string speaker, string text, string kind, long eventSequence = 0L, bool bridgeToSceneHistory = true, int targetAgentIndex = -1, NpcDataPacket npc = null, int playerTargetAgentIndex = -1, string playerTargetName = null, string capturedHistoryKey = null)
    {
        text = (text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            string key = capturedHistoryKey ?? CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);
            if (string.IsNullOrWhiteSpace(key)) return;
            int day = 0; string date = ""; int hour = -1; string scene = "";
            try { day = (int)CampaignTime.Now.ToDays; date = CampaignTime.Now.ToString(); hour = MyBehavior.GetCurrentMemoryGameHourForExternal(); scene = MyBehavior.ResolveCurrentMemorySceneLabelForExternal(); } catch { }
            if (eventSequence <= 0L) eventSequence = NextConversationEventSequence();
            _nativeSessionOwner.Append(key, NativeHistoryIdentityProjectionOwner.BuildEntry(eventSequence, day, date, hour, scene, npcName, speaker, text, kind, targetAgentIndex, playerTargetAgentIndex, playerTargetName));
            if (bridgeToSceneHistory)
                CurrentInstance?.AppendNativeConversationSessionLineToSceneHistory(targetHero, targetCharacter, npcName, speaker, text, kind, eventSequence, targetAgentIndex, npc, playerTargetAgentIndex, playerTargetName);
        }
        catch { }
    }
    private static List<string> CaptureAndBuildVisibleSceneHistoryLines(List<ConversationMessage> history, int viewerAgentIndex, string targetNpcName = "", bool useNpcNameAddress = false)
    {
        if (history == null || history.Count == 0) return null;
        return SceneConversationHistoryOwner.BuildVisibleSceneHistoryLines(history, viewerAgentIndex, targetNpcName, useNpcNameAddress,
            ResolveSceneHeroIdFromAgentIndex(viewerAgentIndex), GetPlayerDisplayNameForShout(), DuelSettings.GetDailyConversationHistoryLineLimitForExternal());
    }
    private List<ConversationMessage> CaptureNpcConversationHistory(int npcAgentIndex)
    {
        lock (_historyLock)
        {
            return SceneHistoryOwner.CaptureNpc(npcAgentIndex, ResolveSceneHeroIdFromAgentIndex(npcAgentIndex),
                new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
        }
    }
    private List<string> CaptureAuxiliarySceneDialogueHistoryLines(int targetAgentIndex, int maxLines)
    {
        if (targetAgentIndex < 0 || maxLines <= 0) return new List<string>();
        lock (_historyLock)
        {
            if (SceneHistoryOwner.PublicCount == 0) return new List<string>();
            return SceneHistoryOwner.CaptureVisiblePublicLines(targetAgentIndex, ResolveSceneHeroIdFromAgentIndex(targetAgentIndex), "", false,
                GetPlayerDisplayNameForShout(), DuelSettings.GetDailyConversationHistoryLineLimitForExternal(), maxLines) ?? new List<string>();
        }
    }
}
