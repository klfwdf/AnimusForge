using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

internal sealed partial class VassalageBehavior
{
    internal bool TryExecuteFormalVassalage(Kingdom suzerain, Kingdom subject, AfVassalageType type, out string reason)
    {
        reason = "";
        if (!IsValidKingdom(suzerain) || !IsValidKingdom(subject) || suzerain == subject
            || suzerain.RulingClan?.Leader?.IsAlive != true || subject.RulingClan?.Leader?.IsAlive != true)
        { reason = "臣属条约未成立：当事国或统治者已失效。"; return false; }
        if (type != AfVassalageType.Tributary && type != AfVassalageType.Garrison && type != AfVassalageType.Vassal)
        { reason = "臣属条约未成立：类型不支持。"; return false; }
        if (GetAnyVassalAgreement(subject) != null || WouldCreateVassalageCycle(suzerain.StringId, subject.StringId, out _))
        { reason = "臣属条约未成立：已有臣属关系或形成循环。"; return false; }
        var agreement = new VassalageAgreement { SuzerainKingdomId = suzerain.StringId, VassalKingdomId = subject.StringId,
            Type = type, CreatedDay = GetCurrentCampaignDay(), NegotiatedByHeroId = subject.RulingClan.Leader.StringId,
            EstablishedNoticeShown = suzerain != GetPlayerKingdom(), FormalInitialSynchronizationPending = true };
        var suzerainEnemies = GetKingdomWarEnemies(suzerain).Where(x => x != subject).ToList();
        var subjectEnemies = GetKingdomWarEnemies(subject).Where(x => x != suzerain).ToList();
        _agreementsByVassalId[subject.StringId] = agreement;
        reason = GetKingdomDisplayName(subject, "臣属国") + "已正式承认" + GetKingdomDisplayName(suzerain, "宗主国")
            + "的宗主权，类型：" + GetVassalageTypeDisplayName(type) + "。";
        try
        {
            if (UsesSubjectIndependence(type)) EnsureGarrisonObedience(agreement);
            if (suzerain == GetPlayerKingdom()) QueueEstablishedNotice(agreement);
            MakePeaceIfNeeded(suzerain, subject, "formal_treaty_mutual_peace");
            SynchronizeCurrentWarsForNewAgreement(suzerain, subject, suzerainEnemies, subjectEnemies, out _);
            agreement.FormalInitialSynchronizationPending = false;
        }
        catch (Exception ex)
        {
            // The persisted treaty is already effective. Maintenance resumes
            // peace/war synchronization; retrying acceptance must not recreate it.
            reason += "和平与战争同步暂缓，待条约维护恢复。";
            Logger.Log("Vassalage", "formal treaty synchronization deferred: " + ex.Message);
        }
        Logger.Log("Vassalage", "formal treaty executed suzerain=" + suzerain.StringId + " subject=" + subject.StringId + " type=" + type);
        return true;
    }

    private IEnumerable<VassalageAgreement> GetGeneralFormalAgreements() => _agreementsByVassalId.Values
        .Where(x => x != null && x.IsValid() && (x.FormalInitialSynchronizationPending
            || (x.ResolveSuzerain() != GetPlayerKingdom()
                && (NormalizeVassalageType(x.Type) != AfVassalageType.Tributary || x.ResolveVassal() == GetPlayerKingdom()))));

    // The player-owned path keeps its existing protections/UI. Other suzerains
    // use the same persisted agreements, peace/war ports and obedience values.
    private void MaintainGeneralFormalAgreements(Kingdom changedFirst = null, Kingdom changedSecond = null)
    {
        if (_isApplyingVassalageDiplomacy) return;
        var agreements = GetGeneralFormalAgreements().Where(x => changedFirst == null
            || x.ResolveSuzerain() == changedFirst || x.ResolveSuzerain() == changedSecond
            || x.ResolveVassal() == changedFirst || x.ResolveVassal() == changedSecond).ToList();
        var enemies = new Dictionary<string, List<Kingdom>>(StringComparer.OrdinalIgnoreCase);
        List<Kingdom> Enemies(Kingdom kingdom)
        {
            if (!enemies.TryGetValue(kingdom.StringId, out var values))
                enemies[kingdom.StringId] = values = GetKingdomWarEnemies(kingdom).ToList();
            return values;
        }
        foreach (var agreement in agreements)
        {
            Kingdom suzerain = agreement.ResolveSuzerain(), subject = agreement.ResolveVassal();
            if (!IsValidKingdom(suzerain) || !IsValidKingdom(subject)) continue;
            try
            {
                TryBreakSubjectAtCurrentThreshold(agreement, "subject_ruler_relation_threshold", "正式臣属履约复核");
                if (GetAnyVassalAgreement(subject) != agreement) continue;
                MakePeaceIfNeeded(suzerain, subject, "formal_subject_mutual_peace");
                SynchronizeCurrentWarsForNewAgreement(suzerain, subject, Enemies(suzerain).Where(x => x != subject).ToList(),
                    Enemies(subject).Where(x => x != suzerain).ToList(), out _);
                agreement.FormalInitialSynchronizationPending = false;
            }
            catch (Exception ex) { Logger.Log("Vassalage", "formal treaty maintenance deferred: " + ex.Message); }
        }
    }

    private bool? ShouldAllowGeneralSubjectWar(Kingdom declarer, Kingdom target, DeclareWarAction.DeclareWarDetail detail)
    {
        var agreement = GetAnyVassalAgreement(declarer);
        var targetAgreement = GetAnyVassalAgreement(target);
        Kingdom playerKingdom = GetPlayerKingdom();
        if (targetAgreement != null && targetAgreement.ResolveSuzerain() == declarer && declarer != playerKingdom)
            return false;
        if (agreement == null || agreement.ResolveSuzerain() == playerKingdom || NormalizeVassalageType(agreement.Type) == AfVassalageType.Tributary) return null;
        Kingdom suzerain = agreement.ResolveSuzerain();
        if (target == suzerain) return false;
        if (targetAgreement != null && targetAgreement.ResolveSuzerain() == suzerain) return false;
        // Both military subject types lack autonomous declaration rights.
        // The shared synchronization port enters with _isApplyingVassalageDiplomacy.
        return false;
    }

    private bool? ShouldAllowGeneralSubjectPeace(Kingdom first, Kingdom second)
    {
        foreach (var subject in new[] { first, second })
        {
            var agreement = GetAnyVassalAgreement(subject);
            if (agreement == null || agreement.ResolveSuzerain() == GetPlayerKingdom()
                || NormalizeVassalageType(agreement.Type) == AfVassalageType.Tributary) continue;
            var other = subject == first ? second : first;
            if (other == agreement.ResolveSuzerain()) return true;
            return NormalizeVassalageType(agreement.Type) == AfVassalageType.Garrison
                ? !IsAtWar(agreement.ResolveSuzerain(), other) : false;
        }
        return null;
    }

    private void SynchronizeGeneralSubjectPeace(Kingdom first, Kingdom second)
    {
        if (_isApplyingVassalageDiplomacy) return;
        foreach (var agreement in GetGeneralFormalAgreements().Where(x => x.ResolveSuzerain() == first || x.ResolveSuzerain() == second).ToList())
        {
            Kingdom suzerain = agreement.ResolveSuzerain(), subject = agreement.ResolveVassal();
            Kingdom enemy = suzerain == first ? second : first;
            if (subject == enemy) continue;
            RemovePendingDeclareWarSyncsByParties(subject, enemy, "formal_suzerain_peace");
            MakePeaceIfNeeded(subject, enemy, "formal_suzerain_peace");
        }
    }
}
