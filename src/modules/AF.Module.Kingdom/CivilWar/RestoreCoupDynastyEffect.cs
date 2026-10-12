using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

internal sealed class RestoreCoupDynastyEffect : ICivilWarEffect
{
    public string Id => CivilWarEffectIds.RestoreDynasty;

    public bool CanApply(CivilWarEffectContext ctx, out string reason)
    {
        var faction = ctx.Faction;
        if (faction == null || !faction.RestorationVictoryConfirmed || faction.ResolutionOutcomeId != "rebels_usurp")
            return CivilWarEffects.Refuse(ctx, "RestorationNotAuthorized", KingdomCivilWarTexts.Raw("KW_Raw_157"), out reason);
        Clan dynasty = CivilWarWorld.FindClan(faction.RestorationClanId);
        Hero oldKing = MBObjectManager.Instance?.GetObject<Hero>(faction.RestorationHeroId);
        if (!CivilWarWorld.IsAlive(ctx.Kingdom) || dynasty == null || dynasty.IsEliminated || oldKing == null
            || string.IsNullOrWhiteSpace(faction.RestorationKingdomName) || string.IsNullOrWhiteSpace(faction.RestorationKingdomShortName))
            return CivilWarEffects.Refuse(ctx, "RestorationRecordsUnavailable", KingdomCivilWarTexts.Raw("KW_Raw_158"), out reason);
        if (dynasty.Kingdom != ctx.Kingdom && dynasty.Kingdom != ctx.RebelKingdom)
            return CivilWarEffects.Refuse(ctx, "RestorationDynastyElsewhere", KingdomCivilWarTexts.Raw("KW_Raw_159"), out reason);
        Hero claimant = oldKing.IsAlive ? oldKing : dynasty.Leader;
        if (claimant == null || !claimant.IsAlive || claimant.IsChild || claimant.IsPrisoner || claimant != dynasty.Leader)
            return CivilWarEffects.Refuse(ctx, "RestorationClaimantInvalid", KingdomCivilWarTexts.Raw("KW_Raw_160"), out reason);
        reason = ""; return true;
    }

    public void Apply(CivilWarEffectContext ctx)
    {
        // The result is already locked as rebels_usurp before ReturnRebels invokes peace.
        CivilWarEffects.ReturnRebels(ctx, ctx.WarClans(), 0, true);
        Clan dynasty = CivilWarWorld.FindClan(ctx.Faction.RestorationClanId);
        if (dynasty?.Kingdom != ctx.Kingdom) throw new CivilWarRetryableEffectException(KingdomCivilWarTexts.Raw("KW_Raw_161"), "RestorationDynastyNotHome");
        if (ctx.Kingdom.RulingClan != dynasty) ChangeRulingClanAction.Apply(ctx.Kingdom, dynasty);
        if (ctx.Kingdom.RulingClan != dynasty || ctx.Kingdom.Leader != dynasty.Leader)
            throw new CivilWarRetryableEffectException(KingdomCivilWarTexts.Raw("KW_Raw_162"), "RestorationNotConfirmed");
        if (ctx.Kingdom.Name.ToString() != ctx.Faction.RestorationKingdomName
            || ctx.Kingdom.InformalName.ToString() != ctx.Faction.RestorationKingdomShortName)
            ctx.Kingdom.ChangeKingdomName(new TextObject(ctx.Faction.RestorationKingdomName), new TextObject(ctx.Faction.RestorationKingdomShortName));
        if (ctx.Kingdom.Name.ToString() != ctx.Faction.RestorationKingdomName || ctx.Kingdom.InformalName.ToString() != ctx.Faction.RestorationKingdomShortName)
            throw new CivilWarRetryableEffectException(KingdomCivilWarTexts.Raw("KW_Raw_163"), "RestorationNameNotConfirmed");
        Hero oldKing = MBObjectManager.Instance.GetObject<Hero>(ctx.Faction.RestorationHeroId);
        ctx.Note(oldKing.IsAlive ? "RestorationKingdomRestored" : "RestorationKingdomHeir",
            KingdomCivilWarTexts.Raw("KW_Raw_164") + CivilWarWorld.ClanName(dynasty) + KingdomCivilWarTexts.Raw("KW_Raw_165") + ctx.Faction.RestorationKingdomName
            + "”；" + (oldKing.IsAlive ? KingdomCivilWarTexts.Raw("KW_Raw_166") : KingdomCivilWarTexts.Raw("KW_Raw_167")),
            CivilWarWorld.ClanName(dynasty), ctx.Faction.RestorationKingdomName);
    }
}
