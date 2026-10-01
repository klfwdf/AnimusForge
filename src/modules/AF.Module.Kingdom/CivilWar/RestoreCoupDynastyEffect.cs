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
        reason = "复位尚未获得叛军胜利结果授权。";
        var faction = ctx.Faction;
        if (faction == null || !faction.RestorationVictoryConfirmed || faction.ResolutionOutcomeId != "rebels_usurp") return false;
        Clan dynasty = CivilWarWorld.FindClan(faction.RestorationClanId);
        Hero oldKing = MBObjectManager.Instance?.GetObject<Hero>(faction.RestorationHeroId);
        reason = "原王朝、旧王记录或原国名不可用。";
        if (!CivilWarWorld.IsAlive(ctx.Kingdom) || dynasty == null || dynasty.IsEliminated || oldKing == null
            || string.IsNullOrWhiteSpace(faction.RestorationKingdomName) || string.IsNullOrWhiteSpace(faction.RestorationKingdomShortName)) return false;
        reason = "旧王家族已离开双方，不能替其他王国更换统治者。";
        if (dynasty.Kingdom != ctx.Kingdom && dynasty.Kingdom != ctx.RebelKingdom) return false;
        reason = "复位人选必须存活、成年且未被俘；旧王存活时必须仍是本家族族长。";
        Hero claimant = oldKing.IsAlive ? oldKing : dynasty.Leader;
        if (claimant == null || !claimant.IsAlive || claimant.IsChild || claimant.IsPrisoner || claimant != dynasty.Leader) return false;
        reason = ""; return true;
    }

    public void Apply(CivilWarEffectContext ctx)
    {
        // The result is already locked as rebels_usurp before ReturnRebels invokes peace.
        CivilWarEffects.ReturnRebels(ctx, ctx.WarClans(), 0, true);
        Clan dynasty = CivilWarWorld.FindClan(ctx.Faction.RestorationClanId);
        if (dynasty?.Kingdom != ctx.Kingdom) throw new CivilWarRetryableEffectException("旧王家族尚未归国，不能复位。");
        if (ctx.Kingdom.RulingClan != dynasty) ChangeRulingClanAction.Apply(ctx.Kingdom, dynasty);
        if (ctx.Kingdom.RulingClan != dynasty || ctx.Kingdom.Leader != dynasty.Leader)
            throw new CivilWarRetryableEffectException("旧王朝复位尚未确认。");
        if (ctx.Kingdom.Name.ToString() != ctx.Faction.RestorationKingdomName
            || ctx.Kingdom.InformalName.ToString() != ctx.Faction.RestorationKingdomShortName)
            ctx.Kingdom.ChangeKingdomName(new TextObject(ctx.Faction.RestorationKingdomName), new TextObject(ctx.Faction.RestorationKingdomShortName));
        if (ctx.Kingdom.Name.ToString() != ctx.Faction.RestorationKingdomName || ctx.Kingdom.InformalName.ToString() != ctx.Faction.RestorationKingdomShortName)
            throw new CivilWarRetryableEffectException("原国名恢复尚未确认。");
        Hero oldKing = MBObjectManager.Instance.GetObject<Hero>(ctx.Faction.RestorationHeroId);
        ctx.Notes.Add("叛军战胜，" + CivilWarWorld.ClanName(dynasty) + "恢复王位，国名恢复为“" + ctx.Faction.RestorationKingdomName
            + "”；" + (oldKing.IsAlive ? "旧王复位" : "旧王已故，由家族现任族长继位"));
    }
}
