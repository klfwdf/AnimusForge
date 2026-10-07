using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
namespace AnimusForge;

// Main-thread request-local capture. No live object is put in prompt values or retained by a worker.
internal sealed class DiplomacyPromptSource : IDiplomacyPromptSource, IDiplomacyOralPromptSource
{
    private readonly Hero _npc;
    internal DiplomacyPromptSource(Hero npc) => _npc = npc;
    public bool UseFormalCommitments => WorldDiplomacyBehavior.UseFormalDiplomacyForConversation;
    public string OralArrangementContext() => UseFormalCommitments
        ? WorldDiplomacyBehavior.BuildOralArrangementContext(_npc) : Template("native_action_main", null);
    public DiplomacyConversationEligibilitySnapshot CaptureEligibility() => DiplomacyBehavior.CaptureEligibilitySnapshot(_npc);
    public bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot)
    {
        var source = new DiplomacyIndependentPeaceSource(_npc);
        snapshot = default;
        if (!DiplomacyIndependentPeaceApplication.CanUse(ref source)) return false;
        var clan = source.PlayerClan; var target = source.TargetKingdom;
        snapshot = new DiplomacyIndependentPeaceContextSnapshot(clan.Name?.ToString() ?? clan.StringId ?? "玩家家族",
            clan.Settlements?.Count ?? 0, target.Name?.ToString() ?? target.StringId ?? "目标王国");
        return true;
    }
    public DiplomacyPromptSnapshot Capture()
    {
        Clan clan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
        var fiefs = (clan?.Settlements ?? Enumerable.Empty<Settlement>()).Where(x => x != null && (x.IsTown || x.IsCastle));
        bool hasFief = false;
        var names = new List<string>(); var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fief in fiefs)
        {
            hasFief = true;
            string name = fief.Name?.ToString() ?? fief.StringId ?? "";
            if (!string.IsNullOrWhiteSpace(name) && unique.Add(name)) names.Add(name);
            if (names.Count == 4) break;
        }
        Kingdom player = Clan.PlayerClan?.Kingdom; Kingdom npc = _npc?.Clan?.Kingdom;
        bool peace = player != null && player != npc && !player.IsEliminated && !FactionManager.IsAtWarAgainstFaction(npc, player);
        return new DiplomacyPromptSnapshot(clan != null, Hero.MainHero != null, clan?.Kingdom != null,
            clan?.IsUnderMercenaryService == true, clan?.Leader == null || clan.Leader == Hero.MainHero, hasFief,
            _npc?.Clan?.IsUnderMercenaryService == true, MyBehavior.BuildPlayerPublicDisplayNameForExternal(),
            clan?.Name?.ToString() ?? "玩家家族", names.Count == 0 ? "无明确据点" : string.Join("、", names),
            peace ? DisplayName(player) : "", RewardSystemBehavior.GetTrustLevelIndex(RewardSystemBehavior.Instance?.GetEffectiveTrust(_npc) ?? 0));
    }
    public IReadOnlyList<DiplomacyPromptWar> CaptureWars()
    {
        Kingdom npc = _npc.Clan.Kingdom; Kingdom player = Clan.PlayerClan?.Kingdom;
        var enemies = new List<Kingdom>();
        foreach (Kingdom kingdom in Kingdom.All)
            if (!kingdom.IsEliminated && kingdom != npc && FactionManager.IsAtWarAgainstFaction(npc, kingdom)) enemies.Add(kingdom);
        int listedCount = enemies.Count;
        if (player != null && player != npc && !player.IsEliminated && !enemies.Contains(player) && FactionManager.IsAtWarAgainstFaction(npc, player)) enemies.Add(player);
        var result = new List<DiplomacyPromptWar>(enemies.Count);
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i]; var stance = npc.GetStanceWith(enemy); var model = Campaign.Current.Models.DiplomacyModel;
            int towns = stance.GetSuccessfulTownSieges(npc);
            result.Add(new DiplomacyPromptWar(enemy.StringId, DisplayName(enemy), (int)stance.WarStartDate.ElapsedDaysUntilNow,
                model.GetWarProgressScore(npc, enemy).ResultNumber, model.GetWarProgressScore(enemy, npc).ResultNumber,
                stance.GetCasualties(enemy), stance.GetCasualties(npc), towns, stance.GetSuccessfulSieges(npc) - towns,
                npc.CurrentTotalStrength, enemy.CurrentTotalStrength, enemy.Fiefs.Sum(x => x.Prosperity), i < listedCount));
        }
        return result;
    }
    public string Template(string key, Dictionary<string, string> tokens) => AIConfigHandler.ResolveRuleRuntimeText("diplomacy", key, forConstraint: false, tokens);
    public string AnnexationInstruction() => KingdomAnnexationBehavior.BuildRuntimeAnnexationInstructionForExternal(_npc);
    public void Log(string message) => Logger.Log("DiplomacyBehavior", message);
    private static string DisplayName(Kingdom kingdom) => kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "未知王国";
}
