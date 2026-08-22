using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

public static class SiegeCompletedInterventionSummaryBuilder
{
    public static string Build(SiegeCompletedInterventionSummaryFacts facts)
    {
        return Build(facts, TownActionPresentationTextCatalog.CreateEnglishFallback());
    }

    public static string Build(SiegeCompletedInterventionSummaryFacts facts, TownActionPresentationTextCatalog text)
    {
        TownActionPresentationTextCatalog resolvedText = TownActionPresentationTextCatalog.Resolve(text);
        if (facts == null)
        {
            return resolvedText.CompletedSummaryFallbackText;
        }

        string settlementName = string.IsNullOrWhiteSpace(facts.SettlementName) ? resolvedText.UnknownSettlementName : facts.SettlementName.Trim();
        string action = resolvedText.GetCompletedLabel(facts.AftermathKind, facts.CulturalRepopulationApplied);
        var sb = new StringBuilder();
        sb.AppendLine(ReplaceMany(
            resolvedText.SummaryTitleTemplate,
            ("{settlement}", settlementName),
            ("{action}", action)));
        sb.AppendLine();
        if (facts.CulturalRepopulationApplied)
        {
            string targetCultureText = string.IsNullOrWhiteSpace(facts.TargetCultureText) ? resolvedText.UnknownTargetCultureText : facts.TargetCultureText.Trim();
            sb.AppendLine(ReplaceMany(
                resolvedText.SummaryCulturalRepopulationTemplate,
                ("{target_culture}", targetCultureText)));
        }
        else if (facts.MassacreStarted)
        {
            sb.AppendLine(resolvedText.SummaryMassacreText);
        }
        else if (facts.PlunderStarted)
        {
            sb.AppendLine(resolvedText.SummaryPlunderText);
        }
        else
        {
            sb.AppendLine(resolvedText.SummaryMercyText);
        }

        sb.AppendLine();
        sb.AppendLine(ReplaceMany(
            resolvedText.SummaryMarketItemsTemplate,
            ("{market_item_total}", facts.MarketItemTotal.ToString()),
            ("{market_stack_kinds}", facts.MarketStackKinds.ToString()),
            ("{market_item_value}", facts.MarketItemValue.ToString())));
        sb.AppendLine(ReplaceMany(
            resolvedText.SummaryMarketGoldTemplate,
            ("{market_gold}", facts.MarketGold.ToString())));
        sb.AppendLine(ReplaceMany(
            resolvedText.SummaryCivilianGoldTemplate,
            ("{civilian_gold}", facts.CivilianGold.ToString()),
            ("{civilian_targets}", facts.CivilianTargetsLooted.ToString())));
        sb.AppendLine();
        sb.AppendLine(resolvedText.SummaryClosingText);
        return sb.ToString();
    }

    public static string DescribeAction(SiegeAftermathResolutionKind aftermathKind, bool culturalRepopulationApplied)
    {
        return TownActionPresentationTextCatalog.CreateEnglishFallback().GetCompletedLabel(aftermathKind, culturalRepopulationApplied);
    }

    public static string DescribeAction(
        SiegeAftermathResolutionKind aftermathKind,
        bool culturalRepopulationApplied,
        TownActionPresentationTextCatalog text)
    {
        return TownActionPresentationTextCatalog.Resolve(text).GetCompletedLabel(aftermathKind, culturalRepopulationApplied);
    }

    private static string ReplaceMany(string value, params (string Token, string Replacement)[] replacements)
    {
        string result = value ?? string.Empty;
        foreach ((string token, string replacement) in replacements)
        {
            result = result.Replace(token, replacement ?? string.Empty);
        }
        return result;
    }
}
