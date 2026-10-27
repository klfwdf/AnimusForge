using TaleWorlds.CampaignSystem;
#if BANNERLORD_1_5_OR_GREATER
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
#endif

namespace RichExecutions.Core;

internal static class ExecutionRelationCompatibility
{
#if BANNERLORD_1_5_OR_GREATER
    private static bool _registered;
#endif

    internal static void Register(CampaignGameStarter starter)
    {
#if BANNERLORD_1_5_OR_GREATER
        if (_registered) return;
        var target = AccessTools.DeclaredMethod(typeof(ExecutionCampaignBehavior),
            "OnBloodFeudStateChanged", new[] { typeof(Clan), typeof(Hero),
                typeof(ChangeBloodFeudStateAction.ChangeBloodFeudActionDetail) });
        if (target == null) throw new MissingMethodException(
            typeof(ExecutionCampaignBehavior).FullName, "OnBloodFeudStateChanged");
        new Harmony("AnimusForge.Vengeance.Bannerlord15.ExecutionRelations").Patch(target,
            transpiler: new HarmonyMethod(typeof(ExecutionRelationCompatibility), nameof(AdaptRelationCalls)));
        _registered = true;
#else
        starter.AddModel(new ScopedExecutionRelationModel());
#endif
    }

#if BANNERLORD_1_5_OR_GREATER
    // 1.5 replaced ExecutionRelationModel with blood-feud event handling. Replace
    // only its two relation calls, retaining feud state, traits and notifications.
    // No per-tick work: this patch is installed once at campaign registration.
    private static IEnumerable<CodeInstruction> AdaptRelationCalls(IEnumerable<CodeInstruction> instructions)
    {
        var signature = new[] { typeof(Hero), typeof(Hero), typeof(int), typeof(bool) };
        var set = AccessTools.DeclaredMethod(typeof(ChangeRelationAction), "SetRelationBetweenHeroes", signature);
        var change = AccessTools.DeclaredMethod(typeof(ChangeRelationAction), "ApplyRelationChangeBetweenHeroes", signature);
        int setCalls = 0, changeCalls = 0;
        foreach (var instruction in instructions)
        {
            MethodInfo replacement = null;
            if (instruction.Calls(set))
            {
                replacement = AccessTools.DeclaredMethod(typeof(ExecutionRelationCompatibility), nameof(SetRelation));
                setCalls++;
            }
            else if (instruction.Calls(change))
            {
                replacement = AccessTools.DeclaredMethod(typeof(ExecutionRelationCompatibility), nameof(ChangeRelation));
                changeCalls++;
            }
            if (replacement != null)
            {
                // Capture this event's executed hero so nested unrelated events
                // cannot inherit the public-execution scope by accident.
                var victim = new CodeInstruction(OpCodes.Ldarg_2);
                victim.MoveLabelsFrom(instruction);
                victim.MoveBlocksFrom(instruction);
                yield return victim;
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }
            yield return instruction;
        }
        if (setCalls != 1 || changeCalls != 1)
            throw new InvalidOperationException("Bannerlord 1.5 blood-feud relation calls changed; refusing partial execution adaptation.");
    }

    private static void SetRelation(Hero hero, Hero target, int value, bool notify, Hero victim)
    {
        if (ExecutionRelationScope.IsActiveFor(victim) && hero == Clan.PlayerClan.Leader)
        {
            TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetHeroesForEffectiveRelation(hero, target,
                out var effectiveHero, out var effectiveTarget);
            int current = CharacterRelationManager.GetHeroRelation(effectiveHero, effectiveTarget);
            value = ExecutionRelationScope.AdjustBloodFeudRelationSet(victim, target, current, value);
            notify = false;
        }
        ChangeRelationAction.SetRelationBetweenHeroes(hero, target, value, notify);
    }

    private static void ChangeRelation(Hero hero, Hero target, int value, bool notify, Hero victim)
    {
        if (ExecutionRelationScope.IsActiveFor(victim) && hero == Clan.PlayerClan.Leader)
        {
            TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetHeroesForEffectiveRelation(hero, target,
                out var effectiveHero, out var effectiveTarget);
            int current = CharacterRelationManager.GetHeroRelation(effectiveHero, effectiveTarget);
            value = ExecutionRelationScope.AdjustBloodFeudRelationChange(victim, target, current, value);
            notify = false;
        }
        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, target, value, notify);
    }
#endif
}
