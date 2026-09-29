using System;
using System.Collections.Generic;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace RichExecutions.Core;

public interface IExecutionConsequenceRule
{
    string StringId { get; }
    void Apply(ExecutionRequest request, ExecutionEffectContext context);
}

public sealed class ExecutionEffectContext
{
    internal ExecutionEffectContext(ExecutionRequest request)
    {
        Request = request;
    }

    public ExecutionRequest Request { get; }
    public float SecurityDelta { get; private set; }
    public float LoyaltyDelta { get; private set; }
    public float InfluenceDelta { get; private set; }
    public int HonorXpDelta { get; private set; }
    public int MercyXpDelta { get; private set; }
    public int LocalRelationDelta { get; private set; }

    public void ChangeTownOrder(float securityDelta, float loyaltyDelta)
    {
        var town = Request.Venue.Town;
        if (town is null)
        {
            return;
        }

        var securityBefore = town.Security;
        var loyaltyBefore = town.Loyalty;
        town.Security += securityDelta;
        town.Loyalty += loyaltyDelta;
        SecurityDelta += town.Security - securityBefore;
        LoyaltyDelta += town.Loyalty - loyaltyBefore;
    }

    public void ChangePlayerInfluence(float delta)
    {
        if (Math.Abs(delta) < 0.001f)
        {
            return;
        }

        var before = Clan.PlayerClan.Influence;
        ChangeClanInfluenceAction.Apply(Clan.PlayerClan, delta);
        InfluenceDelta += Clan.PlayerClan.Influence - before;
    }

    public void ChangePlayerTraits(int honorXp, int mercyXp)
    {
        if (honorXp != 0)
        {
            TraitLevelingHelper.OnIncidentResolved(DefaultTraits.Honor, honorXp);
            HonorXpDelta += honorXp;
        }

        if (mercyXp != 0)
        {
            TraitLevelingHelper.OnIncidentResolved(DefaultTraits.Mercy, mercyXp);
            MercyXpDelta += mercyXp;
        }
    }

    public void ChangeLocalRelations(int relationDelta)
    {
        if (relationDelta == 0)
        {
            return;
        }

        var targets = new HashSet<Hero>();
        var owner = Request.Venue.OwnerClan?.Leader;
        if (owner is not null)
        {
            targets.Add(owner);
        }

        foreach (var notable in Request.Venue.Notables)
        {
            if (notable is not null)
            {
                targets.Add(notable);
            }
        }

        foreach (var target in targets)
        {
            if (target == Hero.MainHero || !target.IsAlive)
            {
                continue;
            }

            try
            {
                ChangeRelationAction.ApplyPlayerRelation(
                    target,
                    relationDelta,
                    affectRelatives: false,
                    showQuickNotification: false);
            }
            catch (Exception exception)
            {
                RexLog.Error($"Failed to change local relation with {target.StringId}.", exception);
            }
        }

        LocalRelationDelta += relationDelta;
    }

    internal ConsequenceDeltas ToDeltas() =>
        new(
            SecurityDelta,
            LoyaltyDelta,
            InfluenceDelta,
            HonorXpDelta,
            MercyXpDelta,
            LocalRelationDelta);
}

public sealed class DefaultExecutionConsequenceRule : IExecutionConsequenceRule
{
    public string StringId => "rex.default_consequences";

    public void Apply(ExecutionRequest request, ExecutionEffectContext context)
    {
        var deltas = ExecutionRuleMath.CalculateConsequences(
            request.LegitimacyTier,
            request.Tone,
            request.Method.StringId,
            influenceCost: 0);

        TryApply("town order", () => context.ChangeTownOrder(deltas.Security, deltas.Loyalty));
        TryApply("influence", () => context.ChangePlayerInfluence(deltas.Influence));
        TryApply("traits", () => context.ChangePlayerTraits(deltas.HonorXp, deltas.MercyXp));
        TryApply("local relations", () => context.ChangeLocalRelations(deltas.LocalRelation));
    }

    private static void TryApply(string component, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            RexLog.Error($"Default consequence component '{component}' failed.", exception);
        }
    }
}
