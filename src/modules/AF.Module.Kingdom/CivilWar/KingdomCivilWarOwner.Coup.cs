using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

internal sealed partial class KingdomCivilWarOwner
{
    internal static bool CanTrackCoupWar(Kingdom kingdom) => CivilWarWorld.IsAlive(kingdom)
        && DuelSettings.IsCivilWarFactionsEnabled();

    // Adopt the already-created rebel kingdom. Registration never repeats kingdom creation.
    internal bool TryRegisterCoupWar(CoupCivilWarRegistration request, out string message)
    {
        message = "政变内战登记条件不满足。";
        if (request == null || string.IsNullOrWhiteSpace(request.CoupId)) return false;
        Kingdom home = CivilWarWorld.FindKingdom(request.KingdomId), rebel = CivilWarWorld.FindKingdom(request.RebelKingdomId);
        Clan leader = CivilWarWorld.FindClan(request.LeaderClanId);
        string id = "coup-war:" + request.CoupId;
        var existing = FindFaction(id, out var existingState);
        if (existing != null)
        {
            bool same = existingState.KingdomId == request.KingdomId && existing.RebelKingdomId == request.RebelKingdomId;
            message = same ? "政变内战已经登记。" : "政变内战身份冲突，保留原记录。";
            return same;
        }
        // A resolved receipt prevents registration replay after the faction is removed.
        if (_storage.Operations.TryGetValue(id, out var receipt))
        { message = receipt.Message; return receipt.Status == (int)CivilWarActionStatus.Applied && receipt.Fingerprint == request.KingdomId + "|" + request.RebelKingdomId; }
        if (!CanTrackCoupWar(home) || !CivilWarWorld.IsAlive(rebel) || rebel == home || leader?.Kingdom != rebel || !home.IsAtWarWith(rebel)) return false;
        Clan dynasty = request.RestoreDynasty ? CivilWarWorld.FindClan(request.FormerClanId) : null;
        if (request.RestoreDynasty && (dynasty == null || dynasty.IsEliminated || dynasty.Kingdom != rebel
            || string.IsNullOrWhiteSpace(request.FormerKingId) || string.IsNullOrWhiteSpace(request.OriginalName)
            || string.IsNullOrWhiteSpace(request.OriginalShortName)))
        { message = "旧王家族尚未加入叛军或复位身份/原国名缺失，暂不登记。"; return false; }
        int week = CivilWarWorld.CurrentWeek(), day = CivilWarWorld.CurrentDay();
        var state = GetOrCreate(home, week);
        var faction = new KingdomCivilWarFactionState
        {
            Id = id, CoupId = request.CoupId, Stage = KingdomCivilWarStage.OpenWar,
            DemandId = CivilWarCatalog.UsurpDemandId, WarGoal = (int)CivilWarWarGoal.Usurp,
            LeaderClanId = leader.StringId, TargetName = request.RestoreDynasty ? "旧王朝复位" : "王位",
            CreatedWeek = week, StageWeek = week, WarRequestWeek = week, WarStartWeek = week,
            WarRequestDay = day, WarStartDay = day, WarEvaluationDay = day,
            RebelKingdomId = rebel.StringId,
            WarClanIds = rebel.Clans.Where(c => c != null && !c.IsEliminated).Select(c => c.StringId).Distinct(StringComparer.Ordinal).ToList(),
            RestorationClanId = dynasty?.StringId ?? "", RestorationHeroId = request.RestoreDynasty ? request.FormerKingId : "",
            RestorationKingdomName = request.RestoreDynasty ? request.OriginalName : "",
            RestorationKingdomShortName = request.RestoreDynasty ? request.OriginalShortName : ""
        };
        state.Factions.Add(faction);
		_openWarFactions.Add(faction);
        foreach (string clanId in faction.WarClanIds)
        {
            var record = GetOrCreateClan(state, CivilWarWorld.FindClan(clanId), week);
            record.Side = KingdomCivilWarSide.Opposition; record.FactionId = id;
        }
        _openWarKingdoms.Add(home.StringId); IndexOppositionSettlements(state, faction);
        faction.RebelFortShareAtStart = CivilWarWorld.FortificationCount(rebel) / (float)Math.Max(1, CivilWarWorld.FortificationCount(home));
        message = request.RestoreDynasty ? "复位内战已登记：仅叛军战胜才恢复旧王朝与原国名，和平谈判不会复位。" : "政变后内战已登记：被扣押旧王及其家族不加入本次叛军。";
        _storage.Operations[id] = new CivilWarOperation { Fingerprint = home.StringId + "|" + rebel.StringId, FactionId = id, Status = (int)CivilWarActionStatus.Applied, Message = message };
        AddHistory(state, week, message); SaveSummary(state); Revision++;
        PublishPoliticalResult(home, state, faction, leader, id + ":war:outbreak", message);
        NotifyPoliticalChange(home, "coup_war_registered");
        return true;
    }
}
