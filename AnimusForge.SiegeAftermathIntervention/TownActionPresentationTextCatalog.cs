using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnimusForge.SiegeAftermathIntervention;

public sealed class TownActionPresentationTextCatalog
{
    public int Version { get; set; }

    public Dictionary<string, TownActionPresentationText> Actions { get; set; }

    public string SharedPoolUnavailableText { get; set; }

    public string ReliefSoldierTargetValidation { get; set; }

    public string ReliefMissingSharedPoolValidation { get; set; }

    public string ReliefRequiredSharedMaterialValidation { get; set; }

    public string CulturalRepopulationTargetValidation { get; set; }

    public string PlayerHeroCultureSourceLabel { get; set; }

    public string PlayerKingdomCultureSourceLabel { get; set; }

    public string PlayerClanCultureSourceLabel { get; set; }

    public string PlayerCultureFallbackLabel { get; set; }

    public string CultureSourceWrapperTemplate { get; set; }

    public string DoneContinueOptionText { get; set; }

    public string DoneMenuFallbackText { get; set; }

    public string CompletedSummaryFallbackText { get; set; }

    public string MassacreVictoryMessage { get; set; }

    public string MassacreVictoryQuickText { get; set; }

    public string LeaveEncounterQuickText { get; set; }

    public string CulturalRepopulationCompletedLabel { get; set; }

    public string DevastateCompletedLabel { get; set; }

    public string PlunderCompletedLabel { get; set; }

    public string MercyCompletedLabel { get; set; }

    public string DefaultCompletedLabel { get; set; }

    public string CompletedEncounterTemplate { get; set; }

    public string LootSettlementSummaryTemplate { get; set; }

    public string SummaryTitleTemplate { get; set; }

    public string UnknownSettlementName { get; set; }

    public string UnknownTargetCultureText { get; set; }

    public string SummaryCulturalRepopulationTemplate { get; set; }

    public string SummaryMassacreText { get; set; }

    public string SummaryPlunderText { get; set; }

    public string SummaryMercyText { get; set; }

    public string SummaryMarketItemsTemplate { get; set; }

    public string SummaryMarketGoldTemplate { get; set; }

    public string SummaryCivilianGoldTemplate { get; set; }

    public string SummaryClosingText { get; set; }

    public TownActionPresentationText GetAction(string key)
    {
        string normalizedKey = NormalizeKey(key);
        if (Actions != null && Actions.TryGetValue(normalizedKey, out TownActionPresentationText action) && action != null)
        {
            return TownActionPresentationText.Resolve(action, CreateEnglishActionFallback(normalizedKey));
        }

        return CreateEnglishActionFallback(normalizedKey);
    }

    public string BuildMemory(
        string key,
        bool repeat,
        string sharedPoolDescription = null,
        string targetCultureDescription = null)
    {
        TownActionPresentationText action = GetAction(key);
        string template = repeat ? action.RepeatMemoryTemplate : action.FirstMemoryTemplate;
        string pool = string.IsNullOrWhiteSpace(sharedPoolDescription)
            ? SharedPoolUnavailableText
            : sharedPoolDescription.Trim();
        return ReplaceManyText(
            template,
            ("{shared_pool}", pool),
            ("{target_culture}", NormalizeTargetCulture(targetCultureDescription)));
    }

    public string BuildCommandConfirmation(string key, string targetCultureDescription = null)
    {
        return ReplaceManyText(
            GetAction(key).CommandConfirmation,
            ("{target_culture}", NormalizeTargetCulture(targetCultureDescription)));
    }

    public string BuildSceneTransition(
        string key,
        string settlementName,
        string targetCultureDescription,
        int killedNotables,
        int spawnedNotables)
    {
        TownActionPresentationText action = GetAction(key);
        return ReplaceManyText(
            action.SceneTransitionTemplate,
            ("{settlement}", string.IsNullOrWhiteSpace(settlementName) ? UnknownSettlementName : settlementName.Trim()),
            ("{target_culture}", NormalizeTargetCulture(targetCultureDescription)),
            ("{killed_notables}", ClampNonNegative(killedNotables).ToString(CultureInfo.InvariantCulture)),
            ("{spawned_notables}", ClampNonNegative(spawnedNotables).ToString(CultureInfo.InvariantCulture)));
    }

    public string DescribeCulture(string cultureName, string sourceLabel)
    {
        string normalizedSource = NormalizeCultureSourceLabel(sourceLabel);
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return normalizedSource;
        }

        string normalizedCulture = cultureName.Trim();
        if (string.IsNullOrWhiteSpace(sourceLabel))
        {
            return normalizedCulture;
        }

        return ReplaceManyText(
            CultureSourceWrapperTemplate,
            ("{culture}", normalizedCulture),
            ("{source}", normalizedSource));
    }

    public string NormalizeCultureSourceLabel(string sourceLabel)
    {
        return string.IsNullOrWhiteSpace(sourceLabel) ? PlayerCultureFallbackLabel : sourceLabel.Trim();
    }

    public string GetCompletedLabel(SiegeAftermathResolutionKind aftermathKind, bool culturalRepopulationApplied)
    {
        return aftermathKind switch
        {
            SiegeAftermathResolutionKind.Devastate => culturalRepopulationApplied
                ? CulturalRepopulationCompletedLabel
                : DevastateCompletedLabel,
            SiegeAftermathResolutionKind.Pillage => PlunderCompletedLabel,
            SiegeAftermathResolutionKind.ShowMercy => MercyCompletedLabel,
            _ => DefaultCompletedLabel,
        };
    }

    public string BuildCompletedEncounterMessage(SiegeAftermathResolutionKind aftermathKind, bool culturalRepopulationApplied)
    {
        return BuildCompletedEncounterMessage(GetCompletedLabel(aftermathKind, culturalRepopulationApplied));
    }

    public string BuildCompletedEncounterMessage(string actionLabel)
    {
        string label = string.IsNullOrWhiteSpace(actionLabel) ? DefaultCompletedLabel : actionLabel.Trim();
        return Replace(CompletedEncounterTemplate, "{action}", label);
    }

    public string BuildLootSettlementSummary(int marketItemTotal, int marketStackKinds, int marketGold, int civilianGold)
    {
        return ReplaceMany(
            LootSettlementSummaryTemplate,
            ("{market_item_total}", ClampNonNegative(marketItemTotal)),
            ("{market_stack_kinds}", ClampNonNegative(marketStackKinds)),
            ("{market_gold}", ClampNonNegative(marketGold)),
            ("{civilian_gold}", ClampNonNegative(civilianGold)));
    }

    public static TownActionPresentationTextCatalog Resolve(TownActionPresentationTextCatalog source)
    {
        TownActionPresentationTextCatalog fallback = CreateEnglishFallback();
        if (source == null)
        {
            return fallback;
        }

        var actions = new Dictionary<string, TownActionPresentationText>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, TownActionPresentationText> pair in fallback.Actions)
        {
            TownActionPresentationText candidate = null;
            source.Actions?.TryGetValue(pair.Key, out candidate);
            actions[pair.Key] = TownActionPresentationText.Resolve(candidate, pair.Value);
        }

        return new TownActionPresentationTextCatalog
        {
            Version = source.Version > 0 ? source.Version : fallback.Version,
            Actions = actions,
            SharedPoolUnavailableText = Pick(source.SharedPoolUnavailableText, fallback.SharedPoolUnavailableText),
            ReliefSoldierTargetValidation = Pick(source.ReliefSoldierTargetValidation, fallback.ReliefSoldierTargetValidation),
            ReliefMissingSharedPoolValidation = Pick(source.ReliefMissingSharedPoolValidation, fallback.ReliefMissingSharedPoolValidation),
            ReliefRequiredSharedMaterialValidation = Pick(source.ReliefRequiredSharedMaterialValidation, fallback.ReliefRequiredSharedMaterialValidation),
            CulturalRepopulationTargetValidation = Pick(source.CulturalRepopulationTargetValidation, fallback.CulturalRepopulationTargetValidation),
            PlayerHeroCultureSourceLabel = Pick(source.PlayerHeroCultureSourceLabel, fallback.PlayerHeroCultureSourceLabel),
            PlayerKingdomCultureSourceLabel = Pick(source.PlayerKingdomCultureSourceLabel, fallback.PlayerKingdomCultureSourceLabel),
            PlayerClanCultureSourceLabel = Pick(source.PlayerClanCultureSourceLabel, fallback.PlayerClanCultureSourceLabel),
            PlayerCultureFallbackLabel = Pick(source.PlayerCultureFallbackLabel, fallback.PlayerCultureFallbackLabel),
            CultureSourceWrapperTemplate = Pick(source.CultureSourceWrapperTemplate, fallback.CultureSourceWrapperTemplate),
            DoneContinueOptionText = Pick(source.DoneContinueOptionText, fallback.DoneContinueOptionText),
            DoneMenuFallbackText = Pick(source.DoneMenuFallbackText, fallback.DoneMenuFallbackText),
            CompletedSummaryFallbackText = Pick(source.CompletedSummaryFallbackText, fallback.CompletedSummaryFallbackText),
            MassacreVictoryMessage = Pick(source.MassacreVictoryMessage, fallback.MassacreVictoryMessage),
            MassacreVictoryQuickText = Pick(source.MassacreVictoryQuickText, fallback.MassacreVictoryQuickText),
            LeaveEncounterQuickText = Pick(source.LeaveEncounterQuickText, fallback.LeaveEncounterQuickText),
            CulturalRepopulationCompletedLabel = Pick(source.CulturalRepopulationCompletedLabel, fallback.CulturalRepopulationCompletedLabel),
            DevastateCompletedLabel = Pick(source.DevastateCompletedLabel, fallback.DevastateCompletedLabel),
            PlunderCompletedLabel = Pick(source.PlunderCompletedLabel, fallback.PlunderCompletedLabel),
            MercyCompletedLabel = Pick(source.MercyCompletedLabel, fallback.MercyCompletedLabel),
            DefaultCompletedLabel = Pick(source.DefaultCompletedLabel, fallback.DefaultCompletedLabel),
            CompletedEncounterTemplate = Pick(source.CompletedEncounterTemplate, fallback.CompletedEncounterTemplate),
            LootSettlementSummaryTemplate = Pick(source.LootSettlementSummaryTemplate, fallback.LootSettlementSummaryTemplate),
            SummaryTitleTemplate = Pick(source.SummaryTitleTemplate, fallback.SummaryTitleTemplate),
            UnknownSettlementName = Pick(source.UnknownSettlementName, fallback.UnknownSettlementName),
            UnknownTargetCultureText = Pick(source.UnknownTargetCultureText, fallback.UnknownTargetCultureText),
            SummaryCulturalRepopulationTemplate = Pick(source.SummaryCulturalRepopulationTemplate, fallback.SummaryCulturalRepopulationTemplate),
            SummaryMassacreText = Pick(source.SummaryMassacreText, fallback.SummaryMassacreText),
            SummaryPlunderText = Pick(source.SummaryPlunderText, fallback.SummaryPlunderText),
            SummaryMercyText = Pick(source.SummaryMercyText, fallback.SummaryMercyText),
            SummaryMarketItemsTemplate = Pick(source.SummaryMarketItemsTemplate, fallback.SummaryMarketItemsTemplate),
            SummaryMarketGoldTemplate = Pick(source.SummaryMarketGoldTemplate, fallback.SummaryMarketGoldTemplate),
            SummaryCivilianGoldTemplate = Pick(source.SummaryCivilianGoldTemplate, fallback.SummaryCivilianGoldTemplate),
            SummaryClosingText = Pick(source.SummaryClosingText, fallback.SummaryClosingText),
        };
    }

    public static TownActionPresentationTextCatalog CreateEnglishFallback()
    {
        return new TownActionPresentationTextCatalog
        {
            Version = 1,
            Actions = new Dictionary<string, TownActionPresentationText>(StringComparer.OrdinalIgnoreCase)
            {
                [TownActionPresentationKeys.Mercy] = new TownActionPresentationText("mercy", "Your order of mercy passes through the occupied streets. The final aftermath will be settled when you leave.", "Mercy", "The player ordered that ordinary residents be spared and protected from killing or robbery.", "The player reaffirmed the order of mercy."),
                [TownActionPresentationKeys.Relief] = new TownActionPresentationText("relief", "Soldiers begin distributing the supplies already handed over through AF while residents watch the new order take shape.", "Relief", "The player ordered AF-delivered money, food, and supplies distributed to residents. Shared pool: {shared_pool}.", "The player repeated the order to distribute the remaining shared supplies. Shared pool: {shared_pool}."),
                [TownActionPresentationKeys.CivilianVerbalRelief] = new TownActionPresentationText("verbal relief", "The promise of protection and restored order spreads from this conversation into the nearby streets.", "Relief", "The player offered protection, discipline, and resettlement through direct conversation without distributing supplies.", "The player continued reassuring defeated residents through direct conversation."),
                [TownActionPresentationKeys.Inspiration] = new TownActionPresentationText("public reassurance", "Soldiers open space for the address as residents gather cautiously to hear what the new ruler intends.", "Public reassurance", "The player gathered residents and publicly declared a protected civic order.", "The player continued the established public reassurance policy."),
                [TownActionPresentationKeys.RallyOath] = new TownActionPresentationText("public oath", "Representatives are called forward and the square settles into an uneasy silence before the oath.", "Public oath", "The player organized a public oath of allegiance and reconciliation.", "The player continued the established public oath policy."),
                [TownActionPresentationKeys.Plunder] = new TownActionPresentationText("plunder", "The order spreads by squad. Soldiers begin searching actual targets and recording what is taken; stopping prevents further seizures but does not erase completed ones.", "Plunder", "The player ordered target-aware plunder. Soldiers began questioning residents and seizing recorded valuables; the order can still be stopped or superseded by mercy before a destructive escalation.", "The player maintained the current plunder order."),
                [TownActionPresentationKeys.Massacre] = new TownActionPresentationText("massacre", "The order turns the occupation violent. Soldiers pursue the captured target list while terrified survivors flee, hide, or surrender; a stop order can still end further killing.", "Massacre", "The player escalated the occupation into a massacre. Soldiers pursue the captured civilian and notable target list; survivors remain frightened even if the order is stopped.", "The player maintained the massacre order."),
                [TownActionPresentationKeys.CulturalRepopulation] = new TownActionPresentationText("colonization", "Colonization is now pending for {target_culture}. Soldiers continue hunting the captured target list; leaving commits immediately, while a stop order before every target dies converts the result to a partial massacre.", "Colonization", "The player ordered destructive colonization toward {target_culture}. The outcome remains pending while captured civilian and notable targets survive.", "The player maintained the pending colonization order.", "The captured colonization targets in {settlement} are gone and the culture transition to {target_culture} has occurred. Notables removed: {killed_notables}; replacements installed: {spawned_notables}. Final devastation settlement still occurs on departure."),
            },
            SharedPoolUnavailableText = "shared pool details unavailable",
            ReliefSoldierTargetValidation = "Order supply distribution through an allied occupation soldier.",
            ReliefMissingSharedPoolValidation = "Transfer money, food, or supplies through AF before ordering soldiers to distribute relief.",
            ReliefRequiredSharedMaterialValidation = "Relief distribution requires money, food, or supplies already transferred through AF. Use mercy when no material distribution is intended.",
            CulturalRepopulationTargetValidation = "Destructive colonization can only be ordered through an allied occupation soldier.",
            PlayerHeroCultureSourceLabel = "player character culture",
            PlayerKingdomCultureSourceLabel = "player kingdom culture",
            PlayerClanCultureSourceLabel = "player clan culture",
            PlayerCultureFallbackLabel = "player culture",
            CultureSourceWrapperTemplate = "{culture} ({source})",
            DoneContinueOptionText = "Continue...",
            DoneMenuFallbackText = "The town aftermath has been completed. Continue to end this siege encounter.",
            CompletedSummaryFallbackText = "The town aftermath has been completed and the siege encounter is ending.",
            MassacreVictoryMessage = "The captured massacre target list has been exhausted. Final loot and consequences remain deferred until departure.",
            MassacreVictoryQuickText = "The massacre target list is complete; final settlement occurs on departure.",
            LeaveEncounterQuickText = "The town aftermath is complete and the siege encounter is ending.",
            CulturalRepopulationCompletedLabel = "colonization",
            DevastateCompletedLabel = "massacre and devastation",
            PlunderCompletedLabel = "plunder",
            MercyCompletedLabel = "mercy",
            DefaultCompletedLabel = "aftermath",
            CompletedEncounterTemplate = "The {action} outcome has now been settled and the siege encounter is ending.",
            LootSettlementSummaryTemplate = "Final loot: {market_item_total} market items in {market_stack_kinds} stacks, {market_gold} market gold, and {civilian_gold} civilian gold.",
            SummaryTitleTemplate = "The post-siege {action} of {settlement} is complete.",
            UnknownSettlementName = "the settlement",
            UnknownTargetCultureText = "the target culture",
            SummaryCulturalRepopulationTemplate = "The irreversible colonization outcome has committed. Native devastation was applied and the settlement culture changed to {target_culture}.",
            SummaryMassacreText = "The town massacre has reached its final settlement state.",
            SummaryPlunderText = "The target-aware plunder ledger has reached its final settlement state.",
            SummaryMercyText = "The town has been settled under mercy and reassurance.",
            SummaryMarketItemsTemplate = "Market goods: {market_item_total} items in {market_stack_kinds} stacks, valued at {market_item_value}.",
            SummaryMarketGoldTemplate = "Market treasury: {market_gold} denars.",
            SummaryCivilianGoldTemplate = "Civilian money: {civilian_gold} denars from {civilian_targets} recorded targets.",
            SummaryClosingText = "Continue to leave the siege encounter.",
        };
    }

    private static TownActionPresentationText CreateEnglishActionFallback(string key)
    {
        TownActionPresentationTextCatalog fallback = CreateEnglishFallback();
        if (fallback.Actions.TryGetValue(key, out TownActionPresentationText action))
        {
            return action;
        }

        return new TownActionPresentationText(key, "The order has been acknowledged.", "Town order", "The player issued a town order.", "The player repeated the town order.");
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
    }

    private static string Pick(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string Replace(string value, string token, string replacement)
    {
        return (value ?? string.Empty).Replace(token, replacement ?? string.Empty);
    }

    private static string ReplaceMany(string value, params (string Token, int Value)[] replacements)
    {
        string result = value ?? string.Empty;
        foreach ((string token, int replacement) in replacements)
        {
            result = Replace(result, token, replacement.ToString(CultureInfo.InvariantCulture));
        }
        return result;
    }

    private static string ReplaceManyText(string value, params (string Token, string Replacement)[] replacements)
    {
        string result = value ?? string.Empty;
        foreach ((string token, string replacement) in replacements)
        {
            result = Replace(result, token, replacement);
        }
        return result;
    }

    private string NormalizeTargetCulture(string targetCultureDescription)
    {
        return string.IsNullOrWhiteSpace(targetCultureDescription)
            ? UnknownTargetCultureText
            : targetCultureDescription.Trim();
    }

    private static int ClampNonNegative(int value)
    {
        return value < 0 ? 0 : value;
    }
}

public sealed class TownActionPresentationText
{
    public TownActionPresentationText()
    {
    }

    public TownActionPresentationText(
        string actionLabel,
        string commandConfirmation,
        string memoryTitle,
        string firstMemoryTemplate,
        string repeatMemoryTemplate,
        string sceneTransitionTemplate = "")
    {
        ActionLabel = actionLabel;
        CommandConfirmation = commandConfirmation;
        MemoryTitle = memoryTitle;
        FirstMemoryTemplate = firstMemoryTemplate;
        RepeatMemoryTemplate = repeatMemoryTemplate;
        SceneTransitionTemplate = sceneTransitionTemplate;
    }

    public string ActionLabel { get; set; }

    public string CommandConfirmation { get; set; }

    public string MemoryTitle { get; set; }

    public string FirstMemoryTemplate { get; set; }

    public string RepeatMemoryTemplate { get; set; }

    public string SceneTransitionTemplate { get; set; }

    public static TownActionPresentationText Resolve(TownActionPresentationText source, TownActionPresentationText fallback)
    {
        return new TownActionPresentationText(
            Pick(source?.ActionLabel, fallback.ActionLabel),
            Pick(source?.CommandConfirmation, fallback.CommandConfirmation),
            Pick(source?.MemoryTitle, fallback.MemoryTitle),
            Pick(source?.FirstMemoryTemplate, fallback.FirstMemoryTemplate),
            Pick(source?.RepeatMemoryTemplate, fallback.RepeatMemoryTemplate),
            Pick(source?.SceneTransitionTemplate, fallback.SceneTransitionTemplate));
    }

    private static string Pick(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
