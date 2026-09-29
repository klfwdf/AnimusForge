using System;
using System.Collections.Generic;

namespace RichExecutions.Core;

public abstract class ExecutionDefinitionCatalog<TDefinition> where TDefinition : class
{
    private readonly Dictionary<string, TDefinition> _byId =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<TDefinition> _ordered = new();

    public IReadOnlyList<TDefinition> All => _ordered;

    protected void RegisterOrReplace(string stringId, TDefinition definition)
    {
        if (_byId.TryGetValue(stringId, out var existing))
        {
            var index = _ordered.IndexOf(existing);
            _ordered[index] = definition;
        }
        else
        {
            _ordered.Add(definition);
        }

        _byId[stringId] = definition;
    }

    public bool TryGet(string stringId, out TDefinition definition) =>
        _byId.TryGetValue(stringId, out definition!);
}

public sealed class ExecutionMethodCatalog : ExecutionDefinitionCatalog<ExecutionMethodDefinition>
{
    public void RegisterOrReplace(ExecutionMethodDefinition method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        RegisterOrReplace(method.StringId, method);
    }

    internal static ExecutionMethodCatalog CreateDefault()
    {
        var catalog = new ExecutionMethodCatalog();
        catalog.RegisterOrReplace(new ExecutionMethodDefinition(
            ExecutionMethodRules.Beheading,
            "{=REX_Method_Beheading}Beheading",
            "{=REX_Method_Beheading_Hint}A noble death when granted to a lord; swift, visible and final.",
            "rex.scene.beheading"));
        catalog.RegisterOrReplace(new ExecutionMethodDefinition(
            ExecutionMethodRules.Hanging,
            "{=REX_Method_Hanging}Hanging",
            "{=REX_Method_Hanging_Hint}A public warning associated with common justice; degrading for a noble.",
            "rex.scene.hanging"));
        catalog.RegisterOrReplace(new ExecutionMethodDefinition(
            ExecutionMethodRules.Burning,
            "{=REX_Method_Burning}Burning at the stake",
            "{=REX_Method_Burning_Hint}The harshest sentence. It inspires fear but carries a heavy moral cost.",
            "rex.scene.burning"));
        catalog.RegisterOrReplace(new ExecutionMethodDefinition(
            ExecutionMethodRules.BreakingWheel,
            "{=REX_Method_Breaking_Wheel}Breaking on the wheel",
            "{=REX_Method_Breaking_Wheel_Hint}A prolonged public punishment built around the execution wheel; brutal and difficult to justify.",
            "rex.scene.breaking_wheel"));
        catalog.RegisterOrReplace(new ExecutionMethodDefinition(
            ExecutionMethodRules.Impalement,
            "{=REX_Method_Impalement}Impalement",
            "{=REX_Method_Impalement_Hint}A terrifying stake execution intended to intimidate enemies and subjects alike.",
            "rex.scene.impalement"));
        catalog.RegisterOrReplace(new ExecutionMethodDefinition(
            ExecutionMethodRules.Stoning,
            "{=REX_Method_Stoning}Stoning",
            "{=REX_Method_Stoning_Hint}A collective and degrading sentence associated with popular judgement rather than noble custom.",
            "rex.scene.stoning"));
        catalog.RegisterOrReplace(new ExecutionMethodDefinition(
            ExecutionMethodRules.CrossbowExecution,
            "{=REX_Method_Crossbow_Execution}Crossbow execution",
            "{=REX_Method_Crossbow_Execution_Hint}A swift ranged execution: efficient, public, and less honorable than the sword or axe.",
            "rex.scene.crossbow_execution"));
        return catalog;
    }
}

public sealed class ExecutionChargeCatalog : ExecutionDefinitionCatalog<ExecutionChargeDefinition>
{
    public void RegisterOrReplace(ExecutionChargeDefinition charge)
    {
        if (charge is null)
        {
            throw new ArgumentNullException(nameof(charge));
        }

        RegisterOrReplace(charge.StringId, charge);
    }

    internal static ExecutionChargeCatalog CreateDefault()
    {
        var catalog = new ExecutionChargeCatalog();
        catalog.RegisterOrReplace(new ExecutionChargeDefinition(
            "treason",
            "{=REX_Charge_Treason}Rebellion or high treason",
            "{=REX_Charge_Treason_Hint}Breaking allegiance, defecting, or taking part in a rebellion."));
        catalog.RegisterOrReplace(new ExecutionChargeDefinition(
            "raiding_civilians",
            "{=REX_Charge_Raiding}Raiding civilians",
            "{=REX_Charge_Raiding_Hint}Looting villages and bringing violence against their inhabitants."));
        catalog.RegisterOrReplace(new ExecutionChargeDefinition(
            "siege_atrocity",
            "{=REX_Charge_Siege}Siege atrocities",
            "{=REX_Charge_Siege_Hint}Pillaging or devastating a captured settlement."));
        catalog.RegisterOrReplace(new ExecutionChargeDefinition(
            "personal_revenge",
            "{=REX_Charge_Revenge}Personal revenge",
            "{=REX_Charge_Revenge_Hint}A private grievance presented as public justice."));
        return catalog;
    }
}

public sealed class ExecutionSceneActCatalog : ExecutionDefinitionCatalog<IExecutionSceneAct>
{
    public void RegisterOrReplace(IExecutionSceneAct act)
    {
        if (act is null)
        {
            throw new ArgumentNullException(nameof(act));
        }

        RegisterOrReplace(act.StringId, act);
    }

    internal static ExecutionSceneActCatalog CreateDefault()
    {
        var catalog = new ExecutionSceneActCatalog();
        catalog.RegisterOrReplace(new BuiltInExecutionSceneAct(
            "rex.scene.beheading",
            "act_cutscene_execution_prisoner_idle",
            "act_ver_cutscene_executioner_idle",
            "act_ver_cutscene_executioner_action",
            "act_death_by_arrow_neck1",
            0.49f,
            4.5f));
        catalog.RegisterOrReplace(new BuiltInExecutionSceneAct(
            "rex.scene.hanging",
            "act_prisoner_conversation_idle_1",
            "act_idle_unarmed_1",
            "act_usage_siege_tower_open_gate_begin",
            "act_death_by_arrow_neck1",
            0.52f,
            4.5f));
        catalog.RegisterOrReplace(new BuiltInExecutionSceneAct(
            "rex.scene.burning",
            "act_ver_burning_crucifix_idle",
            "act_walk_idle_torch",
            "act_pickup_down_begin",
            "act_ver_burning_crucifix_death",
            0.40f,
            4f));
        catalog.RegisterOrReplace(new BuiltInExecutionSceneAct(
            "rex.scene.breaking_wheel",
            "act_ver_burning_crucifix_idle",
            "act_idle_1h_without_shield_1",
            "act_smithing_machine_anvil_part_1",
            "act_ver_burning_crucifix_death",
            0.52f,
            4.5f));
        catalog.RegisterOrReplace(new BuiltInExecutionSceneAct(
            "rex.scene.impalement",
            "act_ver_hanging_struggle",
            "act_idle_unarmed_1",
            "act_pickup_down_begin",
            "act_ver_hanging_death",
            0.52f,
            4.0f));
        catalog.RegisterOrReplace(new BuiltInExecutionSceneAct(
            "rex.scene.stoning",
            "act_cutscene_execution_prisoner_idle",
            "act_idle_unarmed_1",
            "act_release_stone",
            "act_strike_chest_front",
            0.50f,
            3.5f));
        catalog.RegisterOrReplace(new BuiltInExecutionSceneAct(
            "rex.scene.crossbow_execution",
            "act_ver_burning_crucifix_idle",
            "act_idle_crossbow_1",
            "act_release_crossbow",
            "act_ver_burning_crucifix_death",
            0.48f,
            3.5f));
        return catalog;
    }
}

internal sealed class BuiltInExecutionSceneAct : IExecutionSceneAct
{
    public BuiltInExecutionSceneAct(
        string stringId,
        string victimIdleAction,
        string executionerIdleAction,
        string executionAction,
        string deathAction,
        float lethalProgress,
        float maximumActionSeconds)
    {
        StringId = stringId;
        VictimIdleAction = victimIdleAction;
        ExecutionerIdleAction = executionerIdleAction;
        ExecutionAction = executionAction;
        DeathAction = deathAction;
        LethalProgress = lethalProgress;
        MaximumActionSeconds = maximumActionSeconds;
    }

    public string StringId { get; }
    public string VictimIdleAction { get; }
    public string ExecutionerIdleAction { get; }
    public string ExecutionAction { get; }
    public string DeathAction { get; }
    public float LethalProgress { get; }
    public float MaximumActionSeconds { get; }
}
